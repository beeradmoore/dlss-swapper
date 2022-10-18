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
        public string InstallLocation { get; set; } = String.Empty;

        [JsonPropertyName("NamespaceId")]
        public string NamespaceId { get; set; } = String.Empty;

        [JsonPropertyName("ItemId")]
        public string ItemId { get; set; } = String.Empty;

        [JsonPropertyName("ArtifactId")]
        public string ArtifactId { get; set; } = String.Empty;

        [JsonPropertyName("AppVersion")]
        public string AppVersion { get; set; } = String.Empty;

        [JsonPropertyName("AppName")]
        public string AppName { get; set; } = String.Empty;
    }
    */
}
