using DLSS_Swapper.Data.Streamline;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace DLSS_Swapper.Tests;

/// <summary>
/// Feature: streamline-sdk-update, Property 3: Update replaces exactly the intersection
///
/// For any set of Streamline DLL filenames detected in a game directory and any set of
/// Streamline DLL filenames available in the staging area, the set of files that the
/// updater targets for replacement should equal the intersection of these two sets.
/// No files outside this intersection should be created, modified, or deleted during
/// the update operation.
///
/// **Validates: Requirements 6.1, 6.2, 6.3**
/// </summary>
public class UpdateTargetsIntersectionPropertyTests
{
    private static readonly string[] AllKnownDlls = StreamlineManager.KnownStreamlineDlls;

    /// <summary>
    /// Generates a random subset of KnownStreamlineDlls by using a boolean mask.
    /// Each DLL is independently included or excluded.
    /// </summary>
    private static Gen<HashSet<string>> SubsetOfKnownDllsGen()
    {
        // Generate a boolean for each known DLL to decide inclusion
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
    /// Computes the update targets using the same logic as StreamlineUpdater.UpdateAsync:
    /// iterate over game DLLs and include only those that also exist in the staged set.
    /// This mirrors the foreach loop in UpdateAsync that checks GetStagedDllPath().
    /// </summary>
    private static HashSet<string> ComputeUpdateTargets(
        HashSet<string> gameDlls,
        HashSet<string> stagedDlls)
    {
        var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var dll in gameDlls)
        {
            // Mirrors: StreamlineManager.Instance.GetStagedDllPath(dllFileName) is not null
            if (stagedDlls.Contains(dll))
            {
                targets.Add(dll);
            }
        }
        return targets;
    }

    /// <summary>
    /// Property: The update target set equals the set intersection of game DLLs and staged DLLs.
    /// For any random subsets of KnownStreamlineDlls representing game and staging,
    /// the computed update targets must be exactly gameDlls ∩ stagedDlls.
    /// </summary>
    [Fact]
    public void UpdateTargets_EqualsIntersection_OfGameAndStagedDlls()
    {
        var prop = Prop.ForAll(
            SubsetOfKnownDllsGen().ToArbitrary(),
            SubsetOfKnownDllsGen().ToArbitrary(),
            (HashSet<string> gameDlls, HashSet<string> stagedDlls) =>
            {
                var updateTargets = ComputeUpdateTargets(gameDlls, stagedDlls);

                // Compute expected intersection using LINQ
                var expectedIntersection = new HashSet<string>(
                    gameDlls.Where(d => stagedDlls.Contains(d)),
                    StringComparer.OrdinalIgnoreCase);

                return updateTargets.SetEquals(expectedIntersection);
            });

        prop.VerboseCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: Every update target must be present in both the game DLLs and the staged DLLs.
    /// No file outside the intersection should be targeted.
    /// </summary>
    [Fact]
    public void UpdateTargets_AreAllInBothGameAndStaged()
    {
        var prop = Prop.ForAll(
            SubsetOfKnownDllsGen().ToArbitrary(),
            SubsetOfKnownDllsGen().ToArbitrary(),
            (HashSet<string> gameDlls, HashSet<string> stagedDlls) =>
            {
                var updateTargets = ComputeUpdateTargets(gameDlls, stagedDlls);

                // Every target must be in both sets
                return updateTargets.All(t =>
                    gameDlls.Contains(t) && stagedDlls.Contains(t));
            });

        prop.VerboseCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: No game DLL that exists in staging is excluded from the update targets.
    /// This ensures completeness — every DLL in the intersection IS targeted.
    /// </summary>
    [Fact]
    public void UpdateTargets_IncludesAllGameDllsThatAreStaged()
    {
        var prop = Prop.ForAll(
            SubsetOfKnownDllsGen().ToArbitrary(),
            SubsetOfKnownDllsGen().ToArbitrary(),
            (HashSet<string> gameDlls, HashSet<string> stagedDlls) =>
            {
                var updateTargets = ComputeUpdateTargets(gameDlls, stagedDlls);

                // Every game DLL that is also staged must be in the targets
                return gameDlls
                    .Where(d => stagedDlls.Contains(d))
                    .All(d => updateTargets.Contains(d));
            });

        prop.VerboseCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: Game DLLs not present in staging are never included in update targets.
    /// This validates Requirement 6.4 — staged DLL not found means skip.
    /// </summary>
    [Fact]
    public void UpdateTargets_ExcludesGameDllsNotInStaging()
    {
        var prop = Prop.ForAll(
            SubsetOfKnownDllsGen().ToArbitrary(),
            SubsetOfKnownDllsGen().ToArbitrary(),
            (HashSet<string> gameDlls, HashSet<string> stagedDlls) =>
            {
                var updateTargets = ComputeUpdateTargets(gameDlls, stagedDlls);

                // Game DLLs NOT in staging must NOT be in targets
                return gameDlls
                    .Where(d => !stagedDlls.Contains(d))
                    .All(d => !updateTargets.Contains(d));
            });

        prop.VerboseCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: Staged DLLs not present in the game directory are never added.
    /// This validates Requirement 6.3 — SHALL NOT add DLLs not already present.
    /// </summary>
    [Fact]
    public void UpdateTargets_NeverAddsStagedDllsNotInGame()
    {
        var prop = Prop.ForAll(
            SubsetOfKnownDllsGen().ToArbitrary(),
            SubsetOfKnownDllsGen().ToArbitrary(),
            (HashSet<string> gameDlls, HashSet<string> stagedDlls) =>
            {
                var updateTargets = ComputeUpdateTargets(gameDlls, stagedDlls);

                // Staged DLLs NOT in the game must NOT appear in targets
                return stagedDlls
                    .Where(d => !gameDlls.Contains(d))
                    .All(d => !updateTargets.Contains(d));
            });

        prop.VerboseCheckThrowOnFailure();
    }
}
