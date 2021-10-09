using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DLSS_Swapper.Data
{
    internal class DLSSRecord
    {
        [JsonIgnore]
        public string Filename { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("version_number")]
        public ulong VersionNumber { get; set; }

        [JsonPropertyName("additional_label")]
        public string AdditionalLabel { get; set; } = String.Empty;

        [JsonPropertyName("md5_hash")]
        public string MD5Hash { get; set; }

        [JsonPropertyName("zip_md5_hash")]
        public string ZipMD5Hash { get; set; }

        [JsonPropertyName("download_url")]
        public string DownloadUrl { get; set; } = String.Empty;

        [JsonPropertyName("file_description")]
        public string FileDescription { get; set; } = String.Empty;

        [JsonIgnore]
        public DateTime SignedDateTime { get; set; } = DateTime.MinValue;

        [JsonPropertyName("is_signature_valid")]
        public bool IsSignatureValid { get; set; }

        [JsonPropertyName("file_size")]
        public long FileSize { get; set; }

        [JsonPropertyName("zip_file_size")]
        public long ZipFileSize { get; set; }

        public bool ValidateLocalSignature()
        {
            var filename = "";
            return WinTrust.VerifyEmbeddedSignature(filename);
        }
    }
}
