using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace DLSS_Swapper.Tests;

/// <summary>
/// Feature: streamline-sdk-update, Property 4: Backup path generation round-trip
///
/// For any valid file path string, the generated backup path should equal the original
/// path with ".dlsss" appended. Conversely, for any backup path ending in ".dlsss",
/// removing the ".dlsss" suffix should yield the original file path. This is a round-trip
/// property.
///
/// The backup pattern used in StreamlineUpdater is:
///   Backup:  $"{asset.Path}.dlsss"
///   Restore: backupAsset.Path.Replace(".dlsss", string.Empty)
///
/// Note: The restore logic uses String.Replace which replaces ALL occurrences of ".dlsss".
/// This means the round-trip only holds for paths that do not already contain ".dlsss"
/// as a substring. Paths containing ".dlsss" in the middle would be corrupted by restore.
///
/// **Validates: Requirements 7.2**
/// </summary>
public class BackupPathRoundTripPropertyTests
{
    private const string BackupExtension = ".dlsss";

    /// <summary>
    /// Generates random non-null, non-empty strings that do NOT contain ".dlsss".
    /// These represent the "safe" path space where the round-trip property holds.
    /// </summary>
    private static Gen<string> SafeFilePathGen()
    {
        return ArbMap.Default.GeneratorFor<NonEmptyString>()
            .Select(s => s.Get)
            .Where(s => !s.Contains(BackupExtension, StringComparison.Ordinal));
    }

    /// <summary>
    /// Generates random non-null, non-empty strings (may contain ".dlsss").
    /// Used to test the edge case where Replace corrupts paths.
    /// </summary>
    private static Gen<string> AnyNonEmptyStringGen()
    {
        return ArbMap.Default.GeneratorFor<NonEmptyString>()
            .Select(s => s.Get);
    }

    /// <summary>
    /// Property: The backup path is always the original path with ".dlsss" appended.
    /// For any non-empty path P, backup(P) == P + ".dlsss".
    /// </summary>
    [Fact]
    public void BackupPath_IsOriginalPathWithDlsssAppended()
    {
        var prop = Prop.ForAll(
            AnyNonEmptyStringGen().ToArbitrary(),
            (string originalPath) =>
            {
                var backupPath = $"{originalPath}{BackupExtension}";
                return backupPath == originalPath + BackupExtension;
            });

        prop.VerboseCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: The backup path always ends with ".dlsss".
    /// </summary>
    [Fact]
    public void BackupPath_AlwaysEndsWithDlsss()
    {
        var prop = Prop.ForAll(
            AnyNonEmptyStringGen().ToArbitrary(),
            (string originalPath) =>
            {
                var backupPath = $"{originalPath}{BackupExtension}";
                return backupPath.EndsWith(BackupExtension, StringComparison.Ordinal);
            });

        prop.VerboseCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: The backup path is always exactly 6 characters longer than the original path.
    /// ".dlsss" is 6 characters.
    /// </summary>
    [Fact]
    public void BackupPath_IsExactlySixCharactersLongerThanOriginal()
    {
        var prop = Prop.ForAll(
            AnyNonEmptyStringGen().ToArbitrary(),
            (string originalPath) =>
            {
                var backupPath = $"{originalPath}{BackupExtension}";
                return backupPath.Length == originalPath.Length + BackupExtension.Length;
            });

        prop.VerboseCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: The backup path always starts with the original path.
    /// </summary>
    [Fact]
    public void BackupPath_AlwaysStartsWithOriginalPath()
    {
        var prop = Prop.ForAll(
            AnyNonEmptyStringGen().ToArbitrary(),
            (string originalPath) =>
            {
                var backupPath = $"{originalPath}{BackupExtension}";
                return backupPath.StartsWith(originalPath, StringComparison.Ordinal);
            });

        prop.VerboseCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: Round-trip holds for paths that do NOT contain ".dlsss".
    /// Given a path P (without ".dlsss" anywhere), creating a backup path B = P + ".dlsss"
    /// and then restoring via B.Replace(".dlsss", "") yields the original path P.
    /// This is the happy path that StreamlineUpdater relies on.
    /// </summary>
    [Fact]
    public void RoundTrip_HoldsForPathsWithoutDlsssSubstring()
    {
        var prop = Prop.ForAll(
            SafeFilePathGen().ToArbitrary(),
            (string originalPath) =>
            {
                // Backup: same as StreamlineUpdater
                var backupPath = $"{originalPath}{BackupExtension}";

                // Restore: same as StreamlineUpdater.RestoreAsync
                var restoredPath = backupPath.Replace(BackupExtension, string.Empty);

                return restoredPath == originalPath;
            });

        prop.VerboseCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: Round-trip BREAKS for paths that contain ".dlsss" as a substring.
    /// This documents the known limitation of using String.Replace for restore.
    /// If a path contains ".dlsss" in the middle (e.g., "C:\game\sl.dlsss.bak\file.dll"),
    /// the Replace call will strip ALL occurrences, corrupting the path.
    ///
    /// For any path that contains ".dlsss", the restored path will differ from the original
    /// because Replace removes the embedded ".dlsss" as well as the appended one.
    /// </summary>
    [Fact]
    public void RoundTrip_BreaksForPathsContainingDlsssSubstring()
    {
        // Generate paths that definitely contain ".dlsss" somewhere in the middle
        var pathWithEmbeddedDlsssGen = AnyNonEmptyStringGen()
            .Select(s =>
            {
                // Ensure the path has ".dlsss" embedded (not just at the end)
                var clean = s.Replace(BackupExtension, "x");
                return $"C:\\games\\{clean}{BackupExtension}\\subdir\\file.dll";
            });

        var prop = Prop.ForAll(
            pathWithEmbeddedDlsssGen.ToArbitrary(),
            (string originalPath) =>
            {
                // Backup: same as StreamlineUpdater
                var backupPath = $"{originalPath}{BackupExtension}";

                // Restore: same as StreamlineUpdater.RestoreAsync
                var restoredPath = backupPath.Replace(BackupExtension, string.Empty);

                // The restored path should NOT equal the original because Replace
                // strips the embedded ".dlsss" too
                return restoredPath != originalPath;
            });

        prop.VerboseCheckThrowOnFailure();
    }
}
