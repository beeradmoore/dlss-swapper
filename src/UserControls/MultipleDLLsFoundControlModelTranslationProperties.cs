using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.UserControls;

public class MultipleDLLsFoundControlModelTranslationProperties : LocalizedViewModelBase
{
    [TranslationProperty]
    public string BelowMultipleDllFoundYouWillBeAbleToSwapInfo => ResourceHelper.GetString("GamePage_MultipleDllFound_Notice");

    [TranslationProperty]
    public string OpenDllLocationText => ResourceHelper.GetString("GamePage_OpenDllLocation");
}
