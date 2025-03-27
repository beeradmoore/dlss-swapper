using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.UserControls;
public class GameFilterControlViewModel : LocalizedViewModelBase
{
    public GameFilterControlViewModel() : base() { }

    #region LanguageProperties
    [LanguageProperty] public string OptionsText => $"{ResourceHelper.GetString("Options")}:";
    [LanguageProperty] public string GroupingText => $"{ResourceHelper.GetString("Grouping")}:";
    [LanguageProperty] public string HideGamesWithNoSwappableItemsText => ResourceHelper.GetString("HideGamesWithNoSwappableItems");
    [LanguageProperty] public string GroupGamesFromTheSameLibraryTogetherText => ResourceHelper.GetString("GroupGamesFromTheSameLibraryTogether");
    #endregion
}
