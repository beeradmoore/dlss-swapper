
using System.Text.Json.Serialization;
using DLSS_Swapper.JsonConverters;

namespace DLSS_Swapper.Data.DLSS;

public class PresetOption
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("value")]
    [JsonConverter(typeof(HexStringToUintConverter))]
    public uint Value { get; init; }

    [JsonPropertyName("used")]
    public bool Used { get; set; }

    [JsonConstructor]
    public PresetOption(string name, uint value)
    {
        Name = name;
        Value = value;
    }
}
