using DLSS_Swapper.Data;
using DLSS_Swapper.Data.CustomDirectory;
using DLSS_Swapper.Data.EpicGamesStore;
using DLSS_Swapper.Data.GOG;
using DLSS_Swapper.Data.Steam;
using DLSS_Swapper.Data.UbisoftConnect;
using DLSS_Swapper.Data.Xbox;
using DLSS_Swapper.Interfaces;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Win32;
using MvvmHelpers;
using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Principal;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DLSS_Swapper
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : Application
    {
        public ElementTheme GlobalElementTheme { get; set; }

        MainWindow? _window;
        public MainWindow MainWindow => _window ??= new MainWindow();

        public static App CurrentApp => (App)Application.Current;

        internal DLSSRecords DLSSRecords { get; } = new DLSSRecords();
        internal List<DLSSRecord> ImportedDLSSRecords { get; } = new List<DLSSRecord>();

        internal HttpClient _httpClient = new HttpClient();
        public HttpClient HttpClient => _httpClient;


        SQLiteAsyncConnection database;
        public SQLiteAsyncConnection Database => database;

        //public ObservableRangeCollection<DLSSRecord> CurrentDLSSRecords { get; } = new ObservableRangeCollection<DLSSRecord>();


        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            Logger.Init();

            var version = GetVersion();
            var versionString = string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);


            Logger.Info($"App launch - v{versionString}", null);

            _httpClient.DefaultRequestHeaders.Add("User-Agent", $"dlss-swapper v{versionString}");

            GlobalElementTheme = Settings.Instance.AppTheme;

            database = new SQLiteAsyncConnection(Storage.GetDBPath());
            Task.Run(async () =>
            {
                try
                {
                    await database.CreateTableAsync<SteamGame>();
                }
                catch (Exception err)
                {
                    Logger.Error(err.Message);
                }


                try
                {
                    await database.CreateTableAsync<GOGGame>();
                }
                catch (Exception err)
                {
                    Logger.Error(err.Message);
                }


                try
                {
                    await database.CreateTableAsync<EpicGamesStoreGame>();
                }
                catch (Exception err)
                {
                    Logger.Error(err.Message);
                }


                try
                {
                    await database.CreateTableAsync<UbisoftConnectGame>();
                }
                catch (Exception err)
                {
                    Logger.Error(err.Message);
                }


                try
                {
                    await database.CreateTableAsync<XboxGame>();
                }
                catch (Exception err)
                {
                    Logger.Error(err.Message);
                }


                try
                {
                    await database.CreateTableAsync<ManuallyAddedGame>();
                }
                catch (Exception err)
                {
                    Logger.Error(err.Message);
                }

                try
                {
                    await database.CreateTableAsync<GameAsset>();
                }
                catch (Exception err)
                {
                    Logger.Error(err.Message);
                }
            });

            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            MainWindow.Activate();

            // No need to calculate this for portable app.
#if !PORTABLE
            var calculateInstallSizeThread = new Thread(CalculateInstallSize);
            calculateInstallSizeThread.Start();
#endif
        }

#if !PORTABLE
        void CalculateInstallSize()
        {
            try
            {
                long installSize = 0;
                installSize += CalculateDirectorySize(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DLSS Swapper"));

                using (var dlssSwapperRegistryKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\DLSS Swapper", true))
                {
                    var installLocation = dlssSwapperRegistryKey?.GetValue("InstallLocation") as string;
                    if (string.IsNullOrEmpty(installLocation) == false && Directory.Exists(installLocation) == true)
                    {
                        installSize += CalculateDirectorySize(installLocation);
                    }

                    if (installSize > 0)
                    {
                        var installSizeKB = (int)(installSize / 1000);
                        dlssSwapperRegistryKey?.SetValue("EstimatedSize", installSizeKB, Microsoft.Win32.RegistryValueKind.DWord);
                    }
                }
            }
            catch (Exception err)
            {
                Logger.Error(err.Message);
            }
        }

        long CalculateDirectorySize(string path)
        {
            var directorySize = 0L;
            var fileCount = 0;
            var directoryInfo = new DirectoryInfo(path);
            foreach (var fileInfo in directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                directorySize += fileInfo.Length;
                ++fileCount;
            }

            //Logger.Debug($"{path} has {fileCount} files for a total size of {directorySize} bytes");

            return directorySize;
        }
#endif


        internal void LoadLocalRecordFromDLSSRecord(DLSSRecord dlssRecord, bool isImportedRecord = false)
        {
#if PORTABLE
            var dllsPath = Path.Combine("StoredData", (isImportedRecord ? "imported_dlss_zip" : "dlss_zip"));
#else
            var dllsPath = Path.Combine(Storage.GetStorageFolder(), (isImportedRecord ? "imported_dlss_zip" : "dlss_zip"));
#endif

            var expectedPath = Path.Combine(dllsPath, $"{dlssRecord.Version}_{dlssRecord.MD5Hash}.zip");
            
            // Load record.
            var localRecord = LocalRecord.FromExpectedPath(expectedPath, isImportedRecord);

            if (isImportedRecord)
            {
                localRecord.IsImported = true;
                localRecord.IsDownloaded = true;
            }

            // If the record exists we will update existing properties, if not we add it as new property.
            if (dlssRecord.LocalRecord is null)
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
            Logger.Debug($"ExpectedPath: {expectedPath}");
            // Load record.
            var localRecord = await LocalRecord.FromExpectedPathAsync(expectedPath);

            // If the record exists we will update existing properties, if not we add it as new property.
            var existingLocalRecord = LocalRecords.FirstOrDefault(x => x.Equals(localRecord));
            if (existingLocalRecord is null)
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

        public bool IsAdminUser()
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);

            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public void RestartAsAdmin()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = Assembly.GetExecutingAssembly().GetName().Name,
                Verb = "runas"
            };

            try
            {
                Process.Start(startInfo);
                Logger.Info("Restarting as admin.");
            }
            catch (Win32Exception)
            {
                Logger.Warning("User refused the elevation.");
                return;
            }

            App.CurrentApp.Exit();
        }

        /*
        // Disabled as I am unsure how to prompt to run as admin.
        internal void RelaunchAsAdministrator()
        {
            //var currentExe = Process.GetCurrentProcess().MainModule.FileName;

            //var executingAssembly = System.Reflection.Assembly.GetExecutingAssembly();
            //executingAssembly.FullName;
            
            // So this does prompt UAC, this was temporarily used to copy files in UpdateDll and ResetDll
            // but it would prompt for every action. 
            //var startInfo = new ProcessStartInfo()
            //{
            //    WindowStyle = ProcessWindowStyle.Hidden,
            //    FileName = "cmd.exe",
            //    Arguments = $"/C copy \"{dll}\" \"{targetDllPath}\"",
            //    UseShellExecute = true,
            //    Verb = "runas",
            //};
            //Process.Start(startInfo);

            MainWindow.Close();
            //Logger.Error(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }
        */

        public Version GetVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version ?? new Version();
        }

        public string GetVersionString()
        {
            var version = GetVersion();
            if (version.Build == 0 && version.Revision == 0)
            {
                return $"{version.Major}.{version.Minor}";
            }
            else if (version.Revision == 0)
            {
                return $"{version.Major}.{version.Minor}.{version.Build}";
            }
            return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }
    }
}
