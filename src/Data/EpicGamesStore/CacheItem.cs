using System.Text.Json.Serialization;

namespace DLSS_Swapper.Data.EpicGamesStore
{
    internal class CacheItem
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("keyImages")]
        public CacheItemKeyImage[] KeyImages { get; set; } = new CacheItemKeyImage[0];


        internal class CacheItemKeyImage
        {
            [JsonPropertyName("type")]
            public string Type { get; set; } = string.Empty;

            [JsonPropertyName("url")]
            public string Url { get; set; } = string.Empty;

            [JsonPropertyName("width")]
            public int Width { get; set; }

            [JsonPropertyName("height")]
            public int Height { get; set; }

            [JsonPropertyName("size")]
            public int Size { get; set; }

            [JsonPropertyName("uploadedDate")]
            public string UploadedDate { get; set; } = string.Empty;

            [JsonPropertyName("md5")]
            public string MD5 { get; set; } = string.Empty;
        }
    }
}
