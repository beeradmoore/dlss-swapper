using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.Data;

public class DLSSOnScreenIndicatorSetting : LocalizedViewModelBase
{
    public DLSSOnScreenIndicatorSetting(string labelLanguageProperty, int value)
    {
        LabelTranslationProperty = labelLanguageProperty;
        Value = value;
    }

    public string LabelTranslationProperty { get; init; } = "None";
    [TranslationProperty] public string Label => ResourceHelper.GetString(LabelTranslationProperty);
    public int Value { get; init; }

    public override string ToString() => Label;
}
