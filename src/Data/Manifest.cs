using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DLSS_Swapper.Data;

internal class Manifest
{
    [JsonPropertyName("dlss")]
    public List<DLLRecord> DLSS { get; set; } = new List<DLLRecord>();

    [JsonPropertyName("dlss_d")]
    public List<DLLRecord> DLSS_D { get; set; } = new List<DLLRecord>();

    [JsonPropertyName("dlss_g")]
    public List<DLLRecord> DLSS_G { get; set; } = new List<DLLRecord>();

    [JsonPropertyName("fsr_31_dx12")]
    public List<DLLRecord> FSR_31_DX12 { get; set; } = new List<DLLRecord>();

    [JsonPropertyName("fsr_31_vk")]
    public List<DLLRecord> FSR_31_VK { get; set; } = new List<DLLRecord>();

    [JsonPropertyName("xess")]
    public List<DLLRecord> XeSS { get; set; } = new List<DLLRecord>();

    [JsonPropertyName("xell")]
    public List<DLLRecord> XeLL { get; set; } = new List<DLLRecord>();

    [JsonPropertyName("xess_fg")]
    public List<DLLRecord> XeSS_FG { get; set; } = new List<DLLRecord>();

    [JsonPropertyName("xess_dx11")]
    public List<DLLRecord> XeSS_DX11 { get; set; } = new List<DLLRecord>();

    [JsonPropertyName("known_dlls")]
    public KnownDLLs KnownDLLs { get; set; } = new KnownDLLs();
}
