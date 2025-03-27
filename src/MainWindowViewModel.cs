using System;
using CommunityToolkit.Mvvm.ComponentModel;
using DLSS_Swapper.Helpers;

namespace DLSS_Swapper;
public class MainWindowViewModel : ObservableObject, IDisposable
{
    public MainWindowViewModel()
    {
        _languageManager = LanguageManager.Instance;
        _languageManager.OnLanguageChanged += OnLanguageChanged;
    }

    public string Title => ResourceHelper.GetString("ApplicationTitle");
    public string AppTitleText => ResourceHelper.GetString("ApplicationTitle");
    public string NavigationViewItemGamesText => ResourceHelper.GetString("Games");
    public string NavigationViewItemLibraryText => ResourceHelper.GetString("Library");
    public string LoadingProgressText => ResourceHelper.GetString("Loading");

    private void OnLanguageChanged()
    {
        OnPropertyChanged(Title);
        OnPropertyChanged(AppTitleText);
        OnPropertyChanged(NavigationViewItemGamesText);
        OnPropertyChanged(NavigationViewItemLibraryText);
        OnPropertyChanged(LoadingProgressText);
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
