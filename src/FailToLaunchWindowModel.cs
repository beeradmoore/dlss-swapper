using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLSS_Swapper.Helpers;
using Windows.ApplicationModel.DataTransfer;

namespace DLSS_Swapper;

public partial class FailToLaunchWindowModel : ObservableObject, IDisposable
{
    public FailToLaunchWindowModel()
    {
        var systemDetails = new SystemDetails();
        SystemData = systemDetails.GetSystemData();
        _languageManager = LanguageManager.Instance;
        _languageManager.OnLanguageChanged += OnLanguagechanged;
    }

    public string SystemData { get; set; } = string.Empty;

    [RelayCommand]
    void CopyText()
    {
        var package = new DataPackage();
        package.SetText(SystemData);
        Clipboard.SetContent(package);
    }

    #region TranslationProperties
    public string ApplicationFailToLaunchWindowText => ResourceHelper.GetString("ApplicationTitle") + " - " + ResourceHelper.GetString("FailedToLaunch");
    public string PleaseOpenIssuePartial1Text => ResourceHelper.GetString("PleaseOpenIssuePartial1");
    public string PleaseOpenIssuePartial2Text => ResourceHelper.GetString("PleaseOpenIssuePartial2");
    public string PleaseOpenIssuePartial3Text => ResourceHelper.GetString("PleaseOpenIssuePartial3");
    public string ClickToCopyDetailsText => ResourceHelper.GetString("ClickToCopyDetails");
    public string DlssSwapperFailedToLaunchText => ResourceHelper.GetString("DlssSwapperFailedToLaunch");
    #endregion

    private void OnLanguagechanged()
    {
        OnPropertyChanged(ApplicationFailToLaunchWindowText);
        OnPropertyChanged(PleaseOpenIssuePartial1Text);
        OnPropertyChanged(PleaseOpenIssuePartial2Text);
        OnPropertyChanged(PleaseOpenIssuePartial3Text);
        OnPropertyChanged(ClickToCopyDetailsText);
        OnPropertyChanged(DlssSwapperFailedToLaunchText);
    }

    public void Dispose()
    {
        _languageManager.OnLanguageChanged -= OnLanguagechanged;
    }

    ~FailToLaunchWindowModel()
    {
        Dispose();
    }

    private readonly LanguageManager _languageManager;
}
