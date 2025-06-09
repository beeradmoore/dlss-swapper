using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper;

public class TranslationToolsWindowModelTranslationProperties : LocalizedViewModelBase
{
    [TranslationProperty]
    public string ApplicationTilteDiagnosticsWindowText => $"{ResourceHelper.GetString("ApplicationTitle")}"; //  - {ResourceHelper.GetString("Diagnostics")}";
}
