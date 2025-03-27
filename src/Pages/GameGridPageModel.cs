using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLSS_Swapper.Data;
using DLSS_Swapper.Helpers;
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

public enum GameGridViewType
{
    GridView,
    ListView,
}

public partial class GameGridPageModel : ObservableObject, IDisposable
{
    public GameGridPageModel(GameGridPage gameGridPage)
    {
        _languageManager = LanguageManager.Instance;
        _languageManager.OnLanguageChanged += OnLanguageChanged;
        this.gameGridPage = gameGridPage;
        ApplyGameGroupFilter();
    }

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
                    Text = ResourceHelper.GetString("DontShowAgain"),
                },
            };

            var dialog = new EasyContentDialog(gameGridPage.XamlRoot)
            {
                Title = ResourceHelper.GetString("AddingGamesManuallyNoteTitle"),
                PrimaryButtonText = ResourceHelper.GetString("AddGame"),
                SecondaryButtonText = ResourceHelper.GetString("ReportIssue"),
                CloseButtonText = ResourceHelper.GetString("Cancel"),
                DefaultButton = ContentDialogButton.Primary,
                Content = new StackPanel()
                {
                    Children = {
                        new TextBlock()
                        {
                            TextWrapping = TextWrapping.Wrap,
                            Text = ResourceHelper.GetString("AddingGamesManuallyNote"),
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
                Title = ResourceHelper.GetString("AddingGamesManuallyAnotherNote"),
                PrimaryButtonText = ResourceHelper.GetString("AddGame"),
                CloseButtonText = ResourceHelper.GetString("Close"),
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
                        new Run() { Text = "For example, if you have a game at:\n" },
                        new Run() { Text = "C:\\Program Files\\MyGamesFolder\\MyFavouriteGame\\" },
                        new Run() { Text = "\n\n" },
                        new Run() { Text = "You would select the " },
                        new Run() { Text = "MyFavouriteGame", FontWeight = FontWeights.Bold },
                        new Run() { Text = " directory and not the " },
                        new Run() { Text = "MyGamesFolder", FontWeight = FontWeights.Bold },
                        new Run() { Text = " directory." },
                    },
                }
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
            CommitButtonText = ResourceHelper.GetString("SelectGameFolder"),
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
                    CloseButtonText = ResourceHelper.GetString("Okay"),
                    DefaultButton = ContentDialogButton.Close,
                    Title = ResourceHelper.GetString("Error"),
                    Content = ResourceHelper.GetString("TopLevelDirectoryNotSupported"),
                };
                await dialog.ShowAsync();
                return;
            }


            var gameFolderAlreadyExists = GameManager.Instance.CheckIfGameIsAdded(installPath);
            if (gameFolderAlreadyExists == true)
            {
                var dialog = new EasyContentDialog(gameGridPage.XamlRoot)
                {
                    Title = ResourceHelper.GetString("AddingGameError"),
                    CloseButtonText = ResourceHelper.GetString("Close"),
                    Content = ResourceHelper.GetFormattedResourceTemplate("InstallPathAlreadyExistsTemplate", installPath),
                };
                await dialog.ShowAsync();
                return;
            }

            var manuallyAddGameControl = new ManuallyAddGameControl(installPath);
            var addGameDialog = new FakeContentDialog() //XamlRoot
            {
                CloseButtonText = ResourceHelper.GetString("Cancel"),
                PrimaryButtonText = ResourceHelper.GetString("AddGame"),
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
                Title = ResourceHelper.GetString("AddingGameError"),
                CloseButtonText = ResourceHelper.GetString("Close"),
                PrimaryButtonText = ResourceHelper.GetString("ReportIssue"),
                DefaultButton = ContentDialogButton.Primary,
                Content = $"{ResourceHelper.GetString("AddingGameErrorReportIssue")}\n\n{ResourceHelper.GetString("Error message")}: {err.Message}",
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
            Title = ResourceHelper.GetString("Filter"),
            PrimaryButtonText = ResourceHelper.GetString("Apply"),
            CloseButtonText = ResourceHelper.GetString("Cancel"),
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
        CurrentCollectionView = null;
        CurrentCollectionView = GameManager.Instance.GetGameCollection();
    }

    [RelayCommand]
    async Task UnknownAssetsFoundButtonAsync()
    {
        var newDllsControl = new NewDLLsControl();

        var dialog = new EasyContentDialog(gameGridPage.XamlRoot)
        {
            Title = ResourceHelper.GetString("NewDllsFound"),
            CloseButtonText = ResourceHelper.GetString("Close"),
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

    #region TranslationProperties
    public string NewDllsText => ResourceHelper.GetString("NewDLLs");
    public string AddGameText => ResourceHelper.GetString("AddGame");
    public string RefreshText => ResourceHelper.GetString("Refresh");
    public string FilterText => ResourceHelper.GetString("Filter");
    public string SearchText => ResourceHelper.GetString("Search");
    public string ViewTypeText => ResourceHelper.GetString("ViewType");
    public string GridViewText => ResourceHelper.GetString("GridView");
    public string ListViewText => ResourceHelper.GetString("ListView");
    public string GamesText => ResourceHelper.GetString("Games");
    public string ApplicationRunsInAdministrativeModeInfo => ResourceHelper.GetString("ApplicationRunsInAdministrativeModeInfo");
    #endregion

    private void OnLanguageChanged()
    {
        OnPropertyChanged(NewDllsText);
        OnPropertyChanged(AddGameText);
        OnPropertyChanged(RefreshText);
        OnPropertyChanged(FilterText);
        OnPropertyChanged(SearchText);
        OnPropertyChanged(ViewTypeText);
        OnPropertyChanged(GridViewText);
        OnPropertyChanged(ListViewText);
        OnPropertyChanged(GamesText);
        OnPropertyChanged(ApplicationRunsInAdministrativeModeInfo);
    }

    public void Dispose()
    {
        _languageManager.OnLanguageChanged -= OnLanguageChanged;
    }

    ~GameGridPageModel()
    {
        Dispose();
    }

    private readonly LanguageManager _languageManager;
}
