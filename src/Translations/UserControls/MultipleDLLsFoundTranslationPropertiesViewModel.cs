using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.Translations.UserControls;
public class MultipleDLLsFoundTranslationPropertiesViewModel : LocalizedViewModelBase
{
    public MultipleDLLsFoundTranslationPropertiesViewModel() : base() { }

    [TranslationProperty] public string BelowMultipleDllFoundYouWillBeAbleToSwapInfo => ResourceHelper.GetString("BelowMultipleDllFoundYouWillBeAbleToSwapInfo");

    [TranslationProperty] public string OpenDllLocationText => ResourceHelper.GetString("OpenDllLocation");
}
