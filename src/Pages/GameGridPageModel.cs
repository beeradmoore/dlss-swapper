using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLSS_Swapper.Builders;
using DLSS_Swapper.Data;
using DLSS_Swapper.Helpers;
using CommunityToolkit.Mvvm.Messaging;
using DLSS_Swapper.Messages;
using DLSS_Swapper.UserControls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Windows.System;

namespace DLSS_Swapper.Pages;

public enum GameGridViewType
{
    GridView,
    ListView,
}

public partial class GameGridPageModel : ObservableObject
{
    GameGridPage gameGridPage;

    [ObservableProperty]
    public partial Game? SelectedGame { get; set; } = null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLoading))]
    [NotifyPropertyChangedFor(nameof(CanUseFilterAndView))]
    [NotifyPropertyChangedFor(nameof(CanRefresh))]
    public partial bool IsGameListLoading { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLoading))]
    [NotifyPropertyChangedFor(nameof(CanRefresh))]
    public partial bool IsDLSSLoading { get; set; } = true;

    public bool IsLoading => (IsGameListLoading || IsDLSSLoading);

    [ObservableProperty]
    public partial ICollectionView? CurrentCollectionView { get; set; } = null;


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GridViewItemHeight))]
    public partial int GridViewItemWidth { get; set; } = Settings.Instance.GridViewItemWidth;

    public int GridViewItemHeight => (int)(GridViewItemWidth * 1.5);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GameGridViewIcon))]
    public partial GameGridViewType GameGridViewType { get; set; } = Settings.Instance.GameGridViewType;

    public FontIcon GameGridViewIcon => GameGridViewType switch
    {
        GameGridViewType.GridView => new FontIcon() { Glyph = "\xF0E2" },
        GameGridViewType.ListView => new FontIcon() { Glyph = "\xE8FD" },
        _ => new FontIcon() { },
    };

    public GameGridPageModelTranslationProperties TranslationProperties { get; } = new GameGridPageModelTranslationProperties();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanUseFilterAndView))]
    [NotifyPropertyChangedFor(nameof(CanRefresh))]
    public partial bool IsSelectionMode { get; set; } = false;

    public List<Game> SelectedGames { get; } = new List<Game>();

    public string SelectedGamesCountText => ResourceHelper.GetFormattedResourceTemplate("GamesPage_SelectionMode_CountTemplate", SelectedGames.Count);

    public bool CanApplyBatchDll => SelectedGames.Count > 0;

    bool AreAllVisibleGamesSelected
    {
        get
        {
            var visibleCount = gameGridPage.GetVisibleItemCount();
            return visibleCount > 0 && gameGridPage.GetVisibleSelectedCount() == visibleCount;
        }
    }

    public string SelectAllButtonText => AreAllVisibleGamesSelected
        ? ResourceHelper.GetString("GamesPage_SelectionMode_DeselectAll")
        : ResourceHelper.GetString("GamesPage_SelectionMode_SelectAll");

    public bool CanUseFilterAndView => IsGameListLoading == false && IsSelectionMode == false;

    public bool CanRefresh => IsLoading == false && IsSelectionMode == false;

    [RelayCommand]
    void ToggleSelectionMode()
    {
        if (IsSelectionMode == false)
        {
            IsSelectionMode = true;
            gameGridPage.EnterSelectionMode();
        }
        else
        {
            IsSelectionMode = false;
            gameGridPage.ExitSelectionMode();
            SelectedGames.Clear();
            NotifySelectionChanged();
        }
    }

    [RelayCommand]
    void ToggleSelectAll()
    {
        if (AreAllVisibleGamesSelected == true)
        {
            SelectedGames.Clear();
            gameGridPage.ResyncVisualSelection();
        }
        else
        {
            gameGridPage.SelectAllVisible();
        }

        NotifySelectionChanged();
    }

    [RelayCommand]
    void ClearSelection()
    {
        SelectedGames.Clear();
        gameGridPage.ResyncVisualSelection();
        NotifySelectionChanged();
    }

    [RelayCommand(CanExecute = nameof(CanApplyBatchDll))]
    async Task ApplyBatchDllAsync()
    {
        // 1. Configure the actions to apply (DLL and/or preset per type).
        var pickerDialog = new EasyContentDialog(gameGridPage.XamlRoot)
        {
            Title = ResourceHelper.GetString("GamesPage_Batch_Title"),
            PrimaryButtonText = ResourceHelper.GetString("General_Apply"),
            CloseButtonText = ResourceHelper.GetString("General_Cancel"),
            DefaultButton = ContentDialogButton.Primary,
        };
        // The default ContentDialogMaxWidth (548) clips the picker's version and
        // preset columns, which truncates longer localised type names.
        pickerDialog.Resources["ContentDialogMaxWidth"] = 680d;
        var picker = new BatchDllPickerControl(pickerDialog);
        pickerDialog.Content = picker;

        var pickerResult = await pickerDialog.ShowAsync();
        if (pickerResult != ContentDialogResult.Primary)
        {
            return;
        }

        // Collect actions and pre-compose their localised, game-agnostic labels on
        // the UI thread so the worker never touches ResourceHelper.
        var dllActions = picker.ViewModel.PlannedDllActions
            .Select(a => new BatchDllActionItem(
                a.Type,
                a.Record,
                ResourceHelper.GetFormattedResourceTemplate("GamesPage_Batch_DllLabelTemplate", DLLManager.Instance.GetAssetTypeName(a.Type), a.Record.DisplayName)))
            .ToList();

        var presetActions = picker.ViewModel.PlannedPresetActions
            .Select(a => new BatchPresetActionItem(
                a.Type,
                a.Preset.Value,
                ResourceHelper.GetFormattedResourceTemplate("GamesPage_Batch_PresetLabelTemplate", DLLManager.Instance.GetAssetTypeName(a.Type), a.Preset.Name)))
            .ToList();

        if (dllActions.Count == 0 && presetActions.Count == 0)
        {
            return;
        }

        // 2. Make sure every chosen DLL is on disk before touching any game.
        foreach (var action in dllActions)
        {
            // The picker only plans records with a LocalRecord, but re-validate
            // rather than trust it: a null LocalRecord would NRE below.
            if (action.Record.LocalRecord is null)
            {
                var localRecordErrorDialog = new EasyContentDialog(gameGridPage.XamlRoot)
                {
                    Title = ResourceHelper.GetString("General_Error"),
                    CloseButtonText = ResourceHelper.GetString("General_Close"),
                    DefaultButton = ContentDialogButton.Close,
                    Content = ResourceHelper.GetFormattedResourceTemplate("GamesPage_Batch_DownloadFailedTemplate", action.Record.DisplayName),
                };
                await localRecordErrorDialog.ShowAsync();
                return;
            }

            if (action.Record.LocalRecord.IsDownloaded != true)
            {
                var downloaded = await EnsureDownloadedAsync(action.Record);
                if (downloaded == false)
                {
                    return;
                }
            }
        }

        // 3. Progress dialog (determinate, one tick per game).
        var gamesToProcess = SelectedGames.ToList();

        var progressBar = new ProgressBar()
        {
            Minimum = 0,
            Maximum = gamesToProcess.Count,
            Value = 0,
            IsIndeterminate = false,
        };
        var progressTextBlock = new TextBlock();
        var progressStackPanel = new StackPanel()
        {
            Spacing = 16,
            Orientation = Orientation.Vertical,
            Children =
            {
                progressBar,
                progressTextBlock,
            },
        };
        var progressDialog = new EasyContentDialog(gameGridPage.XamlRoot)
        {
            Title = ResourceHelper.GetString("GamesPage_Batch_Title"),
            Content = progressStackPanel,
        };
        _ = progressDialog.ShowAsync();

        // Give UI time to show the dialog (same as ExportAllAsync).
        await Task.Delay(50);

        var results = new List<BatchSwapResult>();
        var progress = new Progress<int>(current =>
        {
            progressBar.Value = current;
            progressTextBlock.Text = ResourceHelper.GetFormattedResourceTemplate("GamesPage_Batch_ApplyingTemplate", current, gamesToProcess.Count);
        });

        // Once an NVAPI permission failure flips IsSupported to false, every later
        // preset call fails immediately. Track it so we report the rest as skipped
        // ("no permission") instead of a cascade of errors.
        var presetPermissionLost = false;

        try
        {
            await Task.Run(async () =>
            {
                var current = 0;
                foreach (var game in gamesToProcess)
                {
                    ++current;
                    ((IProgress<int>)progress).Report(current);

                    // Covers scans that were already running when the batch started.
                    // Emit one skipped result PER planned action so per-action counters stay correct.
                    if (game.Processing == true)
                    {
                        foreach (var dllAction in dllActions)
                        {
                            results.Add(SkippedResult(game, dllAction.Label, "GamesPage_Batch_Skipped_Processing"));
                        }
                        foreach (var presetAction in presetActions)
                        {
                            results.Add(SkippedResult(game, presetAction.Label, "GamesPage_Batch_Skipped_Processing"));
                        }
                        continue;
                    }

                    // DLL actions first, in type order (so a same-type preset applies over the swapped DLL).
                    foreach (var dllAction in dllActions)
                    {
                        try
                        {
                            // Re-checked per action: a scan starting mid-batch mutates
                            // GameAssets, which GetBatchCompatibility reads. The residual
                            // race degrades to an ErrorResult via this try, never a crash.
                            if (game.Processing == true)
                            {
                                results.Add(SkippedResult(game, dllAction.Label, "GamesPage_Batch_Skipped_Processing"));
                                continue;
                            }

                            var compatibility = DLLManager.GetBatchCompatibility(game, dllAction.Record);
                            if (compatibility.Compatible == false)
                            {
                                results.Add(SkippedResult(game, dllAction.Label, compatibility.ReasonKey));
                                continue;
                            }

                            var didUpdate = await game.UpdateDllAsync(dllAction.Record);
                            if (didUpdate.Success == true)
                            {
                                results.Add(SwappedResult(game, dllAction.Label));
                            }
                            else
                            {
                                results.Add(ErrorResult(game, dllAction.Label, didUpdate.Message, didUpdate.PromptToRelaunchAsAdmin));
                            }
                        }
                        catch (Exception err)
                        {
                            Logger.Error(err, $"Batch swap failed for \"{game.Title}\".");
                            results.Add(ErrorResult(game, dllAction.Label, err.Message, false));
                        }
                    }

                    // Preset actions after DLL actions. NVAPI is only ever exercised on
                    // the UI thread (see GameControlModel), so marshal each call there.
                    foreach (var presetAction in presetActions)
                    {
                        if (presetPermissionLost == true)
                        {
                            results.Add(SkippedResult(game, presetAction.Label, "GamesPage_Batch_Preset_Skipped_Unsupported"));
                            continue;
                        }

                        try
                        {
                            var outcome = await RunOnUiAsync(() => ApplyPreset(game, presetAction.Type, presetAction.PresetValue));
                            switch (outcome)
                            {
                                case PresetOutcome.Set:
                                    results.Add(SwappedResult(game, presetAction.Label));
                                    break;
                                case PresetOutcome.SkippedNoAsset:
                                    results.Add(SkippedResult(game, presetAction.Label, "GamesPage_Batch_Preset_Skipped_NoAsset"));
                                    break;
                                case PresetOutcome.SkippedNoProfile:
                                    results.Add(SkippedResult(game, presetAction.Label, "GamesPage_Batch_Preset_Skipped_NoProfile"));
                                    break;
                                case PresetOutcome.SkippedUnsupported:
                                    results.Add(SkippedResult(game, presetAction.Label, "GamesPage_Batch_Preset_Skipped_Unsupported"));
                                    break;
                                default:
                                    results.Add(ErrorResult(game, presetAction.Label, string.Empty, false));
                                    break;
                            }
                        }
                        catch (Exception err)
                        {
                            Logger.Error(err, $"Batch swap failed for \"{game.Title}\".");
                            results.Add(ErrorResult(game, presetAction.Label, err.Message, false));
                        }

                        // A permission failure flips IsSupported off — stop trying presets.
                        if (NVAPIHelper.Instance.IsSupported == false)
                        {
                            presetPermissionLost = true;
                        }
                    }
                }
            });
        }
        finally
        {
            // Ensures the non-dismissible progress dialog is never stranded.
            progressDialog.Hide();
        }

        // 4. Summary (flat list, one line per game+action).
        var summaryDialog = new EasyContentDialog(gameGridPage.XamlRoot)
        {
            Title = ResourceHelper.GetString("GamesPage_Batch_SummaryTitle"),
            CloseButtonText = ResourceHelper.GetString("General_Close"),
            DefaultButton = ContentDialogButton.Close,
            Content = new BatchSwapSummaryControl(results),
        };
        await summaryDialog.ShowAsync();

        // 5. Leave selection mode.
        if (IsSelectionMode == true)
        {
            ToggleSelectionMode();
        }
    }

    // Applies a single preset to a single game. MUST run on the UI thread:
    // GameAssets is a UI-thread collection and NVAPI's DriverSettingsSession is
    // only ever written from the UI thread (mirrors GameControlModel).
    PresetOutcome ApplyPreset(Game game, GameAssetType type, uint presetValue)
    {
        if (game.GameAssets.Any(x => x.AssetType == type) == false)
        {
            return PresetOutcome.SkippedNoAsset;
        }

        if (NVAPIHelper.Instance.IsSupported == false)
        {
            return PresetOutcome.SkippedUnsupported;
        }

        if (NVAPIHelper.Instance.FindGameProfile(game) is null)
        {
            return PresetOutcome.SkippedNoProfile;
        }

        // NOTE: DLL type — only these three support presets. Read .Success only:
        // SetGameDLSSDPreset/SetGameDLSSGPreset return Result == false even on success.
        var success = type switch
        {
            GameAssetType.DLSS => NVAPIHelper.Instance.SetGameDLSSPreset(game, presetValue).Success,
            GameAssetType.DLSS_D => NVAPIHelper.Instance.SetGameDLSSDPreset(game, presetValue).Success,
            GameAssetType.DLSS_G => NVAPIHelper.Instance.SetGameDLSSGPreset(game, presetValue).Success,
            _ => false,
        };

        return success ? PresetOutcome.Set : PresetOutcome.Error;
    }

    // Runs func on the page's UI thread and awaits its result. TryEnqueue's default
    // priority avoids the CS0104 DispatcherQueuePriority ambiguity (this file has
    // `using Windows.System;`).
    Task<T> RunOnUiAsync<T>(Func<T> func)
    {
        var tcs = new TaskCompletionSource<T>();
        var enqueued = gameGridPage.DispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                tcs.SetResult(func());
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        // If the queue rejected the work (e.g. shutdown) the callback never runs;
        // fail the task so the batch worker is never left awaiting forever.
        if (enqueued == false)
        {
            tcs.TrySetException(new InvalidOperationException("Could not enqueue work on the UI thread."));
        }
        return tcs.Task;
    }

    static BatchSwapResult SwappedResult(Game game, string actionLabel) =>
        new BatchSwapResult() { GameTitle = game.Title, Status = BatchSwapStatus.Swapped, ActionLabel = actionLabel };

    static BatchSwapResult SkippedResult(Game game, string actionLabel, string reasonKey) =>
        new BatchSwapResult() { GameTitle = game.Title, Status = BatchSwapStatus.Skipped, ActionLabel = actionLabel, ReasonKey = reasonKey };

    static BatchSwapResult ErrorResult(Game game, string actionLabel, string errorMessage, bool promptToRelaunchAsAdmin) =>
        new BatchSwapResult() { GameTitle = game.Title, Status = BatchSwapStatus.Error, ActionLabel = actionLabel, ErrorMessage = errorMessage, PromptToRelaunchAsAdmin = promptToRelaunchAsAdmin };

    async Task<bool> EnsureDownloadedAsync(DLLRecord dllRecord)
    {
        var isInFlight = dllRecord.LocalRecord?.FileDownloader is not null;

        var downloadStackPanel = new StackPanel()
        {
            Spacing = 16,
            Orientation = Orientation.Vertical,
            Children =
            {
                new ProgressBar() { IsIndeterminate = true },
            },
        };
        if (isInFlight == true)
        {
            downloadStackPanel.Children.Add(new TextBlock()
            {
                Text = ResourceHelper.GetString("GamesPage_Batch_WaitingForDownload"),
                TextWrapping = TextWrapping.Wrap,
            });
        }

        var downloadDialog = new EasyContentDialog(gameGridPage.XamlRoot)
        {
            Title = ResourceHelper.GetFormattedResourceTemplate("GamesPage_Batch_DownloadingTemplate", dllRecord.DisplayName),
            CloseButtonText = ResourceHelper.GetString("General_Cancel"),
            Content = downloadStackPanel,
        };

        // IMPORTANT: DownloadAsync cancels the previous CancellationTokenSource on
        // entry (DLLRecord.cs:275). If a download for this record is already in
        // flight (started from the Library page) we must wait for it instead of
        // calling DownloadAsync again.
        var downloadTask = isInFlight ? WaitForInFlightDownloadAsync(dllRecord) : dllRecord.DownloadAsync();

        // For a download this batch started, Cancel aborts the download itself.
        // For a download the Library page started, Cancel abandons only OUR wait —
        // the other surface's download keeps running. WaitForInFlightDownloadAsync
        // unsubscribes itself when that download eventually finishes.
        var abandonWaitTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        downloadDialog.CloseButtonClick += (sender, args) =>
        {
            if (isInFlight == true)
            {
                abandonWaitTcs.TrySetResult(true);
            }
            else
            {
                dllRecord.CancelDownload();
            }
        };

        _ = downloadDialog.ShowAsync();

        (bool Success, string Message, bool Cancelled) downloadResult;
        if (isInFlight == true)
        {
            var completedTask = await Task.WhenAny(downloadTask, abandonWaitTcs.Task);
            downloadResult = completedTask == downloadTask ? await downloadTask : (false, string.Empty, true);
        }
        else
        {
            downloadResult = await downloadTask;
        }
        downloadDialog.Hide();

        if (downloadResult.Cancelled == true)
        {
            return false;
        }

        if (downloadResult.Success == false)
        {
            var errorDialog = new EasyContentDialog(gameGridPage.XamlRoot)
            {
                Title = ResourceHelper.GetString("General_Error"),
                CloseButtonText = ResourceHelper.GetString("General_Close"),
                DefaultButton = ContentDialogButton.Close,
                Content = ResourceHelper.GetFormattedResourceTemplate("GamesPage_Batch_DownloadFailedTemplate", dllRecord.DisplayName),
            };
            await errorDialog.ShowAsync();
            return false;
        }

        return true;
    }

    // There is no exposed Task for an in-flight download, so completion is observed
    // via DLLRecord's PropertyChanged: success sets IsDownloaded before the finally
    // block clears FileDownloader; error sets HasDownloadError; cancellation clears
    // FileDownloader with neither flag set. All notifications arrive on the UI thread.
    static Task<(bool Success, string Message, bool Cancelled)> WaitForInFlightDownloadAsync(DLLRecord dllRecord)
    {
        var tcs = new TaskCompletionSource<(bool Success, string Message, bool Cancelled)>(TaskCreationOptions.RunContinuationsAsynchronously);

        void ResolveIfFinished()
        {
            var localRecord = dllRecord.LocalRecord;
            if (localRecord is null)
            {
                return;
            }

            if (localRecord.IsDownloaded == true)
            {
                tcs.TrySetResult((true, string.Empty, false));
            }
            else if (localRecord.FileDownloader is null)
            {
                if (localRecord.HasDownloadError == true)
                {
                    tcs.TrySetResult((false, localRecord.DownloadErrorMessage, false));
                }
                else
                {
                    tcs.TrySetResult((false, string.Empty, true));
                }
            }
        }

        PropertyChangedEventHandler handler = (sender, e) => ResolveIfFinished();
        dllRecord.PropertyChanged += handler;

        // The download may have finished between the caller's check and our subscription.
        ResolveIfFinished();

        return tcs.Task.ContinueWith(t =>
        {
            dllRecord.PropertyChanged -= handler;
            return t.Result;
        }, TaskScheduler.Default);
    }

    internal void UpdateSelection(IList<object> addedItems, IList<object> removedItems)
    {
        foreach (var item in removedItems)
        {
            if (item is Game game)
            {
                SelectedGames.Remove(game);
            }
        }

        foreach (var item in addedItems)
        {
            if (item is Game game && SelectedGames.Contains(game) == false)
            {
                SelectedGames.Add(game);
            }
        }

        NotifySelectionChanged();
    }

    void NotifySelectionChanged()
    {
        OnPropertyChanged(nameof(SelectedGamesCountText));
        OnPropertyChanged(nameof(SelectAllButtonText));
        OnPropertyChanged(nameof(CanApplyBatchDll));
        ApplyBatchDllCommand.NotifyCanExecuteChanged();
    }

    public GameGridPageModel(GameGridPage gameGridPage)
    {
        WeakReferenceMessenger.Default.Register<GameLibrariesStateChangedMessage>(this, async (sender, message) =>
        {
            GameManager.Instance.RemoveAllGames();
            await InitialLoadAsync();
        });

        this.gameGridPage = gameGridPage;
        ApplyGameGroupFilter();
    }

    public async Task InitialLoadAsync()
    {
        IsGameListLoading = true;
        IsDLSSLoading = true;

        await GameManager.Instance.LoadGamesFromCacheAsync();

        IsGameListLoading = false;

        await GameManager.Instance.LoadGamesAsync(false);

        IsDLSSLoading = false;
    }

    public void SearchForGameEvent(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            throw new ArgumentException("Sender must be a TextBox");
        }

        if (IsSelectionMode == true)
        {
            // Swapping ItemsSource clears the control's visual selection and fires
            // spurious SelectionChanged events (see the note in ApplyGameGroupFilter).
            gameGridPage.BeginSuppressSelectionEvents();
        }

        if (string.IsNullOrEmpty(textBox.Text))
        {
            CurrentCollectionView = GameManager.Instance.GetGameCollection();
        }
        else
        {
            CurrentCollectionView = GameManager.Instance.GetGameCollection(textBox.Text);
        }

        if (IsSelectionMode == true)
        {
            gameGridPage.ResyncVisualSelectionAfterViewChange();
        }
    }

    [RelayCommand]
    async Task AddManualGameButtonAsync()
    {
        if (Settings.Instance.DontShowManuallyAddingGamesNotice == false)
        {
            var dontShowAgainCheckbox = new CheckBox()
            {
                Content = new TextBlock()
                {
                    Text = ResourceHelper.GetString("General_DontShowAgain"),
                },
            };

            var dialog = new EasyContentDialog(gameGridPage.XamlRoot)
            {
                Title = ResourceHelper.GetString("GamesPage_ManuallyAdding_NoteTitle"),
                PrimaryButtonText = ResourceHelper.GetString("GamesPage_AddGame"),
                SecondaryButtonText = ResourceHelper.GetString("General_ReportIssue"),
                CloseButtonText = ResourceHelper.GetString("General_Cancel"),
                DefaultButton = ContentDialogButton.Primary,
                Content = new StackPanel()
                {
                    Children = {
                        new TextBlock()
                        {
                            TextWrapping = TextWrapping.Wrap,
                            Text = ResourceHelper.GetString("GamesPage_ManuallyAdding_NoteMessage"),
                        },
                        dontShowAgainCheckbox,
                    },
                    Orientation = Orientation.Vertical,
                    Spacing = 16,
                },
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.None)
            {
                return;
            }


            if (result == ContentDialogResult.Primary)
            {
                // Only dismiss the notice for good once the user has proceeded to add games.
                if (dontShowAgainCheckbox.IsChecked == true)
                {
                    Settings.Instance.DontShowManuallyAddingGamesNotice = true;
                }
                await AddGameManually();
            }
            else if (result == ContentDialogResult.Secondary)
            {
                await Launcher.LaunchUriAsync(new Uri("https://github.com/beeradmoore/dlss-swapper/issues"));
            }
        }
        else
        {
            await AddGameManually();
        }
    }

    async Task AddGameManually()
    {
        TextBlockBuilder textBlockBuilder = new TextBlockBuilder(ResourceHelper.GetString("GamesPage_ManuallyAdding_InfoHtml"));

        if (Settings.Instance.HasShownAddGameFolderMessage == false)
        {
            var dialog = new EasyContentDialog(gameGridPage.XamlRoot)
            {
                Title = ResourceHelper.GetString("GamesPage_ManuallyAdding_AnotherNoteTitle"),
                PrimaryButtonText = ResourceHelper.GetString("GamesPage_AddGame"),
                CloseButtonText = ResourceHelper.GetString("General_Close"),
                DefaultButton = ContentDialogButton.Primary,
                Content = textBlockBuilder.Build()
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.None)
            {
                return;
            }

            Settings.Instance.HasShownAddGameFolderMessage = true;
        }

        var installPath = string.Empty;
        try
        {
            // Associate the HWND with the folder picker
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.CurrentApp.MainWindow);


            var folder = FileSystemHelper.OpenFolder(hWnd, okButtonLabel: ResourceHelper.GetString("GamesPage_ManuallyAdding_SelectGameFolder"));

            if (string.IsNullOrWhiteSpace(folder))
            {
                return;
            }

            installPath = folder;

            // If top level directory throw error.
            if (installPath == Path.GetPathRoot(installPath))
            {
                var dialog = new EasyContentDialog(gameGridPage.XamlRoot)
                {
                    CloseButtonText = ResourceHelper.GetString("General_Okay"),
                    DefaultButton = ContentDialogButton.Close,
                    Title = ResourceHelper.GetString("General_Error"),
                    Content = ResourceHelper.GetString("GamesPage_ManuallyAdding_TopLevelDirectoryNotSupported"),
                };
                await dialog.ShowAsync();
                return;
            }


            var gameFolderAlreadyExists = GameManager.Instance.CheckIfGameIsAdded(installPath);
            if (gameFolderAlreadyExists == true)
            {
                var dialog = new EasyContentDialog(gameGridPage.XamlRoot)
                {
                    Title = ResourceHelper.GetString("GamesPage_ManuallyAdding_ErrorTitle"),
                    CloseButtonText = ResourceHelper.GetString("General_Close"),
                    Content = ResourceHelper.GetFormattedResourceTemplate("GamesPage_ManuallyAdding_PathExistsTemplate", installPath),
                };
                await dialog.ShowAsync();
                return;
            }

            var manuallyAddGameControl = new ManuallyAddGameControl(installPath);
            var addGameDialog = new FakeContentDialog() //XamlRoot
            {
                CloseButtonText = ResourceHelper.GetString("General_Cancel"),
                PrimaryButtonText = ResourceHelper.GetString("GamesPage_AddGame"),
                DefaultButton = ContentDialogButton.Primary,
                Content = manuallyAddGameControl,
            };
            addGameDialog.Resources["ContentDialogMinWidth"] = 700;
            addGameDialog.Resources["ContentDialogMaxWidth"] = 700;

            var addGameResult = await addGameDialog.ShowAsync();
            if (manuallyAddGameControl.DataContext is ManuallyAddGameModel manuallyAddGameModel)
            {
                if (addGameResult == ContentDialogResult.Primary)
                {
                    var game = manuallyAddGameModel.Game;
                    await game.SaveToDatabaseAsync();
                    game.ProcessGame();
                    GameManager.Instance.AddGame(game, true);
                }
                else
                {
                    // Cleanup if user is going back.
                    await manuallyAddGameModel.Game.DeleteAsync();
                }
            }
        }
        catch (Exception err)
        {
            Logger.Error(err, $"Attempted to manually add game from path \"{installPath}\" but got an error.");
            var dialog = new EasyContentDialog(gameGridPage.XamlRoot)
            {
                Title = ResourceHelper.GetString("GamesPage_ManuallyAdding_ErrorTitle"),
                CloseButtonText = ResourceHelper.GetString("General_Close"),
                PrimaryButtonText = ResourceHelper.GetString("General_ReportIssue"),
                DefaultButton = ContentDialogButton.Primary,
                Content = $"{ResourceHelper.GetString("GamesPage_ManuallyAdding_CouldntAddError")}\n\n{ResourceHelper.GetString("General_ErrorMessage")}: {err.Message}",
            };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await Launcher.LaunchUriAsync(new Uri("https://github.com/beeradmoore/dlss-swapper/issues"));
            }
        }
    }

    [RelayCommand]
    async Task RefreshGamesButtonAsync()
    {
        IsDLSSLoading = true;

        await GameManager.Instance.LoadGamesAsync(true);

        IsDLSSLoading = false;
    }

    [RelayCommand]
    async Task FilterGamesButtonAsync()
    {
        var gameFilterControl = new GameFilterControl();

        var dialog = new EasyContentDialog(gameGridPage.XamlRoot)
        {
            Title = ResourceHelper.GetString("General_Filter"),
            PrimaryButtonText = ResourceHelper.GetString("General_Apply"),
            CloseButtonText = ResourceHelper.GetString("General_Cancel"),
            DefaultButton = ContentDialogButton.Primary,
            Content = gameFilterControl,
        };
        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            if (gameFilterControl.DataContext is GameFilterControlViewModel gameFilterControlViewModel)
            {
                Settings.Instance.HideNonDLSSGames = gameFilterControlViewModel.HideNonSwappableGames;
                GameManager.Instance.ShowHiddenGames = gameFilterControlViewModel.ShowHiddenGames;
                Settings.Instance.GroupGameLibrariesTogether = gameFilterControlViewModel.GroupGameLibrariesTogether;
            }

            ApplyGameGroupFilter();
        }

    }

    void ApplyGameGroupFilter()
    {
        // TODO: Remove weird hack which otherwise causes MainGridView_SelectionChanged to fire when changing MainGridView.ItemsSource.
        //gameGridPage.MainGridView.SelectionChanged -= MainGridView_SelectionChanged;

        //MainGridView.ItemsSource = null;
        CurrentCollectionView = null;
        CurrentCollectionView = GameManager.Instance.GetGameCollection();
    }

    [RelayCommand]
    async Task UnknownAssetsFoundButtonAsync()
    {
        var newDllsControl = new NewDLLsControl();

        var dialog = new EasyContentDialog(gameGridPage.XamlRoot)
        {
            Title = ResourceHelper.GetString("GamesPage_NewDllsFound"),
            CloseButtonText = ResourceHelper.GetString("General_Close"),
            Content = newDllsControl,
        };
        dialog.Resources["ContentDialogMinWidth"] = 700;
        dialog.Resources["ContentDialogMaxWidth"] = 700;
        await dialog.ShowAsync();
    }

    [RelayCommand]
    void ChangeGameGridView(GameGridViewType gameGridView)
    {
        if (gameGridView == this.GameGridViewType)
        {
            return;
        }

        GameGridViewType = gameGridView;
        gameGridPage.ReloadMainContentControl();
        Settings.Instance.GameGridViewType = gameGridView;
    }
}

enum PresetOutcome
{
    Set,
    Error,
    SkippedNoAsset,
    SkippedNoProfile,
    SkippedUnsupported,
}

readonly record struct BatchDllActionItem(GameAssetType Type, DLLRecord Record, string Label);

readonly record struct BatchPresetActionItem(GameAssetType Type, uint PresetValue, string Label);
