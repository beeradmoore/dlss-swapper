using System;
using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.Translations;
public class SharedTranslationPropertiesViewModel : LocalizedViewModelBase
{
    private SharedTranslationPropertiesViewModel() { }

    public static SharedTranslationPropertiesViewModel Instance => _instance.Value;

    [TranslationProperty] public string Sample => ResourceHelper.GetString("Sample");

    private static readonly Lazy<SharedTranslationPropertiesViewModel> _instance = new Lazy<SharedTranslationPropertiesViewModel>(() => new SharedTranslationPropertiesViewModel());
}
