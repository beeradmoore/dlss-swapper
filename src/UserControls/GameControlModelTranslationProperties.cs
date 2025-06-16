using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.UserControls;

public class GameControlModelTranslationProperties : LocalizedViewModelBase
{
    [TranslationProperty]
    public string RemoveText => ResourceHelper.GetString("Remove");

    [TranslationProperty]
    public string AddCustomCoverText => ResourceHelper.GetString("AddCustomCover");

    [TranslationProperty]
    public string GameNotReadyToPlayStateText => ResourceHelper.GetString("GameNotReadyToPlayState");

    [TranslationProperty]
    public string HelpText => ResourceHelper.GetString("Help");

    [TranslationProperty]
    public string NameText => ResourceHelper.GetString("Name");

    [TranslationProperty]
    public string SaveText => ResourceHelper.GetString("Save");

    [TranslationProperty]
    public string InstallPathText => ResourceHelper.GetString("InstallPath");

    [TranslationProperty]
    public string OpenFolderText => ResourceHelper.GetString("OpenFolder");

    [TranslationProperty]
    public string FavouritedText => ResourceHelper.GetString("Favourited");

    [TranslationProperty]
    public string ClickToFavouriteText => ResourceHelper.GetString("ClickToFavourite");

    [TranslationProperty]
    public string NotesText => ResourceHelper.GetString("Notes");

    [TranslationProperty]
    public string CloseText => ResourceHelper.GetString("Close");

    [TranslationProperty]
    public string MultipleDllsFoundText => ResourceHelper.GetString("MultipleDllsFound");
}
