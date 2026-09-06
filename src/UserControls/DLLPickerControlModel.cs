using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLSS_Swapper.Data;
using DLSS_Swapper.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace DLSS_Swapper.UserControls;

public partial class DLLPickerControlModel : ObservableObject
{
    WeakReference<GameControl> _gameControlWeakReference;
    WeakReference<EasyContentDialog> _parentDialogWeakReference;
    WeakReference<DLLPickerControl> _dllPickerControlWeakReference;

    public Game Game { get; private set; }
    public GameAssetType GameAssetType { get; private set; }

    public List<DLLRecord> DLLRecords { get; private set; }

    [ObservableProperty]
    public partial DLLRecord? SelectedDLLRecord { get; set; } = null;

    [ObservableProperty]
    public partial bool CanSwap { get; set; } = false;

    [ObservableProperty]
    public partial bool AnyDLLsVisible { get; set; } = false;

    [ObservableProperty]
    public partial GameAsset? CurrentGameAsset { get; set; } = null;

    [ObservableProperty]
    public partial GameAsset? BackupGameAsset { get; set; } = null;

    public bool CanCloseParentDialog { get; set; }

    public DLLPickerControlModelTranslationProperties TranslationProperties { get; } = new DLLPickerControlModelTranslationProperties();

    public DLLPickerControlModel(GameControl gameControl, EasyContentDialog parentDialog, DLLPickerControl dllPickerControl, Game game, GameAssetType gameAssetType) : base()
    {
        _gameControlWeakReference = new WeakReference<GameControl>(gameControl);
        _parentDialogWeakReference = new WeakReference<EasyContentDialog>(parentDialog);
        _dllPickerControlWeakReference = new WeakReference<DLLPickerControl>(dllPickerControl);

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
        Game = game;
        GameAssetType = gameAssetType;
        parentDialog.PrimaryButtonCommand = SwapDllCommand;
        parentDialog.SecondaryButtonCommand = ResetDllCommand;

        // NOTE: DLL type
        switch (GameAssetType)
        {
            case GameAssetType.DLSS:
                DLLRecords = [.. DLLManager.Instance.DLSSRecords];
                if (Settings.Instance.OnlyShowDownloadedDlls == true)
                {
                    _ = DLLRecords.RemoveAll(x => x.MD5Hash != Game.CurrentDLSS?.Hash && x.LocalRecord?.IsDownloaded is false);
                }
                break;

            case GameAssetType.DLSS_G:
                DLLRecords = [.. DLLManager.Instance.DLSSGRecords];
                if (Settings.Instance.OnlyShowDownloadedDlls == true)
                {
                    _ = DLLRecords.RemoveAll(x => x.MD5Hash != Game.CurrentDLSS_G?.Hash && x.LocalRecord?.IsDownloaded is false);
                }
                break;

            case GameAssetType.DLSS_D:
                DLLRecords = [.. DLLManager.Instance.DLSSDRecords];
                if (Settings.Instance.OnlyShowDownloadedDlls == true)
                {
                    _ = DLLRecords.RemoveAll(x => x.MD5Hash != Game.CurrentDLSS_D?.Hash && x.LocalRecord?.IsDownloaded is false);
                }
                break;

            case GameAssetType.DLSS_NR:
                DLLRecords = [.. DLLManager.Instance.DLSSNRRecords];
                if (Settings.Instance.OnlyShowDownloadedDlls == true)
                {
                    _ = DLLRecords.RemoveAll(x => x.MD5Hash != Game.CurrentDLSS_NR?.Hash && x.LocalRecord?.IsDownloaded is false);
                }
                break;

            case GameAssetType.FSR_31_DX12:
                DLLRecords = [.. DLLManager.Instance.FSR31DX12Records];
                if (Settings.Instance.OnlyShowDownloadedDlls == true)
                {
                    _ = DLLRecords.RemoveAll(x => x.MD5Hash != Game.CurrentFSR_31_DX12?.Hash && x.LocalRecord?.IsDownloaded is false);
                }
                break;

            case GameAssetType.FSR_31_VK:
                DLLRecords = [.. DLLManager.Instance.FSR31VKRecords];
                if (Settings.Instance.OnlyShowDownloadedDlls == true)
                {
                    _ = DLLRecords.RemoveAll(x => x.MD5Hash != Game.CurrentFSR_31_VK?.Hash && x.LocalRecord?.IsDownloaded is false);
                }
                break;

            case GameAssetType.XeSS:
                DLLRecords = [.. DLLManager.Instance.XeSSRecords];
                if (Settings.Instance.OnlyShowDownloadedDlls == true)
                {
                    _ = DLLRecords.RemoveAll(x => x.MD5Hash != Game.CurrentXeSS?.Hash && x.LocalRecord?.IsDownloaded is false);
                }
                break;

            case GameAssetType.XeSS_FG:
                DLLRecords = [.. DLLManager.Instance.XeSSFGRecords];
                if (Settings.Instance.OnlyShowDownloadedDlls == true)
                {
                    _ = DLLRecords.RemoveAll(x => x.MD5Hash != Game.CurrentXeSS_FG?.Hash && x.LocalRecord?.IsDownloaded is false);
                }
                break;

            case GameAssetType.XeSS_DX11:
                DLLRecords = [.. DLLManager.Instance.XeSSDX11Records];
                if (Settings.Instance.OnlyShowDownloadedDlls == true)
                {
                    _ = DLLRecords.RemoveAll(x => x.MD5Hash != Game.CurrentXeSS_DX11?.Hash && x.LocalRecord?.IsDownloaded is false);
                }
                break;

            case GameAssetType.XeLL:
                DLLRecords = [.. DLLManager.Instance.XeLLRecords];
                if (Settings.Instance.OnlyShowDownloadedDlls == true)
                {
                    _ = DLLRecords.RemoveAll(x => x.MD5Hash != Game.CurrentXeLL?.Hash && x.LocalRecord?.IsDownloaded is false);
                }
                break;

            case GameAssetType.DirectStorage:
                DLLRecords = [.. DLLManager.Instance.DirectStorageRecords];
                if (Settings.Instance.OnlyShowDownloadedDlls == true)
                {
                    _ = DLLRecords.RemoveAll(x => x.MD5Hash != Game.CurrentDirectStorage?.Hash && x.LocalRecord?.IsDownloaded is false);
                }
                break;

            case GameAssetType.DirectStorageCore:
                DLLRecords = [.. DLLManager.Instance.DirectStorageCoreRecords];
                if (Settings.Instance.OnlyShowDownloadedDlls == true)
                {
                    _ = DLLRecords.RemoveAll(x => x.MD5Hash != Game.CurrentDirectStorageCore?.Hash && x.LocalRecord?.IsDownloaded is false);
                }
                break;

            case GameAssetType.FidelityFX_SDK2_Denoiser_DX12:
                DLLRecords = [.. DLLManager.Instance.FidelityFXSDK2DenoiserDX12Records];
                if (Settings.Instance.OnlyShowDownloadedDlls == true)
                {
                    _ = DLLRecords.RemoveAll(x => x.MD5Hash != Game.CurrentFidelityFX_SDK2_Denoiser_DX12?.Hash && x.LocalRecord?.IsDownloaded is false);
                }
                break;

            case GameAssetType.FidelityFX_SDK2_FrameGeneration_DX12:
                DLLRecords = [.. DLLManager.Instance.FidelityFXSDK2FrameGenerationDX12Records];
                if (Settings.Instance.OnlyShowDownloadedDlls == true)
                {
                    _ = DLLRecords.RemoveAll(x => x.MD5Hash != Game.CurrentFidelityFX_SDK2_FrameGeneration_DX12?.Hash && x.LocalRecord?.IsDownloaded is false);
                }
                break;

            case GameAssetType.FidelityFX_SDK2_Loader_DX12:
                DLLRecords = [.. DLLManager.Instance.FidelityFXSDK2LoaderDX12Records];
                if (Settings.Instance.OnlyShowDownloadedDlls == true)
                {
                    _ = DLLRecords.RemoveAll(x => x.MD5Hash != Game.CurrentFidelityFX_SDK2_Loader_DX12?.Hash && x.LocalRecord?.IsDownloaded is false);
                }
                break;

            case GameAssetType.FidelityFX_SDK2_RadianceCache_DX12:
                DLLRecords = [.. DLLManager.Instance.FidelityFXSDK2RadianceCacheDX12Records];
                if (Settings.Instance.OnlyShowDownloadedDlls == true)
                {
                    _ = DLLRecords.RemoveAll(x => x.MD5Hash != Game.CurrentFidelityFX_SDK2_RadianceCache_DX12?.Hash && x.LocalRecord?.IsDownloaded is false);
                }
                break;

            case GameAssetType.FidelityFX_SDK2_Upscaler_DX12:
                DLLRecords = [.. DLLManager.Instance.FidelityFXSDK2UpscalerDX12Records];
                if (Settings.Instance.OnlyShowDownloadedDlls == true)
                {
                    _ = DLLRecords.RemoveAll(x => x.MD5Hash != Game.CurrentFidelityFX_SDK2_Upscaler_DX12?.Hash && x.LocalRecord?.IsDownloaded is false);
                }
                break;

            case GameAssetType.Streamline_Reflex:
                DLLRecords = [.. DLLManager.Instance.StreamlineReflexRecords];
                if (Settings.Instance.OnlyShowDownloadedDlls == true)
                {
                    _ = DLLRecords.RemoveAll(x => x.MD5Hash != Game.CurrentStreamline_Reflex?.Hash && x.LocalRecord?.IsDownloaded is false);
                }
                break;

            case GameAssetType.Streamline_PCL:
                DLLRecords = [.. DLLManager.Instance.StreamlinePCLRecords];
                if (Settings.Instance.OnlyShowDownloadedDlls == true)
                {
                    _ = DLLRecords.RemoveAll(x => x.MD5Hash != Game.CurrentStreamline_PCL?.Hash && x.LocalRecord?.IsDownloaded is false);
                }
                break;

            case GameAssetType.Streamline_NvPerf:
                DLLRecords = [.. DLLManager.Instance.StreamlineNvPerfRecords];
                if (Settings.Instance.OnlyShowDownloadedDlls == true)
                {
                    _ = DLLRecords.RemoveAll(x => x.MD5Hash != Game.CurrentStreamline_NvPerf?.Hash && x.LocalRecord?.IsDownloaded is false);
                }
                break;

            case GameAssetType.Streamline_NIS:
                DLLRecords = [.. DLLManager.Instance.StreamlineNISRecords];
                if (Settings.Instance.OnlyShowDownloadedDlls == true)
                {
                    _ = DLLRecords.RemoveAll(x => x.MD5Hash != Game.CurrentStreamline_NIS?.Hash && x.LocalRecord?.IsDownloaded is false);
                }
                break;

            case GameAssetType.Streamline_Interposer:
                DLLRecords = [.. DLLManager.Instance.StreamlineInterposerRecords];
                if (Settings.Instance.OnlyShowDownloadedDlls == true)
                {
                    _ = DLLRecords.RemoveAll(x => x.MD5Hash != Game.CurrentStreamline_Interposer?.Hash && x.LocalRecord?.IsDownloaded is false);
                }
                break;

            case GameAssetType.Streamline_DLSS_G:
                DLLRecords = [.. DLLManager.Instance.StreamlineDLSSGRecords];
                if (Settings.Instance.OnlyShowDownloadedDlls == true)
                {
                    _ = DLLRecords.RemoveAll(x => x.MD5Hash != Game.CurrentStreamline_DLSS_G?.Hash && x.LocalRecord?.IsDownloaded is false);
                }
                break;

            case GameAssetType.Streamline_DLSS_D:
                DLLRecords = [.. DLLManager.Instance.StreamlineDLSSDRecords];
                if (Settings.Instance.OnlyShowDownloadedDlls == true)
                {
                    _ = DLLRecords.RemoveAll(x => x.MD5Hash != Game.CurrentStreamline_DLSS_D?.Hash && x.LocalRecord?.IsDownloaded is false);
                }
                break;

            case GameAssetType.Streamline_DLSS_NR:
                DLLRecords = [.. DLLManager.Instance.StreamlineDLSSNRRecords];
                if (Settings.Instance.OnlyShowDownloadedDlls == true)
                {
                    _ = DLLRecords.RemoveAll(x => x.MD5Hash != Game.CurrentStreamline_DLSS_NR?.Hash && x.LocalRecord?.IsDownloaded is false);
                }
                break;

            case GameAssetType.Streamline_DLSS:
                DLLRecords = [.. DLLManager.Instance.StreamlineDLSSRecords];
                if (Settings.Instance.OnlyShowDownloadedDlls == true)
                {
                    _ = DLLRecords.RemoveAll(x => x.MD5Hash != Game.CurrentStreamline_DLSS?.Hash && x.LocalRecord?.IsDownloaded is false);
                }
                break;

            case GameAssetType.Streamline_DirectSR:
                DLLRecords = [.. DLLManager.Instance.StreamlineDirectSRRecords];
                if (Settings.Instance.OnlyShowDownloadedDlls == true)
                {
                    _ = DLLRecords.RemoveAll(x => x.MD5Hash != Game.CurrentStreamline_DirectSR?.Hash && x.LocalRecord?.IsDownloaded is false);
                }
                break;

            case GameAssetType.Streamline_DeepDVC:
                DLLRecords = [.. DLLManager.Instance.StreamlineDeepDVCRecords];
                if (Settings.Instance.OnlyShowDownloadedDlls == true)
                {
                    _ = DLLRecords.RemoveAll(x => x.MD5Hash != Game.CurrentStreamline_DeepDVC?.Hash && x.LocalRecord?.IsDownloaded is false);
                }
                break;

            case GameAssetType.Streamline_Common:
                DLLRecords = [.. DLLManager.Instance.StreamlineCommonRecords];
                if (Settings.Instance.OnlyShowDownloadedDlls == true)
                {
                    _ = DLLRecords.RemoveAll(x => x.MD5Hash != Game.CurrentStreamline_Common?.Hash && x.LocalRecord?.IsDownloaded is false);
                }
                break;

            case GameAssetType.DeepDVC:
                DLLRecords = [.. DLLManager.Instance.DeepDVCRecords];
                if (Settings.Instance.OnlyShowDownloadedDlls == true)
                {
                    _ = DLLRecords.RemoveAll(x => x.MD5Hash != Game.CurrentDeepDVC?.Hash && x.LocalRecord?.IsDownloaded is false);
                }
                break;

            case GameAssetType.NvLowLatencyVK:
                DLLRecords = [.. DLLManager.Instance.NvLowLatencyVKRecords];
                if (Settings.Instance.OnlyShowDownloadedDlls == true)
                {
                    _ = DLLRecords.RemoveAll(x => x.MD5Hash != Game.CurrentNvLowLatencyVK?.Hash && x.LocalRecord?.IsDownloaded is false);
                }
                break;

            default:
                DLLRecords = [];
                break;
        }

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

        AnyDLLsVisible = DLLRecords.Count > 0;

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
            if (_parentDialogWeakReference.TryGetTarget(out var dialog))
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

        if (SelectedDLLRecord.LocalRecord.FileDownloader is not null)
        {
            ShowTempInfoBar(string.Empty, ResourceHelper.GetString("GamePage_DllPicker_WaitToDownloadBeforeSwapping"));
            return;
        }
        else if (SelectedDLLRecord.LocalRecord.IsDownloaded == false)
        {
            ShowTempInfoBar(string.Empty, ResourceHelper.GetString("GamePage_DllPicker_StartingDownload"));
            SelectedDLLRecord.DownloadAsync().SafeFireAndForget();
            return;
        }

        var didUpdate = await Game.UpdateDllAsync(SelectedDLLRecord);

        if (didUpdate.Success == false)
        {
            ShowTempInfoBar(ResourceHelper.GetString("General_Error"), didUpdate.Message, severity: InfoBarSeverity.Error);
            return;
        }

        // Allow the dialog to close
        CanCloseParentDialog = true;

        if (_parentDialogWeakReference.TryGetTarget(out var dialog) == true)
        {
            // Is the dialog already closing when we call this?
            dialog.Hide();
        }
    }

