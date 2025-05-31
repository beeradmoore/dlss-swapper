using System.Text.Json.Serialization;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.Data;

public class GameLibrarySettings
{
    [JsonPropertyName("GameLibrary")]
    [JsonConverter(typeof(JsonStringEnumConverter<GameLibrary>))]
    public GameLibrary GameLibrary { get; set; }

    [JsonPropertyName("IsEnabled")]
    public bool IsEnabled { get; set; } = true;
}
