using CommunityToolkit.WinUI.UI.Controls;
using DLSS_Swapper.Data;
using DLSS_Swapper.Data.EpicGamesStore;
using DLSS_Swapper.Data.GOG;
using DLSS_Swapper.Data.GitHub;
using DLSS_Swapper.Data.Steam;
using DLSS_Swapper.Data.UbisoftConnect;
using DLSS_Swapper.Data.Xbox;
using DLSS_Swapper.Interfaces;
using DLSS_Swapper.UserControls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Principal;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;
using Windows.System;
using DLSS_Swapper.Data.CustomDirectory;
using System.Text;
using CommunityToolkit.WinUI.UI;
using System.ComponentModel.DataAnnotations.Schema;
using System.CodeDom;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DLSS_Swapper.Pages
{
    public class GameGroup
    {
        public string Name { get; private set; } = String.Empty;
        public ObservableCollection<Game> Games { get; private set; } = null;

        public GameGroup(string name, ObservableCollection<Game> games)
        {
            Name = name;
            Games = games;
        }
    }
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class GameGridPage : Page
    {
        public List<IGameLibrary> GameLibraries { get; } = new List<IGameLibrary>();

        public List<GameGroup> GroupedGameGroups { get; } = new List<GameGroup>();
        public List<GameGroup> UngroupedGameGroups { get; } = new List<GameGroup>();
        
        ObservableCollection<Game> FavouriteGames = new ObservableCollection<Game>();
        ObservableCollection<Game> AllGames = new ObservableCollection<Game>();

        bool _loadingGamesAndDlls = false;

        public bool RunsAsAdmin { get; } = Environment.IsPrivilegedProcess;

        public GameGridPage()
        {
            this.InitializeComponent();

            DataContext = this;
        }


        async Task LoadGamesAsync()
        {
            // Added this check so if we get to here and this is true we probably crashed loading games last time and we should prompt for that.
            if (Settings.Instance.WasLoadingGames)
            {
                var richTextBlock = new RichTextBlock();
                var paragraph = new Paragraph()
                {
                    Margin = new Thickness(0, 0, 0, 0),
                };
                paragraph.Inlines.Add(new Run()
                {
                    Text = "DLSS Swapper had an issue loading game libraries.  Please try disabling a game library below. You can re-enable these options later in the settings.",
                });
                richTextBlock.Blocks.Add(paragraph);
                paragraph = new Paragraph()
                {
                    Margin = new Thickness(0, 0, 0, 0),
                };
                paragraph.Inlines.Add(new Run()
                {
                    Text = "If this keeps happening please file a bug report ",
                });
                var hyperLink = new Hyperlink()
                {
                    NavigateUri = new Uri("https://github.com/beeradmoore/dlss-swapper/issues"),

                };
                hyperLink.Inlines.Add(new Run()
                {
                    Text = "here"
                });
                paragraph.Inlines.Add(hyperLink);
                paragraph.Inlines.Add(new Run()
                {
                    Text = ".",
                });
                richTextBlock.Blocks.Add(paragraph);




                var grid = new Grid()
                {
                    RowSpacing = 10,
                };
                grid.RowDefinitions.Add(new RowDefinition());
                grid.RowDefinitions.Add(new RowDefinition());

                Grid.SetRow(richTextBlock, 0);
                grid.Children.Add(richTextBlock);


                var gameLibrarySelectorControl = new GameLibrarySelectorControl();

                Grid.SetRow(gameLibrarySelectorControl, 1);
                grid.Children.Add(gameLibrarySelectorControl);


                var dialog = new EasyContentDialog(XamlRoot)
                {
                    Title = "Failed to load game libraries",
                    PrimaryButtonText = "Save",
                    SecondaryButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Primary,
                    Content = grid,
                };
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    gameLibrarySelectorControl.Save();
                }
            }

            Settings.Instance.WasLoadingGames = true;

            GameLibraries.Clear();

            // Auto game library loading.
            // Simply adding IGameLibrary interface means we will load the games.
            var loadGameTasks = new List<Task>(); 
            foreach (GameLibrary gameLibraryEnum in Enum.GetValues<GameLibrary>())
            {
                var gameLibrary = IGameLibrary.GetGameLibrary(gameLibraryEnum);
                if (gameLibrary.IsEnabled())
                {
                    GameLibraries.Add(gameLibrary);
                    loadGameTasks.Add(gameLibrary.ListGamesAsync());
                }
            }

            // Await them all to finish loading games.
            await Task.WhenAll(loadGameTasks);

            Settings.Instance.WasLoadingGames = false;

            DispatcherQueue.TryEnqueue(() =>
            {
                FilterGames();
            });
        }

        void FilterGames()
        {
            // TODO: Remove weird hack which otherwise causes MainGridView_SelectionChanged to fire when changing MainGridView.ItemsSource.
            MainGridView.SelectionChanged -= MainGridView_SelectionChanged;

            //MainGridView.ItemsSource = null;

            if (Settings.Instance.GroupGameLibrariesTogether)
            {

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
            }
            else
            {
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
            }

            // TODO: Remove weird hack which otherwise causes MainGridView_SelectionChanged to fire when changing MainGridView.ItemsSource.
            MainGridView.SelectedIndex = -1;
            MainGridView.SelectionChanged += MainGridView_SelectionChanged;
        }

        bool hasFirstLoaded = false;
        async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (hasFirstLoaded)
            {
                return;
            }
            hasFirstLoaded = true;
            //await LoadGamesAndDlls();
            await LoadGames();
        }

        async void MainGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
            {
                return;
            }

            MainGridView.SelectedIndex = -1;
            if (e.AddedItems[0] is Game game)
            {
                /*
                //var mainGrid = Content as Grid;
                var dialog2 = new EasyContentDialog(XamlRoot)
                {
                    //dialog.Title = "Error";
                    PrimaryButtonText = "Okay",
                    DefaultButton = ContentDialogButton.Primary,
                    Content = $"DLSS was not detected in {game.Title}.",
                };
                await dialog2.ShowAsync();
                return;*/
            
                var gameControl = new GameControl(game);              
                await gameControl.ShowAsync();

                return;
                EasyContentDialog dialog;

                if (game.HasDLSS == false)
                {
                    dialog = new EasyContentDialog(XamlRoot)
                    {
                        //dialog.Title = "Error";
                        PrimaryButtonText = "Okay",
                        DefaultButton = ContentDialogButton.Primary,
                        Content = $"DLSS was not detected in {game.Title}.",
                    };
                    await dialog.ShowAsync();
                    return;
                }

                var dlssPickerControl = new DLSSPickerControl(game);
                dialog = new EasyContentDialog(XamlRoot)
                { 
                    Title = "Select DLSS Version",
                    PrimaryButtonText = "Swap",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Primary,
                    Content = dlssPickerControl,
                };

                if (String.IsNullOrEmpty(game.BaseDLSSVersion) == false)
                {
                    dialog.SecondaryButtonText = "Reset";
                }

                var result = await dialog.ShowAsync();


                if (result == ContentDialogResult.Primary)
                {
                    var selectedDLSSRecord = dlssPickerControl.GetSelectedDLSSRecord();

                    if (selectedDLSSRecord.LocalRecord.IsDownloading == true || selectedDLSSRecord.LocalRecord.IsDownloaded == false)
                    {
                        // TODO: Initiate download here.
                        dialog = new EasyContentDialog(XamlRoot)
                        {
                            Title = "Error",
                            CloseButtonText = "Okay",
                            DefaultButton = ContentDialogButton.Close,
                            Content = "Please download the DLSS record from the downloads page first.",
                        };
                        await dialog.ShowAsync();
                        return;
                    }

                    var didUpdate = game.UpdateDll(selectedDLSSRecord);

                    if (didUpdate.Success == false)
                    {
                        dialog = new EasyContentDialog(XamlRoot)
                        {
                            Title = "Error",
                            PrimaryButtonText = "Okay",
                            DefaultButton = ContentDialogButton.Primary,
                            Content = didUpdate.Message,
                        };

                        if (didUpdate.PromptToRelaunchAsAdmin is true)
                        {
                            dialog.SecondaryButtonText = "Relaunch as Administrator";
                        }

                        var dialogResult = await dialog.ShowAsync();
                        if (dialogResult is ContentDialogResult.Secondary)
                        {
                            App.CurrentApp.RestartAsAdmin();
                        }
                    }
                }
                else if (result == ContentDialogResult.Secondary)
                {
                    var didReset = game.ResetDll();

                    if (didReset.Success == false)
                    {
                        dialog = new EasyContentDialog(XamlRoot)
                        {
                            Title = "Error",
                            PrimaryButtonText = "Okay",
                            DefaultButton = ContentDialogButton.Primary,
                            Content = didReset.Message,
                        };

                        if (didReset.PromptToRelaunchAsAdmin is true)
                        {
                            dialog.SecondaryButtonText = "Relaunch as Administrator";
                        }

                        var dialogResult = await dialog.ShowAsync();
                        if (dialogResult is ContentDialogResult.Secondary)
                        {
                            App.CurrentApp.RestartAsAdmin();
                        }
                    }
                }
            }
        }

        async Task LoadGames()
        {

            var steamLibrary1 = IGameLibrary.GetGameLibrary(GameLibrary.Steam);
            var steamLibrary2 = IGameLibrary.GetGameLibrary(GameLibrary.Steam);


            Debugger.Break();

            foreach (GameLibrary gameLibraryEnum in Enum.GetValues<GameLibrary>())
            {
                var gameLibrary = IGameLibrary.GetGameLibrary(gameLibraryEnum);

                if (gameLibraryEnum == GameLibrary.Steam)
                {
                    var query = App.CurrentApp.Database.Table<ManuallyAddedGame>().ToListAsync();

                }
                else if(gameLibraryEnum == GameLibrary.GOG)
                {

                }
                else if(gameLibraryEnum == GameLibrary.EpicGamesStore)
                {

                }
                else if(gameLibraryEnum == GameLibrary.UbisoftConnect)
                {

                }
                else if(gameLibraryEnum == GameLibrary.XboxApp)
                {

                }
                else if(gameLibraryEnum == GameLibrary.ManuallyAdded)
                {
                    var query = App.CurrentApp.Database.Table<ManuallyAddedGame>().ToListAsync();

                }
                else
                {

                }

                //.Where(s => s.Symbol.StartsWith("A"));

                //.get..
                //GameLibraries.Add(gameLibrary);
            }


            return;


        }

        async Task LoadGamesAndDlls()
        {
            if (_loadingGamesAndDlls)
                return;

            _loadingGamesAndDlls = true;

            // TODO: Fade?
            LoadingStackPanel.Visibility = Visibility.Visible;

            var tasks = new List<Task>();
            tasks.Add(LoadGamesAsync());


            await Task.WhenAll(tasks);

            DispatcherQueue.TryEnqueue(() =>
            {
                LoadingStackPanel.Visibility = Visibility.Collapsed;
                _loadingGamesAndDlls = false;
            });
        }

        async void AddGameButton_Click(object sender, RoutedEventArgs e)
        {
            if (Settings.Instance.HasShownManuallyAddingGamesNotice == false)
            {
                var dialog = new EasyContentDialog(XamlRoot)
                {
                    Title = "Note for manually adding games",
                    PrimaryButtonText = "Add games",
                    SecondaryButtonText = "Report issue",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Primary,
                    Content = new MarkdownTextBlock()
                    {
                        Text = @"DLSS Swapper should find games from your installed game libraries automatically. If your game is not listed there may be a few settings preventing it. Please check:

- Games list filter is not set to ""Hide non-DLSS games""
- Specific game library is enabled in settings

If you have checked these and your game is still not showing up there may be a bug. We would appreciate it if you could report the issue on our GitHub repository so we can make a fix and have your games better detected in the future.",
                        Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
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
                    Settings.Instance.HasShownManuallyAddingGamesNotice = true;
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
            var folderPicker = new FolderPicker()
            {
                SuggestedStartLocation = PickerLocationId.ComputerFolder,
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
                    var dialog = new EasyContentDialog(XamlRoot)
                    {
                        CloseButtonText = "Okay",
                        DefaultButton = ContentDialogButton.Close,
                        Title = "Error",
                        Content = "Adding top level directory is not supported. Please add the root of your game directory.",
                    };
                    await dialog.ShowAsync();
                    return;
                }

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
                    var dialog = new EasyContentDialog(XamlRoot)
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
            }
            catch (Exception err)
            {
                Logger.Error($"Attempted to manually add game from path {installPath} but got an error. ({err.Message})");
                var dialog = new EasyContentDialog(XamlRoot)
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

        async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadGamesAndDlls();
        }

        async void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            var gameFilterControl = new GameFilterControl();

            var dialog = new EasyContentDialog(XamlRoot)
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

                FilterGames();
            }
        }
    }
}
