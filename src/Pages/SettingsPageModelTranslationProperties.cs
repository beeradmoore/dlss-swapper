using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.Pages;

public class SettingsPageModelTranslationProperties : LocalizedViewModelBase
{
    [TranslationProperty]
    public string VersionText => $"{ResourceHelper.GetString("General_Version")}:";

    [TranslationProperty]
    public string BuildDateText => $"{ResourceHelper.GetString("SettingsPage_BuildDate")}:";

    [TranslationProperty]
    public string BuildCommitText => $"{ResourceHelper.GetString("SettingsPage_BuildCommit")}:";

    [TranslationProperty]
    public string CopyText => ResourceHelper.GetString("General_Copy");

    [TranslationProperty]
    public string GiveFeedbackInfo => ResourceHelper.GetString("SettingsPage_GiveFeedbackInfo");

    [TranslationProperty]
    public string NetworkTesterText => ResourceHelper.GetString("SettingsPage_OpenNetworkTester");

    [TranslationProperty]
    public string GeneralTroubleshootingGuideText => ResourceHelper.GetString("SettingsPage_GeneralTroubleshootingGuide");

    [TranslationProperty]
    public string DiagnosticsText => ResourceHelper.GetString("SettingsPage_OpenDiagnostics");

    [TranslationProperty]
    public string AcknowledgementsText => ResourceHelper.GetString("SettingsPage_OpenAcknowledgements");

    [TranslationProperty]
    public string AllowDebugDllsInfo => ResourceHelper.GetString("SettingsPage_AllowDebugDllsInfo");

    [TranslationProperty]
    public string AllowUntrustedInfo => ResourceHelper.GetString("SettingsPage_AllowUntrustedInfo");

    [TranslationProperty]
    public string ApplicationRunsInAdministrativeModeInfo => ResourceHelper.GetString("General_ApplicationRunningAsAdmin");

    [TranslationProperty]
    public string WarningText => ResourceHelper.GetString("General_Warning");

    [TranslationProperty]
    public string YourCurrentLogfileText => ResourceHelper.GetString("SettingsPage_YourCurrentLogFile");

    [TranslationProperty]
    public string OpenTranslationToolboxText => ResourceHelper.GetString("SettingsPage_OpenTranslationToolbox");

    [TranslationProperty]
    public string ThemeLightText => ResourceHelper.GetString("SettingsPage_ThemeLight");

    [TranslationProperty]
    public string ThemeDarkText => ResourceHelper.GetString("SettingsPage_ThemeDark");

    [TranslationProperty]
    public string ThemeSystemSettingDefaultText => ResourceHelper.GetString("SettingsPage_ThemeSystemSettingDefault");

    [TranslationProperty]
    public string ThemeModeText => ResourceHelper.GetString("SettingsPage_ThemeMode");

    [TranslationProperty]
    public string GameLibrariesText => ResourceHelper.GetString("SettingsPage_GameLibraries");

    [TranslationProperty]
    public string IgnoredPathsText => ResourceHelper.GetString("SettingsPage_IgnoredPaths");

    [TranslationProperty]
    public string AddIgnoredPathText => ResourceHelper.GetString("SettingsPage_AddIgnoredPath");

    [TranslationProperty]
    public string DLSSOptionsText => ResourceHelper.GetString("SettingsPage_DLSSOptions");

    [TranslationProperty]
    public string DLSSOptionsGlobalPresetText => ResourceHelper.GetString("SettingsPage_DLSSOptions_GlobalPreset");

    [TranslationProperty]
    public string DLSSDeveloperOptionsText => ResourceHelper.GetString("SettingsPage_DLSSDeveloperOptions");

    [TranslationProperty]
    public string ShowOnScreenIndicatorText => ResourceHelper.GetString("SettingsPage_DLSSDeveloperOptions_ShowOnScreenIndicator");

    [TranslationProperty]
    public string VerboseLoggingText => ResourceHelper.GetString("SettingsPage_DLSSDeveloperOptions_VerboseLogging");

    [TranslationProperty]
    public string EnableLoggingToFileText => ResourceHelper.GetString("SettingsPage_DLSSDeveloperOptions_EnableLoggingToFile");

    [TranslationProperty]
    public string EnableLoggingToConsoleWindowText => ResourceHelper.GetString("SettingsPage_DLSSDeveloperOptions_EnableLoggingToConsoleWindow");

    [TranslationProperty]
    public string AllowUntrustedText => ResourceHelper.GetString("SettingsPage_SettingsAllowUntrusted");

    [TranslationProperty]
    public string AllowDebugDllsText => ResourceHelper.GetString("SettingsPage_AllowDebugDlls");

    [TranslationProperty]
    public string ShowOnlyDownloadedDllsText => ResourceHelper.GetString("SettingsPage_ShowOnlyDownloadedDlls");

    [TranslationProperty]
    public string ApliesOnlyToDllPickerNotLibraryText => ResourceHelper.GetString("SettingsPage_AppliesOnlyToDllPickerNotLibrary");

    [TranslationProperty]
    public string CheckForUpdatesText => ResourceHelper.GetString("SettingsPage_SettingsCheckForUpdates");

    [TranslationProperty]
    public string GiveFeedbackText => ResourceHelper.GetString("SettingsPage_GiveFeedback");

    [TranslationProperty]
    public string TroubleshootingText => ResourceHelper.GetString("SettingsPage_Troubleshooting");

    [TranslationProperty]
    public string PageTitle => ResourceHelper.GetString("SettingsPage_Title");

    [TranslationProperty]
    public string LoggingText => ResourceHelper.GetString("SettingsPage_Logging");

    [TranslationProperty]
    public string AboutText => ResourceHelper.GetString("SettingsPage_About");

    [TranslationProperty]
    public string YesText => ResourceHelper.GetString("General_Yes");

    [TranslationProperty]
    public string NoText => ResourceHelper.GetString("General_No");

    [TranslationProperty]
    public string LanguageText => ResourceHelper.GetString("SettingsPage_Language");

    [TranslationProperty]
    public string DLSSPresetInfoTooltipText => ResourceHelper.GetString("GamePage_DLSSPresetInfo_Tooltip");

    [TranslationProperty]
    public string DLSSPresetInfoText => ResourceHelper.GetString("GamePage_DLSSPresetInfo");
}
