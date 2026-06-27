namespace DLSS_Swapper.Data;

// NOTE: DLL type
// NOTE: This ordering sucks because I (beeradmoore) forgot to keep enums numbered so adding new changed the values of existing.
public enum GameAssetType
{
    Unknown = 0,

    DLSS = 1,
    DLSS_G = 2,
    DLSS_D = 3,

    FSR_31_DX12 = 4,
    FSR_31_VK = 5,

    XeSS = 6,
    XeLL = 7,
    XeSS_FG = 8,
    XeSS_DX11 = 17,

    DirectStorage = 19,
    DirectStorageCore = 21,

    FidelityFX_SDK2_Denoiser_DX12 = 23,
    FidelityFX_SDK2_FrameGeneration_DX12 = 25,
    FidelityFX_SDK2_Loader_DX12 = 27,
    FidelityFX_SDK2_RadianceCache_DX12 = 29,
    FidelityFX_SDK2_Upscaler_DX12 = 31,

    Streamline_Reflex = 33,
    Streamline_PCL = 35,
    Streamline_NvPerf = 37,
    Streamline_NIS = 39,
    Streamline_Interposer = 41,
    Streamline_DLSS_G = 43,
    Streamline_DLSS_D = 45,
    Streamline_DLSS = 47,
    Streamline_DirectSR = 49,
    Streamline_DeepDVC = 51,
    Streamline_Common = 53,

    DeepDVC = 55,
    NvLowLatencyVK = 57,

    // Backup

    DLSS_BACKUP = 9,
    DLSS_G_BACKUP = 10,
    DLSS_D_BACKUP = 11,

    FSR_31_DX12_BACKUP = 12,
    FSR_31_VK_BACKUP = 13,

    XeSS_BACKUP = 14,
    XeLL_BACKUP = 15,
    XeSS_FG_BACKUP = 16,
    XeSS_DX11_BACKUP = 18,

    DirectStorage_BACKUP = 20,
    DirectStorageCore_BACKUP = 22,

    FidelityFX_SDK2_Denoiser_DX12_BACKUP = 24,
    FidelityFX_SDK2_FrameGeneration_DX12_BACKUP = 26,
    FidelityFX_SDK2_Loader_DX12_BACKUP = 28,
    FidelityFX_SDK2_RadianceCache_DX12_BACKUP = 30,
    FidelityFX_SDK2_Upscaler_DX12_BACKUP = 32,

    Streamline_Reflex_BACKUP = 34,
    Streamline_PCL_BACKUP = 36,
    Streamline_NvPerf_BACKUP = 38,
    Streamline_NIS_BACKUP = 40,
    Streamline_Interposer_BACKUP = 42,
    Streamline_DLSS_G_BACKUP = 44,
    Streamline_DLSS_D_BACKUP = 46,
    Streamline_DLSS_BACKUP = 48,
    Streamline_DirectSR_BACKUP = 50,
    Streamline_DeepDVC_BACKUP = 52,
    Streamline_Common_BACKUP = 54,

    DeepDVC_BACKUP = 56,
    NvLowLatencyVK_BACKUP = 58,
}
