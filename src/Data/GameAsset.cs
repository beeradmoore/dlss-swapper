using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DLSS_Swapper.Extensions;
using DLSS_Swapper.Interfaces;
using SQLite;

namespace DLSS_Swapper.Data;

public class GameAsset : IEquatable<GameAsset>
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

    string _displayVersion = string.Empty;

    [property: Ignore]
    public string DisplayVersion
    {
        get
        {
            // return cached version.
            if (string.IsNullOrWhiteSpace(_displayVersion) == false)
            {
                return _displayVersion;
            }

            var version = Version.AsSpan();

            // Remove all the .0's, such that 2.5.0.0 becomes 2.5
            while (version.EndsWith(".0"))
            {
                version = version.Slice(0, version.Length - 2);
            }

            _displayVersion = version.ToString();

            // If the value is a single value, eg 1, make it 1.0
            if (_displayVersion.Length == 1)
            {
                _displayVersion = $"{_displayVersion}.0";
            }

            _displayVersion = $"v{_displayVersion}";

            return  _displayVersion;
        }
    }

    [property: Ignore]
    public string DisplayName
    {
        // TODO: Improve to show relevant name
        get { return DisplayVersion; }
    }

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
            GameAssetType.DLSS_G => GameAssetType.DLSS_G_BACKUP,
            GameAssetType.DLSS_D => GameAssetType.DLSS_D_BACKUP,
            GameAssetType.FSR_31_DX12 => GameAssetType.FSR_31_DX12_BACKUP,
            GameAssetType.FSR_31_VK => GameAssetType.FSR_31_VK_BACKUP,
            GameAssetType.XeSS => GameAssetType.XeSS_BACKUP,
            GameAssetType.XeLL => GameAssetType.XeLL_BACKUP,
            GameAssetType.XeSS_FG => GameAssetType.XeSS_FG_BACKUP,
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

    public bool Equals(GameAsset? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Id.Equals(other.Id) &&
            AssetType.Equals(other.AssetType) &&
            Path.Equals(other.Path) &&
            Version.Equals(other.Version) &&
            Hash.Equals(other.Hash);
    }
}
