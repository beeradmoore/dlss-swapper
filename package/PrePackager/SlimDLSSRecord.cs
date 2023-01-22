using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PrePackager
{
    internal class SlimDLSSRecords
    {
        [JsonPropertyName("stable")]
        public List<SlimDLSSRecord> Stable { get; set; } = new List<SlimDLSSRecord>();

        [JsonPropertyName("experimental")]
        public List<SlimDLSSRecord> Experimental { get; set; } = new List<SlimDLSSRecord>();
    }

    internal class SlimDLSSRecord
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = String.Empty;

        [JsonPropertyName("version_number")]
        public ulong VersionNumber { get; set; } = 0;

        [JsonPropertyName("additional_label")]
        public string AdditionalLabel { get; set; } = String.Empty;

        [JsonPropertyName("md5_hash")]
        public string MD5Hash { get; set; } = String.Empty;

        [JsonPropertyName("zip_md5_hash")]
        public string ZipMD5Hash { get; set; } = String.Empty;

        [JsonPropertyName("download_url")]
        public string DownloadUrl { get; set; } = String.Empty;

        [JsonPropertyName("file_description")]
        public string FileDescription { get; set; } = String.Empty;

        [JsonIgnore]
        public DateTime SignedDateTime { get; set; } = DateTime.MinValue;

        [JsonPropertyName("is_signature_valid")]
        public bool IsSignatureValid { get; set; } = false;

        [JsonPropertyName("file_size")]
        public long FileSize { get; set; } = 0;

        [JsonPropertyName("zip_file_size")]
        public long ZipFileSize { get; set; } = 0;
    }
}
