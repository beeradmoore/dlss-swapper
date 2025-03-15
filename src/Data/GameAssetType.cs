using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DLSS_Swapper.Data;

public enum GameAssetType
{
    Unknown,

    DLSS,
    DLSS_G,
    DLSS_D,
    FSR_31_DX12,
    FSR_31_VK,
    XeSS,
    XeLL,
    XeSS_FG,

    DLSS_BACKUP,
    DLSS_G_BACKUP,
    DLSS_D_BACKUP,
    FSR_31_DX12_BACKUP,
    FSR_31_VK_BACKUP,
    XeSS_BACKUP,
    XeLL_BACKUP,
    XeSS_FG_BACKUP,
}
