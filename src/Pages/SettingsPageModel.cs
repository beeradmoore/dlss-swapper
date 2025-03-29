using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
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
using DLSS_Swapper.Translations.Pages;
using DLSS_Swapper.Collections;
using System.Collections.Specialized;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.Pages;

internal partial class SettingsPageModel : LocalizedViewModelBase
{
    readonly WeakReference<SettingsPage> _weakPage;
    readonly DLSSSettingsManager _dlssSettingsManager;

    public IEnumerable<LoggingLevel> LoggingLevels => Enum.GetValues<LoggingLevel>();
    public string CurrentLogPath => Logger.GetCurrentLogPath();
    public string AppVersion => App.CurrentApp.GetVersionString();

    [ObservableProperty]
    public partial DLSSOnScreenIndicatorSetting SelectedDlssOnScreenIndicator { get; set; }

    public RefreshableObservableCollection<DLSSOnScreenIndicatorSetting> DLSSOnScreenIndicatorOptions { get; init; } = new RefreshableObservableCollection<DLSSOnScreenIndicatorSetting>()
    {
        new DLSSOnScreenIndicatorSetting("None", 0),
        new DLSSOnScreenIndicatorSetting("EnabledForDebugDlssDllOnly", 1),
        new DLSSOnScreenIndicatorSetting("EnabledForAllDlssDlls", 1024)
    };

    public ObservableCollection<KeyValuePair<string, string>> Languages { get; init; } = new ObservableCollection<KeyValuePair<string, string>>()
    {
        KeyValuePair.Create("en-US", "English"),
        KeyValuePair.Create("pl-PL", "Polish")
    };

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
    public partial LoggingLevel LoggingLevel { get; set; } = LoggingLevel.Error;

    [ObservableProperty]
    public partial bool IsCheckingForUpdates { get; set; } = false;

    bool _hasSetDefaults = false;

    public SettingsPageModel(SettingsPage page) : base()
    {
        _languageManager = base._languageManager;
        TranslationProperties = new SettingsPageTranslationPropertiesViewModel();
        _weakPage = new WeakReference<SettingsPage>(page);
        //work with selected language state
        SelectedLanguage = Languages.FirstOrDefault(x => x.Key == CultureInfo.CurrentCulture.Name);

        _dlssSettingsManager = new DLSSSettingsManager();
        LightThemeSelected = Settings.Instance.AppTheme == ElementTheme.Light;
        DarkThemeSelected = Settings.Instance.AppTheme == ElementTheme.Dark;
        DefaultThemeSelected = Settings.Instance.AppTheme == ElementTheme.Default;

        var dlssShowOnScreenIndicatorIndicator = _dlssSettingsManager.GetShowDlssIndicator();
        SelectedDlssOnScreenIndicator = DLSSOnScreenIndicatorOptions.FirstOrDefault(x => x.Value == dlssShowOnScreenIndicatorIndicator);

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
        LoggingLevel = Settings.Instance.LoggingLevel;

        _hasSetDefaults = true;
    }

    [ObservableProperty]
    public partial SettingsPageTranslationPropertiesViewModel TranslationProperties { get; private set; }

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
            _languageManager.ChangeLanguage(SelectedLanguage.Key);
        }
        else if (e.PropertyName == nameof(LightThemeSelected))
        {
            if (LightThemeSelected == true)
            {
                Settings.Instance.AppTheme = ElementTheme.Light;
                ((App)Application.Current).MainWindow.UpdateColors(ElementTheme.Light);
            }
        }
        else if (e.PropertyName == nameof(DarkThemeSelected))
        {
            if (DarkThemeSelected == true)
            {
                Settings.Instance.AppTheme = ElementTheme.Dark;
                ((App)Application.Current).MainWindow.UpdateColors(ElementTheme.Dark);
            }
        }
        else if (e.PropertyName == nameof(DefaultThemeSelected))
        {
            if (DefaultThemeSelected == true)
            {
                Settings.Instance.AppTheme = ElementTheme.Default;
                ((App)Application.Current).MainWindow.UpdateColors(ElementTheme.Default);
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
            Settings.Instance.LoggingLevel = LoggingLevel;
            Logger.ChangeLoggingLevel(LoggingLevel);
        }
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
                    CloseButtonText = ResourceHelper.GetString("Okay"),
                    DefaultButton = ContentDialogButton.Close,
                    Content = ResourceHelper.GetString("NoNewUpdatesAvailable"),
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
                Process.Start("explorer.exe", $"/select,{CurrentLogPath}");
            }
            else
            {
                Process.Start("explorer.exe", Logger.LogDirectory);
            }
        }
        catch (Exception err)
        {
            Logger.Error(err);

            if (_weakPage.TryGetTarget(out SettingsPage? settingsPage))
            {
                var dialog = new EasyContentDialog(settingsPage.XamlRoot)
                {
                    Title = ResourceHelper.GetString("Oops"),
                    CloseButtonText = ResourceHelper.GetString("Okay"),
                    DefaultButton = ContentDialogButton.Close,
                    Content = ResourceHelper.GetString("CouldNotOpenLogFileTryManual"),
                };

                await dialog.ShowAsync();
            }
        }
    }

    [RelayCommand]
    void OpenAcknowledgements()
    {
        // TODO: 
    }

    [RelayCommand]
    void OpenNetworkTester()
    {
        var networkTesterWindow = new NetworkTesterWindow();
        networkTesterWindow.Activate();
    }

    [RelayCommand]
    void OpenDiagnostics()
    {
        var diagnosticsWindow = new DiagnosticsWindow();
        diagnosticsWindow.Activate();
    }

    protected override void OnLanguageChanged()
    {
        DLSSOnScreenIndicatorOptions.RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    private readonly LanguageManager _languageManager;
}
