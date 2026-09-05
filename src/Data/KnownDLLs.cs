using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DLSS_Swapper.Data;

public class KnownDLLs
{
    [JsonPropertyName(DLLRecord.DLSS)]
    public List<HashedKnownDLL> DLSS { get; set; } = new List<HashedKnownDLL>();

    [JsonPropertyName(DLLRecord.DLSS_D)]
    public List<HashedKnownDLL> DLSS_D { get; set; } = new List<HashedKnownDLL>();

    [JsonPropertyName(DLLRecord.DLSS_G)]
    public List<HashedKnownDLL> DLSS_G { get; set; } = new List<HashedKnownDLL>();

    [JsonPropertyName(DLLRecord.DLSS_NR)]
    public List<HashedKnownDLL> DLSS_NR { get; set; } = new List<HashedKnownDLL>();

    [JsonPropertyName(DLLRecord.FSR_31_DX12)]
    public List<HashedKnownDLL> FSR_31_DX12 { get; set; } = new List<HashedKnownDLL>();

    [JsonPropertyName(DLLRecord.FSR_31_VK)]
    public List<HashedKnownDLL> FSR_31_VK { get; set; } = new List<HashedKnownDLL>();

    [JsonPropertyName(DLLRecord.XeSS)]
    public List<HashedKnownDLL> XeSS { get; set; } = new List<HashedKnownDLL>();

    [JsonPropertyName(DLLRecord.XeLL)]
    public List<HashedKnownDLL> XeLL { get; set; } = new List<HashedKnownDLL>();

    [JsonPropertyName(DLLRecord.XeSS_FG)]
    public List<HashedKnownDLL> XeSS_FG { get; set; } = new List<HashedKnownDLL>();

    [JsonPropertyName(DLLRecord.XeSS_DX11)]
    public List<HashedKnownDLL> XeSS_DX11 { get; set; } = new List<HashedKnownDLL>();

    [JsonPropertyName(DLLRecord.DirectStorage)]
    public List<HashedKnownDLL> DirectStorage { get; set; } = new List<HashedKnownDLL>();

    [JsonPropertyName(DLLRecord.DirectStorageCore)]
    public List<HashedKnownDLL> DirectStorageCore { get; set; } = new List<HashedKnownDLL>();

    [JsonPropertyName(DLLRecord.FidelityFX_SDK2_Denoiser_DX12)]
    public List<HashedKnownDLL> FidelityFX_SDK2_Denoiser_DX12 { get; set; } = new List<HashedKnownDLL>();

    [JsonPropertyName(DLLRecord.FidelityFX_SDK2_FrameGeneration_DX12)]
    public List<HashedKnownDLL> FidelityFX_SDK2_FrameGeneration_DX12 { get; set; } = new List<HashedKnownDLL>();

    [JsonPropertyName(DLLRecord.FidelityFX_SDK2_Loader_DX12)]
    public List<HashedKnownDLL> FidelityFX_SDK2_Loader_DX12 { get; set; } = new List<HashedKnownDLL>();

    [JsonPropertyName(DLLRecord.FidelityFX_SDK2_RadianceCache_DX12)]
    public List<HashedKnownDLL> FidelityFX_SDK2_RadianceCache_DX12 { get; set; } = new List<HashedKnownDLL>();

    [JsonPropertyName(DLLRecord.FidelityFX_SDK2_Upscaler_DX12)]
    public List<HashedKnownDLL> FidelityFX_SDK2_Upscaler_DX12 { get; set; } = new List<HashedKnownDLL>();

    [JsonPropertyName(DLLRecord.Streamline_Reflex)]
    public List<HashedKnownDLL> Streamline_Reflex { get; set; } = new List<HashedKnownDLL>();

    [JsonPropertyName(DLLRecord.Streamline_PCL)]
    public List<HashedKnownDLL> Streamline_PCL { get; set; } = new List<HashedKnownDLL>();

    [JsonPropertyName(DLLRecord.Streamline_NvPerf)]
    public List<HashedKnownDLL> Streamline_NvPerf { get; set; } = new List<HashedKnownDLL>();

    [JsonPropertyName(DLLRecord.Streamline_NIS)]
    public List<HashedKnownDLL> Streamline_NIS { get; set; } = new List<HashedKnownDLL>();

    [JsonPropertyName(DLLRecord.Streamline_Interposer)]
    public List<HashedKnownDLL> Streamline_Interposer { get; set; } = new List<HashedKnownDLL>();

    [JsonPropertyName(DLLRecord.Streamline_DLSS_G)]
    public List<HashedKnownDLL> Streamline_DLSS_G { get; set; } = new List<HashedKnownDLL>();

    [JsonPropertyName(DLLRecord.Streamline_DLSS_D)]
    public List<HashedKnownDLL> Streamline_DLSS_D { get; set; } = new List<HashedKnownDLL>();

    [JsonPropertyName(DLLRecord.Streamline_DLSS)]
    public List<HashedKnownDLL> Streamline_DLSS { get; set; } = new List<HashedKnownDLL>();

    [JsonPropertyName(DLLRecord.Streamline_DirectSR)]
    public List<HashedKnownDLL> Streamline_DirectSR { get; set; } = new List<HashedKnownDLL>();

    [JsonPropertyName(DLLRecord.Streamline_DeepDVC)]
    public List<HashedKnownDLL> Streamline_DeepDVC { get; set; } = new List<HashedKnownDLL>();

    [JsonPropertyName(DLLRecord.Streamline_Common)]
    public List<HashedKnownDLL> Streamline_Common { get; set; } = new List<HashedKnownDLL>();

    [JsonPropertyName(DLLRecord.DeepDVC)]
    public List<HashedKnownDLL> DeepDVC { get; set; } = new List<HashedKnownDLL>();

    [JsonPropertyName(DLLRecord.NvLowLatencyVK)]
    public List<HashedKnownDLL> NvLowLatencyVK { get; set; } = new List<HashedKnownDLL>();

}
