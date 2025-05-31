using DLSS_Swapper.Data;
using DLSS_Swapper.UserControls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Threading.Tasks;
using Windows.System;
using AsyncAwaitBestPractices;
using CommunityToolkit.WinUI;
using System.Threading;
using DLSS_Swapper.Helpers;

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
        Timer? _saveScrollSizeTimer = null;

        public GameGridPageModel ViewModel { get; private set; }

        public GameGridPage()
        {
            this.InitializeComponent();
            ViewModel = new GameGridPageModel(this);
            DataContext = ViewModel;
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
                    Logger.Error(err, $"Unable to perform initial load");
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

                if (game.Processing)
                {
                    var dialog = new EasyContentDialog(this.XamlRoot)
                    {
                        Title = "Game Currently Processing",
                        CloseButtonText = "Okay",
                        Content = $"{game.Title} is still processing. Please wait for the loading indicator to complete before opening.",
                    };
                    await dialog.ShowAsync();
                    return;
                }

                var gameControl = new GameControl(game);
                await gameControl.ShowAsync();

            }
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
            App.CurrentApp.RunOnUIThread(() =>
            {
                //LoadingStackPanel.Visibility = Visibility.Collapsed;
                _loadingGamesAndDlls = false;
            });
        }

        internal void ScrollToGame(Game game)
        {
            if (MainContentControl.ContentTemplateRoot is GridView mainGridView)
            {
                App.CurrentApp.RunOnUIThreadAsync(async () =>
                {
                    var indexOfGame = mainGridView.Items.IndexOf(game);
                    if (indexOfGame >= 0)
                    {
                        await mainGridView.SmoothScrollIntoViewWithItemAsync(indexOfGame);
                    }
                }).SafeFireAndForget();
            }
            else if (MainContentControl.ContentTemplateRoot is ListView mainListView)
            {
                App.CurrentApp.RunOnUIThreadAsync(async () =>
                {
                    var indexOfGame = mainListView.Items.IndexOf(game);
                    if (indexOfGame >= 0)
                    {
                        await mainListView.SmoothScrollIntoViewWithItemAsync(indexOfGame);
                    }
                }).SafeFireAndForget();
            }
        }

        internal void ReloadMainContentControl()
        {
            MainContentControl.Content = null;
            MainContentControl.Content = ViewModel;
        }

        // This fires for both the GridView and the ListView
        void GridAndListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is Game selectedGame)
            {
                if (selectedGame.Processing)
                {
                    var dialog = new EasyContentDialog(XamlRoot)
                    {
                        Title = ResourceHelper.GetString("GameCurrentlyProcessing"),
                        CloseButtonText = ResourceHelper.GetString("Okay"),
                        Content = ResourceHelper.GetFormattedResourceTemplate("GameProcessingPleaseWaitTemplate", selectedGame.Title),
                    };
                    _ = dialog.ShowAsync();
                    return;
                }

                var gameControl = new GameControl(selectedGame);
                _ = gameControl.ShowAsync();
            }
        }


        void MainGridView_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (e.KeyModifiers.HasFlag(VirtualKeyModifiers.Control))
            {
                var delta = e.GetCurrentPoint((UIElement)sender).Properties.MouseWheelDelta;

                if (sender is GridView gridView)
                {
                    double scaleAmount = delta > 0 ? 1.05 : 0.95;
                    var newWidth = (int)(ViewModel.GridViewItemWidth * scaleAmount);

                    if (newWidth > 60 && newWidth < 600)
                    {
                        ViewModel.GridViewItemWidth = newWidth;

                        if (_saveScrollSizeTimer is not null)
                        {
                            _saveScrollSizeTimer.Dispose();
                            _saveScrollSizeTimer = null;
                        }

                        _saveScrollSizeTimer = new Timer((state) =>
                        {
                            Settings.Instance.GridViewItemWidth = ViewModel.GridViewItemWidth;
                        }, null, 500, Timeout.Infinite);
                    }
                }

                e.Handled = true;
            }
        }
    }
}
