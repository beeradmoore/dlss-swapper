using System;
using System.IO;
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
