using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLSS_Swapper.Data;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.UserControls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DLSS_Swapper.Collections;
using System.Collections.Specialized;
using DLSS_Swapper.Data.DLSS;
using Windows.System;
using Windows.ApplicationModel.DataTransfer;

namespace DLSS_Swapper.Pages;

public partial class SettingsPageModel : ObservableObject
{
    readonly WeakReference<SettingsPage> _weakPage;
    readonly DLSSSettingsManager _dlssSettingsManager;
    public string CurrentLogPath => Logger.GetCurrentLogPath();
    public string AppVersion => App.CurrentApp.GetVersionString();

    [ObservableProperty]
    public partial ComboBoxOption SelectedDlssOnScreenIndicator { get; set; }

    public RefreshableObservableCollection<ComboBoxOption> DLSSOnScreenIndicatorOptions { get; init; } = new RefreshableObservableCollection<ComboBoxOption>()
    {
        new ComboBoxOption("General_None", 0),
        new ComboBoxOption("SettingsPage_DLSSDeveloperOptions_IndicatorEnabledForDebugDlssDllOnly", 1),
        new ComboBoxOption("SettingsPage_DLSSDeveloperOptions_IndicatorEnabledForAllDlssDlls", 1024)
    };

    [ObservableProperty]
    public partial PresetOption? SelectedGlobalDlssPreset { get; set; }

    PresetOption? _previousGlobalDlssPreset;

    public List<PresetOption> DlssPresetOptions { get; } = new List<PresetOption>();

    // Setting global preset for DLSS D does not perform as expected so it is currently disabled.
    /*
    [ObservableProperty]
    public partial PresetOption? SelectedGlobalDlssDPreset { get; set; }

    public List<PresetOption> DlssDPresetOptions { get; } = new List<PresetOption>();
    */

    public ObservableCollection<KeyValuePair<string, string>> Languages { get; init; } = new ObservableCollection<KeyValuePair<string, string>>();

    [ObservableProperty]
    public partial KeyValuePair<string, string> SelectedLanguage { get; set; }

    [ObservableProperty]
    public partial bool LightThemeSelected { get; set; } = false;

    [ObservableProperty]
    public partial bool DarkThemeSelected { get; set; } = false;

    [ObservableProperty]
    public partial bool DefaultThemeSelected { get; set; } = false;

    [ObservableProperty]
    public partial bool DlssEnableLogging { get; set; } = false;

    [ObservableProperty]
    public partial bool DlssVerboseLogging { get; set; } = false;

    [ObservableProperty]
    public partial bool DlssLoggingToWindow { get; set; } = false;

    [ObservableProperty]
    public partial bool AllowUntrusted { get; set; } = false;

    [ObservableProperty]
    public partial bool AllowDebugDlls { get; set; } = false;

    [ObservableProperty]
    public partial bool OnlyShowDownloadedDlls { get; set; } = false;

    [ObservableProperty]
    public partial ComboBoxOption LoggingLevel { get; set; }

    public RefreshableObservableCollection<ComboBoxOption> LoggingLevelOptions { get; init; } = new RefreshableObservableCollection<ComboBoxOption>()
    {
        new ComboBoxOption("SettingsPage_Logging_Off", (int)DLSS_Swapper.LoggingLevel.Off),
        new ComboBoxOption("SettingsPage_Logging_Verbose", (int)DLSS_Swapper.LoggingLevel.Verbose),
        new ComboBoxOption("SettingsPage_Logging_Debug", (int)DLSS_Swapper.LoggingLevel.Debug),
        new ComboBoxOption("SettingsPage_Logging_Info", (int)DLSS_Swapper.LoggingLevel.Info),
        new ComboBoxOption("SettingsPage_Logging_Warning", (int)DLSS_Swapper.LoggingLevel.Warning),
        new ComboBoxOption("SettingsPage_Logging_Error", (int)DLSS_Swapper.LoggingLevel.Error),
    };

    [ObservableProperty]
    public partial bool IsCheckingForUpdates { get; set; } = false;

