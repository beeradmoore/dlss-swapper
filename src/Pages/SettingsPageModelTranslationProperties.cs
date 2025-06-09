using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.Pages;

public class SettingsPageModelTranslationProperties : LocalizedViewModelBase
{
    [TranslationProperty]
    public string VersionText => $"{ResourceHelper.GetString("Version")}:";

    [TranslationProperty]
    public string GiveFeedbackInfo => ResourceHelper.GetString("SettingsGiveFeedbackInfo");

    [TranslationProperty]
    public string NetworkTesterText => ResourceHelper.GetString("NetworkTester");

    [TranslationProperty]
    public string GeneralTroubleshootingGuideText => ResourceHelper.GetString("SettingsGeneralTroubleshootingGuide");

    [TranslationProperty]
    public string DiagnosticsText => ResourceHelper.GetString("Diagnostics");

    [TranslationProperty]
    public string AcknowledgementsText => ResourceHelper.GetString("Acknowledgements");

    [TranslationProperty]
    public string AllowDebugDllsInfo => ResourceHelper.GetString("SettingsAllowDebugDllsInfo");

    [TranslationProperty]
    public string AllowUntrustedInfo => ResourceHelper.GetString("SettingsAllowUntrustedInfo");

    [TranslationProperty]
    public string ApplicationRunsInAdministrativeModeInfo => ResourceHelper.GetString("ApplicationRunsInAdministrativeModeInfo");

    [TranslationProperty]
    public string WarningText => ResourceHelper.GetString("Warning");

    [TranslationProperty]
    public string YourCurrentLogfileText => ResourceHelper.GetString("SettingsYourCurrentLogfile");

    [TranslationProperty]
    public string ThemeLightText => ResourceHelper.GetString("SettingsThemeLight");

    [TranslationProperty]
    public string ThemeDarkText => ResourceHelper.GetString("SettingsThemeDark");

    [TranslationProperty]
    public string ThemeSystemSettingDefaultText => ResourceHelper.GetString("SettingsThemeSystemSettingDefault");

    [TranslationProperty]
    public string ThemeModeText => ResourceHelper.GetString("SettingsThemeMode");

    [TranslationProperty]
    public string GameLibrariesText => ResourceHelper.GetString("SettingsGameLibraries");

    [TranslationProperty]
    public string DllsDeveloperOptionsText => ResourceHelper.GetString("SettingsDllsDeveloperOptions");

    [TranslationProperty]
    public string ShowOnScreenIndicatorText => ResourceHelper.GetString("SettingsShowOnScreenIndicator");

    [TranslationProperty]
    public string VerboseLoggingText => ResourceHelper.GetString("SettingsVerboseLogging");

    [TranslationProperty]
    public string EnableLoggingToFileText => ResourceHelper.GetString("EnableLoggingToFile");

    [TranslationProperty]
    public string EnableLoggingToConsoleWindowText => ResourceHelper.GetString("SettingsEnableLoggingToConsoleWindow");

    [TranslationProperty]
    public string AllowUntrustedText => ResourceHelper.GetString("SettingsAllowUntrusted");

    [TranslationProperty]
    public string AllowDebugDllsText => ResourceHelper.GetString("SettingsAllowDebugDlls");

    [TranslationProperty]
    public string ShowOnlyDownloadedDllsText => ResourceHelper.GetString("SettingsShowOnlyDownloadedDlls");

    [TranslationProperty]
    public string ApliesOnlyToDllPickerNotLibraryText => ResourceHelper.GetString("SettingsApliesOnlyToDllPickerNotLibrary");

    [TranslationProperty]
    public string CheckForUpdatesText => ResourceHelper.GetString("SettingsCheckForUpdates");

    [TranslationProperty]
    public string GiveFeedbackText => ResourceHelper.GetString("SettingsGiveFeedback");

    [TranslationProperty]
    public string TroubleshootingText => ResourceHelper.GetString("SettingsTroubleshooting");

    [TranslationProperty]
    public string SettingsText => ResourceHelper.GetString("Settings");

    [TranslationProperty]
    public string LoggingText => ResourceHelper.GetString("SettingsLogging");

    [TranslationProperty]
    public string AboutText => ResourceHelper.GetString("SettingsAbout");

    [TranslationProperty]
    public string YesText => ResourceHelper.GetString("SettingsYes");

    [TranslationProperty]
    public string NoText => ResourceHelper.GetString("SettingsNo");

    [TranslationProperty]
    public string LanguageText => ResourceHelper.GetString("SettingsLanguage");
}
