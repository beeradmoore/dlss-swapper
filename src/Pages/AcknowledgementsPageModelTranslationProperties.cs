using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.Pages;

public class AcknowledgementsPageModelTranslationProperties : LocalizedViewModelBase
{
    [TranslationProperty]
    public string LicencesAndAcknowledgementsText => ResourceHelper.GetString("AcknowledgementsPage_Title");
}
