using System;
using CommunityToolkit.Mvvm.ComponentModel;
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
    public string OptionsText => ResourceHelper.GetString("Options") + ":";
    public string GroupingText => ResourceHelper.GetString("Grouping") + ":";
    public string HideGamesWithNoSwappableItemsText => ResourceHelper.GetString("HideGamesWithNoSwappableItems");
    public string GroupGamesFromTheSameLibraryTogetherText => ResourceHelper.GetString("GroupGamesFromTheSameLibraryTogether");
    #endregion

    private void OnLanguageChanged()
    {
        OnPropertyChanged(nameof(OptionsText));
        OnPropertyChanged(nameof(GroupingText));
        OnPropertyChanged(nameof(HideGamesWithNoSwappableItemsText));
        OnPropertyChanged(nameof(GroupGamesFromTheSameLibraryTogetherText));
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
