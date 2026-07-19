using System.Collections.Generic;
using System.Linq;
using DLSS_Swapper.Data;
using DLSS_Swapper.Helpers;

namespace DLSS_Swapper.UserControls;

class BatchSwapSummaryControlModel
{
    // Skips that are expected rather than problems: the user asked for a DLL type
    // the game simply doesn't ship. Listing one line per game+type buries the
    // results that actually need attention, so these are collapsed into a count.
    static readonly HashSet<string> NotApplicableReasonKeys = new HashSet<string>()
    {
        "GamesPage_Batch_Skipped_NoAsset",
        "GamesPage_Batch_Preset_Skipped_NoAsset",
        "GamesPage_Batch_Preset_Skipped_Unsupported",
    };

    public string SwappedSummaryText { get; } = string.Empty;
    public int SkippedCount { get; }
    public int ErrorCount { get; }
    public string NotApplicableMessage { get; } = string.Empty;
    public string AdminMessage { get; } = string.Empty;
    public IReadOnlyList<BatchSwapResult> Results { get; }

    public BatchSwapSummaryControlModelTranslationProperties TranslationProperties { get; } = new BatchSwapSummaryControlModelTranslationProperties();

    public BatchSwapSummaryControlModel(IReadOnlyList<BatchSwapResult> results)
    {
        // Not-applicable skips are counted but kept out of the list.
        Results = results.Where(x => x.Status != BatchSwapStatus.Skipped || NotApplicableReasonKeys.Contains(x.ReasonKey) == false).ToList();

        var swapped = results.Where(x => x.Status == BatchSwapStatus.Swapped).ToList();
        SwappedSummaryText = ResourceHelper.GetFormattedResourceTemplate("GamesPage_Batch_Summary_SwappedTemplate", swapped.Count, swapped.Select(x => x.GameTitle).Distinct().Count());

        SkippedCount = results.Count(x => x.Status == BatchSwapStatus.Skipped && NotApplicableReasonKeys.Contains(x.ReasonKey) == false);
        ErrorCount = results.Count(x => x.Status == BatchSwapStatus.Error);

        var notApplicableCount = results.Count(x => x.Status == BatchSwapStatus.Skipped && NotApplicableReasonKeys.Contains(x.ReasonKey) == true);
        if (notApplicableCount > 0)
        {
            NotApplicableMessage = ResourceHelper.GetFormattedResourceTemplate("GamesPage_Batch_Summary_NotApplicableTemplate", notApplicableCount);
        }

        var adminCount = results.Count(x => x.PromptToRelaunchAsAdmin == true);
        if (adminCount > 0)
        {
            AdminMessage = ResourceHelper.GetFormattedResourceTemplate("GamesPage_Batch_NeedsAdminTemplate", adminCount);
        }
    }
}
