using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.Pages;

public class GameGridPageModelTranslationProperties : LocalizedViewModelBase
{
    [TranslationProperty]
    public string NewDllsText => ResourceHelper.GetString("NewDlls");

    [TranslationProperty]
    public string AddGameText => ResourceHelper.GetString("AddGame");

    [TranslationProperty]
    public string RefreshText => ResourceHelper.GetString("General_Refresh");

    [TranslationProperty]
    public string FilterText => ResourceHelper.GetString("Filter");

    [TranslationProperty]
    public string SearchText => ResourceHelper.GetString("Search");

    [TranslationProperty]
    public string ViewTypeText => ResourceHelper.GetString("ViewType");

    [TranslationProperty]
    public string GridViewText => ResourceHelper.GetString("GridView");

    [TranslationProperty]
    public string ListViewText => ResourceHelper.GetString("ListView");

    [TranslationProperty]
    public string PageTitle => ResourceHelper.GetString("GamesPage_Title");

    [TranslationProperty]
    public string ApplicationRunsInAdministrativeModeInfo => ResourceHelper.GetString("General_ApplicationRunningAsAdmin");
}
