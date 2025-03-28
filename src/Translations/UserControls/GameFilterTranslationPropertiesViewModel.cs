using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;

namespace DLSS_Swapper.Translations.UserControls;
public class GameFilterTranslationPropertiesViewModel
{
    public GameFilterTranslationPropertiesViewModel() : base() { }

    [TranslationProperty] public string OptionsText => $"{ResourceHelper.GetString("Options")}:";
    [TranslationProperty] public string GroupingText => $"{ResourceHelper.GetString("Grouping")}:";
    [TranslationProperty] public string HideGamesWithNoSwappableItemsText => ResourceHelper.GetString("HideGamesWithNoSwappableItems");
    [TranslationProperty] public string GroupGamesFromTheSameLibraryTogetherText => ResourceHelper.GetString("GroupGamesFromTheSameLibraryTogether");
}
