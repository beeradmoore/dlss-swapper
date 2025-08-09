using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using DLSS_Swapper.Extensions;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Helpers.FSR31;

namespace DLSS_Swapper.Data;

public class DLLRecord : IComparable<DLLRecord>, INotifyPropertyChanged
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("version_number")]
    public ulong VersionNumber { get; set; }

    [JsonPropertyName("internal_name")]
    public string InternalName { get; set; } = string.Empty;

    [JsonPropertyName("internal_name_extra")]
    public string InternalNameExtra { get; set; } = string.Empty;

    [JsonPropertyName("additional_label")]
    public string AdditionalLabel { get; set; } = string.Empty;

    [JsonPropertyName("md5_hash")]
    public string MD5Hash { get; set; } = string.Empty;

    /// <summary>
    /// This hash is not guaranteed to be the same as the hash on the zip on the disk.
    /// It is used during download to validate a successful download. However if you
    /// import a DLL that exists in the manifest we will then create the zip for that
    /// file. Doing so will cause the new generateed zip hash and this entry in the
    /// manifest to differ.
    /// </summary>
    [JsonPropertyName("zip_md5_hash")]
    public string ZipMD5Hash { get; set; } = string.Empty;

    [JsonPropertyName("download_url")]
    public string DownloadUrl { get; set; } = string.Empty;

    [JsonPropertyName("file_description")]
    public string FileDescription { get; set; } = string.Empty;

    [JsonPropertyName("signed_datetime")]
    public DateTime SignedDateTime { get; set; } = DateTime.MinValue;

    [JsonPropertyName("is_signature_valid")]
    public bool IsSignatureValid { get; set; }

    [JsonPropertyName("is_dev_file")]
    public bool IsDevFile { get; set; } = false;

    [JsonPropertyName("file_size")]
    public long FileSize { get; set; }

    [JsonPropertyName("zip_file_size")]
    public long ZipFileSize { get; set; }

    [JsonIgnore]
    public string FullName
    {
        get
        {
            if (string.IsNullOrEmpty(AdditionalLabel))
            {
                return Version;
            }

            return $"{Version} - {AdditionalLabel}";
        }
    }

    string _displayVersion = string.Empty;
    [JsonIgnore]
    public string DisplayVersion
    {
        get
        {
            // return cached version.
            if (string.IsNullOrWhiteSpace(_displayVersion) == false)
            {
                return _displayVersion;
            }

            // If FSR the display version is the internal version
            if (AssetType == GameAssetType.FSR_31_DX12 || AssetType == GameAssetType.FSR_31_VK ||
                AssetType == GameAssetType.FSR_31_DX12_BACKUP || AssetType == GameAssetType.FSR_31_VK_BACKUP)
            {
                if (string.IsNullOrEmpty(InternalName) == false)
                {
                    _displayVersion = InternalName;
                    return _displayVersion;
                }

                if (LocalRecord is not null)
                {
                    var latestVersion = FSR31Helper.GetLatestVersion(LocalRecord.ExpectedPath);
                    if (string.IsNullOrWhiteSpace(latestVersion) == false)
                    {
                        _displayVersion = latestVersion;
                        return _displayVersion;
                    }
                }

                // If this isn't loaded we fall back to the existing stuff.
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

            return _displayVersion;
        }
    }

    Version? _displayVersionVersion;
    [JsonIgnore]
    public Version DisplayVersionVersion
    {
        get
        {
            if (_displayVersionVersion is not null)
            {
                return _displayVersionVersion;
            }

            try
            {
                var version = new Version(DisplayVersion);
                _displayVersionVersion = version;
                return version;
            }
            catch (Exception err)
            {
                Logger.Error(err, $"Failed to parse display version ({DisplayVersion}) into a Version object.");
                return new Version(0, 0, 0, 0);
            }
        }
    }


    /// <summary>
    /// Returns the display version (eg 2.5.0.0 slimmed down to 2.5) and prefixes with v, and suffix with additional label if it exists.
    /// </summary>
    [JsonIgnore]
    public string DisplayName
    {
        get
        {
            var devString = IsDevFile ? " (Debug)" : string.Empty;


            if (AssetType == GameAssetType.FSR_31_DX12 || AssetType == GameAssetType.FSR_31_VK ||
                AssetType == GameAssetType.FSR_31_DX12_BACKUP || AssetType == GameAssetType.FSR_31_VK_BACKUP)
            {
                    return $"v{DisplayVersion}{devString} (v{Version})";
            }

            if (string.IsNullOrEmpty(AdditionalLabel))
            {
                return $"v{DisplayVersion}{devString}";
            }

            return $"v{DisplayVersion}{devString} ({AdditionalLabel})";
        }
    }

    LocalRecord? _localRecord = null;

    [JsonIgnore]
    public LocalRecord? LocalRecord
    {
        get => _localRecord;
        set
        {
            _localRecord = value;
            NotifyPropertyChanged();
        }
    }

    [JsonIgnore]
    public GameAssetType AssetType { get; set; } = GameAssetType.Unknown;

    [JsonIgnore]
    public DLLRecordModelTranslationProperties TranslationProperties { get; } = new DLLRecordModelTranslationProperties();

    public int CompareTo(DLLRecord? other)
    {
        if (other is null)
        {
            return -1;
        }

        if (string.IsNullOrWhiteSpace(MD5Hash) == false && MD5Hash == other.MD5Hash)
        {
            return 0;
        }

        if ((AssetType == GameAssetType.FSR_31_DX12 && other.AssetType == GameAssetType.FSR_31_DX12) ||
            (AssetType == GameAssetType.FSR_31_VK && other.AssetType == GameAssetType.FSR_31_VK))
        {
            if (string.IsNullOrEmpty(InternalName) == false && string.IsNullOrEmpty(other.InternalName) == false)
            {
                if (InternalName != other.InternalName)
                {
                    return other.InternalName.CompareTo(InternalName);
                }
            }
        }

        if (VersionNumber == other.VersionNumber)
        {
            if (IsDevFile == other.IsDevFile)
            {
                return other.AdditionalLabel.CompareTo(AdditionalLabel);
            }

            return IsDevFile.CompareTo(other.IsDevFile);
        }

        return other.VersionNumber.CompareTo(VersionNumber);
    }

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged = null;
    internal void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    private CancellationTokenSource? _cancellationTokenSource;

    internal void CancelDownload()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = null;
    }

    internal async Task<(bool Success, string Message, bool Cancelled)> DownloadAsync()
    {
        if (string.IsNullOrEmpty(DownloadUrl))
        {
            return (false, "Invalid download URL.", false);
        }

        if (LocalRecord is null)
        {
            return (false, "Local record is null.", false);
        }

        _cancellationTokenSource?.Cancel();

        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;

        var fileDownloader = new FileDownloader(DownloadUrl);
        var tempZipFile = Path.Combine(Storage.GetTemp(), $"{fileDownloader.Guid.ToString("D").ToUpper()}.zip");

        try
        {
            LocalRecord.FileDownloader = fileDownloader;
            NotifyPropertyChanged(nameof(LocalRecord));


            using (var fileStream = new FileStream(tempZipFile, FileMode.Create, FileAccess.ReadWrite, FileShare.None, FileDownloader.BufferSize, true))
            {
                var didDownload = await LocalRecord.FileDownloader.DownloadFileToStreamAsync(fileStream, cancellationToken).ConfigureAwait(false);

                if (didDownload == false)
                {
                    throw new Exception("Could not download file.");
                }

                if (ZipMD5Hash != fileStream.GetMD5Hash())
                {
                    throw new Exception("Downloaded file was invalid.");
                }

                fileStream.Position = 0;

                using (var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read, true))
                {
                    DLLManager.HandleExtractFromZip(zipArchive, this);
                }
            }

            App.CurrentApp.RunOnUIThread(() =>
            {
                LocalRecord.IsDownloaded = true;
                NotifyPropertyChanged(nameof(LocalRecord));
            });

            return (true, string.Empty, false);
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            App.CurrentApp.RunOnUIThread(() =>
            {
                LocalRecord.IsDownloaded = false;
                NotifyPropertyChanged(nameof(LocalRecord));
            });

            return (false, string.Empty, true);
        }
        catch (Exception err)
        {
            Logger.Error(err);

            Debugger.Break();
            App.CurrentApp.RunOnUIThread(() =>
            {
                LocalRecord.IsDownloaded = false;
                LocalRecord.HasDownloadError = true;
                LocalRecord.DownloadErrorMessage = ResourceHelper.GetFormattedResourceTemplate("DllRecord_CouldNotDownloadAssetTypeTemplate", DLLManager.Instance.GetAssetTypeName(AssetType));
                NotifyPropertyChanged(nameof(LocalRecord));
            });

            return (false, err.Message, false);
        }
        finally
        {
            App.CurrentApp.RunOnUIThread(() =>
            {
                LocalRecord.FileDownloader = null;
                NotifyPropertyChanged(nameof(LocalRecord));
            });

            // Remove temp file.
            try
            {
                File.Delete(tempZipFile);
            }
            catch (Exception)
            {
                // NOOP
            }
        }
    }

    internal string GetRecordSimpleType()
    {
        return AssetType switch
        {
            GameAssetType.DLSS => "dlss",
            GameAssetType.DLSS_G => "dlss_g",
            GameAssetType.DLSS_D => "dlss_d",
            GameAssetType.FSR_31_DX12 => "fsr_31_dx12",
            GameAssetType.FSR_31_VK => "fsr_31_vk",
            GameAssetType.XeSS => "xess",
            GameAssetType.XeLL => "xell",
            GameAssetType.XeSS_FG => "xess_fg",
            _ => string.Empty,
        };
    }

    internal void CopyFrom(DLLRecord newDllRecord)
    {
        Version = newDllRecord.Version;
        VersionNumber = newDllRecord.VersionNumber;
        InternalName = newDllRecord.InternalName;
        AdditionalLabel = newDllRecord.AdditionalLabel;
        MD5Hash = newDllRecord.MD5Hash;
        ZipMD5Hash = newDllRecord.ZipMD5Hash;
        DownloadUrl = newDllRecord.DownloadUrl;
        FileDescription = newDllRecord.FileDescription;
        SignedDateTime = newDllRecord.SignedDateTime;
        IsSignatureValid = newDllRecord.IsSignatureValid;
        IsDevFile = newDllRecord.IsDevFile;
        FileSize = newDllRecord.FileSize;
        ZipFileSize = newDllRecord.ZipFileSize;
        LocalRecord = newDllRecord.LocalRecord;
        AssetType = newDllRecord.AssetType;

        NotifyPropertyChanged(nameof(FullName));
        _displayVersion = string.Empty;
        NotifyPropertyChanged(nameof(DisplayVersion));
        NotifyPropertyChanged(nameof(DisplayName));
    }

}
