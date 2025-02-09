using CommunityToolkit.WinUI;
using DLSS_Swapper.Data;
using DLSS_Swapper.Data.EpicGamesStore;
using DLSS_Swapper.Data.GOG;
using DLSS_Swapper.Data.Steam;
using DLSS_Swapper.Data.UbisoftConnect;
using DLSS_Swapper.Data.Xbox;
using DLSS_Swapper.Interfaces;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Win32;
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

        //internal Manifest Manifest { get; } = new Manifest();
        internal Manifest ImportedManifest { get; } = new Manifest();

        internal HttpClient? _httpClient;
        public HttpClient HttpClient
        {
            get
            {
                if (_httpClient is null)
                {
                    var version = GetVersion();
                    var versionString = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";

                    var httpClientHandler = new HttpClientHandler()
                    {
                        AutomaticDecompression = System.Net.DecompressionMethods.All,
                        UseCookies = true,
                        CookieContainer = new System.Net.CookieContainer(),
                        AllowAutoRedirect = true,
                    };

                    _httpClient = new HttpClient(httpClientHandler);
                    _httpClient.DefaultRequestHeaders.Add("User-Agent", $"dlss-swapper/{versionString}");
                    _httpClient.Timeout = TimeSpan.FromSeconds(20);
                }

                return _httpClient;
            }
        }


        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            Logger.Init();

            UnhandledException += App_UnhandledException;

            GlobalElementTheme = Settings.Instance.AppTheme;

            this.InitializeComponent();
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            Serilog.Log.Error(e.Exception, "UnhandledException");
            Serilog.Log.CloseAndFlush();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
                        
            // If this is the first instance launched, then register it as the "main" instance.
            // If this isn't the first instance launched, then "main" will already be registered,
            // so retrieve it.
            var mainInstance = Microsoft.Windows.AppLifecycle.AppInstance.FindOrRegisterForKey("main");

            // If the instance that's executing the OnLaunched handler right now
            // isn't the "main" instance.
            if (mainInstance.IsCurrent == false)
            {
                // Redirect the activation (and args) to the "main" instance, and exit.
                var activatedEventArgs = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();
                await mainInstance.RedirectActivationToAsync(activatedEventArgs);
                Process.GetCurrentProcess().Kill();
                return;
            }

            if (Environment.SystemDirectory.Equals(Storage.StoragePath, StringComparison.InvariantCultureIgnoreCase))
            {
                var failToLaunchWindow = new FailToLaunchWindow();
                failToLaunchWindow.Activate();
                return;
            }

            var version = GetVersion();
            var versionString = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
            Logger.Info($"App launch - v{versionString}", null);

            Logger.Error($"Storage: {Storage.StoragePath}");

            Database.Instance.Init();

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

        public bool RunOnUIThread(Action action)
        {
            if (Thread.CurrentThread.ManagedThreadId == 1)
            {
                action();
                return true;
            }

            if (MainWindow?.DispatcherQueue is not null)
            {
                var didEnqueue = MainWindow.DispatcherQueue.TryEnqueue(new DispatcherQueueHandler(action));

                if (didEnqueue == false)
                {
                    Logger.Error("TryEnqueue failed.");
                }

                return didEnqueue;
            }

            return false;
        }


        public Task RunOnUIThreadAsync(Func<Task> function)
        {
            if (Thread.CurrentThread.ManagedThreadId == 1)
            {
                return function();
            }

            if (MainWindow?.DispatcherQueue is not null)
            {
                return MainWindow.DispatcherQueue.EnqueueAsync(function);
            }

            return Task.CompletedTask;
        }

    }
}
