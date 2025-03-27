using CommunityToolkit.Mvvm.Input;
using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;
using Windows.ApplicationModel.DataTransfer;

namespace DLSS_Swapper;

public partial class FailToLaunchWindowModel : LocalizedViewModelBase
{
    public FailToLaunchWindowModel() : base()
    {
        var systemDetails = new SystemDetails();
        SystemData = systemDetails.GetSystemData();
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
    [LanguageProperty] public string ApplicationFailToLaunchWindowText => $"{ResourceHelper.GetString("ApplicationTitle")} - {ResourceHelper.GetString("FailedToLaunch")}";
    [LanguageProperty] public string PleaseOpenIssuePartial1Text => ResourceHelper.GetString("PleaseOpenIssuePartial1");
    [LanguageProperty] public string PleaseOpenIssuePartial2Text => ResourceHelper.GetString("PleaseOpenIssuePartial2");
    [LanguageProperty] public string PleaseOpenIssuePartial3Text => ResourceHelper.GetString("PleaseOpenIssuePartial3");
    [LanguageProperty] public string ClickToCopyDetailsText => ResourceHelper.GetString("ClickToCopyDetails");
    [LanguageProperty] public string DlssSwapperFailedToLaunchText => ResourceHelper.GetString("DlssSwapperFailedToLaunch");
    #endregion
}
