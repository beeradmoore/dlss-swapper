using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper;

public class NetworkTesterWindowModelTranslationProperties : LocalizedViewModelBase
{
    [TranslationProperty]
    public string NetworkTesterWindowText => $"{ResourceHelper.GetString("ApplicationTitle")} - {ResourceHelper.GetString("NetworkTesterPage_WindowTitle")}";

    [TranslationProperty]
    public string Test1TitleText => ResourceHelper.GetString("NetworkTesterPage_DiagnosticsTest1Title");

    [TranslationProperty]
    public string Test2TitleText => ResourceHelper.GetString("NetworkTesterPage_DiagnosticsTest2Title");

    [TranslationProperty]
    public string Test3TitleText => ResourceHelper.GetString("NetworkTesterPage_DiagnosticsTest3Title");

    [TranslationProperty]
    public string Test4TitleText => ResourceHelper.GetString("NetworkTesterPage_DiagnosticsTest4Title");

    [TranslationProperty]
    public string Test5TitleText => ResourceHelper.GetString("NetworkTesterPage_DiagnosticsTest5Title");

    [TranslationProperty]
    public string Test6TitleText => ResourceHelper.GetString("NetworkTesterPage_DiagnosticsTest6Title");

    [TranslationProperty]
    public string Test7TitleText => ResourceHelper.GetString("NetworkTesterPage_DiagnosticsTest7Title");

    [TranslationProperty]
    public string Test8TitleText => ResourceHelper.GetString("NetworkTesterPage_DiagnosticsTest8Title");

    [TranslationProperty]
    public string Test9TitleText => ResourceHelper.GetString("NetworkTesterPage_DiagnosticsTest9Title");

    [TranslationProperty]
    public string Test10TitleText => ResourceHelper.GetString("NetworkTesterPage_DiagnosticsTest10Title");

    [TranslationProperty]
    public string Test11TitleText => ResourceHelper.GetString("NetworkTesterPage_DiagnosticsTest11Title");

    [TranslationProperty]
    public string RunTestText => ResourceHelper.GetString("NetworkTesterPage_RunTest");

    [TranslationProperty]
    public string CopyTestResultsText => ResourceHelper.GetString("NetworkTesterPage_CopyTestResults");

    [TranslationProperty]
    public string ResultsText => ResourceHelper.GetString("NetworkTesterPage_Results");

    [TranslationProperty]
    public string CreateBugReportText => ResourceHelper.GetString("NetworkTesterPage_CreateBugReport");

    [TranslationProperty]
    public string CancelCurrentTestText => ResourceHelper.GetString("NetworkTesterPage_CancelCurrentTest");
}
