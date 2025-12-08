using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.Data;

public class ComboBoxOption : LocalizedViewModelBase
{
    public string LabelTranslationProperty { get; init; }

    [TranslationProperty]
    public string Label => ResourceHelper.GetString(LabelTranslationProperty);

    public int Value { get; init; }

    public ComboBoxOption(string labelLanguageProperty, int value)
    {
        LabelTranslationProperty = labelLanguageProperty;
        Value = value;
    }

    public override string ToString()
    {
        return Label;
    }
}
