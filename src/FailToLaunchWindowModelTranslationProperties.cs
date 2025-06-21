using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper;

public class FailToLaunchWindowModelTranslationProperties : LocalizedViewModelBase
{
    [TranslationProperty]
    public string ApplicationFailToLaunchWindowText => $"{ResourceHelper.GetString("ApplicationTitle")} - {ResourceHelper.GetString("FailedToLaunchPage_WindowTitle")}";

    [TranslationProperty]
    public string PleaseOpenIssuePartial1Text => ResourceHelper.GetString("FailedToLaunchPage_PleaseOpenIssuePartial1");

    [TranslationProperty]
    public string PleaseOpenIssuePartial2Text => ResourceHelper.GetString("FailedToLaunchPage_PleaseOpenIssuePartial2");

    [TranslationProperty]
    public string PleaseOpenIssuePartial3Text => ResourceHelper.GetString("FailedToLaunchPage_PleaseOpenIssuePartial3");

    [TranslationProperty]
    public string ClickToCopyDetailsText => ResourceHelper.GetString("DiagnosticsPage_ClickToCopyDetails");

    [TranslationProperty]
    public string DlssSwapperFailedToLaunchText => ResourceHelper.GetString("FailedToLaunchPage_DlssSwapperFailedToLaunch");
}
