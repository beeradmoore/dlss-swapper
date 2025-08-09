
using System.Text.Json.Serialization;

namespace DLSS_Swapper.Data.Steam.SteamAPI;

internal class StoreItemId
{
    [JsonPropertyName("appid")]
    public int AppId { get; set; }

    /*
    [JsonPropertyName("packageid")]
    public int PackageId { get; set; }

    [JsonPropertyName("bundleid")]
    public int BundleId { get; set; }

    [JsonPropertyName("tagid")]
    public int TagId { get; set; }

    [JsonPropertyName("creatorid")]
    public int CreatorId { get; set; }

    [JsonPropertyName("hubcategoryid")]
    public int HubCategoryId { get; set; }
    */
}
