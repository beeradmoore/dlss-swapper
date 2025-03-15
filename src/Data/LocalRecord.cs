using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLSS_Swapper.Helpers;

namespace DLSS_Swapper.Data;

public partial class LocalRecord : ObservableObject, IEquatable<LocalRecord>
{
    public string ExpectedPath { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsDownloaded { get; set; } = false;

    [ObservableProperty]
    public partial bool IsImported { get; set; } = false;

    [ObservableProperty]
    public partial FileDownloader? FileDownloader { get; set; } = null;

    [ObservableProperty]
    public partial bool HasDownloadError { get; set; } = false;

    [ObservableProperty]
    public partial string DownloadErrorMessage { get; set; } = string.Empty;


    private LocalRecord()
    {

    }

    public static LocalRecord FromExpectedPath(string expectedPath, bool imported = false)
    {
        var localRecord = new LocalRecord()
        {
            ExpectedPath = expectedPath,
        };

        if (File.Exists(expectedPath))
        {
            localRecord.IsDownloaded = true;
            if (imported)
            {
                localRecord.IsImported = true;
            }
        }

        return localRecord;
    }


    internal bool Delete()
    {
        try
        {
            File.Delete(ExpectedPath);

            IsDownloaded = false;
            HasDownloadError = false;
            DownloadErrorMessage = string.Empty;
            // We don't update IsImported here as that wont change.
            return true;
        }
        catch (Exception err)
        {
            Logger.Error(err);
            return false;
        }
    }

    /*
    internal async Task<bool> DeleteAsync()
    {
        var storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
        try
        {
            var dlssFile = await storageFolder.GetFileAsync(ExpectedPath);
            await dlssFile.DeleteAsync(Windows.Storage.StorageDeleteOption.PermanentDelete);
            
            IsDownloaded = false;
            IsDownloading = false;
            DownloadProgress = 0;
            HasDownloadError = false;
            DownloadErrorMessage = string.Empty;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
    */

    internal void UpdateFromNewLocalRecord(LocalRecord localRecord)
    {
        // First make sure expected path matches on both.
        if (Equals(localRecord) == false)
        {
            return;
        }

        ExpectedPath = localRecord.ExpectedPath;
        IsDownloaded = localRecord.IsDownloaded;
        FileDownloader = localRecord.FileDownloader;
        HasDownloadError = localRecord.HasDownloadError;
        DownloadErrorMessage = localRecord.DownloadErrorMessage;
        IsImported = localRecord.IsImported;
    }

    public bool Equals(LocalRecord? other)
    {
        if (other is null)
        {
            return false;
        }

        // Make sure other and other.ExpectedPath are not null or empty. This also means
        // that this.ExpectedPath can't be null or empty and have this return true.
        if (string.IsNullOrWhiteSpace(other.ExpectedPath))
        {
            return false;
        }

        return string.Equals(ExpectedPath, other.ExpectedPath, StringComparison.InvariantCultureIgnoreCase);
    }
}
