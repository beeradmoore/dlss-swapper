using System;
using System.Collections.Generic;
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
        Type currentClassType = GetType();
        IEnumerable<string> languageProperties = LanguageManager.GetClassLanguagePropertyNames(currentClassType);
        foreach (string propertyName in languageProperties)
        {
            OnPropertyChanged(propertyName);
        }
    }

    public void Dispose()
    {
        LanguageManager.Instance.OnLanguageChanged -= OnLanguageChanged;
    }

    ~LocalizedViewModelBase()
    {
        Dispose();
    }
}
