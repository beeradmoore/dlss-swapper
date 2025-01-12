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

    public bool CanRemove => Game.GameLibrary == Interfaces.GameLibrary.ManuallyAdded;

    public List<DLLRecord> DLSSRecords { get; } = new List<DLLRecord>(DLLManager.Instance.DLSSRecords);
    public List<DLLRecord> DLSSGRecords { get; } = new List<DLLRecord>(DLLManager.Instance.DLSSGRecords);
    public List<DLLRecord> DLSSDRecords { get; } = new List<DLLRecord>(DLLManager.Instance.DLSSDRecords);
    public List<DLLRecord> FSR31DX12Records { get; } = new List<DLLRecord>(DLLManager.Instance.FSR31DX12Records);
    public List<DLLRecord> FSR31VKRecords { get; } = new List<DLLRecord>(DLLManager.Instance.FSR31VKRecords);
    public List<DLLRecord> XeSSRecords { get; } = new List<DLLRecord>(DLLManager.Instance.XeSSRecords);
    public List<DLLRecord> XeLLRecords { get; } = new List<DLLRecord>(DLLManager.Instance.XeLLRecords);
    public List<DLLRecord> XeSSFGRecords { get; } = new List<DLLRecord>(DLLManager.Instance.XeSSFGRecords);

    [ObservableProperty]
    DLLRecord? currentDLSS;

    [ObservableProperty]
    DLLRecord? currentDLSSG;
    
    [ObservableProperty]
    DLLRecord? currentDLSSD;
    
    [ObservableProperty]
    DLLRecord? currentFSR31DX12;
    
    [ObservableProperty]
    DLLRecord? currentFSR31VK;

    [ObservableProperty]
    DLLRecord? currentXeSS;

    [ObservableProperty]
    DLLRecord? currentXeLL;

    [ObservableProperty]
    DLLRecord? currentXeSSFG;

    public bool IsDLSSAvailable { get; private set; } = false;
    public bool IsDLSSGAvailable { get; private set; } = false;
    public bool IsDLSSDAvailable { get; private set; } = false;
    public bool IsFSR31DX12Available { get; private set; } = false;
    public bool IsFSR31VKAvailable { get; private set; } = false;
    public bool IsXeSSAvailable { get; private set; } = false;
    public bool IsXeLLAvailable { get; private set; } = false;
    public bool IsXeSSFGAvailable { get; private set; } = false;

    public GameControlModel(GameControl gameControl, Game game)
    {
        gameControlWeakReference = new WeakReference<GameControl>(gameControl);
        Game = game;

        if (Game.CurrentDLSS is not null)
        {
            IsDLSSAvailable = true;
            CurrentDLSS = DLSSRecords.FirstOrDefault(v => v.MD5Hash == Game.CurrentDLSS.Hash);
            if (CurrentDLSS is null)
            {
                Debug.WriteLine($"DLSS not found in manifest: {Game.CurrentDLSS.Version}, {Game.CurrentDLSS.Hash}, from {Game.Title}");
            }
        }

        if (Game.CurrentDLSS_FG is not null)
        {
            IsDLSSGAvailable = true;
            CurrentDLSSG = DLSSGRecords.FirstOrDefault(v => v.MD5Hash == Game.CurrentDLSS_FG.Hash);
            if (CurrentDLSSG is null)
            {
                Debug.WriteLine($"DLSS G not found in manifest: {Game.CurrentDLSS_FG.Version}, {Game.CurrentDLSS_FG.Hash}, from {Game.Title}");
            }
        }

        if (Game.CurrentDLSS_RR is not null)
        {
            IsDLSSDAvailable = true;
            CurrentDLSSD = DLSSDRecords.FirstOrDefault(v => v.MD5Hash == Game.CurrentDLSS_RR.Hash);
            if (CurrentDLSSD is null)
            {
                Debug.WriteLine($"DLSS D not found in manifest: {Game.CurrentDLSS_RR.Version}, {Game.CurrentDLSS_RR.Hash}, from {Game.Title}");
            }
        }

        if (Game.CurrentFSR_31_DX12 is not null)
        {
            IsFSR31DX12Available = true;
            CurrentFSR31DX12 = FSR31DX12Records.FirstOrDefault(v => v.MD5Hash == Game.CurrentFSR_31_DX12.Hash);
            if (CurrentFSR31DX12 is null)
            {
                Debug.WriteLine($"FSR_31_DX12 not found in manifest: {Game.CurrentFSR_31_DX12.Version}, {Game.CurrentFSR_31_DX12.Hash}, from {Game.Title}");
            }
        }

        if (Game.CurrentFSR_31_VK is not null)
        {
            IsFSR31VKAvailable = true;
            CurrentFSR31VK = FSR31VKRecords.FirstOrDefault(v => v.MD5Hash == Game.CurrentFSR_31_VK.Hash);
            if (CurrentFSR31VK is null)
            {
                Debug.WriteLine($"FSR_31_VK not found in manifest: {Game.CurrentFSR_31_VK.Version}, {Game.CurrentFSR_31_VK.Hash}, from {Game.Title}");
            }
        }

        if (Game.CurrentXeSS is not null)
        {
            IsXeSSAvailable = true;
            CurrentXeSS = XeSSRecords.FirstOrDefault(v => v.MD5Hash == Game.CurrentXeSS.Hash);
            if (CurrentXeSS is null)
            {
                Debug.WriteLine($"XeSS not found in manifest: {Game.CurrentXeSS.Version}, {Game.CurrentXeSS.Hash}, from {Game.Title}");
            }
        }

        if (Game.CurrentXeLL is not null)
        {
            IsXeLLAvailable = true;
            CurrentXeLL = XeLLRecords.FirstOrDefault(v => v.MD5Hash == Game.CurrentXeLL.Hash);
            if (CurrentXeLL is null)
            {
                Debug.WriteLine($"XeLL not found in manifest: {Game.CurrentXeLL.Version}, {Game.CurrentXeLL.Hash}, from {Game.Title}");
            }
        }

        if (Game.CurrentXeSS_FG is not null)
        {
            IsXeSSFGAvailable = true;
            CurrentXeSSFG = XeSSFGRecords.FirstOrDefault(v => v.MD5Hash == Game.CurrentXeSS_FG.Hash);
            if (CurrentXeSSFG is null)
            {
                Debug.WriteLine($"XeSS FG not found in manifest: {Game.CurrentXeSS_FG.Version}, {Game.CurrentXeSS_FG.Hash}, from {Game.Title}");
            }
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(CurrentDLSS))
        {
            if (CurrentDLSS is not null)
            {
                if (CurrentDLSS.MD5Hash != Game.CurrentDLSSHash)
                {
                    // TODO: Change DLSS
                }
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

    [RelayCommand]
    async Task FavouriteAsync()
    {
        Game.IsFavourite = !Game.IsFavourite;
        await Game.SaveToDatabaseAsync();
    }

    [RelayCommand]
    async Task DLLChangedAsync(DLLRecord dllRecord)
    {
        await Task.Delay(2500);

    }

}
