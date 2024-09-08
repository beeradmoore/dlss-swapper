using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using CommunityToolkit.Labs.WinUI.MarkdownTextBlock;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLSS_Swapper.Data;
using DLSS_Swapper.Data.Steam;
using DLSS_Swapper.UserControls;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Text;

namespace DLSS_Swapper.Pages;

internal partial class GameGridPageModel : ObservableObject
{
    GameGridPage gameGridPage;

    public bool RunsAsAdmin { get; } = Environment.IsPrivilegedProcess;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLoading))]
    bool isGameListLoading = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLoading))]
    bool isDLSSLoading = true;

    public bool IsLoading => (IsGameListLoading || IsDLSSLoading);

    [ObservableProperty]
    ICollectionView currentCollectionView = null;


    public GameGridPageModel(GameGridPage gameGridPage)
    {
        this.gameGridPage = gameGridPage;

        IsDLSSLoading = false;
        IsGameListLoading = false;
        /* 
        CurrentCollectionViewSource = null; new CollectionViewSource()
        {
            IsSourceGrouped = true,
            ItemsPath = new PropertyPath("Games"),
            Source = new ObservableCollection<GameGroup>() { 
                new GameGroup("TEST", 
                    new System.Collections.ObjectModel.ObservableCollection<Data.Game>()
                    {
                        new SteamGame(),
                    })
            },
        };
        */

        ApplyGameGroupFilter();
     }

    public async Task InitialLoadAsync()
    {
        //await Task.Delay(1);

        //IsGameListLoading = true;
        // IsDLSSLoading = true;

        await GameManager.Instance.LoadGamesFromCacheAsync();
        await GameManager.Instance.LoadGamesAsync();
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
                Content = new StackPanel()
                {
                    Children = {
                        new TextBlock()
                        {
                            TextWrapping = TextWrapping.Wrap,
                            Text = @"DLSS Swapper should find games from your installed game libraries automatically. If your game is not listed there may be a few settings preventing it. Please check:

- Games list filter is not set to ""Hide non-DLSS games""
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

    async Task AddGameManually()
    {
        if (Settings.Instance.HasShownAddGameFolderMessage == false)
        {
            var dialog = new EasyContentDialog(gameGridPage.XamlRoot)
            {
                Title = "Another note for manually adding games",
                PrimaryButtonText = "Add Game",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                Content = new TextBlock()
                {
                    TextWrapping = TextWrapping.Wrap,
                    Inlines =
                    {
                        new Run() { Text = "You must select your " },
                        new Run() { Text = "game", FontStyle = FontStyle.Italic },
                        new Run() { Text = " directory, not your " },
                        new Run() { Text = "games", FontStyle = FontStyle.Italic },
                        new Run() { Text = " directory." },
                        new Run() { Text = "\n\n" },
                        new Run() { Text = "For example, iff you have a game at:\n" },
                        new Run() { Text = "C:\\Program Files\\MyGamesFolder\\MyFavouriteGame\\" },
                        new Run() { Text = "\n\n" },
                        new Run() { Text = "You would select the " },
                        new Run() { Text = "MyFavouriteGame", FontWeight = FontWeights.Bold },
                        new Run() { Text = " directory and not the " },
                        new Run() { Text = "MyGamesFolder", FontWeight = FontWeights.Bold },
                        new Run() { Text = " directory." },
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


        var folderPicker = new FolderPicker()
        {
            SuggestedStartLocation = PickerLocationId.ComputerFolder,
            CommitButtonText = "Select Game Folder",
        };

        var installPath = String.Empty;
        try
        {
            // Associate the HWND with the folder picker
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.CurrentApp.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

            var folder = await folderPicker.PickSingleFolderAsync();

            if (folder == null)
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

            /*
            var manualGameLibrary = GameLibraries.Single(x => x.GameLibrary == GameLibrary.ManuallyAdded);

            // This will allow adding existing game manually, EG. Steam game can be added manually. There will be some bad things
            // in the UI where a user may update DLSS in the Steam copy but does not show up as changed in the manually added version.
            // The workaround to this would be to check all games here but it will have the flaw of missing a game (eg. if Steam is disabled,
            // install game on Steam, manually add game to DLSS Swapper, then enable Steam. We can check every library when adding their games
            // that they don't exist in ManuallyAdded list, but then its a real pain to have to answer the questions of "Why doesn't game X show
            // in the Steam section".
            // Keeping this like this allows users to disable Steam but add the one specific Steam game they want to be able to swap DLSS in.
            var gameFolderAlreadyExists = manualGameLibrary.LoadedGames.Any(x => x.InstallPath == installPath);
            if (gameFolderAlreadyExists == true)
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

                    await manualGameLibrary.ListGamesAsync();
                    FilterGames();
                }
                else
                {
                    // Cleanup if user is going back.
                    await manuallyAddGameModel.Game.DeleteAsync();
                }
            }
            */
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
    async Task RefreshGamesButtonAsync()
    {
        await Task.Delay(1);
        //await LoadGamesAndDlls();
    }

    [RelayCommand]
    async Task FilterGamesButtonAsync()
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

    void ApplyGameGroupFilter()
    {
        // TODO: Remove weird hack which otherwise causes MainGridView_SelectionChanged to fire when changing MainGridView.ItemsSource.
        //gameGridPage.MainGridView.SelectionChanged -= MainGridView_SelectionChanged;

        //MainGridView.ItemsSource = null;
        //CurrentCollectionView = null;
        if (Settings.Instance.GroupGameLibrariesTogether)
        {
            CurrentCollectionView = GameManager.Instance.GroupedGameCollectionViewSource.View;

            /*
            var collectionViewSource = new CollectionViewSource()
            {
                IsSourceGrouped = true,
                Source = GameLibraries,
            };

            if (Settings.Instance.HideNonDLSSGames)
            {
                collectionViewSource.ItemsPath = new PropertyPath("LoadedDLSSGames");
            }
            else
            {
                collectionViewSource.ItemsPath = new PropertyPath("LoadedGames");
            }

            MainGridView.ItemsSource = collectionViewSource.View;
            */
        }
        else
        {
            CurrentCollectionView = GameManager.Instance.UngroupedGameCollectionViewSource.View;

            /*
            var games = new List<Game>();

            if (Settings.Instance.HideNonDLSSGames)
            {
                foreach (var gameLibrary in GameLibraries)
                {
                    games.AddRange(gameLibrary.LoadedGames.Where(g => g.HasDLSS == true));
                }
            }
            else
            {
                foreach (var gameLibrary in GameLibraries)
                {
                    games.AddRange(gameLibrary.LoadedGames);
                }
            }
            games.Sort();

            MainGridView.ItemsSource = games;
            */
        }

        // TODO: Remove weird hack which otherwise causes MainGridView_SelectionChanged to fire when changing MainGridView.ItemsSource.
        //gameGridPage.MainGridView.SelectedIndex = -1;
        //gameGridPage.MainGridView.SelectionChanged += MainGridView_SelectionChanged;
        
    }

}
