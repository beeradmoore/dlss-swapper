using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DLSS_Swapper.Data.EpicGameStore
{
    internal class ManifestFile
    {
        // Many items are disabled as we don't need them yet.

        [JsonPropertyName("FormatVersion")]
        public int FormatVersion { get; set; }

        //[JsonPropertyName("bIsIncompleteInstall")]
        //public bool bIsIncompleteInstall { get; set; }

        //[JsonPropertyName("LaunchCommand")]
        //public string LaunchCommand { get; set; } = String.Empty;

        //[JsonPropertyName("LaunchExecutable")]
        //public string LaunchExecutable { get; set; } = String.Empty;

        //[JsonPropertyName("ManifestLocation")]
        //public string ManifestLocation { get; set; } = String.Empty;

        //[JsonPropertyName("ManifestHash")]
        //public string ManifestHash { get; set; } = String.Empty;

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
        public string DisplayName { get; set; } = String.Empty;

        //[JsonPropertyName("InstallationGuid")]
        //public string InstallationGuid { get; set; } = String.Empty;

        [JsonPropertyName("InstallLocation")]
        public string InstallLocation { get; set; } = String.Empty;

        //[JsonPropertyName("InstallSessionId")]
        //public string InstallSessionId { get; set; } = String.Empty;

        //[JsonPropertyName("InstallTags")]
        //public List<???> InstallTags { get; set; }

        //[JsonPropertyName("InstallComponents")]
        //public List<???> InstallComponents { get; set; }

        //[JsonPropertyName("HostInstallationGuid")]
        //public string HostInstallationGuid { get; set; } = String.Empty;

        //[JsonPropertyName("PrereqIds")]
        //public List<???> PrereqIds { get; set; }

        //[JsonPropertyName("PrereqSHA1Hash")]
        //public string PrereqSHA1Hash { get; set; } = String.Empty;

        //[JsonPropertyName("LastPrereqSucceededSHA1Hash")]
        //public string LastPrereqSucceededSHA1Hash { get; set; } = String.Empty;

        //[JsonPropertyName("StagingLocation")]
        //public string StagingLocation { get; set; } = String.Empty;

        //[JsonPropertyName("TechnicalType")]
        //public string TechnicalType { get; set; } = String.Empty;

        //[JsonPropertyName("VaultThumbnailUrl")]
        //public string VaultThumbnailUrl { get; set; } = String.Empty;

        //[JsonPropertyName("VaultTitleText")]
        //public string VaultTitleText { get; set; } = String.Empty;

        //[JsonPropertyName("InstallSize")]
        //public long InstallSize { get; set; }

        //[JsonPropertyName("MainWindowProcessName")]
        //public string MainWindowProcessName { get; set; } = String.Empty;

        //[JsonPropertyName("ProcessNames")]
        //public List<???> ProcessNames { get; set; }

        //[JsonPropertyName("BackgroundProcessNames")]
        //public List<???> BackgroundProcessNames { get; set; }

        //[JsonPropertyName("MandatoryAppFolderName")]
        //public string MandatoryAppFolderName { get; set; } = String.Empty;

        //[JsonPropertyName("OwnershipToken")]
        //public string OwnershipToken { get; set; } = String.Empty;

        //[JsonPropertyName("CatalogNamespace")]
        //public string CatalogNamespace { get; set; } = String.Empty;

        [JsonPropertyName("CatalogItemId")]
        public string CatalogItemId { get; set; } = String.Empty;

        //[JsonPropertyName("AppName")]
        //public string AppName { get; set; } = String.Empty;

        //[JsonPropertyName("AppVersionString")]
        //public string AppVersionString { get; set; } = String.Empty;

        //[JsonPropertyName("MainGameCatalogNamespace")]
        //public string MainGameCatalogNamespace { get; set; } = String.Empty;

        //[JsonPropertyName("MainGameCatalogItemId")]
        //public string MainGameCatalogItemId { get; set; } = String.Empty;

        //[JsonPropertyName("MainGameAppName")]
        //public string MainGameAppName { get; set; } = String.Empty;

        //[JsonPropertyName("AllowedUriEnvVars")]
        //public List<???> AllowedUriEnvVars { get; set; }
    }
}