    public ObservableCollection<string> IgnoredPaths { get; set; }

    bool _hasSetDefaults = false;

    public SettingsPageModelTranslationProperties TranslationProperties { get; } = new SettingsPageModelTranslationProperties();

    public SettingsPageModel(SettingsPage page)
    {
        _weakPage = new WeakReference<SettingsPage>(page);

        LanguageManager.Instance.OnLanguageChanged += () =>
        {
            DLSSOnScreenIndicatorOptions.RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

            foreach (var dlssPresetOption in DlssPresetOptions)
            {
                dlssPresetOption.UpdateNameFromTranslation();
            }
        };

        var knownLanguages = LanguageManager.Instance.GetKnownLanguages();
        foreach (var knownLanguage in knownLanguages)
        {
            var languageName = LanguageManager.Instance.GetLanguageName(knownLanguage);
            Languages.Add(new KeyValuePair<string, string>(knownLanguage, languageName));
        }

        //work with selected language state
        SelectedLanguage = Languages.FirstOrDefault(x => x.Key == Settings.Instance.Language);

        _dlssSettingsManager = new DLSSSettingsManager();
        LightThemeSelected = Settings.Instance.AppTheme == ElementTheme.Light;
        DarkThemeSelected = Settings.Instance.AppTheme == ElementTheme.Dark;
        DefaultThemeSelected = Settings.Instance.AppTheme == ElementTheme.Default;

        var dlssShowOnScreenIndicatorIndicator = _dlssSettingsManager.GetShowDlssIndicator();
        SelectedDlssOnScreenIndicator = DLSSOnScreenIndicatorOptions.FirstOrDefault(x => x.Value == dlssShowOnScreenIndicatorIndicator) ?? DLSSOnScreenIndicatorOptions[0];

        var logLevel = _dlssSettingsManager.GetLogLevel();
        if (logLevel == 1)
        {
            DlssEnableLogging = true;
        }
        else if (logLevel == 2)
        {
            DlssEnableLogging = true;
            DlssVerboseLogging = true;
        }

        DlssLoggingToWindow = _dlssSettingsManager.GetLoggingWindow();
        AllowUntrusted = Settings.Instance.AllowUntrusted;
        AllowDebugDlls = Settings.Instance.AllowDebugDlls;
        OnlyShowDownloadedDlls = Settings.Instance.OnlyShowDownloadedDlls;


        var loggingLevel = Settings.Instance.LoggingLevel;
        LoggingLevel = LoggingLevelOptions.FirstOrDefault(x => x.Value == (int)loggingLevel) ?? LoggingLevelOptions.Last();

        IgnoredPaths = new ObservableCollection<string>(Settings.Instance.IgnoredPaths);

        if (NVAPIHelper.Instance.IsSupported)
        {
            DlssPresetOptions.AddRange(NVAPIHelper.Instance.DlssPresetOptions);
            var dlssGlobalPreset = NVAPIHelper.Instance.GetGlobalDLSSPreset();
            if (dlssGlobalPreset.Success)
            {
                SelectedGlobalDlssPreset = NVAPIHelper.Instance.DlssPresetOptions.FirstOrDefault(x => x.Value == dlssGlobalPreset.Result);
            }

            /*
            DlssDPresetOptions.AddRange(NVAPIHelper.Instance.DlssPresetOptions);
            var dlssDGlobalPreset = NVAPIHelper.Instance.GetGlobalDLSSDPreset();
            SelectedGlobalDlssDPreset = NVAPIHelper.Instance.DlssPresetOptions.FirstOrDefault(x => x.Value == dlssDGlobalPreset);
            */
        }
        else
        {
            var notSupportedPresetOption = new PresetOption(ResourceHelper.GetString("General_NotSupported"), 0);
            DlssPresetOptions.Add(notSupportedPresetOption);
            SelectedGlobalDlssPreset = notSupportedPresetOption;
            /*
            DlssDPresetOptions.AddRange(notSupportedPresetOption);
            SelectedGlobalDlssDPreset = notSupportedPresetOption;
            */
        }

        _hasSetDefaults = true;
    }

