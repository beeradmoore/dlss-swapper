using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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
    public partial bool IsGameListLoading { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLoading))]
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

        if (string.IsNullOrEmpty(textBox.Text))
        {
            CurrentCollectionView = GameManager.Instance.GetGameCollection();
            return;
        }
        CurrentCollectionView = GameManager.Instance.GetGameCollection(textBox.Text);
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
    async Task UpdateAllGamesButtonAsync()
    {
        var games = GameManager.Instance.GetSynchronisedGamesListCopy();
        if (games.Count == 0)
        {
            var noGamesDialog = new EasyContentDialog(gameGridPage.XamlRoot)
            {
                Title = ResourceHelper.GetString("GamesPage_UpdateAll_Title"),
                CloseButtonText = ResourceHelper.GetString("General_Okay"),
                Content = ResourceHelper.GetString("GamesPage_UpdateAll_NoGames"),
            };
            await noGamesDialog.ShowAsync();
            return;
        }

        // Confirmation, with the option to download any missing DLLs as part of the update.
        var downloadCheckbox = new CheckBox()
        {
            Content = new TextBlock()
            {
                Text = ResourceHelper.GetString("GamesPage_UpdateAll_DownloadIfNeeded"),
                TextWrapping = TextWrapping.Wrap,
            },
            IsChecked = true,
        };

        var confirmDialog = new EasyContentDialog(gameGridPage.XamlRoot)
        {
            Title = ResourceHelper.GetString("GamesPage_UpdateAll_Title"),
            PrimaryButtonText = ResourceHelper.GetString("GamesPage_UpdateAll_Confirm"),
            CloseButtonText = ResourceHelper.GetString("General_Cancel"),
            DefaultButton = ContentDialogButton.Primary,
            Content = new StackPanel()
            {
                Orientation = Orientation.Vertical,
                Spacing = 16,
                Children =
                {
                    new TextBlock()
                    {
                        TextWrapping = TextWrapping.Wrap,
                        Text = ResourceHelper.GetString("GamesPage_UpdateAll_Message"),
                    },
                    downloadCheckbox,
                },
            },
        };

        var confirmResult = await confirmDialog.ShowAsync();
        if (confirmResult != ContentDialogResult.Primary)
        {
            return;
        }

        var allowDownloading = downloadCheckbox.IsChecked == true;

        // Progress UI. The Close button doubles as a Cancel button while the update is running.
        using var cancellationTokenSource = new CancellationTokenSource();

        var progressBar = new ProgressBar()
        {
            Minimum = 0,
            Maximum = games.Count,
            Value = 0,
        };
        var statusTextBlock = new TextBlock()
        {
            TextWrapping = TextWrapping.Wrap,
        };
        var detailTextBlock = new TextBlock()
        {
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.7,
        };

        var progressDialog = new EasyContentDialog(gameGridPage.XamlRoot)
        {
            Title = ResourceHelper.GetString("GamesPage_UpdateAll_Updating"),
            CloseButtonText = ResourceHelper.GetString("General_Cancel"),
            Content = new StackPanel()
            {
                Orientation = Orientation.Vertical,
                Spacing = 12,
                Children = { progressBar, statusTextBlock, detailTextBlock },
            },
        };
        progressDialog.CloseButtonClick += (sender, args) =>
        {
            // Don't let the dialog close instantly, let the in-flight game finish and the work loop exit cleanly.
            args.Cancel = true;
            cancellationTokenSource.Cancel();
            statusTextBlock.Text = ResourceHelper.GetString("GamesPage_UpdateAll_Status_Cancelling");
        };

        // Progress<T> captures the current (UI) SynchronizationContext so these callbacks are safe to touch UI.
        var progress = new Progress<BulkUpdateProgress>(update =>
        {
            progressBar.Maximum = update.TotalGames;
            progressBar.Value = update.ProcessedGames;
            statusTextBlock.Text = ResourceHelper.GetFormattedResourceTemplate("GamesPage_UpdateAll_Status_ProgressTemplate", update.ProcessedGames, update.TotalGames, update.CurrentGameTitle);
            detailTextBlock.Text = update.CurrentAction;
        });

        var workTask = Task.Run(() => BulkDllUpdater.UpdateAllAsync(games, allowDownloading, progress, cancellationTokenSource.Token));

        // Start showing the progress dialog, wait for the work to finish, then close it. Hiding after the work
        // completes (rather than auto-hiding from a continuation) avoids a race when the work finishes instantly.
        var showDialogTask = progressDialog.ShowAsync().AsTask();
        var summary = await workTask;
        progressDialog.Hide();
        await showDialogTask;

        await ShowBulkUpdateSummaryAsync(summary);
    }

    async Task ShowBulkUpdateSummaryAsync(BulkUpdateSummary summary)
    {
        var messageLines = new List<string>()
        {
            ResourceHelper.GetFormattedResourceTemplate("GamesPage_UpdateAll_Summary_GamesUpdatedTemplate", summary.GamesUpdated),
            ResourceHelper.GetFormattedResourceTemplate("GamesPage_UpdateAll_Summary_DllsUpdatedTemplate", summary.DllsUpdated),
            ResourceHelper.GetFormattedResourceTemplate("GamesPage_UpdateAll_Summary_AlreadyUpToDateTemplate", summary.GamesAlreadyUpToDate),
        };

        if (summary.GamesFailed > 0)
        {
            messageLines.Add(ResourceHelper.GetFormattedResourceTemplate("GamesPage_UpdateAll_Summary_FailedTemplate", summary.GamesFailed));
        }

        if (summary.Cancelled)
        {
            messageLines.Add(ResourceHelper.GetString("GamesPage_UpdateAll_Summary_Cancelled"));
        }

        var content = new StackPanel()
        {
            Orientation = Orientation.Vertical,
            Spacing = 12,
            Children =
            {
                new TextBlock()
                {
                    TextWrapping = TextWrapping.Wrap,
                    Text = string.Join("\n", messageLines),
                },
            },
        };

        // Show details of any failures so the user knows what went wrong.
        if (summary.Errors.Count > 0)
        {
            content.Children.Add(new TextBlock()
            {
                TextWrapping = TextWrapping.Wrap,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Text = ResourceHelper.GetString("GamesPage_UpdateAll_Summary_Errors"),
            });

            content.Children.Add(new ScrollViewer()
            {
                MaxHeight = 200,
                Content = new TextBlock()
                {
                    TextWrapping = TextWrapping.Wrap,
                    Opacity = 0.8,
                    Text = string.Join("\n", summary.Errors),
                },
            });
        }

        var summaryDialog = new EasyContentDialog(gameGridPage.XamlRoot)
        {
            Title = ResourceHelper.GetString("GamesPage_UpdateAll_Summary_Title"),
            CloseButtonText = ResourceHelper.GetString("General_Okay"),
            DefaultButton = ContentDialogButton.Close,
            Content = content,
        };

        // Offer to relaunch as admin if a write was blocked by permissions.
        if (summary.PromptToRelaunchAsAdmin && App.CurrentApp.IsAdminUser() == false)
        {
            summaryDialog.PrimaryButtonText = ResourceHelper.GetString("General_RestartAsAdministrator");
            summaryDialog.DefaultButton = ContentDialogButton.Primary;
        }

        var result = await summaryDialog.ShowAsync();
        if (result == ContentDialogResult.Primary && summary.PromptToRelaunchAsAdmin)
        {
            App.CurrentApp.RestartAsAdmin();
        }
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
