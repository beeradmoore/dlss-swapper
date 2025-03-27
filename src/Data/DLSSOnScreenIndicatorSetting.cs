using System;
using CommunityToolkit.Mvvm.ComponentModel;
using DLSS_Swapper.Helpers;

namespace DLSS_Swapper.Data;

public class DLSSOnScreenIndicatorSetting : ObservableObject, IDisposable
{
    public DLSSOnScreenIndicatorSetting(string labelLanguageProperty, int value)
    {
        _languageManager = LanguageManager.Instance;
        _languageManager.OnLanguageChanged += OnLanguageChanged;
        LabelLanguageProperty = labelLanguageProperty;
        Value = value;
    }

    public string LabelLanguageProperty { get; init; } = "None";
    public string Label => ResourceHelper.GetString(LabelLanguageProperty);
    public int Value { get; init; }

    public override string ToString() => Label;

    private void OnLanguageChanged()
    {
        OnPropertyChanged(nameof(Label));
    }

    public void Dispose()
    {
        _languageManager.OnLanguageChanged -= OnLanguageChanged;
    }

    ~DLSSOnScreenIndicatorSetting()
    {
        Dispose();
    }

    private readonly LanguageManager _languageManager;
}
