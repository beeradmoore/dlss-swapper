using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.UserControls;

public class GameControlModelTranslationProperties : LocalizedViewModelBase
{
    [TranslationProperty]
    public string RemoveText => ResourceHelper.GetString("General_Remove");

    [TranslationProperty]
    public string AddCustomCoverText => ResourceHelper.GetString("GamePage_AddCustomCover");

    [TranslationProperty]
    public string GameNotReadyToPlayStateText => ResourceHelper.GetString("GamePage_NotReadyToPlayState");

    [TranslationProperty]
    public string HelpText => ResourceHelper.GetString("General_Help");

    [TranslationProperty]
    public string NameText => ResourceHelper.GetString("General_Name");

    [TranslationProperty]
    public string SaveText => ResourceHelper.GetString("General_Save");

    [TranslationProperty]
    public string InstallPathText => ResourceHelper.GetString("GamePage_InstallPath");

    [TranslationProperty]
    public string OpenFolderText => ResourceHelper.GetString("General_OpenFolder");

    [TranslationProperty]
    public string FavouritedText => ResourceHelper.GetString("GamePage_Favorited");

    [TranslationProperty]
    public string ClickToFavouriteText => ResourceHelper.GetString("GamePage_ClickToFavorite");

    [TranslationProperty]
    public string NotesText => ResourceHelper.GetString("GamePage_Notes");

    [TranslationProperty]
    public string CloseText => ResourceHelper.GetString("General_Close");

    [TranslationProperty]
    public string MultipleDllsFoundText => ResourceHelper.GetString("GamePage_MultipleDllsFound");

    [TranslationProperty]
    public string LaunchText => ResourceHelper.GetString("GamePage_Launch");

    [TranslationProperty]
    public string DLSSPresetInfoTooltipText => ResourceHelper.GetString("GamePage_DLSSPresetInfo_Tooltip");

    [TranslationProperty]
    public string ReloadText => ResourceHelper.GetString("General_Reload");

    [TranslationProperty]
    public string HistoryText => ResourceHelper.GetString("GamePage_History");

    [TranslationProperty]
    public string ResetAllText => ResourceHelper.GetString("GamePage_ResetAll");

    [TranslationProperty]
    public string ClickToHideText => ResourceHelper.GetString("GamePage_ClickToHide");

    [TranslationProperty]
    public string HiddenText => ResourceHelper.GetString("General_Hidden");
}
