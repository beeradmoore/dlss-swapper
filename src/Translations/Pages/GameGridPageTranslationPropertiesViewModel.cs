using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.Translations.Pages;
public class GameGridPageTranslationPropertiesViewModel : LocalizedViewModelBase
{
    public GameGridPageTranslationPropertiesViewModel() : base() { }

    [TranslationProperty] public string NewDllsText => ResourceHelper.GetString("NewDlls");
    [TranslationProperty] public string AddGameText => ResourceHelper.GetString("AddGame");
    [TranslationProperty] public string RefreshText => ResourceHelper.GetString("Refresh");
    [TranslationProperty] public string FilterText => ResourceHelper.GetString("Filter");
    [TranslationProperty] public string SearchText => ResourceHelper.GetString("Search");
    [TranslationProperty] public string ViewTypeText => ResourceHelper.GetString("ViewType");
    [TranslationProperty] public string GridViewText => ResourceHelper.GetString("GridView");
    [TranslationProperty] public string ListViewText => ResourceHelper.GetString("ListView");
    [TranslationProperty] public string GamesText => ResourceHelper.GetString("Games");
    [TranslationProperty] public string ApplicationRunsInAdministrativeModeInfo => ResourceHelper.GetString("ApplicationRunsInAdministrativeModeInfo");
}
