using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.UserControls;
internal class ImportDLLSummaryControlModelTranslationProperties : LocalizedViewModelBase
{
    //public ImportDLLSummaryControlModelTranslationProperties() : base() { }

    [TranslationProperty]
    public string SuccessText => $"{ResourceHelper.GetString("Success")}: ";

    [TranslationProperty]
    public string FailedText => $"{ResourceHelper.GetString("Failed")}: ";
}
