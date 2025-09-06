using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EAAppGameListBuilder;

public class SearchResponse
{
    [JsonPropertyName("data")]
    public Data Data { get; set; }
}

public class Data
{
    [JsonPropertyName("gameSearch")]
    public GameSearchResultsOffsetPage? GameSearch { get; set; }
}

public class GameSearchResultsOffsetPage
{
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage { get; set; }

    [JsonPropertyName("items")]
    public GameSearchResult[]? Items { get; set; }

    [JsonPropertyName("__typename")]
    public string Typename { get; set; } = string.Empty;
}

public class GameSearchResult
{
    //[JsonPropertyName("id")]
    //public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    //[JsonPropertyName("baseGameSlug")]
    //public string? BaseGameSlug { get; set; }

    //[JsonPropertyName("gameType")]
    //public string GameType { get; set; } = string.Empty;

    //[JsonPropertyName("prereleaseGameType")]
    //public object? PrereleaseGameType { get; set; }

    //[JsonPropertyName("subscriptionAvailabilities")]
    //public string[] SubscriptionAvailabilities { get; set; } = [];

    //[JsonPropertyName("isFreeToPlay")]
    //public bool IsFreeToPlay { get; set; }

    //[JsonPropertyName("playFirstTrialAvailable")]
    //public bool PlayFirstTrialAvailable { get; set; }

    //[JsonPropertyName("releaseDate")]
    //public object? ReleaseDate { get; set; }

    [JsonPropertyName("logoImage")]
    public Image? LogoImage { get; set; }

    [JsonPropertyName("keyArtImage")]
    public Image? KeyArtImage { get; set; }

    [JsonPropertyName("packArtImage")]
    public Image? PackArtImage { get; set; }

    //[JsonPropertyName("__typename")]
    //public string Typename { get; set; } = string.Empty;
}

public class Image
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    //[JsonPropertyName("__typename")]
    //public string Typename { get; set; } = string.Empty; // Image
}