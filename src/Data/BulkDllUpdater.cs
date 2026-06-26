using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DLSS_Swapper.Helpers;

namespace DLSS_Swapper.Data;

/// <summary>
/// Provides a "update every game in the library to the latest available DLL" workflow so the user
/// does not have to open each game one by one and swap a single DLL at a time.
///
/// This reuses the exact same per-game swap logic (<see cref="Game.UpdateDllAsync(DLLRecord)"/>) and the
/// newest-first ordering of the DLL record lists, it just drives them for every game automatically.
/// </summary>
internal static class BulkDllUpdater
{
    // NOTE: DLL type
    // The primary (non backup) asset types we are able to swap.
    static readonly GameAssetType[] SwappableAssetTypes = new[]
    {
        GameAssetType.DLSS,
        GameAssetType.DLSS_G,
        GameAssetType.DLSS_D,
        GameAssetType.FSR_31_DX12,
        GameAssetType.FSR_31_VK,
        GameAssetType.XeSS,
        GameAssetType.XeLL,
        GameAssetType.XeSS_FG,
        GameAssetType.XeSS_DX11,
    };

    static IReadOnlyList<DLLRecord> GetRecordsForAssetType(GameAssetType assetType)
    {
        // NOTE: DLL type
        return assetType switch
        {
            GameAssetType.DLSS => DLLManager.Instance.DLSSRecords,
            GameAssetType.DLSS_G => DLLManager.Instance.DLSSGRecords,
            GameAssetType.DLSS_D => DLLManager.Instance.DLSSDRecords,
            GameAssetType.FSR_31_DX12 => DLLManager.Instance.FSR31DX12Records,
            GameAssetType.FSR_31_VK => DLLManager.Instance.FSR31VKRecords,
            GameAssetType.XeSS => DLLManager.Instance.XeSSRecords,
            GameAssetType.XeLL => DLLManager.Instance.XeLLRecords,
            GameAssetType.XeSS_FG => DLLManager.Instance.XeSSFGRecords,
            GameAssetType.XeSS_DX11 => DLLManager.Instance.XeSSDX11Records,
            _ => Array.Empty<DLLRecord>(),
        };
    }

    /// <summary>
    /// Returns the newest <see cref="DLLRecord"/> we are allowed to swap a given asset type to, or null if
    /// there is no applicable record. This mirrors the filtering done by the per-game DLL picker.
    /// </summary>
    /// <param name="assetType">The asset type currently installed in the game.</param>
    /// <param name="currentAssets">The installed asset(s) of that type for the game.</param>
    /// <param name="allowDownloading">If false, only DLLs already on disk are considered.</param>
    static DLLRecord? GetLatestApplicableRecord(GameAssetType assetType, IReadOnlyList<GameAsset> currentAssets, bool allowDownloading)
    {
        if (currentAssets.Count == 0)
        {
            return null;
        }

        // The record lists are kept sorted newest-first (see DLLRecord.CompareTo), so the first applicable
        // record in the list is the latest version.
        var records = GetRecordsForAssetType(assetType).ToList();
        if (records.Count == 0)
        {
            return null;
        }

        // Don't bulk update to debug/dev DLLs unless the user has explicitly opted in.
        if (Settings.Instance.AllowDebugDlls == false)
        {
            records = records.Where(x => x.IsDevFile == false).ToList();
        }

        // Mirror the DLL picker behaviour and keep DLSS 1.x games on the 1.x branch and DLSS 2.x+ games on
        // the 2.x+ branch instead of mixing incompatible major versions.
        if (assetType == GameAssetType.DLSS)
        {
            var isVersionOne = currentAssets[0].Version.StartsWith("1.", StringComparison.Ordinal);
            records = records.Where(x => x.Version.StartsWith("1.", StringComparison.Ordinal) == isVersionOne).ToList();
        }

        foreach (var record in records)
        {
            if (record.LocalRecord is null)
            {
                continue;
            }

            // If we aren't downloading we can only use DLLs that are already on disk.
            if (allowDownloading == false && record.LocalRecord.IsDownloaded == false)
            {
                continue;
            }

            return record;
        }

        return null;
    }

    /// <summary>
    /// Builds the list of DLL swaps required to bring a single game up to the latest available DLLs.
    /// Only asset types that the game already has installed (and that have a newer version available) are returned.
    /// </summary>
    public static List<GameUpdatePlanItem> GetUpdatePlan(Game game, bool allowDownloading)
    {
        var plan = new List<GameUpdatePlanItem>();

        foreach (var assetType in SwappableAssetTypes)
        {
            var currentAssets = game.GameAssets.Where(x => x.AssetType == assetType).ToList();
            if (currentAssets.Count == 0)
            {
                continue;
            }

            var latestRecord = GetLatestApplicableRecord(assetType, currentAssets, allowDownloading);
            if (latestRecord is null)
            {
                continue;
            }

            // Already up to date if every installed copy is already on the target hash.
            var allAlreadyOnTarget = currentAssets.All(x => string.Equals(x.Hash, latestRecord.MD5Hash, StringComparison.InvariantCultureIgnoreCase));
            if (allAlreadyOnTarget)
            {
                continue;
            }

            plan.Add(new GameUpdatePlanItem(assetType, latestRecord));
        }

        return plan;
    }