    partial void OnSelectedGlobalDlssPresetChanging(PresetOption? value)
    {
        _previousGlobalDlssPreset = SelectedGlobalDlssPreset;
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (_hasSetDefaults == false)
        {
            return;
        }

        if (e.PropertyName == nameof(SelectedLanguage))
        {
            Settings.Instance.Language = SelectedLanguage.Key;
            LanguageManager.Instance.ChangeLanguage(SelectedLanguage.Key);
        }
        else if (e.PropertyName == nameof(LightThemeSelected))
        {
            if (LightThemeSelected == true)
            {
                Settings.Instance.AppTheme = ElementTheme.Light;
                ((App)Application.Current).WindowManager.UpdateColors(ElementTheme.Light);
            }
        }
        else if (e.PropertyName == nameof(DarkThemeSelected))
        {
            if (DarkThemeSelected == true)
            {
                Settings.Instance.AppTheme = ElementTheme.Dark;
                ((App)Application.Current).WindowManager.UpdateColors(ElementTheme.Dark);
            }
        }
        else if (e.PropertyName == nameof(DefaultThemeSelected))
        {
            if (DefaultThemeSelected == true)
            {
                Settings.Instance.AppTheme = ElementTheme.Default;
                ((App)Application.Current).WindowManager.UpdateColors(ElementTheme.Default);
            }
        }
        else if (e.PropertyName == nameof(SelectedDlssOnScreenIndicator))
        {
            _dlssSettingsManager.SetShowDlssIndicator(SelectedDlssOnScreenIndicator.Value);
        }
        else if (e.PropertyName == nameof(DlssEnableLogging) || e.PropertyName == nameof(DlssVerboseLogging))
        {
            if (DlssEnableLogging == true)
            {
                if (DlssVerboseLogging == true)
                {
                    _dlssSettingsManager.SetLogLevel(2);
                }
                else
                {
                    _dlssSettingsManager.SetLogLevel(1);
                }
            }
            else
            {
                _dlssSettingsManager.SetLogLevel(0);
            }
        }
        else if (e.PropertyName == nameof(DlssLoggingToWindow))
        {
            _dlssSettingsManager.SetLoggingWindow(DlssLoggingToWindow);
        }
        else if (e.PropertyName == nameof(AllowUntrusted))
        {
            Settings.Instance.AllowUntrusted = AllowUntrusted;
            App.CurrentApp.MainWindow.FilterDLLRecords();
        }
        else if (e.PropertyName == nameof(AllowDebugDlls))
        {
            Settings.Instance.AllowDebugDlls = AllowDebugDlls;
            App.CurrentApp.MainWindow.FilterDLLRecords();
        }
        else if (e.PropertyName == nameof(OnlyShowDownloadedDlls))
        {
            Settings.Instance.OnlyShowDownloadedDlls = OnlyShowDownloadedDlls;
        }
        else if (e.PropertyName == nameof(LoggingLevel))
        {
            var loggingLevel  = (DLSS_Swapper.LoggingLevel)LoggingLevel.Value;
            Settings.Instance.LoggingLevel = loggingLevel;
            Logger.ChangeLoggingLevel(loggingLevel);
        }
        else if (e.PropertyName == nameof(SelectedGlobalDlssPreset))
        {
            if (NVAPIHelper.Instance.IsSupported && SelectedGlobalDlssPreset is not null)
            {
                var setDLSSPresetResult = NVAPIHelper.Instance.SetGlobalDLSSPreset(SelectedGlobalDlssPreset.Value);
                if (setDLSSPresetResult.Success == false)
                {
                    if (_weakPage.TryGetTarget(out var page))
                    {
                        page.DispatcherQueue.TryEnqueue(() =>
                        {
                            SelectedGlobalDlssPreset = _previousGlobalDlssPreset;
                        });

                        _ = NVAPIHelper.Instance.DisplayNVAPIErrorAsync(page.XamlRoot);
                    }
                }
            }
        }
        /*
        else if (e.PropertyName == nameof(SelectedGlobalDlssDPreset))
        {
            if (NVAPIHelper.Instance.Supported && SelectedGlobalDlssDPreset is not null)
            {
                var didSet = NVAPIHelper.Instance.SetGlobalDLSSDPreset(SelectedGlobalDlssDPreset.Value);
                if (didSet == false)
                {
                    if (_weakPage.TryGetTarget(out var page))
                    {
                        var dialog = new EasyContentDialog(page.XamlRoot)
                        {
                            Title = ResourceHelper.GetString("General_Error"),
                            CloseButtonText = ResourceHelper.GetString("General_Okay"),
                            Content = ResourceHelper.GetString("GamePage_UnableToChangePreset"),
                        };
                        _ = dialog.ShowAsync();
                    }
                }
            }
        }
        */
    }