    void ShowTempInfoBar(string title, string message, double duration = 5.0, InfoBarSeverity severity = InfoBarSeverity.Informational, int gridIndex = 2)
    {
        if (_dllPickerControlWeakReference.TryGetTarget(out var dllPickerControl) == true)
        {
            if (dllPickerControl.Content is Grid grid)
            {
                var infoBar = new InfoBar();
                infoBar.Message = message;
                infoBar.Severity = severity;
                infoBar.IsOpen = true;
                infoBar.IsClosable = true;

                // Temp workaround until InfoBar has a solid color by default.
                // https://github.com/microsoft/microsoft-ui-xaml/issues/5741
                if (App.Current.Resources.TryGetValue("InfoBarInformationalSeverityBackgroundBrush", out var infoBarInformationalSeverityBackground) && infoBarInformationalSeverityBackground is SolidColorBrush infoBarInformationalSeverityBackgroundBrush)
                {
                    // Temp fix to make download indicator visibile in dark mode.
                    if (WindowManager.CurrentTheme == ElementTheme.Dark)
                    {
                        infoBar.Background = new SolidColorBrush(Color.FromArgb(255, 23, 23, 23));
                    }
                    else
                    {
                        infoBarInformationalSeverityBackgroundBrush.Color = Color.FromArgb(255, infoBarInformationalSeverityBackgroundBrush.Color.R, infoBarInformationalSeverityBackgroundBrush.Color.G, infoBarInformationalSeverityBackgroundBrush.Color.B);
                        infoBar.Background = infoBarInformationalSeverityBackgroundBrush;
                    }
                }

                Grid.SetRow(infoBar, gridIndex);
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
    void OpenDllPath()
    {
        if (CurrentGameAsset is null)
        {
            return;
        }

        try
        {
            if (File.Exists(CurrentGameAsset.Path))
            {
                Process.Start("explorer.exe", $"/select,{CurrentGameAsset.Path}");
            }
            else
            {
                var dllPath = Path.GetDirectoryName(CurrentGameAsset.Path) ?? string.Empty;
                if (Directory.Exists(dllPath))
                {
                    Process.Start("explorer.exe", dllPath);
                }
                else
                {
                    throw new Exception(ResourceHelper.GetFormattedResourceTemplate("GamePage_DllPicker_CouldNotFindFileTemplate", CurrentGameAsset.Path));
                }
            }
        }
        catch (Exception err)
        {
            Logger.Error(err);
            ShowTempInfoBar(ResourceHelper.GetString("General_Error"), err.Message, severity: InfoBarSeverity.Error);
        }
    }

    [RelayCommand]
    async Task ResetDllAsync()
    {
        var didReset = await Game.ResetDllAsync(GameAssetType);

        if (didReset.Success == true)
        {
            if (_parentDialogWeakReference.TryGetTarget(out var parentDialog) == true)
            {
                parentDialog.Hide();
            }
        }
        else
        {
            ShowTempInfoBar(ResourceHelper.GetString("General_Error"), didReset.Message, severity: InfoBarSeverity.Error, gridIndex: 0);
        }
    }

    void ResetSelection()
    {
        // If there are backup records it means we can reset.
        var backupRecordType = DLLManager.Instance.GetAssetBackupType(GameAssetType);
        var existingBackupRecords = Game.GameAssets.Where(x => x.AssetType == backupRecordType).ToList();
        BackupGameAsset = existingBackupRecords.FirstOrDefault();

        // Select the default record
        var existingRecords = Game.GameAssets.Where(x => x.AssetType == GameAssetType).ToList();
        CurrentGameAsset = existingRecords.FirstOrDefault();

        if (CurrentGameAsset is not null)
        {
            SelectedDLLRecord = DLLRecords.FirstOrDefault(x => x.MD5Hash == CurrentGameAsset.Hash);
        }
    }
}
