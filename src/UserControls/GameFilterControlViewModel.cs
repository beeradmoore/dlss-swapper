using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.UserControls;
public class GameFilterControlViewModel : LocalizedViewModelBase
{
    public GameFilterControlViewModel() : base() { }

    #region LanguageProperties
    [TranslationProperty] public string OptionsText => $"{ResourceHelper.GetString("Options")}:";
    [TranslationProperty] public string GroupingText => $"{ResourceHelper.GetString("Grouping")}:";
    [TranslationProperty] public string HideGamesWithNoSwappableItemsText => ResourceHelper.GetString("HideGamesWithNoSwappableItems");
    [TranslationProperty] public string GroupGamesFromTheSameLibraryTogetherText => ResourceHelper.GetString("GroupGamesFromTheSameLibraryTogether");
    #endregion
}
