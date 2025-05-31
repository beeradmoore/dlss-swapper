using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using DLSS_Swapper.Helpers;

namespace DLSS_Swapper.Interfaces;

public abstract class LocalizedViewModelBase : ObservableObject, IDisposable
{
    protected readonly LanguageManager _languageManager;

    public LocalizedViewModelBase()
    {
        _languageManager = LanguageManager.Instance;
        _languageManager.OnLanguageChanged += OnLanguageChanged;
    }

    public LocalizedViewModelBase(LanguageManager languageManager)
    {
        _languageManager = languageManager;
        _languageManager.OnLanguageChanged += OnLanguageChanged;
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
        _languageManager.OnLanguageChanged -= OnLanguageChanged;
    }

    ~LocalizedViewModelBase()
    {
        Dispose();
    }
}
