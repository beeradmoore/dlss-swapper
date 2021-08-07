using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DLSS_Swapper.Data.TechPowerUp
{
    public record TechPowerUpDownloadItem
    {
        [JsonPropertyName("filename")]
        public string FileName { get; set; }

        [JsonPropertyName("md5_hash")]
        public string MD5Hash { get;  set; }
    }
}
