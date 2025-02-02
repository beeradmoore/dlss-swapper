using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLSS_Swapper.Data;
using DLSS_Swapper.UserControls;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Text;

namespace DLSS_Swapper.Pages;

public partial class GameGridPageModel : ObservableObject
{
    private readonly GameGridPage gameGridPage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLoading))]
    public partial bool IsGameListLoading { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLoading))]
    public partial bool IsDLSSLoading { get; set; } = true;

    public bool IsLoading => (IsGameListLoading || IsDLSSLoading);

    [ObservableProperty]
    public partial ICollectionView? CurrentCollectionView { get; set; } = null;

    public GameGridPageModel(GameGridPage gameGridPage)
    {
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

    [RelayCommand]
    private async Task AddManualGameButtonAsync()
    {
        if (Settings.Instance.DontShowManuallyAddingGamesNotice == false)
        {
            var dontShowAgainCheckbox = new CheckBox
            {
                Content = new TextBlock
                {
                    Text = "Don't show again",
                },
            };

            var dialog = new EasyContentDialog(gameGridPage.XamlRoot)
            {
                Title = "Note for manually adding games",
                PrimaryButtonText = "Add Game",
                SecondaryButtonText = "Report Issue",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                Content = new StackPanel
                {
                    Children = {
                        new TextBlock
                        {
                            TextWrapping = TextWrapping.Wrap,
                            Text = @"DLSS Swapper should find games from your installed game libraries automatically. If your game is not listed there may be a few settings preventing it. Please check:

- Games list filter is not set to ""Hide games with no swappable items""
- Specific game library is enabled in settings

If you have checked these and your game is still not showing up there may be a bug. We would appreciate it if you could report the issue on our GitHub repository so we can make a fix and have your games better detected in the future.",
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

    private async Task AddGameManually()
    {
        if (Settings.Instance.HasShownAddGameFolderMessage == false)
        {
            var dialog = new EasyContentDialog(gameGridPage.XamlRoot)
            {
                Title = "Another note for manually adding games",
                PrimaryButtonText = "Add Game",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                Content = new TextBlock
                {
                    TextWrapping = TextWrapping.Wrap,
                    Inlines =
                    {
                        new Run { Text = "You must select your " },
                        new Run { Text = "game", FontStyle = FontStyle.Italic },
                        new Run { Text = " directory, not your " },
                        new Run { Text = "games", FontStyle = FontStyle.Italic },
                        new Run { Text = " directory." },
                        new Run { Text = "\n\n" },
                        new Run { Text = "For example, iff you have a game at:\n" },
                        new Run { Text = "C:\\Program Files\\MyGamesFolder\\MyFavouriteGame\\" },
                        new Run { Text = "\n\n" },
                        new Run { Text = "You would select the " },
                        new Run { Text = "MyFavouriteGame", FontWeight = FontWeights.Bold },
                        new Run { Text = " directory and not the " },
                        new Run { Text = "MyGamesFolder", FontWeight = FontWeights.Bold },
                        new Run { Text = " directory." },
                    },
                },
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.None)
            {
                return;
            }

            Settings.Instance.HasShownAddGameFolderMessage = true;
        }


        var folderPicker = new FolderPicker
        {
            SuggestedStartLocation = PickerLocationId.ComputerFolder,
            CommitButtonText = "Select Game Folder",
        };
        folderPicker.FileTypeFilter.Add("*");

        var installPath = string.Empty;
        try
        {
            // Associate the HWND with the folder picker
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.CurrentApp.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

            var folder = await folderPicker.PickSingleFolderAsync();

            if (folder is null)
            {
                return;
            }

            installPath = folder.Path;

            // If top level directory throw error.
            if (installPath == Path.GetPathRoot(installPath))
            {
                var dialog = new EasyContentDialog(gameGridPage.XamlRoot)
                {
                    CloseButtonText = "Okay",
                    DefaultButton = ContentDialogButton.Close,
                    Title = "Error",
                    Content = "Adding top level directory is not supported. Please add the root of your game directory.",
                };
                await dialog.ShowAsync();
                return;
            }


            var gameFolderAlreadyExists = GameManager.Instance.CheckIfGameIsAdded(installPath);
            if (gameFolderAlreadyExists)
            {
                var dialog = new EasyContentDialog(gameGridPage.XamlRoot)
                {
                    Title = "Error adding your game",
                    CloseButtonText = "Close",
                    Content = $"The install path \"{installPath}\" already exists and can't be added again.",
                };
                await dialog.ShowAsync();
                return;
            }

            var manuallyAddGameControl = new ManuallyAddGameControl(installPath);
            var addGameDialog = new FakeContentDialog() //XamlRoot
            {
                CloseButtonText = "Cancel",
                PrimaryButtonText = "Add Game",
                DefaultButton = ContentDialogButton.Primary,
                Content = manuallyAddGameControl,
                Resources = {
                    ["ContentDialogMinWidth"] = 700,
                    ["ContentDialogMaxWidth"] = 700,
                },
            };

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
            Logger.Error($"Attempted to manually add game from path {installPath} but got an error. ({err.Message})");
            var dialog = new EasyContentDialog(gameGridPage.XamlRoot)
            {
                Title = "Error adding your game",
                CloseButtonText = "Close",
                PrimaryButtonText = "Report issue",
                DefaultButton = ContentDialogButton.Primary,
                Content = $"There was a problem and your game could not be added at this time. Please report this issue.\n\nError message: {err.Message}",
            };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await Launcher.LaunchUriAsync(new Uri("https://github.com/beeradmoore/dlss-swapper/issues"));
            }
        }
    }

    [RelayCommand]
    private async Task RefreshGamesButtonAsync()
    {
        IsDLSSLoading = true;

        await GameManager.Instance.LoadGamesAsync(true);

        IsDLSSLoading = false;
    }

    [RelayCommand]
    private async Task FilterGamesButtonAsync()
    {
        var gameFilterControl = new GameFilterControl();

        var dialog = new EasyContentDialog(gameGridPage.XamlRoot)
        {
            Title = "Filter",
            PrimaryButtonText = "Apply",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            Content = gameFilterControl,
        };
        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            Settings.Instance.HideNonDLSSGames = gameFilterControl.IsHideNonDLSSGamesChecked();
            Settings.Instance.GroupGameLibrariesTogether = gameFilterControl.IsGroupGameLibrariesTogetherChecked();

            ApplyGameGroupFilter();
        }

    }

    private void ApplyGameGroupFilter()
    {
        // TODO: Remove weird hack which otherwise causes MainGridView_SelectionChanged to fire when changing MainGridView.ItemsSource.
        //gameGridPage.MainGridView.SelectionChanged -= MainGridView_SelectionChanged;

        //MainGridView.ItemsSource = null;
        CurrentCollectionView = null;
        CurrentCollectionView = GameManager.Instance.GetGameCollection();
    }

    [RelayCommand]
    private async Task UnknownAssetsFoundButtonAsync()
    {
        var newDllsControl = new NewDLLsControl();

        var dialog = new EasyContentDialog(gameGridPage.XamlRoot)
        {
            Title = "New DLLs Found",
            CloseButtonText = "Close",
            Content = newDllsControl,
            Resources = {
                ["ContentDialogMinWidth"] = 700,
                ["ContentDialogMaxWidth"] = 700,
            },
        };
        await dialog.ShowAsync();
    }

}
