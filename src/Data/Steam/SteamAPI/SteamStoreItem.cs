using System.Text.Json.Serialization;

namespace DLSS_Swapper.Data.Steam.SteamAPI;

internal class SteamStoreItem
{
    /*
    [JsonPropertyName("item_type")]
    public int ItemType { get; set; }
    */

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("success")]
    public int Success { get; set; }

    /*
    [JsonPropertyName("visible")]
    public bool Visible { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("store_url_path")]
    public string StoreUrlPath { get; set; } = string.Empty;
    */

    [JsonPropertyName("appid")]
    public int AppId { get; set; }

    /*
    [JsonPropertyName("type")]
    public int Type { get; set; }

    [JsonPropertyName("related_items")]
    public SteamStoreItemRelatedItems RelatedItems { get; set; }

    [JsonPropertyName("categories")]
    public SteamStoreItemCategories Categories { get; set; }
    */

    [JsonPropertyName("assets")]
    public SteamStoreItemAssets? Assets { get; set; }

}
