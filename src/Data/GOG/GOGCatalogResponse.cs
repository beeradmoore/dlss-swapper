using System;
using System.Text.Json.Serialization;

namespace DLSS_Swapper.Data.GOG;

/// <summary>
/// This class is the json repsonse from https://catalog.gog.com/v1/catalog?order=desc:score&productType=in:game&query=like:gameTitleGoesHere
/// </summary>
class GOGCatalogResponse
{
    [JsonPropertyName("products")]
    public GOGCatalogProduct[] Products { get; set; } = Array.Empty<GOGCatalogProduct>();

    [JsonPropertyName("pages")]
    public int Pages { get; set; }

    [JsonPropertyName("productCount")]
    public int ProductCount { get; set; }

    [JsonPropertyName("currentlyShownProductCount")]
    public int CurrentlyShownProductCount { get; set; }
}

class GOGCatalogProduct
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("coverHorizontal")]
    public string CoverHorizontal { get; set; } = string.Empty;

    [JsonPropertyName("coverVertical")]
    public string CoverVertical { get; set; } = string.Empty;
}
