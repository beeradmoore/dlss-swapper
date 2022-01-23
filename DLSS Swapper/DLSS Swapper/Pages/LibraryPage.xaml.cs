using DLSS_Swapper.Data;
using DLSS_Swapper.Extensions;
using DLSS_Swapper.UserControls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using MvvmHelpers;
using MvvmHelpers.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DLSS_Swapper.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LibraryPage : Page, INotifyPropertyChanged
    {
        public AsyncCommand RefreshCommand { get; }
        public AsyncCommand ExportAllCommand { get; }
        public AsyncCommand ImportCommand { get; }
        public AsyncCommand<DLSSRecord> DeleteRecordCommand { get; }
        public AsyncCommand<DLSSRecord> DownloadRecordCommand { get; }
        public AsyncCommand<DLSSRecord> CancelDownloadRecordCommand { get; }
        public AsyncCommand<DLSSRecord> ExportRecordCommand { get; }
        public AsyncCommand<DLSSRecord> ShowDownloadErrorCommand { get; }


        bool _isRefreshing;
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set
            {
                if (_isRefreshing != value)
                {
                    _isRefreshing = value;
                    RefreshCommand.RaiseCanExecuteChanged();
                }
            }
        }


        bool _isExporting;
        public bool IsExporting
        {
            get => _isExporting;
            set
            {
                if (_isExporting != value)
                {
                    _isExporting = value;
                    ExportAllCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public ObservableRangeCollection<DLSSRecord> CurrentDLSSRecords { get; }

        public LibraryPage()
        {
            CurrentDLSSRecords = App.CurrentApp.MainWindow.CurrentDLSSRecords;

            RefreshCommand = new AsyncCommand(RefreshListAsync, _ => !IsRefreshing);
            ExportAllCommand = new AsyncCommand(ExportAllAsync, _ => !IsExporting);
            ImportCommand = new AsyncCommand(ImportAsync);
            DeleteRecordCommand = new AsyncCommand<DLSSRecord>(async (record) => await DeleteRecordAsync(record));
            DownloadRecordCommand = new AsyncCommand<DLSSRecord>(async (record) => await DownloadRecordAsync(record));
            CancelDownloadRecordCommand = new AsyncCommand<DLSSRecord>(async (record) => await CancelDownloadRecordAsync(record));
            ExportRecordCommand = new AsyncCommand<DLSSRecord>(async (record) => await ExportRecordAsync(record));
            ShowDownloadErrorCommand = new AsyncCommand<DLSSRecord>(async (record) => await ShowDownloadErrorAsync(record));

            this.InitializeComponent();

            DataContext = this;
        }


        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        async Task RefreshListAsync()
        {
            IsRefreshing = true;

            var didUpdate = await App.CurrentApp.MainWindow.UpdateDLSSRecordsAsync();
            if (didUpdate)
            {
                App.CurrentApp.MainWindow.FilterDLSSRecords();
                App.CurrentApp.LoadLocalRecords();
                //await App.CurrentApp.LoadLocalRecordsAsync();
            }
            else
            {
                var errorDialog = new ContentDialog();
                errorDialog.Title = "Error";
                errorDialog.CloseButtonText = "Okay";
                errorDialog.Content = "Unable to update DLSS records.";
                errorDialog.XamlRoot = XamlRoot;
                errorDialog.RequestedTheme = Settings.AppTheme;
                await errorDialog.ShowAsync();
            }

            IsRefreshing = false;
        }

        async Task ExportAllAsync()
        {
            try
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.CurrentApp.MainWindow);
                var savePicker = new Windows.Storage.Pickers.FileSavePicker();
                savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("Zip archive", new List<string>() { ".zip" });
                savePicker.SuggestedFileName = $"nvngx_dlss.zip";
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);
                var saveFile = await savePicker.PickSaveFileAsync();

                // User cancelled.
                if (saveFile == null)
                {
                    return;
                }

                using (var fileStream = File.Create(saveFile.Path))
                {
                    using (var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Create))
                    {
                        var allDlssRecords = new List<DLSSRecord>();
                        allDlssRecords.AddRange(App.CurrentApp.DLSSRecords.Stable);
                        allDlssRecords.AddRange(App.CurrentApp.DLSSRecords.Experimental);

                        foreach (var dlssRecord in allDlssRecords)
                        {
                            if (dlssRecord.LocalRecord?.IsDownloaded == true)
                            {
                                var fullExpectedPath = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, dlssRecord.LocalRecord.ExpectedPath);
                                var internalZipDir = dlssRecord.Version.ToString();
                                if (String.IsNullOrEmpty(dlssRecord.AdditionalLabel) == false)
                                {
                                    internalZipDir += " " + dlssRecord.AdditionalLabel;
                                }
                                zipArchive.CreateEntryFromFile(fullExpectedPath, Path.Combine(internalZipDir, Path.GetFileName(fullExpectedPath)));
                            }
                        }
                    }
                }
            }
            catch (Exception err)
            {
                System.Diagnostics.Debug.WriteLine($"ExportRecordAsync Error: {err.Message}");

                // If the fullExpectedPath does not exist, or there was an error writing it.
                var dialog = new ContentDialog();
                dialog.Title = "Error";
                dialog.CloseButtonText = "Okay";
                dialog.Content = "Could not export DLSS dll.";
                dialog.XamlRoot = XamlRoot;
                dialog.RequestedTheme = Settings.AppTheme;
                await dialog.ShowAsync();
            }
        }

        async Task ImportAsync()
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.CurrentApp.MainWindow);
            var openPicker = new Windows.Storage.Pickers.FileOpenPicker();
            openPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            openPicker.FileTypeFilter.Add(".dll");
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hwnd);
            var openFile = await openPicker.PickSingleFileAsync();

            // User cancelled.
            if (openFile == null)
            {
                return;
            }

            
            var dialog = new ContentDialog();
            dialog.PrimaryButtonText = "Import";
            dialog.CloseButtonText = "Cancel";
            dialog.Title = "Reminder";
            dialog.Content = $"Only import DLLs from sources you trust.";
            dialog.XamlRoot = XamlRoot;
            dialog.RequestedTheme = Settings.AppTheme;
            var response = await dialog.ShowAsync();
            if (response == ContentDialogResult.Primary)
            {
                try
                {
                    var dlssRecord = DLSSRecord.FromImportedFile(openFile.Path);

                    var existingRecords = App.CurrentApp.ImportedDLSSRecords.Where(x => x.VersionNumber == dlssRecord.VersionNumber && x.MD5Hash.Equals(dlssRecord.MD5Hash, StringComparison.InvariantCultureIgnoreCase)).ToList();
                    if (existingRecords.Count > 0)
                    {
                        // If it already exists prompt the user if they want to overwrite it.
                        dialog = new ContentDialog();
                        dialog.PrimaryButtonText = "Overwrite";
                        dialog.CloseButtonText = "Cancel";
                        dialog.Title = "Imported DLSS Record Exists";
                        dialog.Content = $"It appears you have already impored DLSS v{dlssRecord.Version}";
                        dialog.XamlRoot = XamlRoot;
                        dialog.RequestedTheme = Settings.AppTheme;
                        response = await dialog.ShowAsync();
                        if (response != ContentDialogResult.Primary)
                        {
                            return;
                        }
                    }
                    

                    // Check if the dll passes windows cert validation. If it doesn't and user has not allowed untrusted then error.
                    if (Settings.AllowUntrusted == false && dlssRecord.IsSignatureValid == false)
                    {
                        // If it already exists prompt the user if they want to overwrite it.
                        dialog = new ContentDialog();
                        dialog.CloseButtonText = "Okay";
                        dialog.Title = "Import Failed";
                        dialog.Content = $"The dll you imported ({openFile.Path}) is not signed with a valid certificate. If you believe this is a mistake and you want to import anyway enable \"Allow Untrusted\" in DLSS Swapper settings.\n\nONLY enable this setting if you trust where you got the dll from.";
                        dialog.XamlRoot = XamlRoot;
                        dialog.RequestedTheme = Settings.AppTheme;
                        response = await dialog.ShowAsync();
                        return;
                    }


                    // Do gross basic checks to see if it is a DLSS dll (this isn't a security check, its a sanity check)
                    var fileVersionInfo = FileVersionInfo.GetVersionInfo(openFile.Path);
                    if (fileVersionInfo.InternalName.Contains("dlss", StringComparison.InvariantCultureIgnoreCase) == false ||
                        fileVersionInfo.FileDescription.Contains("dlss", StringComparison.InvariantCultureIgnoreCase) == false)
                    {

                        // If it already exists prompt the user if they want to overwrite it.
                        dialog = new ContentDialog();
                        dialog.Title = "Possible Issue With DLL Imported";
                        dialog.PrimaryButtonText = "Import Anyway";
                        dialog.CloseButtonText = "Cancel";
                        dialog.Content = $"It appears the dll you imported potentially isn't a legitimate NVIDIA DLSS dll. Continue only if you trust the source where you obtained the dll.";
                        dialog.XamlRoot = XamlRoot;
                        dialog.RequestedTheme = Settings.AppTheme;
                        response = await dialog.ShowAsync();
                        if (response != ContentDialogResult.Primary)
                        {
                            return;
                        }
                    }


                    // Copy new record to where it should live in DLSS Swapper app directory.
                    var fullExpectedFileName = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, dlssRecord.LocalRecord.ExpectedPath);
                    var fullExpectedPath = Path.GetDirectoryName(fullExpectedFileName);
                    if (Directory.Exists(fullExpectedPath) == false)
                    {
                        Directory.CreateDirectory(fullExpectedPath);
                    }
                    File.Copy(openFile.Path, fullExpectedFileName, true);

                    // If there were other records we are overwriting we remove them.
                    foreach (var existingRecord in existingRecords)
                    {
                        App.CurrentApp.ImportedDLSSRecords.Remove(existingRecord);
                    }
                    
                    // Add our new record.
                    App.CurrentApp.ImportedDLSSRecords.Add(dlssRecord);
                    var didSave = await App.CurrentApp.SaveImportedDLSSRecordsAsync();
                    if (didSave == false)
                    {

                        return;
                    }

                    App.CurrentApp.MainWindow.FilterDLSSRecords();
                }
                catch (Exception err)
                {
                    int x = 0;
                    // TODO: ERROR
                }
            }
        }

        async Task DeleteRecordAsync(DLSSRecord record)
        {
            var dialog = new ContentDialog();
            dialog.PrimaryButtonText = "Delete";
            dialog.CloseButtonText = "Cancel";
            dialog.Content = $"Delete DLSS v{record.Version}?";
            dialog.XamlRoot = XamlRoot;
            dialog.RequestedTheme = Settings.AppTheme;
            var response = await dialog.ShowAsync();
            if (response == ContentDialogResult.Primary)
            {
                var didDelete = record.LocalRecord.Delete();
                if (didDelete)
                {
                    if (record.LocalRecord.IsImported)
                    {
                        App.CurrentApp.ImportedDLSSRecords.Remove(record);
                        App.CurrentApp.MainWindow.FilterDLSSRecords();
                    }
                    else
                    {
                        record.NotifyPropertyChanged(nameof(record.LocalRecord));
                    }
                }
                else
                {
                    var errorDialog = new ContentDialog();
                    errorDialog.Title = "Error";
                    errorDialog.CloseButtonText = "Okay";
                    errorDialog.Content = "Unable to delete DLSS record.";
                    errorDialog.XamlRoot = XamlRoot;
                    errorDialog.RequestedTheme = Settings.AppTheme;
                    await errorDialog.ShowAsync();
                }
            }
        }

        async Task DownloadRecordAsync(DLSSRecord record)
        {
            var result = await record?.DownloadAsync();
            if (result.Success == false && result.Cancelled == false)
            {
                var dialog = new ContentDialog();
                dialog.Title = "Error";
                dialog.CloseButtonText = "Okay";
                dialog.Content = result.Message;
                dialog.XamlRoot = XamlRoot;
                dialog.RequestedTheme = Settings.AppTheme;
                await dialog.ShowAsync();
            }
        }

        async Task CancelDownloadRecordAsync(DLSSRecord record)
        {
            record?.CancelDownload();
            await Task.Delay(10);
        }

        async Task ExportRecordAsync(DLSSRecord record)
        {
            try
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.CurrentApp.MainWindow);
                var savePicker = new Windows.Storage.Pickers.FileSavePicker();
                savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("Zip archive", new List<string>() { ".zip" });
                savePicker.SuggestedFileName = $"nvngx_dlss_{record.Version}.zip";
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);
                var saveFile = await savePicker.PickSaveFileAsync();

                if (saveFile != null)
                {
                    var fullExpectedPath = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, record.LocalRecord.ExpectedPath);

                    using (var fileStream = File.Create(saveFile.Path))
                    {
                        using (var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Create))
                        {
                            zipArchive.CreateEntryFromFile(fullExpectedPath, Path.GetFileName(fullExpectedPath));
                        }
                    }
                }
            }
            catch (Exception err)
            {
                System.Diagnostics.Debug.WriteLine($"ExportRecordAsync Error: {err.Message}");

                // If the fullExpectedPath does not exist, or there was an error writing it.
                var dialog = new ContentDialog();
                dialog.Title = "Error";
                dialog.CloseButtonText = "Okay";
                dialog.Content = "Could not export DLSS dll.";
                dialog.XamlRoot = XamlRoot;
                dialog.RequestedTheme = Settings.AppTheme;
                await dialog.ShowAsync();
            }
        }

        async Task ShowDownloadErrorAsync(DLSSRecord record)
        {
            var dialog = new ContentDialog();
            dialog.Title = "Error";
            dialog.CloseButtonText = "Okay";
            dialog.Content = record.LocalRecord.DownloadErrorMessage;
            dialog.XamlRoot = XamlRoot;
            dialog.RequestedTheme = Settings.AppTheme;
            await dialog.ShowAsync();
        }


        async void MainGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
            {
                return;
            }

            MainGridView.SelectedIndex = -1;
            if (e.AddedItems[0] is DLSSRecord dlssRecord)
            {
                var dialog = new ContentDialog();
                dialog.CloseButtonText = "Cancel";
                dialog.Content = new DLSSRecordInfoControl(dlssRecord);
                dialog.XamlRoot = XamlRoot;
                dialog.RequestedTheme = Settings.AppTheme;
                await dialog.ShowAsync();
            }
        }

        void MainGridView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // via: https://stackoverflow.com/a/41141249
            var columns = Math.Ceiling(MainGridView.ActualWidth / 400);
            ((ItemsWrapGrid)MainGridView.ItemsPanelRoot).ItemWidth = e.NewSize.Width / columns;
        }
    }
}
