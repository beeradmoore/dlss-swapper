using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.UserControls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DLSS_Swapper.Pages;

internal partial class SettingsPageModel : ObservableObject
{
    readonly WeakReference<SettingsPage> _weakPage;
    readonly DLSSSettingsManager _dlssSettingsManager;

    public IEnumerable<LoggingLevel> LoggingLevels => Enum.GetValues<LoggingLevel>();
    public string CurrentLogPath => Logger.GetCurrentLogPath();
    public string AppVersion => App.CurrentApp.GetVersionString();

    [ObservableProperty]
    bool _lightThemeSelected = false;

    [ObservableProperty]
    bool _darkThemeSelected = false;

    [ObservableProperty]
    bool _defaultThemeSelected = false;

    [ObservableProperty]
    bool _dlssShowIndicator = false;

    [ObservableProperty]
    bool _dlssEnableLogging = false;
    
    [ObservableProperty]
    bool _dlssVerboseLogging = false;
    
    [ObservableProperty]
    bool _dlssLoggingToWindow = false;

    [ObservableProperty]
    bool _allowUntrusted = false;

    [ObservableProperty]
    bool _allowDebugDlls = false;

    [ObservableProperty]
    LoggingLevel _loggingLevel = LoggingLevel.Error;

    [ObservableProperty]
    bool _isCheckingForUpdates = false;

    public SettingsPageModel(SettingsPage page)
    {
        _weakPage = new WeakReference<SettingsPage>(page);

        _dlssSettingsManager = new DLSSSettingsManager();

        _lightThemeSelected = Settings.Instance.AppTheme == ElementTheme.Light;
        _darkThemeSelected = Settings.Instance.AppTheme == ElementTheme.Dark;
        _defaultThemeSelected = Settings.Instance.AppTheme == ElementTheme.Default;

        // Load DLSS Developer Settings from registry.
        _dlssShowIndicator = _dlssSettingsManager.GetShowDlssIndicator();
        var logLevel = _dlssSettingsManager.GetLogLevel();
        if (logLevel == 1)
        {
            _dlssEnableLogging = true;
        }
        else if (logLevel == 2)
        {
            _dlssEnableLogging = true;
            _dlssVerboseLogging = true;
        }
        _dlssLoggingToWindow = _dlssSettingsManager.GetLoggingWindow();


        _allowUntrusted = Settings.Instance.AllowUntrusted;
        _allowDebugDlls = Settings.Instance.AllowDebugDlls;
        _loggingLevel = Settings.Instance.LoggingLevel;
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);


        if (e.PropertyName == nameof(LightThemeSelected))
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
        else if (e.PropertyName == nameof(DlssShowIndicator))
        {
            _dlssSettingsManager.SetShowDlssIndicator(DlssShowIndicator);
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
        var newUpdate = await githubUpdater.CheckForNewGitHubRelease();

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
                    CloseButtonText = "Okay",
                    DefaultButton = ContentDialogButton.Close,
                    Content = "No new updates are available.",
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
            Logger.Error(err.Message);

            if (_weakPage.TryGetTarget(out SettingsPage? settingsPage))
            {
                var dialog = new EasyContentDialog(settingsPage.XamlRoot)
                {
                    Title = "Oops",
                    CloseButtonText = "Okay",
                    DefaultButton = ContentDialogButton.Close,
                    Content = "Could not open your log file directly from DLSS Swapper. Please try open it manually.",
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


}
