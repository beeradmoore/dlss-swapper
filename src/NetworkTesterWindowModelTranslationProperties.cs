using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper;

public class NetworkTesterWindowModelTranslationProperties : LocalizedViewModelBase
{
    [TranslationProperty]
    public string NetworkTesterWindowText => $"{ResourceHelper.GetString("ApplicationTitle")} - {ResourceHelper.GetString("NetworkTester")}";

    [TranslationProperty]
    public string Test1TitleText => ResourceHelper.GetString("DiagnosticsTest1Title");

    [TranslationProperty]
    public string Test2TitleText => ResourceHelper.GetString("DiagnosticsTest2Title");

    [TranslationProperty]
    public string Test3TitleText => ResourceHelper.GetString("DiagnosticsTest3Title");

    [TranslationProperty]
    public string Test4TitleText => ResourceHelper.GetString("DiagnosticsTest4Title");

    [TranslationProperty]
    public string Test5TitleText => ResourceHelper.GetString("DiagnosticsTest5Title");

    [TranslationProperty]
    public string Test6TitleText => ResourceHelper.GetString("DiagnosticsTest6Title");

    [TranslationProperty]
    public string Test7TitleText => ResourceHelper.GetString("DiagnosticsTest7Title");

    [TranslationProperty]
    public string Test8TitleText => ResourceHelper.GetString("DiagnosticsTest8Title");

    [TranslationProperty]
    public string Test9TitleText => ResourceHelper.GetString("DiagnosticsTest9Title");

    [TranslationProperty]
    public string Test10TitleText => ResourceHelper.GetString("DiagnosticsTest10Title");

    [TranslationProperty]
    public string Test11TitleText => ResourceHelper.GetString("DiagnosticsTest11Title");

    [TranslationProperty]
    public string RunTestText => ResourceHelper.GetString("RunTest");

    [TranslationProperty]
    public string CopyTestResultsText => ResourceHelper.GetString("CopyTestResults");

    [TranslationProperty]
    public string ResultsText => ResourceHelper.GetString("Results");

    [TranslationProperty]
    public string CreateBugReportText => ResourceHelper.GetString("CreateBugReport");

    [TranslationProperty]
    public string CancelCurrentTestText => ResourceHelper.GetString("CancelCurrentTest");
}
