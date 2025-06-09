using System;
using CommunityToolkit.Mvvm.ComponentModel;
using DLSS_Swapper.Helpers;

namespace DLSS_Swapper.Interfaces;

public abstract class LocalizedViewModelBase : ObservableObject, IDisposable
{
    public LocalizedViewModelBase()
    {
        LanguageManager.Instance.OnLanguageChanged += OnLanguageChanged;
    }

    protected virtual void OnLanguageChanged()
    {
        var currentClassType = GetType();
        var languageProperties = LanguageManager.GetClassLanguagePropertyNames(currentClassType);
        foreach (var propertyName in languageProperties)
        {
            OnPropertyChanged(propertyName);
        }
    }

    ~LocalizedViewModelBase()
    {
        Dispose();
    }

    public void Dispose()
    {
        LanguageManager.Instance.OnLanguageChanged -= OnLanguageChanged;
    }
}
