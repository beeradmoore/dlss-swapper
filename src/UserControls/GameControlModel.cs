using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLSS_Swapper.Data;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics;
using System.IO;

namespace DLSS_Swapper.UserControls;

internal partial class GameControlModel : ObservableObject
{
    WeakReference<GameControl> gameControlWeakReference;

    public Game Game { get; init; }

    public bool CanRemove => Game.GameLibrary == Interfaces.GameLibrary.ManuallyAdded;

    public GameControlModel(GameControl gameControl, Game game)
    {
        gameControlWeakReference = new WeakReference<GameControl>(gameControl);
        Game = game;
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
                throw new Exception($"Could not find path \"{Game.InstallPath}\".");
            }
        }
        catch (Exception err)
        {
            Logger.Error(err.Message);

            if (gameControlWeakReference.TryGetTarget(out GameControl gameControl))
            {
                var dialog = new EasyContentDialog(gameControl.XamlRoot)
                {
                    Title = $"Error",
                    CloseButtonText = "Okay",
                    Content = err.Message,
                };
                await dialog.ShowAsync();
            }
        }
    }

    [RelayCommand]
    async Task EditNotesAsync()
    {
        if (gameControlWeakReference.TryGetTarget(out GameControl gameControl))
        {
            var textBox = new TextBox()
            {
                MinHeight = 400,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
            };
            // This needs to be set after AcceptsReturn otherwise it will strip out the \r
            var dialog = new EasyContentDialog(gameControl.XamlRoot)
            {
                Title = $"Notes - {Game.Title}",
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                Content = textBox,
            };
            dialog.Resources["ContentDialogMinWidth"] = 700;
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                Game.Notes = textBox.Text ?? String.Empty;
                await Game.SaveToDatabaseAsync();
            }
        }
    }

    [RelayCommand]
    async Task ViewHistoryAsync()
    {
        //ViewHistoryCommand
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
        if (gameControlWeakReference.TryGetTarget(out GameControl gameControl))
        {
            gameControl.Hide();
        }
    }

    [RelayCommand]
    async Task RemoveAsync()
    {
        if (gameControlWeakReference.TryGetTarget(out GameControl gameControl))
        {
            // This should never happen
            if (CanRemove == false)
            {
                var cantDeleteDialog = new EasyContentDialog(gameControl.XamlRoot)
                {
                    Title = "Error",
                    CloseButtonText = "Okay",
                    DefaultButton = ContentDialogButton.Close,
                    Content = "This title is not manually added and can't be removed.",
                };
                await cantDeleteDialog.ShowAsync();
                return;
            }


        
            // This needs to be set after AcceptsReturn otherwise it will strip out the \r
            var dialog = new EasyContentDialog(gameControl.XamlRoot)
            {
                Title = $"Remove {Game.Title}?",
                PrimaryButtonText = "Remove",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                Content = "Are you sure you want to remove \"{Game.Title}\" from DLSS Swapper? This will not make any changes to DLSS files that have already been swapped.",
            };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await Game.DeleteAsync();
                gameControl.Hide();
                // TODO: Refresh game lists.
            }
        }
    }

}
