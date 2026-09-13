using DLSS_Swapper.Helpers;

namespace DLSS_Swapper.Data;

public enum BatchSwapStatus
{
    Swapped,
    Skipped,
    Error,
}

public class BatchSwapResult
{
    public string GameTitle { get; init; } = string.Empty;
    public BatchSwapStatus Status { get; init; }

    // Localised, game-agnostic action label built on the UI thread before the
    // worker starts (e.g. "DLSS → 3.7.0" or "Preset DLSS → D"). Never resolved
    // inside the batch worker.
    public string ActionLabel { get; init; } = string.Empty;

    // Resource key explaining a skip. Resolved lazily so it is read on the UI
    // thread at display time, not inside the batch worker.
    public string ReasonKey { get; init; } = string.Empty;

    // Raw (unlocalised) message from Game.UpdateDllAsync when Status is Error.
    public string ErrorMessage { get; init; } = string.Empty;

    public bool PromptToRelaunchAsAdmin { get; init; }

    public string DisplayText => Status switch
    {
        BatchSwapStatus.Skipped => $"{GameTitle} — {ActionLabel} — {ResourceHelper.GetString(ReasonKey)}",
        BatchSwapStatus.Error when string.IsNullOrEmpty(ErrorMessage) => $"{GameTitle} — {ActionLabel} — {ResourceHelper.GetString("GamesPage_Batch_Error_Generic")}",
        BatchSwapStatus.Error => $"{GameTitle} — {ActionLabel} — {ResourceHelper.GetFormattedResourceTemplate("GamesPage_Batch_Error_PossiblyPartialTemplate", ErrorMessage)}",
        _ => $"{GameTitle} — {ActionLabel}",
    };
}