    [RelayCommand]
    async Task CheckForUpdatesAsync()
    {
        IsCheckingForUpdates = true;

        await Task.Delay(2500);
        var githubUpdater = new Data.GitHub.GitHubUpdater();
        var newUpdate = await githubUpdater.CheckForNewGitHubRelease(true);

        if (_weakPage.TryGetTarget(out SettingsPage? settingsPage))
        {
            if (newUpdate is not null)
            {
                await githubUpdater.DisplayNewUpdateDialog(newUpdate, settingsPage.XamlRoot);
            }
            else
            {
                var dialog = new EasyContentDialog(settingsPage.XamlRoot)
                {
                    CloseButtonText = ResourceHelper.GetString("General_Okay"),
                    DefaultButton = ContentDialogButton.Close,
                    Content = ResourceHelper.GetString("SettingsPage_NoNewUpdatesAvailable"),
                };
                await dialog.ShowAsync();

                IsCheckingForUpdates = false;
                return;
            }
        }

        IsCheckingForUpdates = false;
    }

    [RelayCommand]
    async Task OpenLogFileAsync()
    {
        try
        {
            if (File.Exists(CurrentLogPath))
            {
                FileSystemHelper.OpenFolderInExplorerSelectFile(CurrentLogPath);
            }
            else
            {
                FileSystemHelper.OpenFolderInExplorer(Logger.LogDirectory);
            }
        }
        catch (Exception err)
        {
            Logger.Error(err);

            if (_weakPage.TryGetTarget(out SettingsPage? settingsPage))
            {
                var dialog = new EasyContentDialog(settingsPage.XamlRoot)
                {
                    Title = ResourceHelper.GetString("General_Oops"),
                    CloseButtonText = ResourceHelper.GetString("General_Okay"),
                    DefaultButton = ContentDialogButton.Close,
                    Content = ResourceHelper.GetString("SettingsPage_CouldNotOpenLogFileTryManual"),
                };

                await dialog.ShowAsync();
            }
        }
    }

    [RelayCommand]
    void OpenAcknowledgements()
    {
        App.CurrentApp.MainWindow.GoToAcknowledgements();
    }

    [RelayCommand]
    void OpenNetworkTester()
    {
        var networkTesterWindow = new NetworkTesterWindow();
        App.CurrentApp.WindowManager.ShowWindow(networkTesterWindow);
    }

    [RelayCommand]
    void OpenDiagnostics()
    {
        var diagnosticsWindow = new DiagnosticsWindow();
        App.CurrentApp.WindowManager.ShowWindow(diagnosticsWindow);
    }

    [RelayCommand]
    async Task AddIgnoredPathAsync(string path)
    {
        if (_weakPage.TryGetTarget(out SettingsPage? settingsPage))
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.CurrentApp.MainWindow);

            var folderPath = string.Empty;
            try
            {
                var folder = FileSystemHelper.OpenFolder(hWnd, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

                // User cancelled.
                if (string.IsNullOrWhiteSpace(folder))
                {
                    return;
                }

                folderPath = folder;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                var errorDialog = new EasyContentDialog(settingsPage.XamlRoot)
                {
                    Title = ResourceHelper.GetString("General_Error"),
                    CloseButtonText = ResourceHelper.GetString("General_Okay"),
                    DefaultButton = ContentDialogButton.Close,
                    Content = ResourceHelper.GetString("SettingsPage_CouldNotOpenFolderDialog"),
                };
                await errorDialog.ShowAsync();
                return;
            }

            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return;
            }

