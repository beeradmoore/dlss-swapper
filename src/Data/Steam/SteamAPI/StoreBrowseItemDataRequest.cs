using System.Text.Json.Serialization;

namespace DLSS_Swapper.Data.Steam.SteamAPI;

internal class StoreBrowseItemDataRequest
{
    [JsonPropertyName("include_assets")]
    public bool IncludeAssets { get; set; }

    /*
    [JsonPropertyName("include_release")]
    public bool IncludeRelease { get; set; }

    [JsonPropertyName("include_platforms")]
    public bool IncludePlatforms { get; set; }

    [JsonPropertyName("include_all_purchase_options")]
    public bool IncludeAllPurchaseOptions { get; set; }

    [JsonPropertyName("include_screenshots")]
    public bool IncludeScreenshots { get; set; }

    [JsonPropertyName("include_trailers")]
    public bool IncludeTrailers { get; set; }

    [JsonPropertyName("include_ratings")]
    public bool IncludeRatings { get; set; }

    //[JsonPropertyName("include_tag_count")]
    //public int IncludeTagCount { get; set; }

    [JsonPropertyName("include_reviews")]
    public bool IncludeReviews { get; set; }

    [JsonPropertyName("include_basic_info")]
    public bool IncludeBasicInfo { get; set; }

    [JsonPropertyName("include_supported_languages")]
    public bool IncludeSupportedLanguages { get; set; }

    [JsonPropertyName("include_full_description")]
    public bool IncludeFullDescription { get; set; }

    [JsonPropertyName("include_included_items")]
    public bool IncludeIncludedItems { get; set; }

    //[JsonPropertyName("included_item_data_request")]
    //public StoreBrowseItemDataRequest IncludedItemDataRequest { get; set; }

    [JsonPropertyName("include_assets_without_overrides")]
    public bool IncludeAssetsWithoutOverrides { get; set; }

    [JsonPropertyName("apply_user_filters")]
    public bool ApplyUserFilters { get; set; }

    [JsonPropertyName("include_links")]
    public bool IncludeLinks { get; set; }
    */
}
