using DLSS_Swapper.Helpers;
using DLSS_Swapper.Attributes;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.Translations.Windows;
public class FailToLaunchWindowTranslationPropertiesViewModel : LocalizedViewModelBase
{
    public FailToLaunchWindowTranslationPropertiesViewModel() : base() { }

    [TranslationProperty] public string ApplicationFailToLaunchWindowText => $"{ResourceHelper.GetString("ApplicationTitle")} - {ResourceHelper.GetString("FailedToLaunch")}";
    [TranslationProperty] public string PleaseOpenIssuePartial1Text => ResourceHelper.GetString("PleaseOpenIssuePartial1");
    [TranslationProperty] public string PleaseOpenIssuePartial2Text => ResourceHelper.GetString("PleaseOpenIssuePartial2");
    [TranslationProperty] public string PleaseOpenIssuePartial3Text => ResourceHelper.GetString("PleaseOpenIssuePartial3");
    [TranslationProperty] public string ClickToCopyDetailsText => ResourceHelper.GetString("ClickToCopyDetails");
    [TranslationProperty] public string DlssSwapperFailedToLaunchText => ResourceHelper.GetString("DlssSwapperFailedToLaunch");

}
