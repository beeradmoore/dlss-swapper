using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace DLSS_Swapper.Tests;

/// <summary>
/// Feature: streamline-sdk-update, Property 2: Version comparison correctness
///
/// For any two valid version strings (in Major.Minor.Build.Revision format),
/// comparing them should produce a result consistent with standard numeric version
/// ordering: the version with a higher major component is newer; if major is equal,
/// the higher minor is newer; and so on. The comparison should be symmetric
/// (if A > B then B < A) and transitive.
///
/// **Validates: Requirements 4.1**
/// </summary>
public class VersionComparisonPropertyTests
{
    /// <summary>
    /// Generator for non-negative version component tuples.
    /// System.Version requires non-negative values for major, minor, build, revision.
    /// </summary>
    private static Arbitrary<Version> VersionArbitrary()
    {
        var gen = ArbMap.Default.GeneratorFor<int>()
            .Select(i => Math.Abs(i) % 10000) // Keep values reasonable
            .Four()
            .Select(t => new Version(t.Item1, t.Item2, t.Item3, t.Item4));

        return gen.ToArbitrary();
    }

    /// <summary>
    /// Property: Reflexivity — any version is equal to itself.
    /// </summary>
    [Fact]
    public void Version_IsEqualToItself()
    {
        var prop = Prop.ForAll(
            VersionArbitrary(),
            (Version v) =>
            {
                return v.CompareTo(v) == 0 && v.Equals(v);
            });

        prop.VerboseCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: Symmetry — if A > B then B &lt; A, if A == B then B == A.
    /// </summary>
    [Fact]
    public void Version_ComparisonIsSymmetric()
    {
        var prop = Prop.ForAll(
            VersionArbitrary(),
            VersionArbitrary(),
            (Version a, Version b) =>
            {
                var cmpAB = a.CompareTo(b);
                var cmpBA = b.CompareTo(a);

                if (cmpAB > 0)
                    return cmpBA < 0;
                if (cmpAB < 0)
                    return cmpBA > 0;
                // cmpAB == 0
                return cmpBA == 0;
            });

        prop.VerboseCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: Transitivity — if A > B and B > C then A > C.
    /// </summary>
    [Fact]
    public void Version_ComparisonIsTransitive()
    {
        var prop = Prop.ForAll(
            VersionArbitrary(),
            VersionArbitrary(),
            VersionArbitrary(),
            (Version a, Version b, Version c) =>
            {
                var cmpAB = a.CompareTo(b);
                var cmpBC = b.CompareTo(c);
                var cmpAC = a.CompareTo(c);

                // If A > B and B > C then A > C
                if (cmpAB > 0 && cmpBC > 0)
                    return cmpAC > 0;

                // If A < B and B < C then A < C
                if (cmpAB < 0 && cmpBC < 0)
                    return cmpAC < 0;

                // If A == B and B == C then A == C
                if (cmpAB == 0 && cmpBC == 0)
                    return cmpAC == 0;

                // Other combinations — no constraint to check, property holds vacuously
                return true;
            });

        prop.VerboseCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: Consistency with numeric ordering — version comparison follows
    /// lexicographic ordering of (Major, Minor, Build, Revision) tuples.
    /// A version with a higher major is greater; if major is equal, compare minor; etc.
    /// </summary>
    [Fact]
    public void Version_ComparisonConsistentWithNumericOrdering()
    {
        var prop = Prop.ForAll(
            VersionArbitrary(),
            VersionArbitrary(),
            (Version a, Version b) =>
            {
                var cmp = a.CompareTo(b);

                // Compute expected comparison via lexicographic tuple ordering
                var expected = CompareTuples(
                    (a.Major, a.Minor, a.Build, a.Revision),
                    (b.Major, b.Minor, b.Build, b.Revision));

                return Math.Sign(cmp) == Math.Sign(expected);
            });

        prop.VerboseCheckThrowOnFailure();
    }

    /// <summary>
    /// Lexicographic comparison of version component tuples.
    /// </summary>
    private static int CompareTuples(
        (int Major, int Minor, int Build, int Revision) a,
        (int Major, int Minor, int Build, int Revision) b)
    {
        if (a.Major != b.Major) return a.Major.CompareTo(b.Major);
        if (a.Minor != b.Minor) return a.Minor.CompareTo(b.Minor);
        if (a.Build != b.Build) return a.Build.CompareTo(b.Build);
        return a.Revision.CompareTo(b.Revision);
    }
}
