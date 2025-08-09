using System.Text.Json.Serialization;

namespace DLSS_Swapper.Data.Steam.SteamAPI;

internal class SteamAPIResponse<T>
{
    [JsonPropertyName("response")]
    public T? Response { get; set; }
}
