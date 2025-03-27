using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper;
public class MainWindowViewModel : LocalizedViewModelBase
{
    public MainWindowViewModel() : base() { }

    [LanguageProperty] public string Title => ResourceHelper.GetString("ApplicationTitle");
    [LanguageProperty] public string AppTitleText => ResourceHelper.GetString("ApplicationTitle");
    [LanguageProperty] public string NavigationViewItemGamesText => ResourceHelper.GetString("Games");
    [LanguageProperty] public string NavigationViewItemLibraryText => ResourceHelper.GetString("Library");
    [LanguageProperty] public string LoadingProgressText => ResourceHelper.GetString("Loading");

}
