using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using DLSS_Swapper.Data;
using DLSS_Swapper.Data.NVIDIA;
using DLSS_Swapper.Extensions;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.UserControls;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Windows.UI.Text;

namespace DLSS_Swapper.Pages;

public partial class LibraryPageModel : ObservableObject
{
    LibraryPage libraryPage;

    internal ObservableCollection<DLLRecord>? SelectedLibraryList { get; private set; }

    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

    [ObservableProperty]
    public partial SelectorBarItem? SelectedSelectorBarItem { get; set; } = null;

    public LibraryPageModelTranslationProperties TranslationProperties { get; } = new LibraryPageModelTranslationProperties();

    public LibraryPageModel(LibraryPage libraryPage)
    {
        this.libraryPage = libraryPage;

        var upscalerSelectorBar = libraryPage.FindChild("UpscalerSelectorBar") as SelectorBar;
        if (upscalerSelectorBar is not null)
        {
            // NOTE: DLL type
            // TODO: Change order based on prefered upscaler.
            upscalerSelectorBar.Items.Add(new SelectorBarItem() { Text = DLLManager.Instance.GetAssetTypeName(GameAssetType.DLSS), Tag = GameAssetType.DLSS });
            upscalerSelectorBar.Items.Add(new SelectorBarItem() { Text = DLLManager.Instance.GetAssetTypeName(GameAssetType.DLSS_G), Tag = GameAssetType.DLSS_G });
            upscalerSelectorBar.Items.Add(new SelectorBarItem() { Text = DLLManager.Instance.GetAssetTypeName(GameAssetType.DLSS_D), Tag = GameAssetType.DLSS_D });
            upscalerSelectorBar.Items.Add(new SelectorBarItem() { Text = DLLManager.Instance.GetAssetTypeName(GameAssetType.FSR_31_DX12), Tag = GameAssetType.FSR_31_DX12 });
            upscalerSelectorBar.Items.Add(new SelectorBarItem() { Text = DLLManager.Instance.GetAssetTypeName(GameAssetType.FSR_31_VK), Tag = GameAssetType.FSR_31_VK });
            upscalerSelectorBar.Items.Add(new SelectorBarItem() { Text = DLLManager.Instance.GetAssetTypeName(GameAssetType.XeSS), Tag = GameAssetType.XeSS });
            upscalerSelectorBar.Items.Add(new SelectorBarItem() { Text = DLLManager.Instance.GetAssetTypeName(GameAssetType.XeSS_DX11), Tag = GameAssetType.XeSS_DX11 });
            upscalerSelectorBar.Items.Add(new SelectorBarItem() { Text = DLLManager.Instance.GetAssetTypeName(GameAssetType.XeSS_FG), Tag = GameAssetType.XeSS_FG });
            upscalerSelectorBar.Items.Add(new SelectorBarItem() { Text = DLLManager.Instance.GetAssetTypeName(GameAssetType.XeLL), Tag = GameAssetType.XeLL });

            SelectedSelectorBarItem = upscalerSelectorBar.Items[0];
        }

        LanguageManager.Instance.OnLanguageChanged += OnLanguageChanged;
    }

    void OnLanguageChanged()
    {
        var upscalerSelectorBar = libraryPage.FindChild("UpscalerSelectorBar") as SelectorBar;
        if (upscalerSelectorBar is null)
        {
            return;
        }

        foreach (var item in upscalerSelectorBar.Items)
        {
            if (item?.Tag is GameAssetType gameAssetType)
            {
                item.Text = DLLManager.Instance.GetAssetTypeName(gameAssetType);
            }
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

    [RelayCommand]
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
                Title = ResourceHelper.GetString("General_Error"),
                CloseButtonText = ResourceHelper.GetString("General_Okay"),
                DefaultButton = ContentDialogButton.Close,
                Content = ResourceHelper.GetString("LibraryPage_UnableToUpdateDllRecord"),
            };
            await errorDialog.ShowAsync();
        }

