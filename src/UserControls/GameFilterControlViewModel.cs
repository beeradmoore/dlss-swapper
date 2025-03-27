using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;

namespace DLSS_Swapper.UserControls;
public class GameFilterControlViewModel : ObservableObject, IDisposable
{
    public GameFilterControlViewModel()
    {
        _languageManager = LanguageManager.Instance;
        _languageManager.OnLanguageChanged += OnLanguageChanged;
    }

    #region LanguageProperties
    [LanguageProperty] public string OptionsText => ResourceHelper.GetString("Options") + ":";
    [LanguageProperty] public string GroupingText => ResourceHelper.GetString("Grouping") + ":";
    [LanguageProperty] public string HideGamesWithNoSwappableItemsText => ResourceHelper.GetString("HideGamesWithNoSwappableItems");
    [LanguageProperty] public string GroupGamesFromTheSameLibraryTogetherText => ResourceHelper.GetString("GroupGamesFromTheSameLibraryTogether");
    #endregion

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

    ~GameFilterControlViewModel()
    {
        Dispose();
    }

    private readonly LanguageManager _languageManager;
}
