
using System.Text.Json.Serialization;

namespace DLSS_Swapper.Data.Steam.SteamAPI;

internal class StoreBrowseContext
{
    /*
    [JsonPropertyName("language")]
    public string Language { get; set; } = string.Empty;

    [JsonPropertyName("elanguage")]
    public string ELanguage { get; set; } = string.Empty;
    */

    [JsonPropertyName("country_code")]
    public string CountryCode { get; set; } = "US";

    /*
    [JsonPropertyName("steam_realm")]
    public string SteamRealm { get; set; } = string.Empty;
    */
}
