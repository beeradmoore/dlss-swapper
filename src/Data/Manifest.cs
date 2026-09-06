using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DLSS_Swapper.Data;

internal class Manifest
{
    [JsonPropertyName(DLLRecord.DLSS)]
    public List<DLLRecord> DLSS { get; set; } = new List<DLLRecord>();

    [JsonPropertyName(DLLRecord.DLSS_D)]
    public List<DLLRecord> DLSS_D { get; set; } = new List<DLLRecord>();

    [JsonPropertyName(DLLRecord.DLSS_G)]
    public List<DLLRecord> DLSS_G { get; set; } = new List<DLLRecord>();

    [JsonPropertyName(DLLRecord.DLSS_NR)]
    public List<DLLRecord> DLSS_NR { get; set; } = new List<DLLRecord>();

    [JsonPropertyName(DLLRecord.FSR_31_DX12)]
    public List<DLLRecord> FSR_31_DX12 { get; set; } = new List<DLLRecord>();

    [JsonPropertyName(DLLRecord.FSR_31_VK)]
    public List<DLLRecord> FSR_31_VK { get; set; } = new List<DLLRecord>();

    [JsonPropertyName(DLLRecord.XeSS)]
    public List<DLLRecord> XeSS { get; set; } = new List<DLLRecord>();

    [JsonPropertyName(DLLRecord.XeLL)]
    public List<DLLRecord> XeLL { get; set; } = new List<DLLRecord>();

    [JsonPropertyName(DLLRecord.XeSS_FG)]
    public List<DLLRecord> XeSS_FG { get; set; } = new List<DLLRecord>();

    [JsonPropertyName(DLLRecord.XeSS_DX11)]
    public List<DLLRecord> XeSS_DX11 { get; set; } = new List<DLLRecord>();

    [JsonPropertyName(DLLRecord.DirectStorage)]
    public List<DLLRecord> DirectStorage { get; set; } = new List<DLLRecord>();

    [JsonPropertyName(DLLRecord.DirectStorageCore)]
    public List<DLLRecord> DirectStorageCore { get; set; } = new List<DLLRecord>();

    [JsonPropertyName(DLLRecord.FidelityFX_SDK2_Denoiser_DX12)]
    public List<DLLRecord> FidelityFX_SDK2_Denoiser_DX12 { get; set; } = new List<DLLRecord>();

    [JsonPropertyName(DLLRecord.FidelityFX_SDK2_FrameGeneration_DX12)]
    public List<DLLRecord> FidelityFX_SDK2_FrameGeneration_DX12 { get; set; } = new List<DLLRecord>();

    [JsonPropertyName(DLLRecord.FidelityFX_SDK2_Loader_DX12)]
    public List<DLLRecord> FidelityFX_SDK2_Loader_DX12 { get; set; } = new List<DLLRecord>();

    [JsonPropertyName(DLLRecord.FidelityFX_SDK2_RadianceCache_DX12)]
    public List<DLLRecord> FidelityFX_SDK2_RadianceCache_DX12 { get; set; } = new List<DLLRecord>();

    [JsonPropertyName(DLLRecord.FidelityFX_SDK2_Upscaler_DX12)]
    public List<DLLRecord> FidelityFX_SDK2_Upscaler_DX12 { get; set; } = new List<DLLRecord>();

    [JsonPropertyName(DLLRecord.Streamline_Reflex)]
    public List<DLLRecord> Streamline_Reflex { get; set; } = new List<DLLRecord>();

    [JsonPropertyName(DLLRecord.Streamline_PCL)]
    public List<DLLRecord> Streamline_PCL { get; set; } = new List<DLLRecord>();

    [JsonPropertyName(DLLRecord.Streamline_NvPerf)]
    public List<DLLRecord> Streamline_NvPerf { get; set; } = new List<DLLRecord>();

    [JsonPropertyName(DLLRecord.Streamline_NIS)]
    public List<DLLRecord> Streamline_NIS { get; set; } = new List<DLLRecord>();

    [JsonPropertyName(DLLRecord.Streamline_Interposer)]
    public List<DLLRecord> Streamline_Interposer { get; set; } = new List<DLLRecord>();

    [JsonPropertyName(DLLRecord.Streamline_DLSS_G)]
    public List<DLLRecord> Streamline_DLSS_G { get; set; } = new List<DLLRecord>();

    [JsonPropertyName(DLLRecord.Streamline_DLSS_D)]
    public List<DLLRecord> Streamline_DLSS_D { get; set; } = new List<DLLRecord>();

    [JsonPropertyName(DLLRecord.Streamline_DLSS)]
    public List<DLLRecord> Streamline_DLSS { get; set; } = new List<DLLRecord>();

    [JsonPropertyName(DLLRecord.Streamline_DLSS_NR)]
    public List<DLLRecord> Streamline_DLSS_NR { get; set; } = new List<DLLRecord>();

    [JsonPropertyName(DLLRecord.Streamline_DirectSR)]
    public List<DLLRecord> Streamline_DirectSR { get; set; } = new List<DLLRecord>();

    [JsonPropertyName(DLLRecord.Streamline_DeepDVC)]
    public List<DLLRecord> Streamline_DeepDVC { get; set; } = new List<DLLRecord>();

    [JsonPropertyName(DLLRecord.Streamline_Common)]
    public List<DLLRecord> Streamline_Common { get; set; } = new List<DLLRecord>();

    [JsonPropertyName(DLLRecord.DeepDVC)]
    public List<DLLRecord> DeepDVC { get; set; } = new List<DLLRecord>();

    [JsonPropertyName(DLLRecord.NvLowLatencyVK)]
    public List<DLLRecord> NvLowLatencyVK { get; set; } = new List<DLLRecord>();

    [JsonPropertyName("dll_sets")]
    public DLLSets DLLSets { get; set; } = new DLLSets();

    [JsonPropertyName("known_dlls")]
    public KnownDLLs KnownDLLs { get; set; } = new KnownDLLs();
}
