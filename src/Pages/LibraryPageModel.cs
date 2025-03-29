using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncAwaitBestPractices.MVVM;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Collections;
using DLSS_Swapper.Data;
using DLSS_Swapper.Extensions;
using DLSS_Swapper.UserControls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;

namespace DLSS_Swapper.Pages;

public partial class LibraryPageModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    LibraryPage libraryPage;

    internal ObservableCollection<DLLRecord>? SelectedLibraryList { get; private set; } = null;

    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

    [ObservableProperty]
    public partial SelectorBarItem? SelectedSelectorBarItem { get; set; } = null;

    public LibraryPageModel(LibraryPage libraryPage)
    {
        this.libraryPage = libraryPage;

        var upscalerSelectorBar = libraryPage.FindChild("UpscalerSelectorBar") as SelectorBar;
        if (upscalerSelectorBar is not null)
        {
            // TODO: Change order based on prefered upscaler.
            upscalerSelectorBar.Items.Add(new SelectorBarItem() { Text = DLLManager.Instance.GetAssetTypeName(GameAssetType.DLSS), Tag = GameAssetType.DLSS });
            upscalerSelectorBar.Items.Add(new SelectorBarItem() { Text = DLLManager.Instance.GetAssetTypeName(GameAssetType.DLSS_G), Tag = GameAssetType.DLSS_G });
            upscalerSelectorBar.Items.Add(new SelectorBarItem() { Text = DLLManager.Instance.GetAssetTypeName(GameAssetType.DLSS_D), Tag = GameAssetType.DLSS_D });
            upscalerSelectorBar.Items.Add(new SelectorBarItem() { Text = DLLManager.Instance.GetAssetTypeName(GameAssetType.FSR_31_DX12), Tag = GameAssetType.FSR_31_DX12 });
            upscalerSelectorBar.Items.Add(new SelectorBarItem() { Text = DLLManager.Instance.GetAssetTypeName(GameAssetType.FSR_31_VK), Tag = GameAssetType.FSR_31_VK });
            upscalerSelectorBar.Items.Add(new SelectorBarItem() { Text = DLLManager.Instance.GetAssetTypeName(GameAssetType.XeSS), Tag = GameAssetType.XeSS });
            upscalerSelectorBar.Items.Add(new SelectorBarItem() { Text = DLLManager.Instance.GetAssetTypeName(GameAssetType.XeSS_FG), Tag = GameAssetType.XeSS_FG });
            upscalerSelectorBar.Items.Add(new SelectorBarItem() { Text = DLLManager.Instance.GetAssetTypeName(GameAssetType.XeLL), Tag = GameAssetType.XeLL });

            SelectedSelectorBarItem = upscalerSelectorBar.Items[0];
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(SelectedSelectorBarItem))
        {
            if (SelectedSelectorBarItem?.Tag is GameAssetType gameAssetType)
            {
                SelectLibrary(gameAssetType);
            }
        }
    } 

    [RelayCommand()]
    async Task RefreshAsync()
    {
        IsRefreshing = true;

        var didUpdate = await DLLManager.Instance.UpdateManifestAsync();

        if (didUpdate)
        {
            // Reload selected library.
            if (SelectedSelectorBarItem?.Tag is GameAssetType gameAssetType)
            {
                SelectLibrary(gameAssetType);
            }
        }
        else
        {
            var errorDialog = new EasyContentDialog(libraryPage.XamlRoot)
            {
                Title = "Error",
                CloseButtonText = "Okay",
                DefaultButton = ContentDialogButton.Close,
                Content = "Unable to update manifest of DLL records.",
            };
            await errorDialog.ShowAsync();
        }

        IsRefreshing = false;
    }

    [RelayCommand]
    async Task ExportAllAsync()
    {
        // Check that there are records to export first.
        var allDllRecords = new List<DLLRecord>();
        allDllRecords.AddRange(DLLManager.Instance.DLSSRecords.Where(x => x.LocalRecord?.IsDownloaded == true));
        allDllRecords.AddRange(DLLManager.Instance.DLSSGRecords.Where(x => x.LocalRecord?.IsDownloaded == true));
        allDllRecords.AddRange(DLLManager.Instance.DLSSDRecords.Where(x => x.LocalRecord?.IsDownloaded == true));
        allDllRecords.AddRange(DLLManager.Instance.FSR31DX12Records.Where(x => x.LocalRecord?.IsDownloaded == true));
        allDllRecords.AddRange(DLLManager.Instance.FSR31VKRecords.Where(x => x.LocalRecord?.IsDownloaded == true));
        allDllRecords.AddRange(DLLManager.Instance.XeSSRecords.Where(x => x.LocalRecord?.IsDownloaded == true));
        allDllRecords.AddRange(DLLManager.Instance.XeSSFGRecords.Where(x => x.LocalRecord?.IsDownloaded == true));
        allDllRecords.AddRange(DLLManager.Instance.XeLLRecords.Where(x => x.LocalRecord?.IsDownloaded == true));

        if (allDllRecords.Count == 0)
        {
            var dialog = new EasyContentDialog(libraryPage.XamlRoot)
            {
                CloseButtonText = "Okay",
                DefaultButton = ContentDialogButton.Close,
                Title = "Error",
                Content = $"You have no DLL records to export.",
            };
            await dialog.ShowAsync();
            return;
        }


        var exportingDialog = new EasyContentDialog(libraryPage.XamlRoot)
        {
            Title = "Exporting",
            Content = new ProgressRing()
            {
                IsIndeterminate = true,
            },
        };

        var tempExportPath = Path.Combine(Storage.GetTemp(), "export");
        var finalExportZip = string.Empty;
        try
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.CurrentApp.MainWindow);
            var savePicker = new Windows.Storage.Pickers.FileSavePicker();
            savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("Zip archive", new List<string>() { ".zip" });
            savePicker.SuggestedFileName = $"dlss_swapper_export.zip";
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);
            var saveFile = await savePicker.PickSaveFileAsync();

            // User cancelled.
            if (saveFile is null)
            {
                return;
            }

            finalExportZip = saveFile.Path;

            Storage.CreateDirectoryIfNotExists(tempExportPath);

            _ = exportingDialog.ShowAsync();

            // Give UI time to update and show import screen.
            await Task.Delay(50);

            var exportCount = 0;

            using (var fileStream = File.Create(finalExportZip))
            {
                using (var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Create))
                {
                    foreach (var dllRecord in allDllRecords)
                    {
                        if (dllRecord.LocalRecord is null)
                        {
                            continue;
                        }

                        // TODO: When fixing imported system, make sure to update this to use full path
                        var internalZipDir = DLLManager.Instance.GetAssetTypeName(dllRecord.AssetType);
                        if (dllRecord.LocalRecord.IsImported == true)
                        {
                            internalZipDir = Path.Combine("Imported", internalZipDir);
                        }

                        internalZipDir = Path.Combine(internalZipDir, dllRecord.DisplayName);

                        using (var dlssFileStream = File.OpenRead(dllRecord.LocalRecord.ExpectedPath))
                        {
                            using (var dlssZip = new ZipArchive(dlssFileStream, ZipArchiveMode.Read))
                            {
                                var zippedDlls = dlssZip.Entries.Where(x => x.Name.EndsWith(".dll")).ToArray();

                                // If there is more than one dll something has gone wrong.
                                if (zippedDlls.Length != 1)
                                {
                                    throw new Exception($"Could not export due to \"{dllRecord.LocalRecord.ExpectedPath}\" having {zippedDlls.Length} dlls instead of 1.");
                                }

                                var tempFileExportPath = Path.Combine(tempExportPath, Guid.NewGuid().ToString("D"));
                                Storage.CreateDirectoryIfNotExists(tempFileExportPath);

                                var tempFile = Path.Combine(tempFileExportPath, Path.GetFileName(zippedDlls[0].Name));
                                zippedDlls[0].ExtractToFile(tempFile);
                                zipArchive.CreateEntryFromFile(tempFile, Path.Combine(internalZipDir, Path.GetFileName(tempFile)));

                                // Try clean up as we go.
                                try
                                {
                                    Directory.Delete(tempFileExportPath, true);
                                }
                                catch
                                {
                                    // NOOP
                                }
                            }
                        }

                        ++exportCount;
                    }
                }
            }

            exportingDialog.Hide();

            var dialog = new EasyContentDialog(libraryPage.XamlRoot)
            {
                CloseButtonText = "Okay",
                DefaultButton = ContentDialogButton.Close,
                Title = "Success",
                Content = $"Exported {exportCount} DLSS dll{(exportCount == 1 ? string.Empty : "s")}.",
            };
            await dialog.ShowAsync();
        }
        catch (Exception err)
        {
            // If we failed to export lets delete teh temp zip file that was create.
            if (string.IsNullOrEmpty(finalExportZip) == false && File.Exists(finalExportZip))
            {
                try
                {
                    if (File.Exists(finalExportZip))
                    {
                        File.Delete(finalExportZip);
                    }
                }
                catch (Exception err2)
                {
                    Logger.Error(err2);
                }
            }

            exportingDialog.Hide();

            Logger.Error(err);

            // If the fullExpectedPath does not exist, or there was an error writing it.
            var dialog = new EasyContentDialog(libraryPage.XamlRoot)
            {
                Title = "Error",
                CloseButtonText = "Okay",
                DefaultButton = ContentDialogButton.Close,
                Content = "Could not export DLSS dll.",
            };
            await dialog.ShowAsync();
        }
        finally
        {
            // Clean up temp export path.
            try
            {
                if (Directory.Exists(tempExportPath))
                {
                    Directory.Delete(tempExportPath, true);
                }
            }
            catch (Exception err)
            {
                Logger.Error(err);
            }
        }
    }


    [RelayCommand]
    async Task ImportAsync()
    {
        if (DLLManager.Instance.ImportedManifest is null)
        {
            var couldNotImportDialog = new EasyContentDialog(libraryPage.XamlRoot)
            {
                Title = "Could not load imported DLLs",
                DefaultButton = ContentDialogButton.Close,
                Content = new ImportSystemDisabledView(),
                CloseButtonText = "Close",
            };
            await couldNotImportDialog.ShowAsync();
            return;
        }

        if (Settings.Instance.HasShownWarning == false)
        {
            var warningDialog = new EasyContentDialog(libraryPage.XamlRoot)
            {
                Title = "Warning",
                CloseButtonText = "Okay",
                DefaultButton = ContentDialogButton.Close,
                Content = @"Replacing dlls on your computer can be dangerous.

Placing a malicious dll into a game is as bad as running Linking_park_-_nUmB_mp3.exe that you just downloaded from LimeWire.

Only import dlls from sources you trust.",
            };
            await warningDialog.ShowAsync();

            Settings.Instance.HasShownWarning = true;
        }

        
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.CurrentApp.MainWindow);
        var openPicker = new Windows.Storage.Pickers.FileOpenPicker();
        openPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
        openPicker.FileTypeFilter.Add(".dll");
        openPicker.FileTypeFilter.Add(".zip");
        WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hwnd);
        var openFileList = await openPicker.PickMultipleFilesAsync();

        // User cancelled.
        if (openFileList is null || openFileList.Count == 0)
        {
            return;
        }

        var filesProgressBar = new ProgressBar()
        {
            IsIndeterminate = true
        };
        var dllInZipProgressBar = new ProgressBar()
        {
            IsIndeterminate = true
        };
        var progressTextBlock = new TextBlock()
        {
            Text = string.Empty,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        progressTextBlock.Inlines.Add(new Run() { Text = "Processed DLLs: " });
        var progressRun = new Run() { Text = "0" };
        progressTextBlock.Inlines.Add(progressRun);
        var progressStackPanel = new StackPanel()
        {
            Spacing = 16,
            Orientation = Orientation.Vertical,
            Children =
            {
                filesProgressBar,
                dllInZipProgressBar,
                progressTextBlock,
            }
        };

        var loadingDialog = new EasyContentDialog(libraryPage.XamlRoot)
        {
            Title = "Importing",
            // I would like this to be a progress ring but for some reason the ring will not show.
            Content = progressStackPanel,
        };
        _ = loadingDialog.ShowAsync();

        var taskCompletionSource = new TaskCompletionSource<List<DLLImportResult>>();

        bool HandleLocalDLLRecordZip(string importedPath, DLLRecord dllRecord, List<DLLImportResult> importResults)
        {
            if (dllRecord.LocalRecord is not null)
            {
                if (File.Exists(dllRecord.LocalRecord.ExpectedPath))
                {
                    importResults.Add(DLLImportResult.FromSucces(dllRecord.LocalRecord.ExpectedPath, "Already downloaded.", true));
                    return true;
                }

                File.Copy(importedPath, dllRecord.LocalRecord.ExpectedPath);
                App.CurrentApp.RunOnUIThread(() =>
                {
                    var localRecord = dllRecord.LocalRecord;
                    localRecord.IsDownloaded = true;
                    dllRecord.LocalRecord = null;
                    dllRecord.LocalRecord = localRecord;
                });
                importResults.Add(DLLImportResult.FromSucces(importedPath, "Imported as existing DLL record.", true));
                return true;
            }
            else
            {
                // This should never happen.
                Logger.Error("dllRecord.LocalRecord is null");
                Debugger.Break();
                return false;
            }
        }

        if (openFileList.Count == 1)
        {
            filesProgressBar.Visibility = Visibility.Collapsed;
        }
        else
        {
            filesProgressBar.IsIndeterminate = false;
        }

        filesProgressBar.Value = 0;
        filesProgressBar.Maximum = openFileList.Count;

        var selectedFilesProcessed = 0;
        var totalDllsProcessed = 0;

        ThreadPool.QueueUserWorkItem((stateInfo) =>
        {
            var importResults = new List<DLLImportResult>();

            // Used only if we import a zip
            var tempExtractPath = Path.Combine(Storage.GetTemp(), "import", Guid.NewGuid().ToString("D"));
            Storage.CreateDirectoryIfNotExists(tempExtractPath);

           
            foreach (var importFile in openFileList)
            {
                ++selectedFilesProcessed;
                App.CurrentApp.RunOnUIThread(() =>
                {
                    filesProgressBar.Value = selectedFilesProcessed;
                });

                if (importFile is null || File.Exists(importFile.Path) == false)
                {
                    importResults.Add(DLLImportResult.FromFail(importFile?.Path ?? string.Empty, "File not found."));
                    continue;
                }

                try
                {
                    if (importFile.Path.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // If we are importing a zip, first check if its hash is one 
                        // that we expect.Then we can just bypass everything.                        
                        var newZipHash = string.Empty;
                        using (var fileStream = File.OpenRead(importFile.Path))
                        {
                            newZipHash = fileStream.GetMD5Hash();
                        }

                        if (string.IsNullOrWhiteSpace(newZipHash) == false)
                        {
                            var dlssRecord = DLLManager.Instance.DLSSRecords.FirstOrDefault(x => string.Equals(x.ZipMD5Hash, newZipHash, StringComparison.InvariantCultureIgnoreCase));
                            if (dlssRecord is not null)
                            {
                                if (HandleLocalDLLRecordZip(importFile.Path, dlssRecord, importResults))
                                {
                                    ++totalDllsProcessed;
                                    App.CurrentApp.RunOnUIThread(() =>
                                    {
                                        progressRun.Text = totalDllsProcessed.ToString();
                                    });
                                    continue;
                                }
                            }

                            var dlssDRecord = DLLManager.Instance.DLSSDRecords.FirstOrDefault(x => string.Equals(x.ZipMD5Hash, newZipHash, StringComparison.InvariantCultureIgnoreCase));
                            if (dlssDRecord is not null)
                            {
                                if (HandleLocalDLLRecordZip(importFile.Path, dlssDRecord, importResults))
                                {
                                    ++totalDllsProcessed;
                                    App.CurrentApp.RunOnUIThread(() =>
                                    {
                                        progressRun.Text = totalDllsProcessed.ToString();
                                    });
                                    continue;
                                }
                            }

                            var dlssGRecord = DLLManager.Instance.DLSSGRecords.FirstOrDefault(x => string.Equals(x.ZipMD5Hash, newZipHash, StringComparison.InvariantCultureIgnoreCase));
                            if (dlssGRecord is not null)
                            {
                                if (HandleLocalDLLRecordZip(importFile.Path, dlssGRecord, importResults))
                                {
                                    ++totalDllsProcessed;
                                    App.CurrentApp.RunOnUIThread(() =>
                                    {
                                        progressRun.Text = totalDllsProcessed.ToString();
                                    });
                                    continue;
                                }
                            }

                            var fsr31dx12Record = DLLManager.Instance.FSR31DX12Records.FirstOrDefault(x => string.Equals(x.ZipMD5Hash, newZipHash, StringComparison.InvariantCultureIgnoreCase));
                            if (fsr31dx12Record is not null)
                            {
                                if (HandleLocalDLLRecordZip(importFile.Path, fsr31dx12Record, importResults))
                                {
                                    ++totalDllsProcessed;
                                    App.CurrentApp.RunOnUIThread(() =>
                                    {
                                        progressRun.Text = totalDllsProcessed.ToString();
                                    });
                                    continue;
                                }
                            }

                            var fsr32vkRecord = DLLManager.Instance.FSR31VKRecords.FirstOrDefault(x => string.Equals(x.ZipMD5Hash, newZipHash, StringComparison.InvariantCultureIgnoreCase));
                            if (fsr32vkRecord is not null)
                            {
                                if (HandleLocalDLLRecordZip(importFile.Path, fsr32vkRecord, importResults))
                                {
                                    ++totalDllsProcessed;
                                    App.CurrentApp.RunOnUIThread(() =>
                                    {
                                        progressRun.Text = totalDllsProcessed.ToString();
                                    });
                                    continue;
                                }
                            }

                            var xessRecord = DLLManager.Instance.XeSSRecords.FirstOrDefault(x => string.Equals(x.ZipMD5Hash, newZipHash, StringComparison.InvariantCultureIgnoreCase));
                            if (xessRecord is not null)
                            {
                                if (HandleLocalDLLRecordZip(importFile.Path, xessRecord, importResults))
                                {
                                    ++totalDllsProcessed;
                                    App.CurrentApp.RunOnUIThread(() =>
                                    {
                                        progressRun.Text = totalDllsProcessed.ToString();
                                    });
                                    continue;
                                }
                            }

                            var xellRecord = DLLManager.Instance.XeLLRecords.FirstOrDefault(x => string.Equals(x.ZipMD5Hash, newZipHash, StringComparison.InvariantCultureIgnoreCase));
                            if (xellRecord is not null)
                            {
                                if (HandleLocalDLLRecordZip(importFile.Path, xellRecord, importResults))
                                {
                                    ++totalDllsProcessed;
                                    App.CurrentApp.RunOnUIThread(() =>
                                    {
                                        progressRun.Text = totalDllsProcessed.ToString();
                                    });
                                    continue;
                                }
                            }

                            var xessFGRecord = DLLManager.Instance.XeSSFGRecords.FirstOrDefault(x => string.Equals(x.ZipMD5Hash, newZipHash, StringComparison.InvariantCultureIgnoreCase));
                            if (xessFGRecord is not null)
                            {
                                if (HandleLocalDLLRecordZip(importFile.Path, xessFGRecord, importResults))
                                {
                                    ++totalDllsProcessed;
                                    App.CurrentApp.RunOnUIThread(() =>
                                    {
                                        progressRun.Text = totalDllsProcessed.ToString();
                                    });
                                    continue;
                                }
                            }
                        }


                        // Now that we know the zip itself is not a known zip we will extract each DLL and import them.
                        using (var archive = ZipFile.OpenRead(importFile.Path))
                        {
                            var zippedDlls = archive.Entries.Where(x => x.Name.EndsWith(".dll")).ToArray();
                            if (zippedDlls.Length == 0)
                            {
                                throw new Exception("Zip did not contain any dlls.");
                            }

                            var dllsInZip = zippedDlls.Length;
                            var processedDllsInZip = 0;

                            App.CurrentApp.RunOnUIThread(() =>
                            {
                                dllInZipProgressBar.IsIndeterminate = false;
                                dllInZipProgressBar.Value = processedDllsInZip;
                                dllInZipProgressBar.Maximum = dllsInZip;
                            });

                            foreach (var zippedDll in zippedDlls)
                            {
                                var tempFile = Path.Combine(tempExtractPath, zippedDll.Name);
                                zippedDll.ExtractToFile(tempFile, true);

                                ++processedDllsInZip;
                                ++totalDllsProcessed;
                                App.CurrentApp.RunOnUIThread(() =>
                                {
                                    dllInZipProgressBar.Value = processedDllsInZip;
                                    progressRun.Text = totalDllsProcessed.ToString();
                                });


                                try
                                {
                                    // In future when DLLs will have multiple per bundle we will have to extract them all and pass them as a list.
                                    importResults.Add(DLLManager.Instance.ImportDll(tempFile, zippedDll.FullName));
                                }
                                catch (Exception err)
                                {
                                    Logger.Error(err);
                                    importResults.Add(DLLImportResult.FromFail(zippedDll.FullName, err.Message));
                                }

                                // Clean up temp file.
                                File.Delete(tempFile);
                            }
                        }
                    }
                    else if (importFile.Path.EndsWith(".dll", StringComparison.InvariantCultureIgnoreCase))
                    {
                        try
                        {
                            importResults.Add(DLLManager.Instance.ImportDll(importFile.Path));
                        }
                        catch (Exception err)
                        {
                            Logger.Error(err);
                            importResults.Add(DLLImportResult.FromFail(importFile.Path, err.Message));
                        }

                        ++totalDllsProcessed;
                        App.CurrentApp.RunOnUIThread(() =>
                        {
                            progressRun.Text = totalDllsProcessed.ToString();
                        });
                    }
                }
                catch (Exception err)
                {
                    Logger.Error(err);
                    importResults.Add(DLLImportResult.FromFail(importFile.Path, err.Message));
                }
            }

            // Clean up tempExtractPath if it exists
            if (Directory.Exists(tempExtractPath))
            {
                try
                {
                    Directory.Delete(tempExtractPath, true);
                }
                catch (Exception err2)
                {
                    Logger.Error(err2);
                }
            }

            taskCompletionSource.SetResult(importResults);
        });

        var importResults = await taskCompletionSource.Task;

        if (importResults.Any(x => x.Success == true))
        {
            await DLLManager.Instance.SaveImportedManifestJsonAsync();
            App.CurrentApp.MainWindow.FilterDLLRecords();
        }

        loadingDialog.Hide();
  
        var dialog = new EasyContentDialog(libraryPage.XamlRoot)
        {
            CloseButtonText = "Okay",
            DefaultButton = ContentDialogButton.Close,
            Title = "Finished",
            Content = new ImportDLLSummaryControl(importResults),
        };
        await dialog.ShowAsync();
    }

    [RelayCommand]
    async Task DeleteRecordAsync(DLLRecord record)
    {
        if (record.LocalRecord is null)
        {
            Logger.Error("Could not delete record, LocalRecord is null.");
            return;
        }

        var assetTypeName = DLLManager.Instance.GetAssetTypeName(record.AssetType);
        var dialog = new EasyContentDialog(libraryPage.XamlRoot)
        {
            Title = "Delete DLL",
            Content = $"Are you sure you want to delete {assetTypeName} v{record.Version}?",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
        };
        var response = await dialog.ShowAsync();
        if (response == ContentDialogResult.Primary)
        {
            var didDelete = record.LocalRecord.Delete();
            if (didDelete)
            {
                if (record.LocalRecord.IsImported)
                {
                    // TODO: What to do here?
                    DLLManager.Instance.DeleteImportedDllRecord(record);
                    await DLLManager.Instance.SaveImportedManifestJsonAsync();
                    App.CurrentApp.MainWindow.FilterDLLRecords();
                }
                else
                {
                    record.NotifyPropertyChanged(nameof(record.LocalRecord));
                }
            }
            else
            {
                var errorDialog = new EasyContentDialog(libraryPage.XamlRoot)
                {
                    Title = "Error",
                    CloseButtonText = "Okay",
                    DefaultButton = ContentDialogButton.Close,
                    Content = $"Unable to delete {assetTypeName} record.",
                };
                await errorDialog.ShowAsync();
            }
        }
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    async Task DownloadRecordAsync(DLLRecord record)
    {
        var result = await record.DownloadAsync();
        if (result.Success is false && result.Cancelled is false)
        {
            var dialog = new EasyContentDialog(libraryPage.XamlRoot)
            {
                Title = "Error",
                CloseButtonText = "Okay",
                DefaultButton = ContentDialogButton.Close,
                Content = result.Message,
            };

            await dialog.ShowAsync();
        }
    }

    [RelayCommand]
    async Task CancelDownloadRecordAsync(DLLRecord record)
    {
        record?.CancelDownload();
        await Task.Delay(10);
    }

    [RelayCommand]
    async Task ExportRecordAsync(DLLRecord record)
    {
        if (record.LocalRecord is null)
        {
            return;
        }

        var exportingDialog = new EasyContentDialog(libraryPage.XamlRoot)
        {
            Title = "Exporting",
            // I would like this to be a progress ring but for some reason the ring will not show.
            Content = new ProgressRing()
            {
                IsIndeterminate = true,
            },
        };

        try
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.CurrentApp.MainWindow);
            var savePicker = new Windows.Storage.Pickers.FileSavePicker();
            savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("Zip archive", new List<string>() { ".zip" });
            savePicker.SuggestedFileName = $"dlss_swapper_export_{record.DisplayName.Replace(" ", "_")}.zip";
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);
            var saveFile = await savePicker.PickSaveFileAsync();

            if (saveFile is not null)
            {
                // This will likley not be seen, but keeping it here incase export is very slow (eg. copy over very slow network).
                _ = exportingDialog.ShowAsync();

                // Give UI time to update and show import screen.
                await Task.Delay(50);

                File.Copy(record.LocalRecord.ExpectedPath, saveFile.Path, true);

                exportingDialog.Hide();

                var dialog = new EasyContentDialog(libraryPage.XamlRoot)
                {
                    Title = "Success",
                    CloseButtonText = "Okay",
                    DefaultButton = ContentDialogButton.Close,
                    Content = $"Exported DLL {record.DisplayName}.",
                };
                await dialog.ShowAsync();
            }
        }
        catch (Exception err)
        {
            exportingDialog.Hide();
            Logger.Error(err);

            // If the fullExpectedPath does not exist, or there was an error writing it.
            var dialog = new EasyContentDialog(libraryPage.XamlRoot)
            {
                Title = "Error",
                CloseButtonText = "Okay",
                DefaultButton = ContentDialogButton.Close,
                Content = "Could not export DLL.",
            };
            await dialog.ShowAsync();
        }
    }

    [RelayCommand]
    async Task ShowDownloadErrorAsync(DLLRecord record)
    {
        var dialog = new EasyContentDialog(libraryPage.XamlRoot)
        {
            Title = "Error",
            CloseButtonText = "Okay",
            Content = record.LocalRecord?.DownloadErrorMessage ?? "Could not download at this time.",
        };
        await dialog.ShowAsync();
    }

    internal void SelectLibrary(GameAssetType gameAssetType)
    {
        var newList = gameAssetType switch
        {
            GameAssetType.DLSS => DLLManager.Instance.DLSSRecords,
            GameAssetType.DLSS_G => DLLManager.Instance.DLSSGRecords,
            GameAssetType.DLSS_D => DLLManager.Instance.DLSSDRecords,
            GameAssetType.FSR_31_DX12 => DLLManager.Instance.FSR31DX12Records,
            GameAssetType.FSR_31_VK => DLLManager.Instance.FSR31VKRecords,
            GameAssetType.XeSS => DLLManager.Instance.XeSSRecords,
            GameAssetType.XeLL => DLLManager.Instance.XeLLRecords,
            GameAssetType.XeSS_FG => DLLManager.Instance.XeSSFGRecords,
            _ => null,
        };
        SelectedLibraryList = null;
        SelectedLibraryList = newList;
        OnPropertyChanged(nameof(SelectedLibraryList));
    }
}
