using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DLSS_Swapper.Data;

internal class Manifest
{
    [JsonPropertyName("dlss")]
    public List<DLLRecord> DLSS { get; set; } = new();

    [JsonPropertyName("dlss_d")]
    public List<DLLRecord> DLSS_D { get; set; } = new();

    [JsonPropertyName("dlss_g")]
    public List<DLLRecord> DLSS_G { get; set; } = new();

    [JsonPropertyName("fsr_31_dx12")]
    public List<DLLRecord> FSR_31_DX12 { get; set; } = new();

    [JsonPropertyName("fsr_31_vk")]
    public List<DLLRecord> FSR_31_VK { get; set; } = new();

    [JsonPropertyName("xess")]
    public List<DLLRecord> XeSS { get; set; } = new();

    [JsonPropertyName("xell")]
    public List<DLLRecord> XeLL { get; set; } = new();
    
    [JsonPropertyName("xess_fg")]
    public List<DLLRecord> XeSS_FG { get; set; } = new();
}
