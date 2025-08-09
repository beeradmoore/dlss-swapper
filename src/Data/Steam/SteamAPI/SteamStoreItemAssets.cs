using System.Text.Json.Serialization;

namespace DLSS_Swapper.Data.Steam.SteamAPI;

internal class SteamStoreItemAssets
{
    [JsonPropertyName("asset_url_format")]
    public string AssetUrlFormat { get; set; } = string.Empty;

    [JsonPropertyName("main_capsule")]
    public string MainCapsule { get; set; } = string.Empty;

    [JsonPropertyName("small_capsule")]
    public string SmallCapsule { get; set; } = string.Empty;

    [JsonPropertyName("header")]
    public string Header { get; set; } = string.Empty;

    [JsonPropertyName("page_background")]
    public string PageBackground { get; set; } = string.Empty;

    [JsonPropertyName("hero_capsule")]
    public string HeroCapsule { get; set; } = string.Empty;

    [JsonPropertyName("hero_capsule_2x")]
    public string HeroCapsule2x { get; set; } = string.Empty;

    [JsonPropertyName("library_capsule")]
    public string LibraryCapsule { get; set; } = string.Empty;

    [JsonPropertyName("library_capsule_2x")]
    public string LibraryCapsule2x { get; set; } = string.Empty;

    [JsonPropertyName("library_hero")]
    public string LibraryHero { get; set; } = string.Empty;

    [JsonPropertyName("library_hero_2x")]
    public string LibraryHero2x { get; set; } = string.Empty;

    [JsonPropertyName("community_icon")]
    public string CommunityIcon { get; set; } = string.Empty;

    [JsonPropertyName("page_background_path")]
    public string PageBackgroundPath { get; set; } = string.Empty;

    [JsonPropertyName("raw_page_background")]
    public string RawPageBackground { get; set; } = string.Empty;
}
