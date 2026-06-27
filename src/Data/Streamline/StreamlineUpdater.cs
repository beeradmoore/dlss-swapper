using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DLSS_Swapper.Data.Streamline;

internal static class StreamlineUpdater
{
    /// <summary>
    /// Batch-updates all detected Streamline DLLs in the game with staged versions.
    /// Returns (Success, Message, PromptToRelaunchAsAdmin).
    /// </summary>
    internal static async Task<(bool Success, string Message, bool PromptToRelaunchAsAdmin)> UpdateAsync(Game game)
    {
        if (StreamlineManager.Instance.IsStagingReady == false)
        {
            return (false, "Streamline SDK staging is not ready. Please try again later.", false);
        }

        // Get all Streamline game assets.
        var streamlineAssets = game.GameAssets
            .Where(x => x.AssetType == GameAssetType.Streamline)
            .ToList();

        if (streamlineAssets.Count == 0)
        {
            return (false, "No Streamline DLLs found in this game.", false);
        }

        // Compute intersection: only update DLLs that exist in both the game and the staging area.
        var assetsToUpdate = new List<(GameAsset GameAsset, string StagedPath)>();
        foreach (var asset in streamlineAssets)
        {
            var dllFileName = Path.GetFileName(asset.Path);
            var stagedPath = StreamlineManager.Instance.GetStagedDllPath(dllFileName);
            if (stagedPath is not null)
            {
                assetsToUpdate.Add((asset, stagedPath));
            }
            else
            {
                Logger.Warning($"Staged DLL not found for {dllFileName}, skipping.");
            }
        }

        if (assetsToUpdate.Count == 0)
        {
            return (false, "No matching staged Streamline DLLs found for this game.", false);
        }

        // Phase 1: Backup each existing DLL as .dlsss (overwrite existing backup per Req 7.3).
        var backedUpAssets = new List<(GameAsset GameAsset, string BackupPath)>();
        foreach (var (asset, _) in assetsToUpdate)
        {
            var backupPath = $"{asset.Path}.dlsss";
            try
            {
                // Overwrite existing backup if present (Req 7.3).
                File.Copy(asset.Path, backupPath, overwrite: true);

                // Verify backup was written successfully (Req 7.4).
                if (File.Exists(backupPath) == false)
                {
                    Logger.Error($"Backup verification failed for {asset.Path}.");
                    // Roll back any already-replaced files (none replaced yet in backup phase).
                    return (false, "Unable to update Streamline. Backup verification failed.", false);
                }

                backedUpAssets.Add((asset, backupPath));
            }
            catch (UnauthorizedAccessException err)
            {
                Logger.Error(err);
                if (App.CurrentApp.IsAdminUser() is false)
                {
                    return (false, "Unable to update Streamline as we are unable to write to the target directory. Running DLSS Swapper as administrator may fix this.", true);
                }
                else
                {
                    return (false, "Unable to update Streamline as we are unable to write to the target directory.", false);
                }
            }
            catch (Exception err)
            {
                Logger.Error(err);
                return (false, "Unable to update Streamline. Please check your error log for more information.", false);
            }
        }

        // Phase 2: Copy staged DLLs over game DLLs.
        var replacedAssets = new List<(GameAsset GameAsset, string BackupPath)>();
        foreach (var (asset, stagedPath) in assetsToUpdate)
        {
            var backupPath = $"{asset.Path}.dlsss";
            try
            {
                File.Copy(stagedPath, asset.Path, overwrite: true);
                replacedAssets.Add((asset, backupPath));
            }
            catch (UnauthorizedAccessException err)
            {
                Logger.Error(err);

                // Roll back all already-replaced files from their backups (Req 6.6).
                RollbackReplacedFiles(replacedAssets);

                if (App.CurrentApp.IsAdminUser() is false)
                {
                    return (false, "Unable to update Streamline as we are unable to write to the target directory. Running DLSS Swapper as administrator may fix this.", true);
                }
                else
                {
                    return (false, "Unable to update Streamline as we are unable to write to the target directory.", false);
                }
            }
            catch (IOException err)
            {
                Logger.Error(err);

                // Roll back all already-replaced files from their backups (Req 6.6).
                RollbackReplacedFiles(replacedAssets);

                return (false, "Unable to update Streamline. A file may be in use by another program. Is your game currently running?", false);
            }
            catch (Exception err)
            {
                Logger.Error(err);

                // Roll back all already-replaced files from their backups (Req 6.6).
                RollbackReplacedFiles(replacedAssets);

                return (false, "Unable to update Streamline. Please check your error log for more information.", false);
            }
        }

        // Phase 3: Record GameHistory entries and update game assets (Req 9.1).
        var dllHistory = new List<GameHistory>();
        var newGameAssets = new List<GameAsset>();

        foreach (var (asset, stagedPath) in assetsToUpdate)
        {
            var backupPath = $"{asset.Path}.dlsss";

            // Create the new game asset with updated version info.
            var newGameAsset = new GameAsset()
            {
                Id = asset.Id,
                AssetType = GameAssetType.Streamline,
                Path = asset.Path,
            };
            newGameAsset.LoadVersionAndHash();
            newGameAssets.Add(newGameAsset);

            // Create backup game asset entry.
            var backupGameAsset = new GameAsset()
            {
                Id = asset.Id,
                AssetType = GameAssetType.Streamline_BACKUP,
                Path = backupPath,
            };
            backupGameAsset.LoadVersionAndHash();
            newGameAssets.Add(backupGameAsset);

            dllHistory.Add(new GameHistory()
            {
                GameId = game.ID,
                EventType = GameHistoryEventType.DLLSwapped,
                EventTime = DateTime.Now,
                AssetType = GameAssetType.Streamline,
                AssetPath = asset.Path,
                AssetVersion = newGameAsset.DisplayName,
            });
        }

        // Remove old Streamline assets and add new ones.
        foreach (var (asset, _) in assetsToUpdate)
        {
            game.GameAssets.Remove(asset);

            // Also remove any existing backup asset for this path.
            var existingBackup = game.GameAssets
                .FirstOrDefault(x => x.AssetType == GameAssetType.Streamline_BACKUP &&
                                     x.Path.Equals($"{asset.Path}.dlsss", StringComparison.OrdinalIgnoreCase));
            if (existingBackup is not null)
            {
                game.GameAssets.Remove(existingBackup);
            }
        }
        game.GameAssets.AddRange(newGameAssets);

        // Persist to database (Req 6.5).
        using (await Database.Instance.Mutex.LockAsync())
        {
            await Database.Instance.Connection.InsertAllAsync(dllHistory, false).ConfigureAwait(false);
            await Database.Instance.Connection.ExecuteAsync("DELETE FROM game_asset WHERE id = ?", game.ID).ConfigureAwait(false);
            await Database.Instance.Connection.InsertAllAsync(game.GameAssets, false).ConfigureAwait(false);
        }

        return (true, string.Empty, false);
    }

