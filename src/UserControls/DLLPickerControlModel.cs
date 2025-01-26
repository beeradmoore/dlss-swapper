using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLSS_Swapper.Data;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation.Metadata;

namespace DLSS_Swapper.UserControls;

public partial class DLLPickerControlModel : ObservableObject
{
    WeakReference<DLLPickerControl> dllPickerControlWeakReference;
    WeakReference<EasyContentDialog> parentDialogWeakReference;

    public Game Game { get; private set; }
    public GameAssetType GameAssetType { get; private set; }

    public List<DLLRecord> DLLRecords { get; private set;  }

    [ObservableProperty]
    public partial DLLRecord? SelectedDLLRecord { get; set; } = null;

    [ObservableProperty]
    public partial bool CanReset { get; set; } = false;

    [ObservableProperty]
    public partial bool CanSwap { get; set; } = false;

    public bool CanCloseParentDialog { get; set; } = false;

    public DLLPickerControlModel(EasyContentDialog parentDialog, DLLPickerControl dllPickerControl, Game game, GameAssetType gameAssetType)
    {
        parentDialogWeakReference = new WeakReference<EasyContentDialog>(parentDialog);
        parentDialog.Closing += (ContentDialog sender, ContentDialogClosingEventArgs args) =>
        {
            if (args.Result == ContentDialogResult.Primary)
            {
                if (CanCloseParentDialog == false)
                {
                    args.Cancel = true;
                }
            }
        };


        dllPickerControlWeakReference = new WeakReference<DLLPickerControl>(dllPickerControl);
        Game = game;
        GameAssetType = gameAssetType;
        parentDialog.PrimaryButtonCommand = SwapDllCommand;
        parentDialog.SecondaryButtonCommand = ResetDllCommand;

       
        DLLRecords = GameAssetType switch
        {
            GameAssetType.DLSS => new List<DLLRecord>(DLLManager.Instance.DLSSRecords),
            GameAssetType.DLSS_G => new List<DLLRecord>(DLLManager.Instance.DLSSGRecords),
            GameAssetType.DLSS_D => new List<DLLRecord>(DLLManager.Instance.DLSSDRecords),
            GameAssetType.FSR_31_DX12 => new List<DLLRecord>(DLLManager.Instance.FSR31DX12Records),
            GameAssetType.FSR_31_VK => new List<DLLRecord>(DLLManager.Instance.FSR31VKRecords),
            GameAssetType.XeSS => new List<DLLRecord>(DLLManager.Instance.XeSSRecords),
            GameAssetType.XeLL => new List<DLLRecord>(DLLManager.Instance.XeLLRecords),
            GameAssetType.XeSS_FG => new List<DLLRecord>(DLLManager.Instance.XeSSFGRecords),
            _ => new List<DLLRecord>(),
        };

        if (Settings.Instance.AllowDebugDlls == false)
        {
            DLLRecords.RemoveAll(x => x.IsDevFile == true);
        }

        // Prevent DLSS 1.0 showing up with DLSS 2/3 and vice versa
        if (GameAssetType == GameAssetType.DLSS)
        {
            var dlssRecords = Game.GameAssets.Where(x => x.AssetType == GameAssetType.DLSS).ToList();
            if (dlssRecords.Count > 0)
            {
                if (dlssRecords[0].Version.StartsWith("1."))
                {
                    DLLRecords.RemoveAll(x => x.Version.StartsWith("1.") == false);
                }
                else
                {
                    DLLRecords.RemoveAll(x => x.Version.StartsWith("1.") == true);
                }
            }
        }

        ResetSelection();
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(SelectedDLLRecord))
        {
            if (SelectedDLLRecord is null)
            {
                CanSwap = false;
            }
            else if (SelectedDLLRecord.LocalRecord is null)
            {
                // This should never happen
                CanSwap = false;
            }
            else
            {
                CanSwap = true;
            }
        }
        else if (e.PropertyName == nameof(CanSwap))
        {
            if (parentDialogWeakReference.TryGetTarget(out EasyContentDialog? dialog))
            {
                dialog.IsPrimaryButtonEnabled = CanSwap;
            }                
        }
    }

    [RelayCommand]
    async Task SwapDllAsync()
    {
        if (SelectedDLLRecord?.LocalRecord is null)
        {
            return;
        }

        if (SelectedDLLRecord.LocalRecord.IsDownloaded == false)
        {
            ShowTempInfoBar(string.Empty, "Starting download");
            SelectedDLLRecord.DownloadAsync().SafeFireAndForget();
            return;
        }
        else if (SelectedDLLRecord.LocalRecord.IsDownloading)
        {
            ShowTempInfoBar(string.Empty, "Please wait for download to complete");
            return;
        }

        var didUpdate = await Game.UpdateDllAsync(SelectedDLLRecord);

        if (didUpdate.Success == false)
        {
            ShowTempInfoBar("Error", didUpdate.Message, severity: InfoBarSeverity.Error);
            return;
        }

        // Allow the dialog to close
        CanCloseParentDialog = true;

        if (this.parentDialogWeakReference.TryGetTarget(out EasyContentDialog? dialog) == true)
        {
            // Is the dialog already closing when we call this?
            dialog.Hide();
        }
    }

    void ShowTempInfoBar(string title, string message, double duration = 3.0, InfoBarSeverity severity = InfoBarSeverity.Informational)
    {
        if (dllPickerControlWeakReference.TryGetTarget(out DLLPickerControl? dllPickerControl) == true)
        {
            if (dllPickerControl.Content is Grid grid)
            {
                var infoBar = new InfoBar();
                infoBar.Message = message;
                infoBar.Severity = severity;
                infoBar.IsOpen = true;
                infoBar.IsClosable = false;
                Grid.SetRow(infoBar, 2);
                grid.Children.Add(infoBar);

                var dispatcherTimer = new DispatcherTimer();
                dispatcherTimer.Tick += (object? sender, object e) =>
                {
                    // If the page has gone away, parent should be null and this should not cause problems
                    if (infoBar?.Parent is Grid parentGrid)
                    {
                        infoBar.IsOpen = false;
                        parentGrid.Children.Remove(infoBar);
                    }

                    if (sender is DispatcherTimer timer)
                    {
                        timer.Stop();
                    }
                };
                dispatcherTimer.Interval = TimeSpan.FromSeconds(duration);
                dispatcherTimer.Start();
            }
        }

    }


    [RelayCommand]
    async Task ResetDllAsync()
    {
        var didReset = await Game.ResetDllAsync(GameAssetType);

        if (didReset.Success == true)
        {
            ResetSelection();
        }
        else
        { 
            ShowTempInfoBar("Error", didReset.Message, severity: InfoBarSeverity.Error);
        }
    }

    void ResetSelection()
    {
        // If there are backup records it means we can reset.
        var backupRecordType = DLLManager.Instance.GetAssetBackupType(GameAssetType);
        var existingBackupRecords = Game.GameAssets.Where(x => x.AssetType == backupRecordType).ToList();
        CanReset = existingBackupRecords.Count > 0;

        // Select the default record
        var existingRecords = Game.GameAssets.Where(x => x.AssetType == GameAssetType).ToList();
        if (existingRecords.Count == 1)
        {
            var existingRecord = existingRecords[0];
            SelectedDLLRecord = DLLRecords.FirstOrDefault(x => x.MD5Hash == existingRecord.Hash);
        }
    }
}
