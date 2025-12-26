using System.Text.Json.Serialization;

namespace DLSS_Swapper.Data.EpicGamesStore;

internal class ManifestFile
{
    // Many items are disabled as we don't need them yet.

    [JsonPropertyName("FormatVersion")]
    public int FormatVersion { get; set; }

    //[JsonPropertyName("bIsIncompleteInstall")]
    //public bool bIsIncompleteInstall { get; set; }

    //[JsonPropertyName("LaunchCommand")]
    //public string LaunchCommand { get; set; } = string.Empty;

    //[JsonPropertyName("LaunchExecutable")]
    //public string LaunchExecutable { get; set; } = string.Empty;

    //[JsonPropertyName("ManifestLocation")]
    //public string ManifestLocation { get; set; } = string.Empty;

    //[JsonPropertyName("ManifestHash")]
    //public string ManifestHash { get; set; } = string.Empty;

    //[JsonPropertyName("bIsApplication")]
    //public bool IsApplication { get; set; }

    //[JsonPropertyName("bIsExecutable")]
    //public bool IsExecutable { get; set; }

    //[JsonPropertyName("bIsManaged")]
    //public bool IsManaged { get; set; }

    //[JsonPropertyName("bNeedsValidation")]
    //public bool NeedsValidation { get; set; }

    //[JsonPropertyName("bRequiresAuth")]
    //public bool RequiresAuth { get; set; }

    //[JsonPropertyName("bAllowMultipleInstances")]
    //public bool AllowMultipleInstances { get; set; }

    //[JsonPropertyName("bCanRunOffline")]
    //public bool CanRunOffline { get; set; }

    //[JsonPropertyName("bAllowUriCmdArgs")]
    //public int AllowUriCmdArgs { get; set; }

    //[JsonPropertyName("BaseURLs")]
    //public string[] BaseURLs { get; set; }

    //[JsonPropertyName("BuildLabel")]
    //public string BuildLabel { get; set; }

    [JsonPropertyName("AppCategories")]
    public string[] AppCategories { get; set; } = new string[0];

    //[JsonPropertyName("ChunkDbs")]
    //public List<???> ChunkDbs { get; set; }

    //[JsonPropertyName("CompatibleApps")]
    //public List<???> CompatibleApps { get; set; }

    [JsonPropertyName("DisplayName")]
    public string DisplayName { get; set; } = string.Empty;

    //[JsonPropertyName("InstallationGuid")]
    //public string InstallationGuid { get; set; } = string.Empty;

    [JsonPropertyName("InstallLocation")]
    public string InstallLocation { get; set; } = string.Empty;

    //[JsonPropertyName("InstallSessionId")]
    //public string InstallSessionId { get; set; } = string.Empty;

    //[JsonPropertyName("InstallTags")]
    //public List<???> InstallTags { get; set; }

    //[JsonPropertyName("InstallComponents")]
    //public List<???> InstallComponents { get; set; }

    //[JsonPropertyName("HostInstallationGuid")]
    //public string HostInstallationGuid { get; set; } = string.Empty;

    //[JsonPropertyName("PrereqIds")]
    //public List<???> PrereqIds { get; set; }

    //[JsonPropertyName("PrereqSHA1Hash")]
    //public string PrereqSHA1Hash { get; set; } = string.Empty;

    //[JsonPropertyName("LastPrereqSucceededSHA1Hash")]
    //public string LastPrereqSucceededSHA1Hash { get; set; } = string.Empty;

    //[JsonPropertyName("StagingLocation")]
    //public string StagingLocation { get; set; } = string.Empty;

    //[JsonPropertyName("TechnicalType")]
    //public string TechnicalType { get; set; } = string.Empty;

    //[JsonPropertyName("VaultThumbnailUrl")]
    //public string VaultThumbnailUrl { get; set; } = string.Empty;

    //[JsonPropertyName("VaultTitleText")]
    //public string VaultTitleText { get; set; } = string.Empty;

    //[JsonPropertyName("InstallSize")]
    //public long InstallSize { get; set; }

    //[JsonPropertyName("MainWindowProcessName")]
    //public string MainWindowProcessName { get; set; } = string.Empty;

    //[JsonPropertyName("ProcessNames")]
    //public List<???> ProcessNames { get; set; }

    //[JsonPropertyName("BackgroundProcessNames")]
    //public List<???> BackgroundProcessNames { get; set; }

    //[JsonPropertyName("MandatoryAppFolderName")]
    //public string MandatoryAppFolderName { get; set; } = string.Empty;

    //[JsonPropertyName("OwnershipToken")]
    //public string OwnershipToken { get; set; } = string.Empty;

    //[JsonPropertyName("CatalogNamespace")]
    //public string CatalogNamespace { get; set; } = string.Empty;

    [JsonPropertyName("CatalogItemId")]
    public string CatalogItemId { get; set; } = string.Empty;

    [JsonPropertyName("AppName")]
    public string AppName { get; set; } = string.Empty;

    //[JsonPropertyName("AppVersionString")]
    //public string AppVersionString { get; set; } = string.Empty;

    //[JsonPropertyName("MainGameCatalogNamespace")]
    //public string MainGameCatalogNamespace { get; set; } = string.Empty;

    //[JsonPropertyName("MainGameCatalogItemId")]
    //public string MainGameCatalogItemId { get; set; } = string.Empty;

    [JsonPropertyName("MainGameAppName")]
    public string MainGameAppName { get; set; } = string.Empty;

    //[JsonPropertyName("AllowedUriEnvVars")]
    //public List<???> AllowedUriEnvVars { get; set; }
}