    /// <summary>
    /// Batch-restores all Streamline DLLs from .dlsss backups.
    /// Returns (Success, Message, PromptToRelaunchAsAdmin).
    /// </summary>
    internal static async Task<(bool Success, string Message, bool PromptToRelaunchAsAdmin)> RestoreAsync(Game game)
    {
        var backupAssets = game.GameAssets
            .Where(x => x.AssetType == GameAssetType.Streamline_BACKUP)
            .ToList();

        if (backupAssets.Count == 0)
        {
            Logger.Info("No Streamline backup records found.");
            return (false, "Unable to restore Streamline to default. No backup files found.", false);
        }

        var dllHistory = new List<GameHistory>();
        var assetsToRemove = new List<GameAsset>();
        var assetsToAdd = new List<GameAsset>();
        var hadFailure = false;
        var promptAdmin = false;

        // Process each file independently (partial failure per Req 8.5).
        foreach (var backupAsset in backupAssets)
        {
            var originalPath = backupAsset.Path.Replace(".dlsss", string.Empty);
            var existingRecord = game.GameAssets
                .FirstOrDefault(x => x.AssetType == GameAssetType.Streamline &&
                                     x.Path.Equals(originalPath, StringComparison.OrdinalIgnoreCase));

            if (existingRecord is null)
            {
                Logger.Warning($"No matching Streamline asset found for backup {backupAsset.Path}, skipping.");
                continue;
            }

            try
            {
                // Move backup back to original path (Req 8.1).
                File.Move(backupAsset.Path, existingRecord.Path, overwrite: true);

                // Create new game asset with restored version info.
                var restoredGameAsset = new GameAsset()
                {
                    Id = game.ID,
                    AssetType = GameAssetType.Streamline,
                    Path = existingRecord.Path,
                    Version = backupAsset.Version,
                    Hash = backupAsset.Hash,
                };

                dllHistory.Add(new GameHistory()
                {
                    GameId = game.ID,
                    EventType = GameHistoryEventType.DLLReset,
                    EventTime = DateTime.Now,
                    AssetType = GameAssetType.Streamline,
                    AssetPath = existingRecord.Path,
                    AssetVersion = backupAsset.DisplayName,
                });

                assetsToRemove.Add(existingRecord);
                assetsToRemove.Add(backupAsset);
                assetsToAdd.Add(restoredGameAsset);
            }
            catch (UnauthorizedAccessException err)
            {
                Logger.Error(err);
                hadFailure = true;
                if (App.CurrentApp.IsAdminUser() is false)
                {
                    promptAdmin = true;
                }
                // Continue processing other files (Req 8.5).
            }
            catch (Exception err)
            {
                Logger.Error(err);
                hadFailure = true;
                // Continue processing other files (Req 8.5).
            }
        }

        // Update game assets list.
        foreach (var asset in assetsToRemove)
        {
            game.GameAssets.Remove(asset);
        }
        game.GameAssets.AddRange(assetsToAdd);

        // Persist to database (Req 8.4).
        using (await Database.Instance.Mutex.LockAsync())
        {
            await Database.Instance.Connection.InsertAllAsync(dllHistory, false).ConfigureAwait(false);
            await Database.Instance.Connection.ExecuteAsync("DELETE FROM game_asset WHERE id = ?", game.ID).ConfigureAwait(false);
            await Database.Instance.Connection.InsertAllAsync(game.GameAssets, false).ConfigureAwait(false);
        }

        if (hadFailure)
        {
            if (promptAdmin)
            {
                return (false, "Unable to restore some Streamline files. Running DLSS Swapper as administrator may fix this.", true);
            }
            else
            {
                return (false, "Unable to restore some Streamline files. Please repair your game manually.", false);
            }
        }

        return (true, string.Empty, false);
    }

    /// <summary>
    /// Rolls back already-replaced files from their backups during a failed update.
    /// </summary>
    private static void RollbackReplacedFiles(List<(GameAsset GameAsset, string BackupPath)> replacedAssets)
    {
        foreach (var (asset, backupPath) in replacedAssets)
        {
            try
            {
                if (File.Exists(backupPath))
                {
                    File.Copy(backupPath, asset.Path, overwrite: true);
                    Logger.Info($"Rolled back {asset.Path} from backup.");
                }
            }
            catch (Exception rollbackErr)
            {
                Logger.Error(rollbackErr, $"Failed to roll back {asset.Path}. Please repair your game manually.");
            }
        }
    }
}
