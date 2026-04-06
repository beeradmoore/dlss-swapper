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


}
