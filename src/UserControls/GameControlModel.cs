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
using System.Diagnostics;
using System.IO;
using System.ComponentModel;

namespace DLSS_Swapper.UserControls;

public partial class GameControlModel : ObservableObject
{
    WeakReference<GameControl> gameControlWeakReference;

    public Game Game { get; init; }

    public bool IsManuallyAdded => Game.GameLibrary == Interfaces.GameLibrary.ManuallyAdded;

    private string _selectedDllPath;
    private string _textBoxText;

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

    public GameControlModel(GameControl gameControl, Game game)
    {
        gameControlWeakReference = new WeakReference<GameControl>(gameControl);
        Game = game;
        GameTitle = game.Title;
        SelectedDllPath = string.Empty;
    }

    public string SelectedDllPath
    {
        get => _selectedDllPath;
        set
        {
            if (_selectedDllPath != value)
            {
                _selectedDllPath = value;
                UpdateTextBoxText();
            }
        }
    }

    public string DllPathTextBox
    {
        get => _textBoxText;
        set
        {
            if (_textBoxText != value)
            {
                _textBoxText = value;
                OnPropertyChanged(nameof(DllPathTextBox));
            }
        }
    }

    private void UpdateTextBoxText()
    {
        try
        {
            DllPathTextBox = SelectedDllPath switch
            {
                "DLSS" => Path.GetDirectoryName(Game.CurrentDLSS?.Path) ?? "Not found",
                "DLSS G" => Path.GetDirectoryName(Game.CurrentDLSS_G?.Path) ?? "Not found",
                "DLSS D" => Path.GetDirectoryName(Game.CurrentDLSS_D?.Path) ?? "Not found",
                "FSR DX12" => Path.GetDirectoryName(Game.CurrentFSR_31_DX12?.Path) ?? "Not found",
                "FSR VK" => Path.GetDirectoryName(Game.CurrentFSR_31_VK?.Path) ?? "Not found",
                "XeSS" => Path.GetDirectoryName(Game.CurrentXeSS?.Path) ?? "Not found",
                "XeSS FG" => Path.GetDirectoryName(Game.CurrentXeSS_FG?.Path) ?? "Not found",
                "XeLL" => Path.GetDirectoryName(Game.CurrentXeLL?.Path) ?? "Not found",
                _ => "Select a DLL type"
            };
        }
        catch (Exception)
        {
            DllPathTextBox = "Path not available";
        }
    }

    [RelayCommand]
    async Task OpenDllPathAsync()
    {
        try
        {
            if (Directory.Exists(DllPathTextBox))
            {
                Process.Start("explorer.exe", DllPathTextBox);
            }
            else
            {
                throw new Exception($"Could not find path \"{DllPathTextBox}\".");
            }
        }
        catch (Exception err)
        {
            Logger.Error(err.Message);

            if (gameControlWeakReference.TryGetTarget(out GameControl? gameControl))
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

            if (gameControlWeakReference.TryGetTarget(out GameControl? gameControl))
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
                Content = $"Are you sure you want to remove \"{Game.Title}\" from DLSS Swapper? This will not make any changes to DLSS files that have already been swapped.",
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
                Title = $"Select {DLLManager.Instance.GetAssetTypeName(gameAssetType)} version",
                PrimaryButtonText = "Swap",
                IsPrimaryButtonEnabled = false,
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
            };

            var dllPickerControl = new DLLPickerControl(gameControlWeakReference, dialog, Game, gameAssetType);
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
                Title = $"Multiple {DLLManager.Instance.GetAssetTypeName(gameAssetType)} DLLs Found",
                PrimaryButtonText = "Okay",
                DefaultButton = ContentDialogButton.Primary,
                Content = new MultipleDLLsFoundControl(Game, gameAssetType),
            };

            await dialog.ShowAsync();
        }
    }
}
