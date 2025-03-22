using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DLSS_Swapper.Data.GOG;

class GOGProduct
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("images")]
    public GOGProductImages? Images { get; set; }
}

class GOGProductImages
{
    [JsonPropertyName("background")]
    public string? Background { get; set; }

    [JsonPropertyName("logo")]
    public string? Logo { get; set; }

    [JsonPropertyName("logo2x")]
    public string? Logo2x { get; set; }

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("sidebarIcon")]
    public string? SidebarIcon { get; set; }

    [JsonPropertyName("sidebarIcon2x")]
    public string? SidebarIcon2x { get; set; }

    [JsonPropertyName("menuNotificationAv")]
    public string? MenuNotificationAv { get; set; }

    [JsonPropertyName("menuNotificationAv2")]
    public string? MenuNotificationAv2 { get; set; }
}
