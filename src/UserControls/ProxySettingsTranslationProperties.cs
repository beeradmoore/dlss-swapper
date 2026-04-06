using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.UserControls;

public class ProxySettingsTranslationProperties : LocalizedViewModelBase
{
    [TranslationProperty]
    public string WarningText => ResourceHelper.GetString("General_Warning");

    [TranslationProperty]
    public string WarningMessageText => ResourceHelper.GetString("ProxySettings_WarningMessage");

    [TranslationProperty]
    public string UseProxySettingsText => ResourceHelper.GetString("ProxySettings_UseProxySettings");

    [TranslationProperty]
    public string UseAuthenticationText => ResourceHelper.GetString("ProxySettings_UseAuthentication");

    [TranslationProperty]
    public string ServerText => ResourceHelper.GetString("ProxySettings_Server");

    [TranslationProperty]
    public string UsernameText => ResourceHelper.GetString("ProxySettings_Username");

    [TranslationProperty]
    public string PasswordText => ResourceHelper.GetString("ProxySettings_Password");

    [TranslationProperty]
    public string ServerPrefixWarningText => ResourceHelper.GetString("ProxySettings_ServerPrefixWarning");




    






}
