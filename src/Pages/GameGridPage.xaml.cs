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
using System.Text;
using System.ComponentModel.DataAnnotations.Schema;
using System.CodeDom;
using System.Collections.Concurrent;
using AsyncAwaitBestPractices;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DLSS_Swapper.Pages
{

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class GameGridPage : Page
    {
        /*
        public List<IGameLibrary> GameLibraries { get; } = new List<IGameLibrary>();

        Dictionary<GameLibrary, ObservableCollection<Game>> allGames = new Dictionary<GameLibrary, ObservableCollection<Game>>();


        public List<GameGroup> GroupedGameGroups { get; } = new List<GameGroup>();
        public List<GameGroup> UngroupedGameGroups { get; } = new List<GameGroup>();
        
        ObservableCollection<Game> FavouriteGames = new ObservableCollection<Game>();
        ObservableCollection<Game> AllGames = new ObservableCollection<Game>();
        */
        
        bool _loadingGamesAndDlls = false;

        public GameGridPage()
        {
            this.InitializeComponent();
            DataContext = new GameGridPageModel(this);
        }


        async Task LoadGamesAsync()
        {
            // TODO: REMOVE
            await Task.Delay(1);
            /*
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
            */
        }

       

        bool hasFirstLoaded = false;
        void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (hasFirstLoaded)
            {
                return;
            }
            hasFirstLoaded = true;

            if (DataContext is GameGridPageModel gameGridPageModel)
            {
                gameGridPageModel.InitialLoadAsync().SafeFireAndForget((err) =>
                {
                    Logger.Error($"Unable to perform initial load - {err.Message}");
                });
            }

            //await LoadGamesAndDlls();
            //await LoadGamesFromCacheAsync();
            //UpdateGameLibraries();
            //await LoadGames();
        }

        async void MainGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (e.AddedItems.Count == 0)
            {
                return;
            }

            if (e.AddedItems[0] is Game game)
            {
                // Deselect currently selected item.
                if (sender is GridView gridView)
                {
                    gridView.SelectedItem = null;

                }

                var gameControl = new GameControl(game);
                await gameControl.ShowAsync();

            }
        }

     

        void UpdateGameLibraries()
        {
            /*
            GameLibraries.Clear();

            foreach (GameLibrary gameLibraryEnum in Enum.GetValues<GameLibrary>())
            {
                var gameLibrary = IGameLibrary.GetGameLibrary(gameLibraryEnum);
                if (gameLibrary.IsEnabled() == true)
                {
                    GameLibraries.Add(gameLibrary);
                }
            }
            */
        }


        async Task LoadGamesAndDlls()
        {
            // TODO: REMOVE
            await Task.Delay(1);

            if (_loadingGamesAndDlls)
                return;

            _loadingGamesAndDlls = true;

            // TODO: Fade?
            //LoadingStackPanel.Visibility = Visibility.Visible;

            /*
            var tasks = new List<Task>();
            tasks.Add(LoadGamesAsync());


            await Task.WhenAll(tasks);
            
            */
            DispatcherQueue.TryEnqueue(() =>
            {
                //LoadingStackPanel.Visibility = Visibility.Collapsed;
                _loadingGamesAndDlls = false;
            });
        }
    }

}
