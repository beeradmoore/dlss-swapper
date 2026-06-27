using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DLSS_Swapper.Data.GitHub;
using DLSS_Swapper.Extensions;
using DLSS_Swapper.Helpers;

namespace DLSS_Swapper.Data.Streamline;

internal class StreamlineManager
{
    public static StreamlineManager Instance { get; } = new StreamlineManager();

    /// <summary>
    /// Known Streamline DLL filenames that are part of the SDK.
    /// </summary>
    public static readonly string[] KnownStreamlineDlls = new[]
    {
        "sl.common.dll",
        "sl.deepdvc.dll",
        "sl.directsr.dll",
        "sl.dlss.dll",
        "sl.dlss_d.dll",
        "sl.dlss_g.dll",
        "sl.interposer.dll",
        "sl.nis.dll",
        "sl.nvperf.dll",
        "sl.pcl.dll",
        "sl.reflex.dll",
    };

    /// <summary>
    /// The DLL used to determine the Streamline SDK version.
    /// </summary>
    public const string VersionIndicatorDll = "sl.interposer.dll";

    /// <summary>
    /// The tag of the latest fetched release (e.g., "v2.7.0").
    /// </summary>
    public string? LatestReleaseTag { get; private set; }

    /// <summary>
    /// The version string from the staged sl.interposer.dll.
    /// </summary>
    public string? StagedVersion { get; private set; }

    /// <summary>
    /// Whether staged DLLs are available for use.
    /// </summary>
    public bool IsStagingReady { get; private set; }

    private StreamlineManager()
    {
        // Check if there is an existing staging directory from a previous run.
        var lastTag = Settings.Instance.StreamlineLastCheckedTag;
        if (string.IsNullOrEmpty(lastTag) == false)
        {
            var stagingDir = Path.Combine(Storage.GetStreamlineStagingPath(), lastTag);
            var versionIndicatorPath = Path.Combine(stagingDir, VersionIndicatorDll);
            if (Directory.Exists(stagingDir) && File.Exists(versionIndicatorPath))
            {
                LatestReleaseTag = lastTag;
                IsStagingReady = true;
                ReadStagedVersion();
            }
        }
    }

