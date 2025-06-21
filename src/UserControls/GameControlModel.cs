using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLSS_Swapper.Data;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System.Diagnostics;
using System.IO;
using Windows.System;
using DLSS_Swapper.Helpers;

namespace DLSS_Swapper.UserControls;

public partial class GameControlModel : ObservableObject
{
    WeakReference<GameControl> gameControlWeakReference;

    public Game Game { get; init; }

    public bool IsManuallyAdded => Game.GameLibrary == Interfaces.GameLibrary.ManuallyAdded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GameTitleHasChanged))]
    public partial string GameTitle { get; set; }

    public bool GameTitleHasChanged
    {
        get
        {
            if (IsManuallyAdded == false)
            {
                return false;
            }

            if (string.IsNullOrEmpty(GameTitle))
            {
                return false;
            }

            return GameTitle.Equals(Game.Title) == false;
        }
    }

    public GameControlModelTranslationProperties TranslationProperties { get; } = new GameControlModelTranslationProperties();

    public GameControlModel(GameControl gameControl, Game game) : base()
    {
        gameControlWeakReference = new WeakReference<GameControl>(gameControl);
        Game = game;
        GameTitle = game.Title;
    }

    [RelayCommand]
    async Task OpenInstallPathAsync()
    {
        try
        {
            if (Directory.Exists(Game.InstallPath))
            {
                Process.Start("explorer.exe", Game.InstallPath);
            }
            else
            {
                throw new Exception(ResourceHelper.GetFormattedResourceTemplate("GamePage_CouldNotFindGameInstallPathTemplate", Game.InstallPath));
            }
        }
        catch (Exception err)
        {
            Logger.Error(err);

            if (gameControlWeakReference.TryGetTarget(out GameControl? gameControl))
            {
                var dialog = new EasyContentDialog(gameControl.XamlRoot)
                {
                    Title = ResourceHelper.GetString("General_Error"),
                    CloseButtonText = ResourceHelper.GetString("General_Okay"),
                    Content = err.Message,
                };
                await dialog.ShowAsync();
            }
        }
    }

    [RelayCommand]
    async Task EditNotesAsync()
    {
        if (gameControlWeakReference.TryGetTarget(out GameControl? gameControl))
        {
            var textBox = new TextBox()
            {
                MinHeight = 400,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
            };
            // This needs to be set after AcceptsReturn otherwise it will strip out the \r
            textBox.Text = Game.Notes;

            var dialog = new EasyContentDialog(gameControl.XamlRoot)
            {
                Title = $"{ResourceHelper.GetString("GamePage_Notes")} - {Game.Title}",
                PrimaryButtonText = ResourceHelper.GetString("General_Save"),
                CloseButtonText = ResourceHelper.GetString("General_Cancel"),
                DefaultButton = ContentDialogButton.Primary,
                Content = textBox,
            };
            dialog.Resources["ContentDialogMinWidth"] = 700;
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                Game.Notes = textBox.Text ?? string.Empty;
                await Game.SaveToDatabaseAsync();
            }
        }
    }

    [RelayCommand]
    async Task ViewHistoryAsync()
    {
        //ViewHistoryCommand
        // TODO: implement
        await Task.Delay(100);
    }

    [RelayCommand]
    async Task AddCoverImageAsync()
    {
        if (Game.CoverImage == Game.ExpectedCustomCoverImage)
        {
            await Game.PromptToRemoveCustomCover();
            return;
        }

        await Game.PromptToBrowseCustomCover();
    }

    [RelayCommand]
    void Close()
    {
        if (gameControlWeakReference.TryGetTarget(out GameControl? gameControl))
        {
            gameControl.Hide();
        }
    }

    [RelayCommand]
    async Task RemoveAsync()
    {
        if (gameControlWeakReference.TryGetTarget(out GameControl? gameControl))
        {
            // This should never happen
            if (IsManuallyAdded == false)
            {
                var cantDeleteDialog = new EasyContentDialog(gameControl.XamlRoot)
                {
                    Title = ResourceHelper.GetString("General_Error"),
                    CloseButtonText = ResourceHelper.GetString("General_Okay"),
                    DefaultButton = ContentDialogButton.Close,
                    Content = ResourceHelper.GetString("GamePage_ManuallyAddedGameCantBeRemoved"),
                };
                await cantDeleteDialog.ShowAsync();
                return;
            }



            // This needs to be set after AcceptsReturn otherwise it will strip out the \r
            var dialog = new EasyContentDialog(gameControl.XamlRoot)
            {
                Title = $"{ResourceHelper.GetString("General_Remove")} {Game.Title}?",
                PrimaryButtonText = ResourceHelper.GetString("General_Remove"),
                CloseButtonText = ResourceHelper.GetString("General_Cancel"),
                DefaultButton = ContentDialogButton.Primary,
                Content = ResourceHelper.GetFormattedResourceTemplate("RemoveDlssSwapperGameTemplate", Game.Title),
            };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await Game.DeleteAsync();
                GameManager.Instance.RemoveGame(Game);
                gameControl.Hide();
            }
        }
    }

    [RelayCommand]
    async Task FavouriteAsync()
    {
        Game.IsFavourite = !Game.IsFavourite;
        await Game.SaveToDatabaseAsync();
    }

    [RelayCommand]
    async Task ChangeRecordAsync(GameAssetType gameAssetType)
    {
        if (gameControlWeakReference.TryGetTarget(out GameControl? gameControl))
        {
            var dialog = new EasyContentDialog(gameControl.XamlRoot)
            {
                Title = ResourceHelper.GetFormattedResourceTemplate("SelectVersionTemplate", DLLManager.Instance.GetAssetTypeName(gameAssetType)),
                PrimaryButtonText = ResourceHelper.GetString("General_Swap"),
                IsPrimaryButtonEnabled = false,
                CloseButtonText = ResourceHelper.GetString("General_Cancel"),
                DefaultButton = ContentDialogButton.Primary,
            };

            var dllPickerControl = new DLLPickerControl(gameControl, dialog, Game, gameAssetType);
            dialog.Content = dllPickerControl;
            await dialog.ShowAsync();
        }
    }

    [RelayCommand]
    async Task SaveTitleAsync()
    {
        Game.Title = GameTitle;
        await Game.SaveToDatabaseAsync();
        OnPropertyChanged(nameof(GameTitleHasChanged));
    }

    [RelayCommand]
    async Task MultipleDLLsFoundAsync(GameAssetType gameAssetType)
    {
        if (gameControlWeakReference.TryGetTarget(out GameControl? gameControl))
        {
            var dialog = new EasyContentDialog(gameControl.XamlRoot)
            {
                Title = ResourceHelper.GetFormattedResourceTemplate("MultipleDllsFoundTemplate", DLLManager.Instance.GetAssetTypeName(gameAssetType)),
                PrimaryButtonText = ResourceHelper.GetString("General_Okay"),
                DefaultButton = ContentDialogButton.Primary,
                Content = new MultipleDLLsFoundControl(Game, gameAssetType),
            };

            await dialog.ShowAsync();
        }
    }

    [RelayCommand]
    async Task ReadyToPlayStateMoreInformationAsync()
    {
        await Launcher.LaunchUriAsync(new Uri("https://github.com/beeradmoore/dlss-swapper/wiki/Troubleshooting#game-is-not-in-a-ready-to-play-state"));
    }
}
