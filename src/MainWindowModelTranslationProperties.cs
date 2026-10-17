using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper;

public class MainWindowModelTranslationProperties : LocalizedViewModelBase
{
    [TranslationProperty]
    public string Title => ResourceHelper.GetString("ApplicationTitle");

    [TranslationProperty]
    public string AppTitleText => ResourceHelper.GetString("ApplicationTitle");

    [TranslationProperty]
    public string NavigationViewItemGamesText => ResourceHelper.GetString("GamesPage_Title");

    [TranslationProperty]
    public string NavigationViewItemLibraryText => ResourceHelper.GetString("LibraryPage_Title");

    [TranslationProperty]
    public string NavigationViewItemAcknowledgementsText => ResourceHelper.GetString("AcknowledgementsPage_Title");

    [TranslationProperty]
    public string LoadingProgressText => ResourceHelper.GetString("General_Loading");

    [TranslationProperty]
    public string UpdateText => ResourceHelper.GetString("General_Update");

    [TranslationProperty]
    public string UpdateAvailableBannerTitleText => ResourceHelper.GetString("MainWindow_UpdateAvailableBannerTitle");


}