    /// <summary>
    /// Updates every supplied game to the latest available DLL for each asset type it currently has installed.
    /// </summary>
    /// <param name="games">The games to update.</param>
    /// <param name="allowDownloading">If true, missing DLLs are downloaded as needed; if false only already downloaded DLLs are used.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Token used to stop processing further games.</param>
    public static async Task<BulkUpdateSummary> UpdateAllAsync(IReadOnlyList<Game> games, bool allowDownloading, IProgress<BulkUpdateProgress>? progress, CancellationToken cancellationToken)
    {
        var summary = new BulkUpdateSummary();
        var totalGames = games.Count;
        var processedGames = 0;

        void Report(string title, string action)
        {
            progress?.Report(new BulkUpdateProgress(processedGames, totalGames, title, action));
        }

        foreach (var game in games)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                summary.Cancelled = true;
                break;
            }

            Report(game.Title, ResourceHelper.GetString("GamesPage_UpdateAll_Status_Checking"));

            List<GameUpdatePlanItem> plan;
            try
            {
                plan = GetUpdatePlan(game, allowDownloading);
            }
            catch (Exception err)
            {
                Logger.Error(err, $"Failed to build update plan for {game.Title}.");
                summary.Errors.Add($"{game.Title}: {err.Message}");
                summary.GamesFailed++;
                processedGames++;
                Report(game.Title, string.Empty);
                continue;
            }

            if (plan.Count == 0)
            {
                summary.GamesAlreadyUpToDate++;
                processedGames++;
                Report(game.Title, string.Empty);
                continue;
            }

            var gameUpdated = false;
            var gameFailed = false;

            foreach (var planItem in plan)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    summary.Cancelled = true;
                    break;
                }

                var assetTypeName = DLLManager.Instance.GetAssetTypeName(planItem.AssetType);
                var targetRecord = planItem.TargetRecord;

                // Download the target DLL if we don't already have it on disk.
                if (targetRecord.LocalRecord?.IsDownloaded == false)
                {
                    if (allowDownloading == false)
                    {
                        // Shouldn't happen because GetLatestApplicableRecord already filters these out, but be safe.
                        continue;
                    }

                    Report(game.Title, ResourceHelper.GetFormattedResourceTemplate("GamesPage_UpdateAll_Status_DownloadingTemplate", assetTypeName, targetRecord.DisplayName));

                    var (downloadSuccess, downloadMessage, downloadCancelled) = await targetRecord.DownloadAsync().ConfigureAwait(false);
                    if (downloadCancelled)
                    {
                        summary.Cancelled = true;
                        break;
                    }

                    if (downloadSuccess == false)
                    {
                        summary.Errors.Add($"{game.Title} ({assetTypeName}): {downloadMessage}");
                        gameFailed = true;
                        continue;
                    }
                }

                Report(game.Title, ResourceHelper.GetFormattedResourceTemplate("GamesPage_UpdateAll_Status_SwappingTemplate", assetTypeName, targetRecord.DisplayName));

                var (success, message, promptToRelaunchAsAdmin) = await game.UpdateDllAsync(targetRecord).ConfigureAwait(false);
                if (success)
                {
                    summary.DllsUpdated++;
                    gameUpdated = true;
                }
                else
                {
                    summary.Errors.Add($"{game.Title} ({assetTypeName}): {message}");
                    gameFailed = true;
                    if (promptToRelaunchAsAdmin)
                    {
                        summary.PromptToRelaunchAsAdmin = true;
                    }
                }
            }

            if (gameUpdated)
            {
                summary.GamesUpdated++;
            }

            if (gameFailed)
            {
                summary.GamesFailed++;
            }

            processedGames++;
            Report(game.Title, string.Empty);

            if (summary.Cancelled)
            {
                break;
            }
        }

        return summary;
    }
}

/// <summary>
/// A single DLL swap that needs to happen to bring a game up to date.
/// </summary>
internal sealed class GameUpdatePlanItem
{
    public GameAssetType AssetType { get; }
    public DLLRecord TargetRecord { get; }

    public GameUpdatePlanItem(GameAssetType assetType, DLLRecord targetRecord)
    {
        AssetType = assetType;
        TargetRecord = targetRecord;
    }
}

/// <summary>
/// Progress snapshot for the bulk update workflow.
/// </summary>
internal sealed class BulkUpdateProgress
{
    public int ProcessedGames { get; }
    public int TotalGames { get; }
    public string CurrentGameTitle { get; }
    public string CurrentAction { get; }

    public BulkUpdateProgress(int processedGames, int totalGames, string currentGameTitle, string currentAction)
    {
        ProcessedGames = processedGames;
        TotalGames = totalGames;
        CurrentGameTitle = currentGameTitle;
        CurrentAction = currentAction;
    }
}

/// <summary>
/// Result of a bulk update run.
/// </summary>
internal sealed class BulkUpdateSummary
{
    public int GamesUpdated { get; set; }
    public int DllsUpdated { get; set; }
    public int GamesAlreadyUpToDate { get; set; }
    public int GamesFailed { get; set; }
    public bool PromptToRelaunchAsAdmin { get; set; }
    public bool Cancelled { get; set; }
    public List<string> Errors { get; } = new List<string>();
}
