using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DLSS_Swapper.Extensions;
using SQLite;

namespace DLSS_Swapper.Data;

public enum GameAssetType
{
    Unknown,
    DLSS,
    DLSS_FG,
    DLSS_RR,
    FSR_31_DX12,
    FSR_31_VK,
    XeSS,


    DLSS_BACKUP,
    DLSS_FG_BACKUP,
    DLSS_RR_BACKUP,
    FSR_31_DX12_BACKUP,
    FSR_31_VK_BACKUP,
    XeSS_BACKUP,
}

public class GameAsset
{
    [Indexed]
    [property: Column("id")]
    public string Id { get; set; } = string.Empty;

    [property: Column("asset_type")]
    public GameAssetType AssetType { get; set; } = GameAssetType.Unknown;

    [property: Column("path")]
    public string Path { get; set; } = string.Empty;

    [property: Column("version")]
    public string Version { get; set; } = string.Empty;

    [property: Column("Hash")]
    public string Hash { get; set; } = string.Empty;

    public void LoadVersionAndHash()
    {
        if (File.Exists(Path) == false)
        {
            return;
        }

        var fileVersionInfo = FileVersionInfo.GetVersionInfo(Path);
        Version = fileVersionInfo.GetFormattedFileVersion();
        Hash = fileVersionInfo.GetMD5Hash();
    }

    public GameAsset? GetBackup()
    {
        var backypType = AssetType switch
        {
            GameAssetType.DLSS => GameAssetType.DLSS_BACKUP,
            GameAssetType.DLSS_FG => GameAssetType.DLSS_FG_BACKUP,
            GameAssetType.DLSS_RR => GameAssetType.DLSS_RR_BACKUP,
            GameAssetType.FSR_31_DX12 => GameAssetType.FSR_31_DX12_BACKUP,
            GameAssetType.FSR_31_VK => GameAssetType.FSR_31_VK_BACKUP,
            GameAssetType.XeSS => GameAssetType.XeSS_BACKUP,
            _ => GameAssetType.Unknown
        };

        if (backypType == GameAssetType.Unknown)
        {
            return null;
        }

        var backupPath = Path + ".dlsss";
        if (File.Exists(backupPath) == false)
        {
            return null;
        }

        var backupGameAsset = new GameAsset()
        {
            Id = Id,
            AssetType = backypType,
            Path = backupPath,
        };
        backupGameAsset.LoadVersionAndHash();

        return backupGameAsset;
    }
}
