using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using DLSS_Swapper.Extensions;

namespace DLSS_Swapper.Data
{
    public class DLLRecord : IComparable<DLLRecord>, INotifyPropertyChanged
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("version_number")]
        public ulong VersionNumber { get; set; }

        [JsonPropertyName("additional_label")]
        public string AdditionalLabel { get; set; } = string.Empty;

        [JsonPropertyName("md5_hash")]
        public string MD5Hash { get; set; } = string.Empty;

        [JsonPropertyName("zip_md5_hash")]
        public string ZipMD5Hash { get; set; } = string.Empty;

        [JsonPropertyName("download_url")]
        public string DownloadUrl { get; set; } = string.Empty;

        [JsonPropertyName("file_description")]
        public string FileDescription { get; set; } = string.Empty;

        [JsonIgnore]
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
                if (_displayVersion != string.Empty)
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

                return _displayVersion;
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

        public int CompareTo(DLLRecord? other)
        {
            if (other is null)
            {
                return -1;
            }

            if (VersionNumber == other.VersionNumber)
            {
                if (IsDevFile == other.IsDevFile)
                {
                    return other.AdditionalLabel.CompareTo(AdditionalLabel);
                }
                
                return other.IsDevFile.CompareTo(IsDevFile);
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

        internal async Task<(bool Success, string Message, bool Cancelled)> DownloadAsync(Action<int>? ProgressCallback = null)
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

            HttpResponseMessage response;
            try
            {
                response = await App.CurrentApp.HttpClient.GetAsync(DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                LocalRecord.IsDownloading = true;
                LocalRecord.DownloadProgress = 0;
                NotifyPropertyChanged("LocalRecord");
            }
            catch (HttpRequestException ex)
            {
                Logger.Error(ex);

                LocalRecord.IsDownloading = false;
                LocalRecord.HasDownloadError = true;
                LocalRecord.DownloadErrorMessage = "Could not download DLSS. Please check your internet connection!";
                NotifyPropertyChanged("LocalRecord");

                return (false, "Could not download DLSS. Please check your internet connection!", false);
            }
            
            if (response.StatusCode is not System.Net.HttpStatusCode.OK)
            {
                App.CurrentApp.RunOnUIThread(() =>
                {
                    LocalRecord.IsDownloading = false;
                    LocalRecord.DownloadProgress = 0;
                    LocalRecord.HasDownloadError = true;
                    LocalRecord.DownloadErrorMessage = "Could not download DLSS.";
                    NotifyPropertyChanged("LocalRecord");
                });

                return (false, "Could not download DLSS.", false);
            }

            var totalDownloadSize = response.Content.Headers.ContentLength ?? 0L;
            var totalBytesRead = 0L;
            var buffer = new byte[1024 * 8];
            var isMoreToRead = true;

            var guid = Guid.NewGuid().ToString().ToUpper();

            var tempPath = Storage.GetTemp();
            var tempZipFile = Path.Combine(tempPath, $"{guid}.zip");

            var targetZipDirectory = DLLManager.Instance.GetExpectedZipPath(this, false);
            Storage.CreateDirectoryIfNotExists(targetZipDirectory);

            try
            {
                using (var fileStream = new FileStream(tempZipFile, FileMode.Create, FileAccess.ReadWrite, FileShare.None, buffer.Length, true))
                {
                    using (var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    {
                        var lastUpdated = DateTimeOffset.Now;
                        do
                        {
                            var bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);

                            if (bytesRead is 0)
                            {
                                isMoreToRead = false;
                                continue;
                            }

                            await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);

                            totalBytesRead += bytesRead;

                            if ((DateTimeOffset.Now - lastUpdated).TotalMilliseconds > 100)
                            {
                                lastUpdated = DateTimeOffset.Now;
                                if (totalDownloadSize > 0)
                                {
                                    App.CurrentApp.RunOnUIThread(() =>
                                    {
                                        var percent = (int)Math.Ceiling((totalBytesRead / (double)totalDownloadSize) * 100L);
                                        ProgressCallback?.Invoke(percent);
                                        LocalRecord.DownloadProgress = percent;
                                        Debug.WriteLine($"Precent: {percent}");
                                        NotifyPropertyChanged("LocalRecord");
                                    });
                                }
                            }
                        }
                        while (isMoreToRead);
                    }

                    if (ZipMD5Hash != fileStream.GetMD5Hash())
                    {
                        throw new Exception("Downloaded file was invalid.");
                    }
                }

                App.CurrentApp.RunOnUIThread(() =>
                {
                    LocalRecord.DownloadProgress = 100;
                    NotifyPropertyChanged("LocalRecord");
                });

                File.Move(tempZipFile, Path.Combine(targetZipDirectory, $"{Version}_{MD5Hash}.zip"), true);

                App.CurrentApp.RunOnUIThread(() =>
                {
                    LocalRecord.IsDownloaded = true;
                    LocalRecord.IsDownloading = false;
                    LocalRecord.DownloadProgress = 0;
                    NotifyPropertyChanged("LocalRecord");
                });

                return (true, string.Empty, false);
            }
            catch (TaskCanceledException)
            {
                App.CurrentApp.RunOnUIThread(() =>
                {
                    LocalRecord.IsDownloading = false;
                    LocalRecord.DownloadProgress = 0;
                    LocalRecord.IsDownloaded = false;
                    NotifyPropertyChanged("LocalRecord");
                });

                return (false, string.Empty, true);
            }
            catch (Exception err)
            {
                Logger.Error(err);

                App.CurrentApp.RunOnUIThread(() =>
                {
                    LocalRecord.IsDownloading = false;
                    LocalRecord.DownloadProgress = 0;
                    LocalRecord.IsDownloaded = false;
                    LocalRecord.HasDownloadError = true;
                    LocalRecord.DownloadErrorMessage = "Could not download DLSS.";
                    NotifyPropertyChanged("LocalRecord");
                });

                return (false, err.Message, false);
            }
            finally
            {
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


        /*
        internal static DLSSRecord FromImportedFile(string fileName)
        {
            if (File.Exists(fileName) == false)
            {
                return null;
            }

            var versionInfo = FileVersionInfo.GetVersionInfo(fileName);

            var dlssRecord = new DLSSRecord()
            {
                Version = versionInfo.GetFormattedFileVersion(),
                VersionNumber = versionInfo.GetFileVersionNumber(),
                MD5Hash = versionInfo.GetMD5Hash(),
                FileSize = 3,
                ZipFileSize = 0,
                ZipMD5Hash = string.Empty,
                IsSignatureValid = WinTrust.VerifyEmbeddedSignature(fileName),
            };

            // TODO: Maybe don't load from here.

            App.CurrentApp.LoadLocalRecordFromDLSSRecord(dlssRecord, true);

            return dlssRecord;
        }
        */

       
    }
}
