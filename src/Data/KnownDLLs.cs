using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DLSS_Swapper.Data;

public class KnownDLLs
{
    [JsonPropertyName("dlss")]
    public List<HashedKnownDLL> DLSS { get; set; } = new List<HashedKnownDLL>();

    [JsonPropertyName("dlss_d")]
    public List<HashedKnownDLL> DLSS_D { get; set; } = new List<HashedKnownDLL>();

    [JsonPropertyName("dlss_g")]
    public List<HashedKnownDLL> DLSS_G { get; set; } = new List<HashedKnownDLL>();

    [JsonPropertyName("fsr_31_dx12")]
    public List<HashedKnownDLL> FSR_31_DX12 { get; set; } = new List<HashedKnownDLL>();

    [JsonPropertyName("fsr_31_vk")]
    public List<HashedKnownDLL> FSR_31_VK { get; set; } = new List<HashedKnownDLL>();

    [JsonPropertyName("xess")]
    public List<HashedKnownDLL> XeSS { get; set; } = new List<HashedKnownDLL>();

    [JsonPropertyName("xell")]
    public List<HashedKnownDLL> XeLL { get; set; } = new List<HashedKnownDLL>();

    [JsonPropertyName("xess_fg")]
    public List<HashedKnownDLL> XeSS_FG { get; set; } = new List<HashedKnownDLL>();
}
