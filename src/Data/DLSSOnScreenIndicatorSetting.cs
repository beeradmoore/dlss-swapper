using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using DLSS_Swapper.Attributes;
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
    [TranslationProperty] public string Label => ResourceHelper.GetString(LabelLanguageProperty);
    public int Value { get; init; }

    public override string ToString() => Label;

    private void OnLanguageChanged()
    {
        Type currentClassType = GetType();
        IEnumerable<string> languageProperties = LanguageManager.GetClassLanguagePropertyNames(currentClassType);
        foreach (string propertyName in languageProperties)
        {
            OnPropertyChanged(propertyName);
        }
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
