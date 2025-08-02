
using System;
using System.Globalization;

namespace DLSS_Swapper;

internal static class BuildInfo
{
    public static string GitBranch { get; } = string.Empty;
    public static string GitCommit { get; } = string.Empty;
    public static string GitTag { get; } = string.Empty;
    public static long BuildTimestamp { get; }


    public static string GitCommitShort
    {
        get
        {
            if (string.IsNullOrWhiteSpace(GitCommit) || GitCommit.Length < 7)
            {
                return string.Empty;
            }

            return GitCommit.Substring(0, 7);
        }
    }
    public static DateTime BuildDateTime => DateTimeOffset.FromUnixTimeSeconds(BuildTimestamp).LocalDateTime;
    public static string BuildDateTimeFormattedString => BuildDateTime.ToString("g", CultureInfo.CurrentCulture);
    public static bool IsFromTagBuild => string.IsNullOrWhiteSpace(GitTag) == false;
}
