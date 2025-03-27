using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper;
public class MainWindowViewModel : LocalizedViewModelBase
{
    public MainWindowViewModel() : base() { }

    [TranslationProperty] public string Title => ResourceHelper.GetString("ApplicationTitle");
    [TranslationProperty] public string AppTitleText => ResourceHelper.GetString("ApplicationTitle");
    [TranslationProperty] public string NavigationViewItemGamesText => ResourceHelper.GetString("Games");
    [TranslationProperty] public string NavigationViewItemLibraryText => ResourceHelper.GetString("Library");
    [TranslationProperty] public string LoadingProgressText => ResourceHelper.GetString("Loading");

}
