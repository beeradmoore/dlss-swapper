
using System.ComponentModel;
using System.Text.Json.Serialization;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.JsonConverters;

namespace DLSS_Swapper.Data.DLSS;

public class PresetOption : INotifyPropertyChanged
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("value")]
    [JsonConverter(typeof(HexStringToUintConverter))]
    public uint Value { get; init; }

    [JsonPropertyName("used")]
    public bool Used { get; set; }

    public bool Deprecated { get; set; }

    [JsonConstructor]
    public PresetOption(string name, uint value)
    {
        Name = name;
        Value = value;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void UpdateNameFromTranslation()
    {
        var newName = Value switch
        {
            0x00000000 => ResourceHelper.GetString("DLSS_Preset_Default"),
            0x00000001 => ResourceHelper.GetFormattedResourceTemplate(Deprecated ? "DLSS_Preset_Letter_Deprecated" : "DLSS_Preset_Letter", "A"),
            0x00000002 => ResourceHelper.GetFormattedResourceTemplate(Deprecated ? "DLSS_Preset_Letter_Deprecated" : "DLSS_Preset_Letter", "B"),
            0x00000003 => ResourceHelper.GetFormattedResourceTemplate(Deprecated ? "DLSS_Preset_Letter_Deprecated" : "DLSS_Preset_Letter", "C"),
            0x00000004 => ResourceHelper.GetFormattedResourceTemplate(Deprecated ? "DLSS_Preset_Letter_Deprecated" : "DLSS_Preset_Letter", "D"),
            0x00000005 => ResourceHelper.GetFormattedResourceTemplate(Deprecated ? "DLSS_Preset_Letter_Deprecated" : "DLSS_Preset_Letter", "E"),
            0x00000006 => ResourceHelper.GetFormattedResourceTemplate(Deprecated ? "DLSS_Preset_Letter_Deprecated" : "DLSS_Preset_Letter", "F"),
            //0x00000007 => ResourceHelper.GetFormattedResourceTemplate(Deprecated ? "DLSS_Preset_Letter_Deprecated" : "DLSS_Preset_Letter", "G"),
            //0x00000008 => ResourceHelper.GetFormattedResourceTemplate(Deprecated ? "DLSS_Preset_Letter_Deprecated" : "DLSS_Preset_Letter", "H"),
            //0x00000009 => ResourceHelper.GetFormattedResourceTemplate(Deprecated ? "DLSS_Preset_Letter_Deprecated" : "DLSS_Preset_Letter", "I"),
            0x0000000A => ResourceHelper.GetFormattedResourceTemplate(Deprecated ? "DLSS_Preset_Letter_Deprecated" : "DLSS_Preset_Letter", "J"),
            0x0000000B => ResourceHelper.GetFormattedResourceTemplate(Deprecated ? "DLSS_Preset_Letter_Deprecated" : "DLSS_Preset_Letter", "K"),
            0x00FFFFFF => ResourceHelper.GetString("DLSS_Preset_AlwaysUseLatest"),
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(newName) == false)
        {
            Name = newName;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
        }
    }
}
