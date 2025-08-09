using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DLSS_Swapper.Data.GOG;

/// <summary>
/// This class is the json repsonse from https://embed.gog.com/games/ajax/filtered?mediaType=game&search=gameTitleGoesHere
/// </summary>
class GOGEmbedFilteredResponse
{
    [JsonPropertyName("products")]
    public GOGEmbedFilteredProduct[] Products { get; set; } = Array.Empty<GOGEmbedFilteredProduct>();

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("totalResults")]
    public int TotalResults { get; set; }

    [JsonPropertyName("totalGamesFound")]
    public int TotalGamesFound { get; set; }
}

class GOGEmbedFilteredProduct
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("image")]
    public string Image { get; set; } = string.Empty;

    [JsonPropertyName("boxImage")]
    public string BoxImage { get; set; } = string.Empty;
}
