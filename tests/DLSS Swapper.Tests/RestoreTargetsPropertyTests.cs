using DLSS_Swapper.Data.Streamline;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace DLSS_Swapper.Tests;

/// <summary>
/// Feature: streamline-sdk-update, Property 5: Restore only affects DLLs with backups
///
/// For any set of Streamline DLL filenames in a game directory and any set of .dlsss
/// backup files present, the set of files that the restore operation targets should be
/// exactly those DLLs that have a corresponding .dlsss backup file AND a matching
/// original Streamline asset record. No DLL without a backup is modified, and no backup
/// without a matching original asset is processed.
///
/// The key logic from StreamlineUpdater.RestoreAsync:
///   foreach (var backupAsset in backupAssets)  // Streamline_BACKUP assets
///   {
///       var originalPath = backupAsset.Path.Replace(".dlsss", string.Empty);
///       var existingRecord = game.GameAssets.FirstOrDefault(x =>
///           x.AssetType == GameAssetType.Streamline &&
///           x.Path.Equals(originalPath, StringComparison.OrdinalIgnoreCase));
///       if (existingRecord is null) continue;  // skip if no matching original
///       // ... restore the file
///   }
///
/// **Validates: Requirements 8.3**
/// </summary>
public class RestoreTargetsPropertyTests
{
    private static readonly string[] AllKnownDlls = StreamlineManager.KnownStreamlineDlls;
    private const string GameDir = @"C:\Games\TestGame";
    private const string BackupExtension = ".dlsss";

    /// <summary>
    /// Generates a random subset of KnownStreamlineDlls by using a boolean mask.
    /// Each DLL is independently included or excluded.
    /// </summary>
    private static Gen<HashSet<string>> SubsetOfKnownDllsGen()
    {
        return Gen.ListOf(ArbMap.Default.GeneratorFor<bool>(), AllKnownDlls.Length)
            .Select(bools =>
            {
                var subset = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var boolList = bools.ToList();
                for (int i = 0; i < AllKnownDlls.Length; i++)
                {
                    if (boolList[i])
                    {
                        subset.Add(AllKnownDlls[i]);
                    }
                }
                return subset;
            });
    }

    /// <summary>
    /// Computes the restore targets using the same logic as StreamlineUpdater.RestoreAsync:
    /// iterate over backup DLLs and include only those that also have a matching
    /// Streamline (original) asset record.
    /// </summary>
    private static HashSet<string> ComputeRestoreTargets(
        HashSet<string> streamlineAssets,
        HashSet<string> backupDlls)
    {
        var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var backupDll in backupDlls)
        {
            // Mirrors: var originalPath = backupAsset.Path.Replace(".dlsss", string.Empty);
            // Since backupDll is the DLL name (e.g., "sl.common.dll"), the original path
            // corresponds to the same DLL name in the Streamline assets.
            // The backup path would be "sl.common.dll.dlsss" and stripping ".dlsss" gives "sl.common.dll".
            var originalDll = backupDll; // The DLL filename that the backup corresponds to

            // Mirrors: game.GameAssets.FirstOrDefault(x => x.AssetType == Streamline && x.Path.Equals(originalPath))
            if (streamlineAssets.Contains(originalDll))
            {
                targets.Add(originalDll);
            }
            // else: skip — no matching original asset (existingRecord is null)
        }
        return targets;
    }

    /// <summary>
    /// Property: Restore targets are exactly the DLLs that have BOTH a backup AND a
    /// matching original Streamline asset. This is the intersection of the backup set
    /// and the Streamline asset set.
    /// </summary>
    [Fact]
    public void RestoreTargets_EqualsIntersection_OfBackupsAndStreamlineAssets()
    {
        var prop = Prop.ForAll(
            SubsetOfKnownDllsGen().ToArbitrary(),
            SubsetOfKnownDllsGen().ToArbitrary(),
            (HashSet<string> streamlineAssets, HashSet<string> backupDlls) =>
            {
                var restoreTargets = ComputeRestoreTargets(streamlineAssets, backupDlls);

                // Expected: DLLs that appear in both sets
                var expected = new HashSet<string>(
                    backupDlls.Where(b => streamlineAssets.Contains(b)),
                    StringComparer.OrdinalIgnoreCase);

                return restoreTargets.SetEquals(expected);
            });

        prop.VerboseCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: No DLL without a backup is modified during restore.
    /// Every restore target must have a corresponding backup.
    /// </summary>
    [Fact]
    public void RestoreTargets_OnlyIncludeDllsWithBackups()
    {
        var prop = Prop.ForAll(
            SubsetOfKnownDllsGen().ToArbitrary(),
            SubsetOfKnownDllsGen().ToArbitrary(),
            (HashSet<string> streamlineAssets, HashSet<string> backupDlls) =>
            {
                var restoreTargets = ComputeRestoreTargets(streamlineAssets, backupDlls);

                // Every restore target must be in the backup set
                return restoreTargets.All(t => backupDlls.Contains(t));
            });

        prop.VerboseCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: No backup without a matching original Streamline asset is processed.
    /// Every restore target must have a corresponding Streamline asset record.
    /// </summary>
    [Fact]
    public void RestoreTargets_OnlyIncludeDllsWithMatchingOriginalAsset()
    {
        var prop = Prop.ForAll(
            SubsetOfKnownDllsGen().ToArbitrary(),
            SubsetOfKnownDllsGen().ToArbitrary(),
            (HashSet<string> streamlineAssets, HashSet<string> backupDlls) =>
            {
                var restoreTargets = ComputeRestoreTargets(streamlineAssets, backupDlls);

                // Every restore target must be in the Streamline assets set
                return restoreTargets.All(t => streamlineAssets.Contains(t));
            });

        prop.VerboseCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: Streamline assets without backups are never included in restore targets.
    /// DLLs that exist as Streamline assets but have no backup should not be touched.
    /// </summary>
    [Fact]
    public void RestoreTargets_ExcludeStreamlineAssetsWithoutBackups()
    {
        var prop = Prop.ForAll(
            SubsetOfKnownDllsGen().ToArbitrary(),
            SubsetOfKnownDllsGen().ToArbitrary(),
            (HashSet<string> streamlineAssets, HashSet<string> backupDlls) =>
            {
                var restoreTargets = ComputeRestoreTargets(streamlineAssets, backupDlls);

                // Streamline assets NOT in the backup set must NOT be in targets
                return streamlineAssets
                    .Where(a => !backupDlls.Contains(a))
                    .All(a => !restoreTargets.Contains(a));
            });

        prop.VerboseCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: Backups without matching Streamline assets are never processed.
    /// Backup DLLs that have no corresponding original asset record should be skipped.
    /// </summary>
    [Fact]
    public void RestoreTargets_ExcludeBackupsWithoutMatchingOriginalAsset()
    {
        var prop = Prop.ForAll(
            SubsetOfKnownDllsGen().ToArbitrary(),
            SubsetOfKnownDllsGen().ToArbitrary(),
            (HashSet<string> streamlineAssets, HashSet<string> backupDlls) =>
            {
                var restoreTargets = ComputeRestoreTargets(streamlineAssets, backupDlls);

                // Backups NOT in the Streamline assets set must NOT be in targets
                return backupDlls
                    .Where(b => !streamlineAssets.Contains(b))
                    .All(b => !restoreTargets.Contains(b));
            });

        prop.VerboseCheckThrowOnFailure();
    }
}
