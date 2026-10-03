using DLSS_Swapper.Data;
using DLSS_Swapper.UserControls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;
using AsyncAwaitBestPractices;
using CommunityToolkit.WinUI;
using System.Threading;
using DLSS_Swapper.Helpers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DLSS_Swapper.Pages;


/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class GameGridPage : Page
{
    public static string PageTag { get; } = "PageTag_Games";

    /*
    public List<IGameLibrary> GameLibraries { get; } = new List<IGameLibrary>();

    Dictionary<GameLibrary, ObservableCollection<Game>> allGames = new Dictionary<GameLibrary, ObservableCollection<Game>>();


    public List<GameGroup> GroupedGameGroups { get; } = new List<GameGroup>();
    public List<GameGroup> UngroupedGameGroups { get; } = new List<GameGroup>();

    ObservableCollection<Game> FavouriteGames = new ObservableCollection<Game>();
    ObservableCollection<Game> AllGames = new ObservableCollection<Game>();
    */

    bool _loadingGamesAndDlls;
    Timer? _saveScrollSizeTimer;

    public GameGridPageModel ViewModel { get; private set; }

    public GameGridPage()
    {
        this.InitializeComponent();
        ViewModel = new GameGridPageModel(this);
        DataContext = ViewModel;
    }

    bool hasFirstLoaded;
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
                    Title = ResourceHelper.GetString("Game_CurrentlyProcessing"),
                    CloseButtonText = ResourceHelper.GetString("General_Okay"),
                    Content = ResourceHelper.GetFormattedResourceTemplate("GamePage_ProcessingPleaseWaitTemplate", selectedGame.Title),
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

    private void ClearSearchBox_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Text = string.Empty;
    }

    internal string SearchText => SearchBox.Text;

    bool _isSyncingSelection;
    int _viewSyncGeneration;

    ListViewBase? GetActiveListControl()
    {
        return MainContentControl.ContentTemplateRoot as ListViewBase;
    }

    internal void EnterSelectionMode()
    {
        var listControl = GetActiveListControl();
        if (listControl is null)
        {
            return;
        }

        listControl.SelectionMode = ListViewSelectionMode.Multiple;
        listControl.IsItemClickEnabled = false;
        listControl.SelectionChanged += ListControl_SelectionChanged;
        ResyncVisualSelection();
    }

    internal void ExitSelectionMode()
    {
        var listControl = GetActiveListControl();
        if (listControl is null)
        {
            return;
        }

        listControl.SelectionChanged -= ListControl_SelectionChanged;
        _isSyncingSelection = true;
        try
        {
            if (listControl.Items.Count > 0)
            {
                listControl.DeselectRange(new ItemIndexRange(0, (uint)listControl.Items.Count));
            }
        }
        finally
        {
            _isSyncingSelection = false;
        }
        listControl.SelectionMode = ListViewSelectionMode.None;
        listControl.IsItemClickEnabled = true;
    }

    internal IEnumerable<Game> GetDistinctVisibleGames()
    {
        var list = GetActiveListControl();
        return list is null ? Enumerable.Empty<Game>() : list.Items.OfType<Game>().Distinct().ToList();
    }

    void ListControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncingSelection == true)
        {
            return;
        }

        ViewModel.UpdateSelection(e.AddedItems, e.RemovedItems);
        ResyncVisualSelection();
    }

    internal void BeginSuppressSelectionEvents()
    {
        _isSyncingSelection = true;
        _viewSyncGeneration++;
    }

    // Called after CurrentCollectionView changes while selection mode is active.
    // Swapping the ItemsSource clears the list's visual selection, so it is
    // re-applied from the view model's SelectedGames afterwards.
    internal void ResyncVisualSelectionAfterViewChange()
    {
        var generation = _viewSyncGeneration;
        var enqueued = DispatcherQueue.TryEnqueue(() =>
        {
            if (generation != _viewSyncGeneration)
            {
                // A newer view change has already been requested; ignore this stale callback.
                return;
            }

            ResyncVisualSelection();
            ViewModel.NotifySelectionChanged();
        });

        // BeginSuppressSelectionEvents set _isSyncingSelection before the ItemsSource
        // swap; if the resync could not be queued, nothing else would ever reset it
        // and all selection events would be ignored from here on.
        if (enqueued == false)
        {
            _isSyncingSelection = false;
        }
    }

    internal void ResyncVisualSelection()
    {
        var listControl = GetActiveListControl();
        if (listControl is null)
        {
            _isSyncingSelection = false;
            return;
        }

        _isSyncingSelection = true;
        try
        {
            if (listControl.Items.Count > 0)
            {
                listControl.DeselectRange(new ItemIndexRange(0, (uint)listControl.Items.Count));
            }

            for (var index = 0; index < listControl.Items.Count; index++)
            {
                if (listControl.Items[index] is Game game && ViewModel.SelectedGames.Contains(game))
                {
                    listControl.SelectRange(new ItemIndexRange(index, 1));
                }
            }
        }
        finally
        {
            _isSyncingSelection = false;
        }
    }
}
