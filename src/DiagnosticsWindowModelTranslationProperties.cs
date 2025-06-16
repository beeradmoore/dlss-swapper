using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper;

public class DiagnosticsWindowModelTranslationProperties : LocalizedViewModelBase
{
    [TranslationProperty]
    public string ApplicationTilteDiagnosticsWindowText => $"{ResourceHelper.GetString("ApplicationTitle")} - {ResourceHelper.GetString("Diagnostics")}";

    [TranslationProperty]
    public string ClickToCopyDetailsText => ResourceHelper.GetString("ClickToCopyDetails");
}
