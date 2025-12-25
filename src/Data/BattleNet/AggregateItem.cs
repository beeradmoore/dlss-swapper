using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DLSS_Swapper.Data.BattleNet;

internal class AggregateItem
{
    [JsonPropertyName("box_art_uri")]
    public string BoxArtId { get; set; } = string.Empty;

    [JsonPropertyName("icon_index")]
    public long IconIndex { get; set; } = 0;

    [JsonPropertyName("icon_path")]
    public string IconPath { get; set; } = string.Empty;

    [JsonPropertyName("last_played_timestamp")]
    public long LastPlayedTimestamp { get; set; } = 0;

    [JsonPropertyName("launch_uri")]
    public string LaunchUri { get; set; } = string.Empty;

    [JsonPropertyName("logo_art_uri")]
    public string LogoArtUri { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("product_id")]
    public string ProductId { get; set; } = string.Empty;
}
