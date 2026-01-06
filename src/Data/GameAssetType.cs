namespace DLSS_Swapper.Data;

// NOTE: DLL type
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

    DLSS_BACKUP = 9,
    DLSS_G_BACKUP = 10,
    DLSS_D_BACKUP = 11,
    FSR_31_DX12_BACKUP = 12,
    FSR_31_VK_BACKUP = 13,
    XeSS_BACKUP = 14,
    XeLL_BACKUP = 15,
    XeSS_FG_BACKUP = 16,
    XeSS_DX11_BACKUP = 18,
}
