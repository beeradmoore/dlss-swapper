using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.UserControls;

public class ManuallyAddGameModelTranslationProperties : LocalizedViewModelBase
{
    [TranslationProperty]
    public string AddCoverText => ResourceHelper.GetString("AddCover");

    [TranslationProperty]
    public string OptionalText => ResourceHelper.GetString("Optional");

    [TranslationProperty]
    public string NameText => ResourceHelper.GetString("Name");

    [TranslationProperty]
    public string InstallPathText => ResourceHelper.GetString("InstallPath");
}
