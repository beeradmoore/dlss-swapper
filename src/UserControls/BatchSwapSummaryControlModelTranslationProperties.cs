using DLSS_Swapper.Attributes;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.UserControls;

public class BatchSwapSummaryControlModelTranslationProperties : LocalizedViewModelBase
{
    [TranslationProperty]
    public string SwappedText => ResourceHelper.GetString("GamesPage_Batch_Summary_Swapped");

    [TranslationProperty]
    public string SkippedText => ResourceHelper.GetString("GamesPage_Batch_Summary_Skipped");

    [TranslationProperty]
    public string ErrorsText => ResourceHelper.GetString("GamesPage_Batch_Summary_Errors");
}
