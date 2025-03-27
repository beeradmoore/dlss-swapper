using CommunityToolkit.WinUI;
using DLSS_Swapper.Data;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

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
                    _httpClient.Timeout = TimeSpan.FromMinutes(30);
                    _httpClient.DefaultRequestVersion = new Version(2, 0);
                    _httpClient.DefaultRequestHeaders.ConnectionClose = true;
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

            string language = Settings.Instance.Language;
            Thread.CurrentThread.CurrentCulture = new CultureInfo(language);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(language);

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

            if (Storage.StoragePath.Trim(Path.DirectorySeparatorChar).Contains(Environment.SystemDirectory, StringComparison.InvariantCultureIgnoreCase))
            {
                var failToLaunchWindow = new FailToLaunchWindow();
                failToLaunchWindow.Activate();
                return;
            }

            var version = GetVersion();
            var versionString = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
            Logger.Info($"App launch - v{versionString}", null);
            Logger.Info($"StoragePath: {Storage.StoragePath}");

            Database.Instance.Init();

            MainWindow.Activate();

            // No need to calculate this for portable app.
#if !PORTABLE
            // No need to calculate this for portable app.
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
                Logger.Error(err);
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
                    try
                    {
                        // I am sure there is a better way to fill out a stacktrace than throwing an exception
                        throw new Exception("TryEnqueue failed.");
                    }
                    catch (Exception err)
                    {
                        Logger.Error(err);
                    }
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
