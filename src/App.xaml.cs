using CommunityToolkit.WinUI;
using DLSS_Swapper.Helpers;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.ComponentModel;
using System.Diagnostics;
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
        public WindowManager WindowManager { get; } = new WindowManager();

        public static App CurrentApp => (App)Application.Current;


        public HttpClient HttpClient { get; init; }


        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            Logger.Init();

            // Setup HttpClient.
            var version = GetVersion();
            var versionString = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";

            var httpClientHandler = new HttpClientHandler()
            {
                AutomaticDecompression = System.Net.DecompressionMethods.All,
                UseCookies = true,
                CookieContainer = new System.Net.CookieContainer(),
                AllowAutoRedirect = true,
            };
            HttpClient = new HttpClient(httpClientHandler);
            HttpClient.DefaultRequestHeaders.Add("User-Agent", $"dlss-swapper/{versionString}");
            HttpClient.Timeout = TimeSpan.FromMinutes(30);
            HttpClient.DefaultRequestVersion = new Version(2, 0);
            HttpClient.DefaultRequestHeaders.ConnectionClose = true;

            var language = Settings.Instance.Language;

            // Language is not set, try to fetch from system.
            if (string.IsNullOrWhiteSpace(language))
            {
                // Try the language of the current thread.
                var currentLauguage = Thread.CurrentThread.CurrentCulture.Name;
                var knownLanguages = LanguageManager.Instance.GetKnownLanguages();
                foreach (var knownLanguage in knownLanguages)
                {
                    if (string.Equals(currentLauguage, knownLanguage, StringComparison.InvariantCultureIgnoreCase))
                    {
                        language = knownLanguage;
                        break;
                    }
                }

                // TODO: Can we fallback to other languages? eg. Is fr-CA acceptable to fallback to fr-FR or does the app just default back to en-US?
            }

            // If we failed to fetch the users language, default to en-US.
            if (string.IsNullOrWhiteSpace(language))
            {
                language = "en-US";
            }
            Settings.Instance.Language = language;

            LanguageManager.Instance.ChangeLanguage(language);

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
                WindowManager.ShowWindow(failToLaunchWindow);
                return;
            }

            var version = GetVersion();
            var versionString = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
            Logger.Info($"App launch - v{versionString}", null);
            Logger.Info($"StoragePath: {Storage.StoragePath}");

            // Check if its the first launch of the app from a new version.
            var lastLaunchVersion = Settings.Instance.LastLaunchVersion;
            if (lastLaunchVersion != versionString)
            {
                try
                {
                    var manifestPath = Storage.GetManifestPath();
                    if (File.Exists(manifestPath))
                    {
                        var fileInfo = new FileInfo(manifestPath);
                        using (var staticManifestStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DLSS_Swapper.Assets.static_manifest.json"))
                        {
                            if (staticManifestStream is not null)
                            {
                                // If the static manifest is larger than the file, we likely want to replace the current manifest.
                                if (staticManifestStream.Length >= fileInfo.Length)
                                {
                                    using (var fileWriter = File.Create(manifestPath))
                                    {
                                        var length = fileWriter.Length;
                                        staticManifestStream.CopyTo(fileWriter);
                                    }
                                }
                            }
                        }
                    }

                    Settings.Instance.LastLaunchVersion = lastLaunchVersion;
                }
                catch (Exception err)
                {
                    Logger.Error(err, "Unable to perform first launch duties.");
                }
            }

            Database.Instance.Init();

            WindowManager.ShowWindow(MainWindow);

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
