using DLSS_Swapper.Data;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using MvvmHelpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DLSS_Swapper
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        MainWindow _window;
        public MainWindow MainWindow => _window;

        public static App CurrentApp => (App)Application.Current;

        internal DLSSRecords DLSSRecords { get; } = new DLSSRecords();
        internal List<DLSSRecord> ImportedDLSSRecords { get; } = new List<DLSSRecord>();

        internal HttpClient _httpClient = new HttpClient();
        public HttpClient HttpClient => _httpClient;
        //public ObservableRangeCollection<DLSSRecord> CurrentDLSSRecords { get; } = new ObservableRangeCollection<DLSSRecord>();


        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            var version = Windows.ApplicationModel.Package.Current.Id.Version;
            var versionString = String.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);

            _httpClient.DefaultRequestHeaders.Add("User-Agent", $"dlss-swapper v{versionString}");

            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {

            _window = new MainWindow();
            _window.ExtendsContentIntoTitleBar = true;
            _window.SetTitleBar(_window.AppTitleBar);
            _window.Activate();
        }


        internal void LoadLocalRecordFromDLSSRecord(DLSSRecord dlssRecord, bool isImportedRecord = false)
        {
            var dllsPath = isImportedRecord ? "imported_dlls" : "dlls";
            var expectedPath = Path.Combine(dllsPath, $"{dlssRecord.Version}_{dlssRecord.MD5Hash}", "nvngx_dlss.dll");
            System.Diagnostics.Debug.WriteLine($"ExpectedPath: {expectedPath}");
            // Load record.
            var localRecord = LocalRecord.FromExpectedPath(expectedPath);

            if (isImportedRecord)
            {
                localRecord.IsImported = true;
                localRecord.IsDownloaded = true;
            }

            // If the record exists we will update existing properties, if not we add it as new property.
            if (dlssRecord.LocalRecord == null)
            {
                dlssRecord.LocalRecord = localRecord;
            }
            else
            {
                dlssRecord.LocalRecord.UpdateFromNewLocalRecord(localRecord);
            }
        }

        /*
        // Disabled because the non-async method seems faster.
        internal async Task LoadLocalRecordFromDLSSRecordAsync(DLSSRecord dlssRecord)
        {
            var expectedPath = Path.Combine("dlls", $"{dlssRecord.Version}_{dlssRecord.MD5Hash}", "nvngx_dlss.dll");
            System.Diagnostics.Debug.WriteLine($"ExpectedPath: {expectedPath}");
            // Load record.
            var localRecord = await LocalRecord.FromExpectedPathAsync(expectedPath);

            // If the record exists we will update existing properties, if not we add it as new property.
            var existingLocalRecord = LocalRecords.FirstOrDefault(x => x.Equals(localRecord));
            if (existingLocalRecord == null)
            {
                dlssRecord.LocalRecord = localRecord;
                LocalRecords.Add(localRecord);
            }
            else
            {
                existingLocalRecord.UpdateFromNewLocalRecord(localRecord);

                // Probably don't need to set this again.
                dlssRecord.LocalRecord = existingLocalRecord;
            }
        }
        */

        internal void LoadLocalRecords()
        {
            // We attempt to load all local records, even if experemental is not enabled.
            foreach (var dlssRecord in DLSSRecords.Stable)
            {
                LoadLocalRecordFromDLSSRecord(dlssRecord);
            }
            foreach (var dlssRecord in DLSSRecords.Experimental)
            {
                LoadLocalRecordFromDLSSRecord(dlssRecord);
            }
            foreach (var dlssRecord in ImportedDLSSRecords)
            {
                LoadLocalRecordFromDLSSRecord(dlssRecord, true);
            }
        }


        /*
        // Disabled because the non-async method seems faster. 
        internal async Task LoadLocalRecordsAsync()
        {
            var tasks = new List<Task>();

            // We attempt to load all local records, even if experemental is not enabled.
            foreach (var dlssRecord in DLSSRecords.Stable)
            {
                tasks.Add(LoadLocalRecordFromDLSSRecordAsync(dlssRecord));
            }
            foreach (var dlssRecord in DLSSRecords.Experimental)
            {
                tasks.Add(LoadLocalRecordFromDLSSRecordAsync(dlssRecord));
            }
            await Task.WhenAll(tasks);
        }
        */



        internal async Task<bool> SaveImportedDLSSRecordsAsync()
        {
            try
            {
                var importedDlssRecordsFile = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "imported_dlss_records.json");
                var json = JsonSerializer.Serialize<List<DLSSRecord>>(App.CurrentApp.ImportedDLSSRecords);
                await File.WriteAllTextAsync(importedDlssRecordsFile, json);
                return true;
            }
            catch (Exception err)
            {
                Debug.WriteLine($"LoadDLSSRecords Error: {err.Message}");
                return false;
            }
        }
    }
}
