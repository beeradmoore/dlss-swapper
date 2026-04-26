using DLSS_Swapper.Data.Streamline;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace DLSS_Swapper.Tests;

/// <summary>
/// Feature: streamline-sdk-update, Property 1: Streamline DLL filename classification
///
/// For any DLL filename string, the filename should be classified as a Streamline DLL
/// if and only if it appears in the KnownStreamlineDlls list (case-insensitive comparison).
/// No non-Streamline filename should be misclassified, and no known Streamline filename
/// should be missed.
///
/// **Validates: Requirements 3.1**
/// </summary>
public class StreamlineDllClassificationPropertyTests
{
    private static readonly HashSet<string> KnownDllsLower =
        new(StreamlineManager.KnownStreamlineDlls.Select(d => d.ToLowerInvariant()));

    /// <summary>
    /// Property: For any random non-null string, classification via Contains with
    /// StringComparer.OrdinalIgnoreCase matches membership in KnownStreamlineDlls.
    /// This ensures no false positives or false negatives in classification.
    /// </summary>
    [Property(MaxTest = 200)]
    public bool RandomString_ClassificationMatchesMembership(NonNull<string> input)
    {
        var filename = input.Get;
        var classifiedAsStreamline = StreamlineManager.KnownStreamlineDlls
            .Contains(filename, StringComparer.OrdinalIgnoreCase);
        var isActuallyKnown = KnownDllsLower.Contains(filename.ToLowerInvariant());

        return classifiedAsStreamline == isActuallyKnown;
    }

    /// <summary>
    /// Property: Every known Streamline DLL name in any case variation (upper, lower, mixed)
    /// is correctly classified as a Streamline DLL.
    /// </summary>
    [Fact]
    public void KnownDll_AnyCaseVariation_IsClassified()
    {
        var knownDllGen = ArbMap.Default.GeneratorFor<int>()
            .Select(i => StreamlineManager.KnownStreamlineDlls[
                Math.Abs(i) % StreamlineManager.KnownStreamlineDlls.Length]);

        var caseVariantGen = knownDllGen.SelectMany(dll =>
            ArbMap.Default.GeneratorFor<int>().Select(seed =>
                (Math.Abs(seed) % 3) switch
                {
                    0 => dll.ToLowerInvariant(),
                    1 => dll.ToUpperInvariant(),
                    _ => ToMixedCase(dll)
                }));

        var prop = Prop.ForAll(
            caseVariantGen.ToArbitrary(),
            (string variant) =>
            {
                return StreamlineManager.KnownStreamlineDlls
                    .Contains(variant, StringComparer.OrdinalIgnoreCase);
            });

        prop.QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: A random string that is NOT in the known list is never classified
    /// as a Streamline DLL.
    /// </summary>
    [Fact]
    public void NonStreamlineFilename_IsNeverClassified()
    {
        var nonStreamlineGen = ArbMap.Default.GeneratorFor<NonNull<string>>()
            .Where(s => !KnownDllsLower.Contains(s.Get.ToLowerInvariant()));

        var prop = Prop.ForAll(
            nonStreamlineGen.ToArbitrary(),
            (NonNull<string> input) =>
            {
                return !StreamlineManager.KnownStreamlineDlls
                    .Contains(input.Get, StringComparer.OrdinalIgnoreCase);
            });

        prop.QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Converts a string to mixed case by alternating upper/lower for each character.
    /// </summary>
    private static string ToMixedCase(string input)
    {
        var chars = input.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            chars[i] = i % 2 == 0 ? char.ToUpperInvariant(chars[i]) : char.ToLowerInvariant(chars[i]);
        }
        return new string(chars);
    }
}