            // Ensure ends with a directory separator
            if (folderPath.EndsWith(Path.DirectorySeparatorChar) == false)
            {
                folderPath += Path.DirectorySeparatorChar;
            }

            if (IgnoredPaths.Contains(folderPath))
            {
                var errorDialog = new EasyContentDialog(settingsPage.XamlRoot)
                {
                    Title = ResourceHelper.GetString("General_Error"),
                    CloseButtonText = ResourceHelper.GetString("General_Okay"),
                    DefaultButton = ContentDialogButton.Close,
                    Content = ResourceHelper.GetFormattedResourceTemplate("SettingsPage_PathAlreadyIgnored", folderPath),
                };
                await errorDialog.ShowAsync();
                return;
            }

            IgnoredPaths.Add(folderPath);
            Settings.Instance.IgnoredPaths = IgnoredPaths.ToArray();
        }
    }

    [RelayCommand]
    async Task DeleteIgnoredPathAsync(string path)
    {
        if (_weakPage.TryGetTarget(out SettingsPage? settingsPage))
        {
            var dialog = new EasyContentDialog(settingsPage.XamlRoot)
            {
                Title = ResourceHelper.GetString("SettingsPage_DeleteIgnoredPathTitle"),
                CloseButtonText = ResourceHelper.GetString("General_Cancel"),
                DefaultButton = ContentDialogButton.Close,
                PrimaryButtonText = ResourceHelper.GetString("General_Delete"),
                Content = ResourceHelper.GetFormattedResourceTemplate("SettingsPage_DeleteIgnoredPathMessage", path),
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                IgnoredPaths.Remove(path);
                Settings.Instance.IgnoredPaths = IgnoredPaths.ToArray();
            }
        }
    }

    [RelayCommand]
    void OpenTranslationToolbox()
    {
        var translationToolboxWindow = new TranslationToolboxWindow();
        App.CurrentApp.WindowManager.ShowWindow(translationToolboxWindow);
    }

    [RelayCommand]
    async Task DLSSPresetInfoAsync()
    {
        if (_weakPage.TryGetTarget(out var page))
        {
            var dialog = new EasyContentDialog(page.XamlRoot)
            {
                Title = ResourceHelper.GetString("GamePage_DLSSPresetInfo_Title"),
                PrimaryButtonText = ResourceHelper.GetString("General_Okay"),
                SecondaryButtonText = ResourceHelper.GetString("GamePage_DLSSPresetInfo_OnScreenIndicator"),
                DefaultButton = ContentDialogButton.Primary,
                Content = ResourceHelper.GetString("GamePage_DLSSPresetInfo_Message"),
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Secondary)
            {
                await Launcher.LaunchUriAsync(new Uri("https://github.com/beeradmoore/dlss-swapper/wiki/DLSS-Developer-Options#on-screen-indicator"));
            }
        }
    }

    [RelayCommand]
    async Task OpenVersionAsync()
    {
        if (string.IsNullOrWhiteSpace(BuildInfo.GitTag))
        {
            await Launcher.LaunchUriAsync(new Uri("https://github.com/beeradmoore/dlss-swapper/releases"));
        }
        else
        {
            await Launcher.LaunchUriAsync(new Uri($"https://github.com/beeradmoore/dlss-swapper/releases/tag/{BuildInfo.GitTag}"));
        }
    }

    [RelayCommand]
    void CopyGitCommit()
    {
        var package = new DataPackage();
        package.SetText(BuildInfo.GitCommitShort);
        Clipboard.SetContent(package);
    }

    [RelayCommand]
    async Task NVAPIErrorAsync()
    {
        if (_weakPage.TryGetTarget(out var page))
        {
            await NVAPIHelper.Instance.DisplayNVAPIErrorAsync(page.XamlRoot);
        }
    }
}
