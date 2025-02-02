using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DLSS_Swapper.Data;

internal class Manifest
{
    [JsonPropertyName("dlss")]
    public List<DLLRecord> DLSS { get; set; } = [];

    [JsonPropertyName("dlss_d")]
    public List<DLLRecord> DLSS_D { get; set; } = [];

    [JsonPropertyName("dlss_g")]
    public List<DLLRecord> DLSS_G { get; set; } = [];

    [JsonPropertyName("fsr_31_dx12")]
    public List<DLLRecord> FSR_31_DX12 { get; set; } = [];

    [JsonPropertyName("fsr_31_vk")]
    public List<DLLRecord> FSR_31_VK { get; set; } = [];

    [JsonPropertyName("xess")]
    public List<DLLRecord> XeSS { get; set; } = [];

    [JsonPropertyName("xell")]
    public List<DLLRecord> XeLL { get; set; } = [];
    
    [JsonPropertyName("xess_fg")]
    public List<DLLRecord> XeSS_FG { get; set; } = [];
}