    /// <summary>
    /// Fetches the latest release from GitHub, downloads and extracts if newer.
    /// </summary>
    /// <returns>True if staging is ready (either freshly downloaded or previously cached).</returns>
    public async Task<bool> FetchAndStageLatestAsync()
    {
        try
        {
            var release = await FetchLatestReleaseAsync().ConfigureAwait(false);
            if (release is null)
            {
                Logger.Warning("Could not fetch latest Streamline release from GitHub.");
                return IsStagingReady;
            }

            var tag = release.TagName;
            if (string.IsNullOrWhiteSpace(tag))
            {
                Logger.Error("Streamline release has an empty tag name.");
                return IsStagingReady;
            }

            // If we already have this tag staged, just update the check time and return.
            if (string.Equals(tag, Settings.Instance.StreamlineLastCheckedTag, StringComparison.Ordinal))
            {
                var stagingDir = Path.Combine(Storage.GetStreamlineStagingPath(), tag);
                var versionIndicatorPath = Path.Combine(stagingDir, VersionIndicatorDll);
                if (Directory.Exists(stagingDir) && File.Exists(versionIndicatorPath))
                {
                    LatestReleaseTag = tag;
                    IsStagingReady = true;
                    ReadStagedVersion();
                    Settings.Instance.StreamlineLastCheckedTime = DateTime.UtcNow;
                    return true;
                }
            }

            // Find the ZIP asset to download.
            var zipAsset = release.Assets.FirstOrDefault(a =>
                a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
                a.ContentType.Contains("zip", StringComparison.OrdinalIgnoreCase));

            // Fallback: try any .zip asset regardless of content type.
            zipAsset ??= release.Assets.FirstOrDefault(a =>
                a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));

            if (zipAsset is null)
            {
                Logger.Error("Streamline release does not contain a ZIP asset.");
                return IsStagingReady;
            }

            // Download and extract.
            var success = await DownloadAndExtractAsync(zipAsset, tag).ConfigureAwait(false);
            if (success)
            {
                LatestReleaseTag = tag;
                IsStagingReady = true;
                ReadStagedVersion();
                Settings.Instance.StreamlineLastCheckedTag = tag;
                Settings.Instance.StreamlineLastCheckedTime = DateTime.UtcNow;
            }

            return IsStagingReady;
        }
        catch (Exception err)
        {
            Logger.Error(err, "Failed to fetch and stage latest Streamline release.");
            return IsStagingReady;
        }
    }

    /// <summary>
    /// Returns the full path to a staged DLL by filename, or null if not staged.
    /// </summary>
    public string? GetStagedDllPath(string dllFileName)
    {
        if (IsStagingReady == false || string.IsNullOrEmpty(LatestReleaseTag))
        {
            return null;
        }

        var path = Path.Combine(Storage.GetStreamlineStagingPath(), LatestReleaseTag, dllFileName);
        if (File.Exists(path))
        {
            return path;
        }

        return null;
    }

    /// <summary>
    /// Returns the staging directory path for the current version.
    /// </summary>
    public string GetStagingDirectory()
    {
        if (string.IsNullOrEmpty(LatestReleaseTag))
        {
            return Storage.GetStreamlineStagingPath();
        }

        return Path.Combine(Storage.GetStreamlineStagingPath(), LatestReleaseTag);
    }

    /// <summary>
    /// Queries the GitHub API for the latest Streamline release.
    /// Caches the response JSON to disk.
    /// </summary>
    private async Task<GitHubRelease?> FetchLatestReleaseAsync()
    {
        var cachedReleasePath = Path.Combine(Storage.GetDynamicJsonFolder(), "streamline_release.json");

        try
        {
            using (var memoryStream = new MemoryStream())
            {
                var fileDownloader = new FileDownloader("https://api.github.com/repos/NVIDIA-RTX/Streamline/releases/latest", 0);
                await fileDownloader.DownloadFileToStreamAsync(memoryStream).ConfigureAwait(false);

                memoryStream.Position = 0;

                var release = JsonSerializer.Deserialize(memoryStream, SourceGenerationContext.Default.GitHubRelease);
                if (release is null)
                {
                    Logger.Error("Could not deserialize Streamline GitHub release data.");
                    return LoadCachedRelease(cachedReleasePath);
                }

                // Cache the raw JSON to disk.
                memoryStream.Position = 0;
                Storage.CreateDirectoryForFileIfNotExists(cachedReleasePath);
                using (var fileStream = File.Create(cachedReleasePath))
                {
                    await memoryStream.CopyToAsync(fileStream).ConfigureAwait(false);
                }

                return release;
            }
        }
        catch (Exception err)
        {
            Logger.Error(err, "Failed to fetch Streamline release from GitHub API.");
            return LoadCachedRelease(cachedReleasePath);
        }
    }

    /// <summary>
    /// Loads a previously cached release JSON from disk.
    /// </summary>
    private static GitHubRelease? LoadCachedRelease(string cachedReleasePath)
    {
        if (File.Exists(cachedReleasePath) == false)
        {
            return null;
        }

        try
        {
            using (var fileStream = File.OpenRead(cachedReleasePath))
            {
                return JsonSerializer.Deserialize(fileStream, SourceGenerationContext.Default.GitHubRelease);
            }
        }
        catch (Exception err)
        {
            Logger.Error(err, "Failed to load cached Streamline release JSON.");
            return null;
        }
    }

    /// <summary>
    /// Downloads the ZIP asset and extracts known Streamline DLLs from bin/x64/ to the staging directory.
    /// </summary>
    private async Task<bool> DownloadAndExtractAsync(GitHubReleaseAsset zipAsset, string tag)
    {
        var stagingDir = Path.Combine(Storage.GetStreamlineStagingPath(), tag);
        var tempZipPath = Path.Combine(Storage.GetTemp(), $"streamline_{tag}.zip");

        try
        {
            // Download the ZIP to a temp file.
            Storage.CreateDirectoryForFileIfNotExists(tempZipPath);
            using (var fileStream = File.Create(tempZipPath))
            {
                var fileDownloader = new FileDownloader(zipAsset.BrowserDownloadUrl, 0);
                var didDownload = await fileDownloader.DownloadFileToStreamAsync(fileStream).ConfigureAwait(false);
                if (didDownload == false)
                {
                    Logger.Error("Failed to download Streamline ZIP asset.");
                    return false;
                }
            }

            // Extract DLLs from bin/x64/ within the ZIP.
            return ExtractDllsFromZip(tempZipPath, stagingDir);
        }
        catch (Exception err)
        {
            Logger.Error(err, "Failed to download and extract Streamline ZIP.");

            // Clean up partial extraction.
            CleanupDirectory(stagingDir);

            return false;
        }
        finally
        {
            // Clean up the temp ZIP file.
            try
            {
                if (File.Exists(tempZipPath))
                {
                    File.Delete(tempZipPath);
                }
            }
            catch (Exception err)
            {
                Logger.Warning($"Could not delete temp ZIP file: {tempZipPath}. {err.Message}");
            }
        }
    }

    /// <summary>
    /// Extracts known Streamline DLLs from the bin/x64/ path within the ZIP archive.
    /// </summary>
    private bool ExtractDllsFromZip(string zipPath, string stagingDir)
    {
        try
        {
            using (var fileStream = File.OpenRead(zipPath))
            using (var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read))
            {
                // Find entries under bin/x64/ that match known DLL names.
                var binX64Entries = zipArchive.Entries
                    .Where(e => e.FullName.Contains("bin/x64/", StringComparison.OrdinalIgnoreCase) &&
                                KnownStreamlineDlls.Contains(e.Name, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                if (binX64Entries.Count == 0)
                {
                    Logger.Error("Streamline ZIP does not contain expected bin/x64/ path with known DLLs.");
                    return false;
                }

                // Create the staging directory.
                Storage.CreateDirectoryIfNotExists(stagingDir);

                var extractedCount = 0;
                foreach (var entry in binX64Entries)
                {
                    var destPath = Path.Combine(stagingDir, entry.Name);
                    entry.ExtractToFile(destPath, overwrite: true);
                    extractedCount++;
                }

                Logger.Info($"Extracted {extractedCount} Streamline DLLs to {stagingDir}.");

                // Verify the version indicator DLL was extracted.
                var versionIndicatorPath = Path.Combine(stagingDir, VersionIndicatorDll);
                if (File.Exists(versionIndicatorPath) == false)
                {
                    Logger.Error($"Version indicator DLL ({VersionIndicatorDll}) was not found after extraction.");
                    CleanupDirectory(stagingDir);
                    return false;
                }

                return true;
            }
        }
        catch (InvalidDataException err)
        {
            Logger.Error(err, "Streamline ZIP archive is corrupt.");
            CleanupDirectory(stagingDir);
            return false;
        }
        catch (Exception err)
        {
            Logger.Error(err, "Failed to extract DLLs from Streamline ZIP.");
            CleanupDirectory(stagingDir);
            return false;
        }
    }

    /// <summary>
    /// Reads the FileVersionInfo from the staged sl.interposer.dll to populate StagedVersion.
    /// </summary>
    private void ReadStagedVersion()
    {
        try
        {
            var versionIndicatorPath = Path.Combine(Storage.GetStreamlineStagingPath(), LatestReleaseTag!, VersionIndicatorDll);
            if (File.Exists(versionIndicatorPath))
            {
                var fileVersionInfo = FileVersionInfo.GetVersionInfo(versionIndicatorPath);
                StagedVersion = fileVersionInfo.GetFormattedFileVersion();
                Logger.Info($"Staged Streamline version: {StagedVersion}");
            }
            else
            {
                StagedVersion = null;
                Logger.Warning($"Staged version indicator DLL not found at {versionIndicatorPath}.");
            }
        }
        catch (Exception err)
        {
            Logger.Error(err, "Failed to read staged Streamline version.");
            StagedVersion = null;
        }
    }

    /// <summary>
    /// Safely deletes a directory and all its contents.
    /// </summary>
    private static void CleanupDirectory(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
        catch (Exception err)
        {
            Logger.Error(err, $"Failed to clean up directory: {directory}");
        }
    }
}
