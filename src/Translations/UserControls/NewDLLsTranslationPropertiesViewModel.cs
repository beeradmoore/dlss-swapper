using System;
using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.Translations.UserControls;
public class NewDLLsTranslationPropertiesViewModel : LocalizedViewModelBase
{
    public NewDLLsTranslationPropertiesViewModel() : base() { }

    [TranslationProperty] public string Title => $"{ResourceHelper.GetString("NewDllFoundOn")} {DateTime.Now.ToString("yyyy-MM-dd")}";
    [TranslationProperty] public string WhileLoadingYourGameNewDllsDiscoveredHelpUsText => ResourceHelper.GetString("WhileLoadingYourGameNewDllsDiscoveredHelpUs");
    [TranslationProperty] public string StepOneCreateNewIssueText => ResourceHelper.GetString("StepOneCreateNewIssue");
    [TranslationProperty] public string CreateNewGithubIssueText => ResourceHelper.GetString("CreateNewGithubIssue");
    [TranslationProperty] public string GithubAccountRequiredText => ResourceHelper.GetString("GithubAccountRequired");
    [TranslationProperty] public string IfButtonDoesntWorkTryHereText => ResourceHelper.GetString("IfButtonDoesntWorkTryHere");
    [TranslationProperty] public string StepTwoCopyTitleText => ResourceHelper.GetString("StepTwoCopyTitle");
    [TranslationProperty] public string CopyText => ResourceHelper.GetString("Copy");
    [TranslationProperty] public string StepThreeCopyBodyText => ResourceHelper.GetString("StepThreeCopyBody");
    [TranslationProperty] public string StepFourSubmitYourIssueText => ResourceHelper.GetString("StepFourSubmitYourIssue");
    [TranslationProperty] public string YouDoNotHaveToSubmitDllAutoTrackText => ResourceHelper.GetString("YouDoNotHaveToSubmitDllAutoTrack");
    [TranslationProperty] public string ThanksForHelpingText => ResourceHelper.GetString("ThanksForHelping");
}
