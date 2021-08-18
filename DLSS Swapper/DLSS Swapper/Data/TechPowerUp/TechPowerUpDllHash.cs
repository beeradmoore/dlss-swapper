using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DLSS_Swapper.Data.TechPowerUp
{
    public class TechPowerUpDllHash
    {
        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("version_number")]
        public ulong VersionNumber { get; set; }

        [JsonPropertyName("sha1")]
        public string SHA1Hash { get; set; }

        [JsonPropertyName("md5")]
        public string MD5Hash { get; set; }
    }
}