        IsRefreshing = false;
    }

    [RelayCommand]
    async Task ExportAllAsync()
    {
        // NOTE: DLL type
        // Check that there are records to export first.
        var allDllRecords = new List<DLLRecord>();
        allDllRecords.AddRange(DLLManager.Instance.DLSSRecords.Where(x => x.LocalRecord?.IsDownloaded == true));
        allDllRecords.AddRange(DLLManager.Instance.DLSSGRecords.Where(x => x.LocalRecord?.IsDownloaded == true));
        allDllRecords.AddRange(DLLManager.Instance.DLSSDRecords.Where(x => x.LocalRecord?.IsDownloaded == true));
        allDllRecords.AddRange(DLLManager.Instance.FSR31DX12Records.Where(x => x.LocalRecord?.IsDownloaded == true));
        allDllRecords.AddRange(DLLManager.Instance.FSR31VKRecords.Where(x => x.LocalRecord?.IsDownloaded == true));
        allDllRecords.AddRange(DLLManager.Instance.XeSSRecords.Where(x => x.LocalRecord?.IsDownloaded == true));
        allDllRecords.AddRange(DLLManager.Instance.XeSSFGRecords.Where(x => x.LocalRecord?.IsDownloaded == true));
        allDllRecords.AddRange(DLLManager.Instance.XeSSDX11Records.Where(x => x.LocalRecord?.IsDownloaded == true));
        allDllRecords.AddRange(DLLManager.Instance.XeLLRecords.Where(x => x.LocalRecord?.IsDownloaded == true));

        if (allDllRecords.Count == 0)
        {
            var dialog = new EasyContentDialog(libraryPage.XamlRoot)
            {
                CloseButtonText = ResourceHelper.GetString("General_Okay"),
                DefaultButton = ContentDialogButton.Close,
                Title = ResourceHelper.GetString("General_Error"),
                Content = ResourceHelper.GetString("LibraryPage_NoDllsToExport"),
            };
            await dialog.ShowAsync();
            return;
        }



        var filesProgressBar = new ProgressBar()
        {
            IsIndeterminate = true
        };
        var progressTextBlock = new TextBlock()
        {
            Text = string.Empty,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        progressTextBlock.Inlines.Add(new Run() { Text = ResourceHelper.GetString("LibraryPage_ExportedDLLs"), FontWeight = FontWeights.Bold });
        var progressRun = new Run() { Text = "0" };
        progressTextBlock.Inlines.Add(progressRun);
        var progressStackPanel = new StackPanel()
        {
            Spacing = 16,
            Orientation = Orientation.Vertical,
            Children =
            {
                filesProgressBar,
                progressTextBlock,
            }
        };

        var str = ResourceHelper.GetString("LibraryPage_Exporting");
        var exportingDialog = new EasyContentDialog(libraryPage.XamlRoot)
        {
            Title = ResourceHelper.GetString("LibraryPage_Exporting"),
            Content = progressStackPanel,
        };

        var tempExportPath = Path.Combine(Storage.GetTemp(), "export");
        var finalExportZip = string.Empty;
        try
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.CurrentApp.MainWindow);

            var fileFilters = new List<FileSystemHelper.FileFilter>()
            {
                new FileSystemHelper.FileFilter("Zip files", "*.zip"),
            };

            finalExportZip = FileSystemHelper.SaveFile(hWnd, fileFilters, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "dlss_swapper_export.zip", defaultExtension: "zip");

            // User cancelled.
            if (string.IsNullOrWhiteSpace(finalExportZip))
            {
                return;
            }

            Storage.CreateDirectoryIfNotExists(tempExportPath);

            _ = exportingDialog.ShowAsync();

            // Give UI time to update and show export loading wheel.
            await Task.Delay(50);

            var toExport = new List<(string SourceFileName, string EntryName)>();

            foreach (var dllRecord in allDllRecords)
            {
                if (dllRecord.LocalRecord is null || dllRecord.LocalRecord.IsDownloaded == false)
                {
                    continue;
                }

                var expectedPathDirectory = Path.GetDirectoryName(dllRecord.LocalRecord.ExpectedPath);
                if (string.IsNullOrWhiteSpace(expectedPathDirectory))
                {
                    continue;
                }

                // TODO: When fixing imported system, make sure to update this to use full path
                var internalZipDir = DLLManager.Instance.GetAssetTypeName(dllRecord.AssetType);
                if (dllRecord.LocalRecord.IsImported == true)
                {
                    internalZipDir = Path.Combine("Imported", internalZipDir);
                }
                var directoryInfo = new DirectoryInfo(expectedPathDirectory);

                internalZipDir = Path.Combine(internalZipDir, directoryInfo.Name);

                toExport.Add((dllRecord.LocalRecord.ExpectedPath, Path.Combine(internalZipDir, Path.GetFileName(dllRecord.LocalRecord.ExpectedPath))));
            }


            Exception? exportError = null;

            if (toExport.Count == 0)
            {
                exportingDialog.Hide();

                var dialog = new EasyContentDialog(libraryPage.XamlRoot)
                {
                    Title = ResourceHelper.GetString("General_Error"),
                    CloseButtonText = ResourceHelper.GetString("General_Okay"),
                    DefaultButton = ContentDialogButton.Close,
                    Content = ResourceHelper.GetString("LibraryPage_NoDLLsForExport_Message"),
                };
                await dialog.ShowAsync();
            }
            else
            {
                filesProgressBar.IsIndeterminate = false;
                filesProgressBar.Value = 0;
                filesProgressBar.Maximum = toExport.Count;

                var progress = new Progress<int>();
                progress.ProgressChanged += (s, i) =>
                {
                    filesProgressBar.Value = i;
                    progressRun.Text = i.ToString(CultureInfo.CurrentCulture);
                };

                await Task.Run(() =>
                {
                    exportError = ExportDllWorker(finalExportZip, toExport, progress);
                });

                exportingDialog.Hide();

                if (exportError is null)
                {
                    var dialog = new EasyContentDialog(libraryPage.XamlRoot)
                    {
                        CloseButtonText = ResourceHelper.GetString("General_Okay"),
                        DefaultButton = ContentDialogButton.Close,
                        Title = ResourceHelper.GetString("General_Success"),
                        Content = ResourceHelper.GetFormattedResourceTemplate("LibraryPage_ExportedDLLsCount_Message", toExport.Count),
                    };
                    await dialog.ShowAsync();
                }
                else
                {
                    throw new Exception("Worker thread failed to export.", exportError);
                }
            }
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
                Title = ResourceHelper.GetString("General_Error"),
                CloseButtonText = ResourceHelper.GetString("General_Okay"),
                DefaultButton = ContentDialogButton.Close,
                Content = ResourceHelper.GetString("LibraryPage_CouldntExportDll"),
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

    Exception? ExportDllWorker(string zipPath, List<(string SourceFileName, string EntryName)> filesToAdd, IProgress<int>? progress)
    {
        try
        {
            using (var fileStream = File.Create(zipPath))
            {
                using (var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Create))
                {
                    var exported = 0;
                    foreach (var fileToAdd in filesToAdd)
                    {
                        zipArchive.CreateEntryFromFile(fileToAdd.SourceFileName, fileToAdd.EntryName);
                        ++exported;

                        progress?.Report(exported);
                    }
                }
            }

            return null;
        }
        catch (Exception err)
        {
            Logger.Error(err);
            return err;
        }
    }


    [RelayCommand]
    async Task ImportAsync()
    {
        if (DLLManager.Instance.ImportedManifest is null)
        {
            var couldNotImportDialog = new EasyContentDialog(libraryPage.XamlRoot)
            {
                Title = ResourceHelper.GetString("LibraryPage_CouldNotLoadImportedDlls"),
                DefaultButton = ContentDialogButton.Close,
                Content = new ImportSystemDisabledView(),
                CloseButtonText = ResourceHelper.GetString("General_Close"),
            };
            await couldNotImportDialog.ShowAsync();
            return;
        }

        if (Settings.Instance.HasShownWarning == false)
        {
            var warningDialog = new EasyContentDialog(libraryPage.XamlRoot)
            {
                Title = ResourceHelper.GetString("General_Warning"),
                CloseButtonText = ResourceHelper.GetString("General_Okay"),
                DefaultButton = ContentDialogButton.Close,
                Content = ResourceHelper.GetString("LibraryPage_MaliciousDllsInfo"),
            };
            await warningDialog.ShowAsync();

            Settings.Instance.HasShownWarning = true;
        }


        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.CurrentApp.MainWindow);

        var fileFilters = new List<FileSystemHelper.FileFilter>()
        {
            new FileSystemHelper.FileFilter("Supported file types", "*.dll; *.zip"),
            new FileSystemHelper.FileFilter("DLL files", "*.dll"),
            new FileSystemHelper.FileFilter("ZIP files", "*.zip")
        };

        var openFileList = FileSystemHelper.OpenMultipleFiles(hWnd, fileFilters, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

        // User cancelled.
        if (openFileList.Count == 0)
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
        progressTextBlock.Inlines.Add(new Run() { Text = ResourceHelper.GetString("LibraryPage_ProcessedDlls"), FontWeight = FontWeights.Bold });
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
            Title = ResourceHelper.GetString("LibraryPage_Importing"),
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
                    App.CurrentApp.RunOnUIThread(() =>
                    {
                        var localRecord = dllRecord.LocalRecord;
                        localRecord.IsDownloaded = true;
                        dllRecord.LocalRecord = null;
                        dllRecord.LocalRecord = localRecord;
                    });

                    importResults.Add(DLLImportResult.FromSucces(dllRecord.LocalRecord.ExpectedPath, ResourceHelper.GetString("LibraryPage_AlreadyDownloaded"), true));
                    return true;
                }

                try
                {
                    using (var fileStream = File.OpenRead(importedPath))
                    {
                        using (var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read, true))
                        {
                            DLLManager.HandleExtractFromZip(zipArchive, dllRecord);
                        }
                    }
                }
                catch (Exception err)
                {
                    Logger.Error(err);
                    importResults.Add(DLLImportResult.FromFail(importedPath, "Failed to extract DLL from zip."));
                    return false;
                }

                App.CurrentApp.RunOnUIThread(() =>
                {
                    var localRecord = dllRecord.LocalRecord;
                    localRecord.IsDownloaded = true;
                    dllRecord.LocalRecord = null;
                    dllRecord.LocalRecord = localRecord;
                });
                importResults.Add(DLLImportResult.FromSucces(importedPath, ResourceHelper.GetString("LibraryPage_ImportedAsExistingRecord"), true));
                return true;
            }
            else
            {
                // This should never happen.
                Logger.Error("dllRecord.LocalRecord is null");
                Debugger.Break();
                importResults.Add(DLLImportResult.FromFail(importedPath, "dllRecord.LocalRecord is null"));
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

                if (importFile is null || File.Exists(importFile) == false)
                {
                    importResults.Add(DLLImportResult.FromFail(importFile ?? string.Empty, ResourceHelper.GetString("LibraryPage_FileNotFound")));
                    continue;
                }

                try
                {
                    if (importFile.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // If we are importing a zip, first check if its hash is one
                        // that we expect.Then we can just bypass everything.
                        var newZipHash = string.Empty;
                        using (var fileStream = File.OpenRead(importFile))
                        {
                            newZipHash = fileStream.GetMD5Hash();
                        }

                        if (string.IsNullOrWhiteSpace(newZipHash) == false)
                        {
                            // NOTE: DLL type
                            var dlssRecord = DLLManager.Instance.DLSSRecords.FirstOrDefault(x => string.Equals(x.ZipMD5Hash, newZipHash, StringComparison.InvariantCultureIgnoreCase));
                            if (dlssRecord is not null)
                            {
                                if (HandleLocalDLLRecordZip(importFile, dlssRecord, importResults))
                                {
                                    ++totalDllsProcessed;
                                    App.CurrentApp.RunOnUIThread(() =>
                                    {
                                        progressRun.Text = totalDllsProcessed.ToString(CultureInfo.CurrentCulture);
                                    });
                                    continue;
                                }
                            }

                            var dlssDRecord = DLLManager.Instance.DLSSDRecords.FirstOrDefault(x => string.Equals(x.ZipMD5Hash, newZipHash, StringComparison.InvariantCultureIgnoreCase));
                            if (dlssDRecord is not null)
                            {
                                if (HandleLocalDLLRecordZip(importFile, dlssDRecord, importResults))
                                {
                                    ++totalDllsProcessed;
                                    App.CurrentApp.RunOnUIThread(() =>
                                    {
                                        progressRun.Text = totalDllsProcessed.ToString(CultureInfo.CurrentCulture);
                                    });
                                    continue;
                                }
                            }

                            var dlssGRecord = DLLManager.Instance.DLSSGRecords.FirstOrDefault(x => string.Equals(x.ZipMD5Hash, newZipHash, StringComparison.InvariantCultureIgnoreCase));
                            if (dlssGRecord is not null)
                            {
                                if (HandleLocalDLLRecordZip(importFile, dlssGRecord, importResults))
                                {
                                    ++totalDllsProcessed;
                                    App.CurrentApp.RunOnUIThread(() =>
                                    {
                                        progressRun.Text = totalDllsProcessed.ToString(CultureInfo.CurrentCulture);
                                    });
                                    continue;
                                }
                            }

                            var fsr31dx12Record = DLLManager.Instance.FSR31DX12Records.FirstOrDefault(x => string.Equals(x.ZipMD5Hash, newZipHash, StringComparison.InvariantCultureIgnoreCase));
                            if (fsr31dx12Record is not null)
                            {
                                if (HandleLocalDLLRecordZip(importFile, fsr31dx12Record, importResults))
                                {
                                    ++totalDllsProcessed;
                                    App.CurrentApp.RunOnUIThread(() =>
                                    {
                                        progressRun.Text = totalDllsProcessed.ToString(CultureInfo.CurrentCulture);
                                    });
                                    continue;
                                }
                            }

                            var fsr32vkRecord = DLLManager.Instance.FSR31VKRecords.FirstOrDefault(x => string.Equals(x.ZipMD5Hash, newZipHash, StringComparison.InvariantCultureIgnoreCase));
                            if (fsr32vkRecord is not null)
                            {
                                if (HandleLocalDLLRecordZip(importFile, fsr32vkRecord, importResults))
                                {
                                    ++totalDllsProcessed;
                                    App.CurrentApp.RunOnUIThread(() =>
                                    {
                                        progressRun.Text = totalDllsProcessed.ToString(CultureInfo.CurrentCulture);
                                    });
                                    continue;
                                }
                            }

                            var xessRecord = DLLManager.Instance.XeSSRecords.FirstOrDefault(x => string.Equals(x.ZipMD5Hash, newZipHash, StringComparison.InvariantCultureIgnoreCase));
                            if (xessRecord is not null)
                            {
                                if (HandleLocalDLLRecordZip(importFile, xessRecord, importResults))
                                {
                                    ++totalDllsProcessed;
                                    App.CurrentApp.RunOnUIThread(() =>
                                    {
                                        progressRun.Text = totalDllsProcessed.ToString(CultureInfo.CurrentCulture);
                                    });
                                    continue;
                                }
                            }

                            var xellRecord = DLLManager.Instance.XeLLRecords.FirstOrDefault(x => string.Equals(x.ZipMD5Hash, newZipHash, StringComparison.InvariantCultureIgnoreCase));
                            if (xellRecord is not null)
                            {
                                if (HandleLocalDLLRecordZip(importFile, xellRecord, importResults))
                                {
                                    ++totalDllsProcessed;
                                    App.CurrentApp.RunOnUIThread(() =>
                                    {
                                        progressRun.Text = totalDllsProcessed.ToString(CultureInfo.CurrentCulture);
                                    });
                                    continue;
                                }
                            }

                            var xessDX11Record = DLLManager.Instance.XeSSDX11Records.FirstOrDefault(x => string.Equals(x.ZipMD5Hash, newZipHash, StringComparison.InvariantCultureIgnoreCase));
                            if (xessDX11Record is not null)
                            {
                                if (HandleLocalDLLRecordZip(importFile, xessDX11Record, importResults))
                                {
                                    ++totalDllsProcessed;
                                    App.CurrentApp.RunOnUIThread(() =>
                                    {
                                        progressRun.Text = totalDllsProcessed.ToString(CultureInfo.CurrentCulture);
                                    });
                                    continue;
                                }
                            }

                            var xessFGRecord = DLLManager.Instance.XeSSFGRecords.FirstOrDefault(x => string.Equals(x.ZipMD5Hash, newZipHash, StringComparison.InvariantCultureIgnoreCase));
                            if (xessFGRecord is not null)
                            {
                                if (HandleLocalDLLRecordZip(importFile, xessFGRecord, importResults))
                                {
                                    ++totalDllsProcessed;
                                    App.CurrentApp.RunOnUIThread(() =>
                                    {
                                        progressRun.Text = totalDllsProcessed.ToString(CultureInfo.CurrentCulture);
                                    });
                                    continue;
                                }
                            }
                        }


                        // Now that we know the zip itself is not a known zip we will extract each DLL and import them.
                        using (var archive = ZipFile.OpenRead(importFile))
                        {
                            var zippedDlls = archive.Entries.Where(x => x.Name.EndsWith(".dll")).ToArray();
                            if (zippedDlls.Length == 0)
                            {
                                throw new Exception(ResourceHelper.GetString("LibraryPage_ZipDidNotContainAnyDlls"));
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
                                var tempFile = Path.Combine(tempExtractPath, Guid.NewGuid().ToString("D"), zippedDll.Name);
                                Storage.CreateDirectoryForFileIfNotExists(tempFile);

                                zippedDll.ExtractToFile(tempFile, true);

                                ++processedDllsInZip;
                                ++totalDllsProcessed;
                                App.CurrentApp.RunOnUIThread(() =>
                                {
                                    dllInZipProgressBar.Value = processedDllsInZip;
                                    progressRun.Text = totalDllsProcessed.ToString(CultureInfo.CurrentCulture);
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
                    else if (importFile.EndsWith(".dll", StringComparison.InvariantCultureIgnoreCase))
                    {
                        try
                        {
                            importResults.Add(DLLManager.Instance.ImportDll(importFile));
                        }
                        catch (Exception err)
                        {
                            Logger.Error(err);
                            importResults.Add(DLLImportResult.FromFail(importFile, err.Message));
                        }

                        ++totalDllsProcessed;
                        App.CurrentApp.RunOnUIThread(() =>
                        {
                            progressRun.Text = totalDllsProcessed.ToString(CultureInfo.CurrentCulture);
                        });
                    }
                }
                catch (Exception err)
                {
                    Logger.Error(err);
                    importResults.Add(DLLImportResult.FromFail(importFile, err.Message));
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
            CloseButtonText = ResourceHelper.GetString("General_Okay"),
            DefaultButton = ContentDialogButton.Close,
            Title = ResourceHelper.GetString("LibraryPage_Finished"),
            Content = new ImportDLLSummaryControl(importResults),
        };
        await dialog.ShowAsync();
    }

    [RelayCommand]
    async Task ImportFromNVIDIADriverAsync()
    {
        var loadingProgressRing = new ProgressRing()
        {
            IsIndeterminate = true
        };
        var loadingDialog = new EasyContentDialog(libraryPage.XamlRoot)
        {
            Title = ResourceHelper.GetString("LibraryPage_ImportFromNVIDIADriver"),
            Content = loadingProgressRing,
            CloseButtonText = ResourceHelper.GetString("General_Cancel"),
        };
        using var cancellationTokenSource = new CancellationTokenSource();
        loadingDialog.CloseButtonClick += (ContentDialog sender, ContentDialogButtonClickEventArgs args) => {
            cancellationTokenSource.Cancel();
        };

        _ = loadingDialog.ShowAsync();

        var models = new List<NGXModel>();
        await Task.Run(() =>
        {
            models.AddRange(NVAPIHelper.Instance.GetNGXModels());
        });

        loadingDialog.Hide();

        if (cancellationTokenSource.IsCancellationRequested)
        {
            return;
        }

        if (models.Count == 0)
        {
            var errorDialog = new EasyContentDialog(libraryPage.XamlRoot)
            {
                Title = ResourceHelper.GetString("General_Error"),
                Content = ResourceHelper.GetString("LibraryPage_CouldNotImportFromDriver"),
                CloseButtonText = ResourceHelper.GetString("General_Close"),
            };
            await errorDialog.ShowAsync();
            return;
        }
                
        var ngxModelImporter = new NGXModelImporter(models);
        var dialog = new EasyContentDialog(libraryPage.XamlRoot)
        {
            Title = ResourceHelper.GetString("LibraryPage_ImportFromNVIDIADriver"),
            DefaultButton = ContentDialogButton.Primary,
            Content = ngxModelImporter,
            PrimaryButtonText = ResourceHelper.GetString("General_Import"),
            CloseButtonText = ResourceHelper.GetString("General_Close"),
        };
        dialog.Resources["ContentDialogMinWidth"] = 700;
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            var modelsToImport = new List<NGXModel>();
            foreach (var modelRow in ngxModelImporter.ViewModel.Models)
            {
                if (modelRow.IsChecked == true)
                {
                    modelsToImport.Add(modelRow.NGXModel);
                }
            }

            if (modelsToImport.Count == 0)
            {
                return;
            }


            var filesProgressBar = new ProgressBar()
            {
                IsIndeterminate = true
            };
            var progressTextBlock = new TextBlock()
            {
                Text = string.Empty,
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            progressTextBlock.Inlines.Add(new Run() { Text = ResourceHelper.GetString("LibraryPage_ImportedDLLs"), FontWeight = FontWeights.Bold });
            var progressRun = new Run() { Text = "0" };
            progressTextBlock.Inlines.Add(progressRun);
            var progressStackPanel = new StackPanel()
            {
                Spacing = 16,
                Orientation = Orientation.Vertical,
                Children =
                {
                    filesProgressBar,
                    progressTextBlock,
                }
            };


            var importingDialog = new EasyContentDialog(libraryPage.XamlRoot)
            {
                Title = ResourceHelper.GetString("LibraryPage_Importing"),
                Content = progressStackPanel,
            };

            filesProgressBar.IsIndeterminate = false;
            filesProgressBar.Value = 0;
            filesProgressBar.Maximum = modelsToImport.Count;

            _ = importingDialog.ShowAsync();

            var successCount = 0;
            var failedCount = 0;

            await Task.Run(() => {
                for (var i = 0; i < modelsToImport.Count; ++i)
                {
                    try
                    {
                        var didImport = DLLManager.Instance.ImportDll(modelsToImport[i].FilePath, overrideFileName: DLLManager.DllNameForGameAssetType(modelsToImport[i].GameAssetType));
                        if (didImport.Success)
                        {
                            ++successCount;
                        }
                        else
                        {
                            ++failedCount;
                        }
                    }
                    catch (Exception ex)
                    {
                        ++failedCount;
                        Logger.Error(ex, "Error importing NGX model.");
                    }
                    finally
                    {
                        App.CurrentApp.RunOnUIThread(() =>
                        {
                            filesProgressBar.Value = i;
                            progressRun.Text = i.ToString(CultureInfo.CurrentCulture);
                        });
                    }
                }
            });

            importingDialog.Hide();

            var completeDialog = new EasyContentDialog(libraryPage.XamlRoot)
            {
                Title = ResourceHelper.GetString("LibraryPage_ImportFromNVIDIADriver"),
                DefaultButton = ContentDialogButton.Close,
                Content = $"{ResourceHelper.GetString("General_Success")}: {successCount}\n{ResourceHelper.GetString("General_Failed")}: {failedCount}",
                CloseButtonText = ResourceHelper.GetString("General_Close"),
            };
            completeDialog.ShowAsync();

        }
    }


    [GeneratedRegex(@"^d6e9b45e-d4f6-4a84-a460-bf61decae3e8\/(?<asset_type>dlss|dlssg|dlssd)\/versions\/(?<version_packed>\d*)\/files\/160_E658700\.bin$", RegexOptions.IgnoreCase)]
    private static partial Regex IsNGXModelWeCanUse();

    [RelayCommand]
    async Task ImportFromNVIDIAServerAsync()
    {
        var loadingProgressRing = new ProgressRing()
        {
            IsIndeterminate = true
        };
        var loadingDialog = new EasyContentDialog(libraryPage.XamlRoot)
        {
            Title = ResourceHelper.GetString("LibraryPage_FetchingFileList"),
            Content = loadingProgressRing,
            CloseButtonText = ResourceHelper.GetString("General_Cancel"),
        };
        using var cancellationTokenSource = new CancellationTokenSource();
        loadingDialog.CloseButtonClick += (ContentDialog sender, ContentDialogButtonClickEventArgs args) => {
            cancellationTokenSource.Cancel();
        };

        _ = loadingDialog.ShowAsync();

        var ngxOtaUrl = "https://ngx.download.nvidia.com";
        var xmlDownloader = new FileDownloader(ngxOtaUrl);

        var availableModels = new List<NGXModel>();

        using (var memoryStream = new MemoryStream())
        {
            try
            {
                var didDownload = await xmlDownloader.DownloadFileToStreamAsync(memoryStream, cancellationTokenSource.Token);
                if (didDownload == false)
                {
                    throw new Exception("Could not download xml stream.");
                }

                memoryStream.Position = 0;

                var serializer = new XmlSerializer(typeof(ListBucketResult));
                var listBucketResult = serializer.Deserialize(memoryStream) as ListBucketResult;

                if (listBucketResult is null)
                {
                    throw new Exception("ListBucketResult was null.");
                }


                foreach (var content in listBucketResult.Contents)
                {
                    if (content is null || content.Size == 0)
                    {
                        continue;
                    }

                    // We only give the option of 160_E658700.bin. Other files do exist.
                    // 160 is from NV_GPU_ARCHITECTURE_ID of Turing GPUs. But it appears everyone has this for DLSS files.
                    // As for what E658700, no idea.
                    // https://github.com/SimonMacer/AnWave/issues/52#issuecomment-3025720063
                    // https://docs.nvidia.com/nvapi/group__gpu.html
                    if (content.Key.EndsWith("files/160_E658700.bin") == false)
                    {
                        continue;
                    }

                    var match = IsNGXModelWeCanUse().Match(content.Key);
                    if (match.Success == false)
                    {
                        continue;
                    }

                    GameAssetType? gameAssetType = match.Groups["asset_type"].Value switch
                    {
                        "dlss" => GameAssetType.DLSS,
                        "dlssd" => GameAssetType.DLSS_D,
                        "dlssg" => GameAssetType.DLSS_G,
                        _ => null,
                    };


                    if (gameAssetType == null)
                    {
                        continue;
                    }

                    if (Int32.TryParse(match.Groups["version_packed"].ValueSpan, out var versionInt) == false)
                    {
                        Logger.Error($"Could not convert {match.Groups["version_packed"].Value} to a version number.");
                        continue;
                    }

                    var major = (versionInt >> 16) & 0xFFFF;
                    var minor = (versionInt >> 8) & 0xFF;
                    var build = versionInt & 0xFF;
                    var version = new Version(major, minor, build, 0);

                    availableModels.Add(new NGXModel($"{ngxOtaUrl}/{content.Key}", version, gameAssetType.Value, (long)content.Size, content.ETag));
                }

            }
            catch (TaskCanceledException) when (cancellationTokenSource.IsCancellationRequested)
            {
                // NOOP: User cancelled
                return;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);

                loadingDialog.Hide();

                var errorDialog = new EasyContentDialog(libraryPage.XamlRoot)
                {
                    Title = ResourceHelper.GetString("General_Error"),
                    Content = ResourceHelper.GetString("LibraryPage_Error_NVIDIA_Importing"),
                    CloseButtonText = ResourceHelper.GetString("General_Cancel"),
                };
                await errorDialog.ShowAsync();
                return;
            }
        }


        if (availableModels.Count == 0)
        {
            loadingDialog.Hide();
            var errorDialog = new EasyContentDialog(libraryPage.XamlRoot)
            {
                Title = ResourceHelper.GetString("General_Error"),
                Content = ResourceHelper.GetString("LibraryPage_Error_NVIDIA_Downloading"),
                CloseButtonText = ResourceHelper.GetString("General_Cancel"),
            };
            await errorDialog.ShowAsync();
            return;
        }

        var ngxModelImporter = new NGXModelImporter(availableModels);

        foreach (var modelRow in ngxModelImporter.ViewModel.Models)
        {
            var versionNumber = modelRow.NGXModel.Version.GetVersionNumber();

            var existingRecordsToTest = new List<DLLRecord>();

            if (modelRow.NGXModel.GameAssetType == GameAssetType.DLSS)
            {
                var existingDLLRecords = DLLManager.Instance.DLSSRecords.Where(x => x.VersionNumber == versionNumber && x.LocalRecord is not null && x.LocalRecord.IsDownloaded);
                existingRecordsToTest.AddRange(existingDLLRecords);
            }
            else if (modelRow.NGXModel.GameAssetType == GameAssetType.DLSS_D)
            {
                var existingDLLRecords = DLLManager.Instance.DLSSDRecords.Where(x => x.VersionNumber == versionNumber && x.LocalRecord is not null && x.LocalRecord.IsDownloaded);
                existingRecordsToTest.AddRange(existingDLLRecords);
            }
            else if (modelRow.NGXModel.GameAssetType == GameAssetType.DLSS_G)
            {
                var existingDLLRecords = DLLManager.Instance.DLSSGRecords.Where(x => x.VersionNumber == versionNumber && x.LocalRecord is not null && x.LocalRecord.IsDownloaded);
                existingRecordsToTest.AddRange(existingDLLRecords);
            }

            foreach (var existingRecordToTest in existingRecordsToTest)
            {
                try
                {
                    if (File.Exists(existingRecordToTest?.LocalRecord?.ExpectedPath))
                    {
                        using (var fileStream = File.OpenRead(existingRecordToTest.LocalRecord.ExpectedPath))
                        {
                            var isValid = NVAPIHelper.Instance.ValidateNVIDIAOtaHash(fileStream, modelRow.NGXModel.ETag);
                            if (isValid)
                            {
                                modelRow.IsEnabled = false;
                                modelRow.StatusMessage = ResourceHelper.GetString("LibraryPage_AlreadyDownloaded");
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }
        }

        loadingDialog.Hide();

        var dialog = new EasyContentDialog(libraryPage.XamlRoot)
        {
            Title = ResourceHelper.GetString("LibraryPage_DownloadFromNVIDIA"),
            DefaultButton = ContentDialogButton.Primary,
            Content = ngxModelImporter,
            PrimaryButtonText = ResourceHelper.GetString("General_Download"),
            CloseButtonText = ResourceHelper.GetString("General_Close"),
        };
        dialog.Resources["ContentDialogMinWidth"] = 700;
        var result = await dialog.ShowAsync();

        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        var modelsToDownload = ngxModelImporter.ViewModel.Models.Where(x => x.IsChecked).ToList();
        if (modelsToDownload.Count == 0)
        {
            return;
        }


        var totalFilesProgressBar = new ProgressBar()
        {
            IsIndeterminate = false,
            Value = 0,
            Maximum = modelsToDownload.Count,
        };
        var totalFilesTextBlock = new TextBlock()
        {
            Text = string.Empty,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        totalFilesTextBlock.Inlines.Add(new Run() { Text = ResourceHelper.GetString("LibraryPage_DownloadedCount"), FontWeight = FontWeights.Bold });
        var totalFilesProgressRun = new Run() { Text = "0" };
        totalFilesTextBlock.Inlines.Add(totalFilesProgressRun);


        var currentFileProgressBar = new ProgressBar()
        {
            IsIndeterminate = false,
            Value = 0,
            Maximum = 1,
            Margin = new Thickness(0, 8, 0, 0),
        };
        var currentFileTextBlock = new TextBlock()
        {
            Text = string.Empty,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        currentFileTextBlock.Inlines.Add(new Run() { Text = ResourceHelper.GetString("LibraryPage_ProgressPercent"), FontWeight = FontWeights.Bold });
        var currentFileProgressRun = new Run() { Text = "0" };
        currentFileTextBlock.Inlines.Add(currentFileProgressRun);

        var progressStackPanel = new StackPanel()
        {
            Spacing = 16,
            Orientation = Orientation.Vertical,
            Children =
            {
                totalFilesProgressBar,
                totalFilesTextBlock,
                currentFileProgressBar,
                currentFileTextBlock,
            }
        };

        var downloadingDialog = new EasyContentDialog(libraryPage.XamlRoot)
        {
            Title = ResourceHelper.GetString("General_Downloading"),
            Content = progressStackPanel,
            CloseButtonText = ResourceHelper.GetString("General_Cancel"),
        };

        downloadingDialog.CloseButtonClick += (ContentDialog sender, ContentDialogButtonClickEventArgs args) => {
            cancellationTokenSource.Cancel();
        };

        _ = downloadingDialog.ShowAsync();

        var successCount = 0;
        var failCount = 0;

        await Task.Run(async () => {
            for (var i = 0; i < modelsToDownload.Count; ++i)
            {
                if (cancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }

                App.CurrentApp.RunOnUIThread(() =>
                {
                    currentFileProgressBar.Value = 0;
                });

                try
                {
                    var tempFileName = $"{Guid.NewGuid().ToString("D")}.tmp";
                    var tempFilePath = Path.Combine(Storage.GetTemp(), tempFileName);

                    var didDownload = false;
                    using (var fileStream = File.Create(tempFilePath))
                    {
                        var fileDownloader = new FileDownloader(modelsToDownload[i].NGXModel.FilePath);
                        didDownload = await fileDownloader.DownloadFileToStreamAsync(fileStream, cancellationTokenSource.Token, progressCallback: (DownloadedBytes, TotalBytesToDownload, Percent) =>
                        {
                            App.CurrentApp.RunOnUIThread(() =>
                            {
                                var smallPercent = Percent / 100.0;
                                currentFileProgressBar.Value = smallPercent;
                                currentFileProgressRun.Text = smallPercent.ToString("P", CultureInfo.CurrentCulture);
                            });
                        });
                    }

                    if (didDownload)
                    {
                        var didImport = DLLManager.Instance.ImportDll(tempFilePath, overrideFileName: DLLManager.DllNameForGameAssetType(modelsToDownload[i].NGXModel.GameAssetType));
                        if (didImport.Success)
                        {
                            ++successCount;
                        }
                        else
                        {
                            ++failCount;
                        }
                    }
                    else
                    {
                        ++failCount;
                    }
                }
                catch (TaskCanceledException) when (cancellationTokenSource.IsCancellationRequested)
                {
                    // NOOP
                }
                catch (Exception ex)
                {
                    ++failCount;
                    Logger.Error(ex, "Error downloading or importing NGX model.");
                }
                finally
                {
                    App.CurrentApp.RunOnUIThread(() =>
                    {
                        totalFilesProgressBar.Value += 1;
                        totalFilesProgressRun.Text = totalFilesProgressBar.Value.ToString(CultureInfo.CurrentCulture);
                    });
                }
            }
        });


        if (cancellationTokenSource.IsCancellationRequested == false)
        {
            await DLLManager.Instance.SaveImportedManifestJsonAsync();

            downloadingDialog.Hide();

            var completeDialog = new EasyContentDialog(libraryPage.XamlRoot)
            {
                Title = ResourceHelper.GetString("LibraryPage_DownloadFromNVIDIA"),
                DefaultButton = ContentDialogButton.Close,
                Content = $"{ResourceHelper.GetString("General_Success")}: {successCount}\n{ResourceHelper.GetString("General_Failed")}: {failCount}",
                CloseButtonText = ResourceHelper.GetString("General_Close"),
            };

            await completeDialog.ShowAsync();

        }
        else
        {
            downloadingDialog.Hide();
        }
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
            Title = ResourceHelper.GetString("LibraryPage_DeleteDll"),
            PrimaryButtonText = ResourceHelper.GetString("General_Delete"),
            CloseButtonText = ResourceHelper.GetString("General_Cancel"),
            DefaultButton = ContentDialogButton.Primary,
            Content = ResourceHelper.GetFormattedResourceTemplate("LibraryPage_DeleteDllVersionTemplate", assetTypeName, record.Version),
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
                    Title = ResourceHelper.GetString("General_Error"),
                    CloseButtonText = ResourceHelper.GetString("General_Okay"),
                    DefaultButton = ContentDialogButton.Close,
                    Content = ResourceHelper.GetFormattedResourceTemplate("LibraryPage_UnableToDeleteRecord", assetTypeName),
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
                Title = ResourceHelper.GetString("General_Error"),
                CloseButtonText = ResourceHelper.GetString("General_Okay"),
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
    async Task ExportRecordAsync(DLLRecord dllRecord)
    {
        if (dllRecord.LocalRecord is null)
        {
            return;
        }

        var exportingDialog = new EasyContentDialog(libraryPage.XamlRoot)
        {
            Title = ResourceHelper.GetString("LibraryPage_Exporting"),
            // I would like this to be a progress ring but for some reason the ring will not show.
            Content = new ProgressRing()
            {
                IsIndeterminate = true,
            },
        };

        try
        {
            var exportName = $"dlss_swapper_export_{dllRecord.DisplayName.Replace(" ", "_")}.zip";

            var expectedPathDirectory = Path.GetDirectoryName(dllRecord.LocalRecord.ExpectedPath);
            if (string.IsNullOrWhiteSpace(expectedPathDirectory) == false)
            {
                var directoryInfo = new DirectoryInfo(expectedPathDirectory);
                exportName = $"export_{directoryInfo.Name}.zip";
            }

            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.CurrentApp.MainWindow);

            var fileFilters = new List<FileSystemHelper.FileFilter>()
            {
                new FileSystemHelper.FileFilter("Zip files", "*.zip"),
            };

            var saveFile = FileSystemHelper.SaveFile(hWnd, fileFilters, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), exportName, defaultExtension: "zip");

            if (string.IsNullOrWhiteSpace(saveFile))
            {
                // User cancelled.
                return;
            }

           
            // This will likley not be seen, but keeping it here in case export is very slow (eg. copy over very slow network).
            _ = exportingDialog.ShowAsync();

            // Give UI time to update and show import screen.
            await Task.Delay(50);

            var toExport = new List<(string SourceFileName, string EntryName)>();
            toExport.Add((dllRecord.LocalRecord.ExpectedPath, Path.GetFileName(dllRecord.LocalRecord.ExpectedPath)));

            Exception? exportError = null;
            await Task.Run(() =>
            {
                exportError = ExportDllWorker(saveFile, toExport, null);
            });

            if (exportError is not null)
            {
                throw new Exception("Worker thread failed to export.", exportError);
            }

            exportingDialog.Hide();

            var dialog = new EasyContentDialog(libraryPage.XamlRoot)
            {
                Title = ResourceHelper.GetString("General_Success"),
                CloseButtonText = ResourceHelper.GetString("General_Okay"),
                DefaultButton = ContentDialogButton.Close,
                Content = ResourceHelper.GetFormattedResourceTemplate("LibraryPage_ExportedDllTemplate", dllRecord.DisplayName),
            };
            await dialog.ShowAsync();
        }
        catch (Exception err)
        {
            exportingDialog.Hide();
            Logger.Error(err);

            // If the fullExpectedPath does not exist, or there was an error writing it.
            var dialog = new EasyContentDialog(libraryPage.XamlRoot)
            {
                Title = ResourceHelper.GetString("General_Error"),
                CloseButtonText = ResourceHelper.GetString("General_Okay"),
                DefaultButton = ContentDialogButton.Close,
                Content = ResourceHelper.GetString("LibraryPage_CouldntExportDll"),
            };
            await dialog.ShowAsync();
        }
    }

    [RelayCommand]
    async Task ShowDownloadErrorAsync(DLLRecord record)
    {
        var dialog = new EasyContentDialog(libraryPage.XamlRoot)
        {
            Title = ResourceHelper.GetString("General_Error"),
            CloseButtonText = ResourceHelper.GetString("General_Okay"),
            Content = record.LocalRecord?.DownloadErrorMessage ?? ResourceHelper.GetString("LibraryPage_CouldntDownload"),
        };
        await dialog.ShowAsync();
    }

    internal void SelectLibrary(GameAssetType gameAssetType)
    {
        // NOTE: DLL type
        var newList = gameAssetType switch
        {
            GameAssetType.DLSS => DLLManager.Instance.DLSSRecords,
            GameAssetType.DLSS_G => DLLManager.Instance.DLSSGRecords,
            GameAssetType.DLSS_D => DLLManager.Instance.DLSSDRecords,
            GameAssetType.FSR_31_DX12 => DLLManager.Instance.FSR31DX12Records,
            GameAssetType.FSR_31_VK => DLLManager.Instance.FSR31VKRecords,
            GameAssetType.XeSS => DLLManager.Instance.XeSSRecords,
            GameAssetType.XeLL => DLLManager.Instance.XeLLRecords,
            GameAssetType.XeSS_DX11 => DLLManager.Instance.XeSSDX11Records,
            GameAssetType.XeSS_FG => DLLManager.Instance.XeSSFGRecords,
            _ => null,
        };
        SelectedLibraryList = null;
        SelectedLibraryList = newList;
        OnPropertyChanged(nameof(SelectedLibraryList));
    }

    [RelayCommand]
    async Task DownloadLatestAsync()
    {
        // NOTE: DLL type
        var startedDownloads = 0;
        startedDownloads += DownloadLatestRecord(DLLManager.Instance.DLSSRecords);
        startedDownloads += DownloadLatestRecord(DLLManager.Instance.DLSSDRecords);
        startedDownloads += DownloadLatestRecord(DLLManager.Instance.DLSSGRecords);
        startedDownloads += DownloadLatestRecord(DLLManager.Instance.FSR31DX12Records);
        startedDownloads += DownloadLatestRecord(DLLManager.Instance.FSR31VKRecords);
        startedDownloads += DownloadLatestRecord(DLLManager.Instance.XeSSRecords);
        startedDownloads += DownloadLatestRecord(DLLManager.Instance.XeSSFGRecords);
        startedDownloads += DownloadLatestRecord(DLLManager.Instance.XeSSDX11Records);
        startedDownloads += DownloadLatestRecord(DLLManager.Instance.XeLLRecords);

        if (startedDownloads == 0)
        {
            var dialog = new EasyContentDialog(libraryPage.XamlRoot)
            {
                Title = ResourceHelper.GetString("LibraryPage_NoNewDLLs_Title"),
                CloseButtonText = ResourceHelper.GetString("General_Okay"),
                DefaultButton = ContentDialogButton.Close,
                Content = ResourceHelper.GetString("LibraryPage_NoNewDLLs_Message"),
            };
            await dialog.ShowAsync();
        }
        else
        {
            var dialog = new EasyContentDialog(libraryPage.XamlRoot)
            {
                Title = ResourceHelper.GetString("LibraryPage_DownloadsStarted_Title"),
                CloseButtonText = ResourceHelper.GetString("General_Okay"),
                DefaultButton = ContentDialogButton.Close,
                Content = ResourceHelper.GetFormattedResourceTemplate("LibraryPage_DownloadsStarted_Message", startedDownloads),
            };
            await dialog.ShowAsync();
        }
    }

    int DownloadLatestRecord(IReadOnlyList<DLLRecord> records)
    {
        var startedCount = 0;
        var record = GetLatestRecord(records, false);
        if (record?.LocalRecord?.IsDownloaded == false)
        {
            _ = record.DownloadAsync();
            ++startedCount;
        }

        if (Settings.Instance.AllowDebugDlls)
        {
            record = GetLatestRecord(records, true);
            if (record?.LocalRecord?.IsDownloaded == false)
            {
                _ = record.DownloadAsync();
                ++startedCount;
            }
        }

        return startedCount;
    }

    DLLRecord? GetLatestRecord(IReadOnlyList<DLLRecord> records, bool devDllsOnly)
    {
        if (records.Count == 0)
        {
            return null;
        }

        DLLRecord? latestRecord = null;
        foreach (var record in records)
        {
            if (record.IsDevFile == devDllsOnly)
            {
                if (latestRecord is null)
                {
                    latestRecord = record;
                }
                else
                {
                    if (record.AssetType == GameAssetType.FSR_31_DX12 ||
                        record.AssetType == GameAssetType.FSR_31_VK ||
                        record.AssetType == GameAssetType.FSR_31_DX12_BACKUP ||
                        record.AssetType == GameAssetType.FSR_31_VK_BACKUP)
                    {
                        if (record.DisplayVersionVersion > latestRecord.DisplayVersionVersion)
                        {
                            latestRecord = record;
                        }
                    }
                    else
                    {
                        if (record.VersionNumber > latestRecord.VersionNumber)
                        {
                            latestRecord = record;
                        }
                    }
                }
            }
        }

        return latestRecord;
    }
}
