using System.Text.Json.Serialization;

namespace DLSS_Swapper.Data.EAApp;

internal class GameSearchResult
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("logoImage")]
    public GameSearchResultImage? LogoImage { get; set; }

    [JsonPropertyName("keyArtImage")]
    public GameSearchResultImage? KeyArtImage { get; set; }

    [JsonPropertyName("packArtImage")]
    public GameSearchResultImage? PackArtImage { get; set; }
}

internal class GameSearchResultImage
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;
}
