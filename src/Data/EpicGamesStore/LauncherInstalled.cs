using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DLSS_Swapper.Data.EpicGamesStore
{
    // Did not end up using LauncherInstalled.dat but keeping this here incase we ever do wish to.
    /*
    internal class LauncherInstalled
    {
        [JsonPropertyName("InstallationList")]
        public List<LancherInstalledItem> InstallationList { get; set; } = new List<LancherInstalledItem>();
    }

    internal class LancherInstalledItem
    {
        [JsonPropertyName("InstallLocation")]
        public string InstallLocation { get; set; } = string.Empty;

        [JsonPropertyName("NamespaceId")]
        public string NamespaceId { get; set; } = string.Empty;

        [JsonPropertyName("ItemId")]
        public string ItemId { get; set; } = string.Empty;

        [JsonPropertyName("ArtifactId")]
        public string ArtifactId { get; set; } = string.Empty;

        [JsonPropertyName("AppVersion")]
        public string AppVersion { get; set; } = string.Empty;

        [JsonPropertyName("AppName")]
        public string AppName { get; set; } = string.Empty;
    }
    */
}
