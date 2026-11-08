using System.Text.Json.Serialization;

namespace DLSS_Swapper.Data.EpicGamesStore;

// Represents a single line in the new (eglv2 / "beta") Epic Games Launcher's
// %LOCALAPPDATA%\com.epicgames.eglv2\installations.jsonl file. This file is an
// append-only log, so multiple lines can share the same InstallationId; the last
// line for a given InstallationId reflects its current state.
internal class InstallationEntry
{
    [JsonPropertyName("installationId")]
    public string InstallationId { get; set; } = string.Empty;

    [JsonPropertyName("complete")]
    public bool Complete { get; set; }

    [JsonPropertyName("dir")]
    public string Dir { get; set; } = string.Empty;

    [JsonPropertyName("namespace")]
    public string Namespace { get; set; } = string.Empty;

    [JsonPropertyName("itemId")]
    public string ItemId { get; set; } = string.Empty;

    [JsonPropertyName("artifactId")]
    public string ArtifactId { get; set; } = string.Empty;

    // Present on sub-components/DLC that are installed alongside a base game rather
    // than being a standalone installation.
    [JsonPropertyName("parentInstallationId")]
    public string ParentInstallationId { get; set; } = string.Empty;

    [JsonPropertyName("writtenAt")]
    public string WrittenAt { get; set; } = string.Empty;
}
