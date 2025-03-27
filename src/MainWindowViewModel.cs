using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;

namespace DLSS_Swapper;
public class MainWindowViewModel : ObservableObject, IDisposable
{
    public MainWindowViewModel()
    {
        _languageManager = LanguageManager.Instance;
        _languageManager.OnLanguageChanged += OnLanguageChanged;
    }

    [LanguageProperty] public string Title => ResourceHelper.GetString("ApplicationTitle");
    [LanguageProperty] public string AppTitleText => ResourceHelper.GetString("ApplicationTitle");
    [LanguageProperty] public string NavigationViewItemGamesText => ResourceHelper.GetString("Games");
    [LanguageProperty] public string NavigationViewItemLibraryText => ResourceHelper.GetString("Library");
    [LanguageProperty] public string LoadingProgressText => ResourceHelper.GetString("Loading");

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

    ~MainWindowViewModel()
    {
        Dispose();
    }

    private readonly LanguageManager _languageManager;
}
