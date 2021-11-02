using DLSS_Swapper.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace DLSS_Swapper.Data
{
    public class DLSSRecord : IComparable<DLSSRecord>, INotifyPropertyChanged
    {
        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("version_number")]
        public ulong VersionNumber { get; set; }

        [JsonPropertyName("additional_label")]
        public string AdditionalLabel { get; set; } = String.Empty;

        [JsonPropertyName("md5_hash")]
        public string MD5Hash { get; set; }

        [JsonPropertyName("zip_md5_hash")]
        public string ZipMD5Hash { get; set; }

        [JsonPropertyName("download_url")]
        public string DownloadUrl { get; set; } = String.Empty;

        [JsonPropertyName("file_description")]
        public string FileDescription { get; set; } = String.Empty;

        [JsonIgnore]
        public DateTime SignedDateTime { get; set; } = DateTime.MinValue;

        [JsonPropertyName("is_signature_valid")]
        public bool IsSignatureValid { get; set; }

        [JsonPropertyName("file_size")]
        public long FileSize { get; set; }

        [JsonPropertyName("zip_file_size")]
        public long ZipFileSize { get; set; }

        [JsonIgnore]
        public string FullName
        {
            get
            {
                if (String.IsNullOrEmpty(AdditionalLabel))
                {
                    return Version;
                }

                return $"{Version} - {AdditionalLabel}";
            }
        }

        [JsonIgnore]
        public LocalRecord LocalRecord { get; set; }

        public int CompareTo(DLSSRecord other)
        {
            if (VersionNumber == other.VersionNumber)
            {
                return other.AdditionalLabel.CompareTo(AdditionalLabel);
            }

            return other.VersionNumber.CompareTo(VersionNumber);
        }


        public bool ValidateLocalSignature()
        {
            var filename = "";
            return WinTrust.VerifyEmbeddedSignature(filename);
        }


        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        internal void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        private CancellationTokenSource _cancellationTokenSource;

        internal void CancelDownload()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = null;
        }

        internal async Task<(bool Success, string Message, bool Cancelled)> DownloadAsync(Action<int> ProgressCallback = null)
        {

            var dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

            if (String.IsNullOrEmpty(DownloadUrl))
            {
                return (false, "Invalid download URL.", false);
            }

            _cancellationTokenSource?.Cancel();

            LocalRecord.IsDownloading = true;
            LocalRecord.DownloadProgress = 0;
            LocalRecord.HasDownloadError = false;
            LocalRecord.DownloadErrorMessage = String.Empty;
            NotifyPropertyChanged("LocalRecord");

            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;
            var response = await App.CurrentApp.HttpClient.GetAsync(DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {

                dispatcherQueue.TryEnqueue(() =>
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

            var tempPath = Windows.Storage.ApplicationData.Current.TemporaryFolder.Path;
            var tempZipFile = Path.Combine(tempPath, $"{guid}.zip");
            var tempDllFile = Path.Combine(tempPath, $"{guid}.dll");
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

                            if (bytesRead == 0)
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
                                    dispatcherQueue.TryEnqueue(() =>
                                    {
                                        var percent = (int)Math.Ceiling((totalBytesRead / (double)totalDownloadSize) * 100L);
                                        ProgressCallback?.Invoke(percent);
                                        LocalRecord.DownloadProgress = percent;
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

                dispatcherQueue.TryEnqueue(() =>
                {
                    LocalRecord.DownloadProgress = 100;
                    NotifyPropertyChanged("LocalRecord");
                });


                var storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                var dllsFolder = await storageFolder.CreateFolderAsync("dlls", Windows.Storage.CreationCollisionOption.OpenIfExists);



                using (var archive = ZipFile.OpenRead(tempZipFile))
                {
                    var zippedDlls = archive.Entries.Where(x => x.Name.EndsWith(".dll")).ToArray();
                    if (zippedDlls.Length != 1)
                    {
                        throw new Exception("Downloaded file was invalid.");
                    }

                    zippedDlls[0].ExtractToFile(tempDllFile, true);
                }

                var versionInfo = FileVersionInfo.GetVersionInfo(tempDllFile);

                var dlssVersion = versionInfo.GetFormattedFileVersion();
                if (MD5Hash != versionInfo.GetMD5Hash())
                {
                    throw new Exception("Downloaded file was invalid.");
                }

                if (Settings.AllowUntrusted == false)
                {
                    var isTrusted = WinTrust.VerifyEmbeddedSignature(tempDllFile);
                    if (isTrusted == false)
                    {
                        throw new Exception("Downloaded file was not trusted by Windows.");
                    }
                }

                var dlssFolder = await dllsFolder.CreateFolderAsync($"{dlssVersion}_{MD5Hash}", Windows.Storage.CreationCollisionOption.OpenIfExists);
                var dlssFile = Path.Combine(dlssFolder.Path, "nvngx_dlss.dll");
                File.Move(tempDllFile, dlssFile, true);

                dispatcherQueue.TryEnqueue(() =>
                {
                    LocalRecord.IsDownloaded = true;
                    LocalRecord.IsDownloading = false;
                    LocalRecord.DownloadProgress = 0;
                    NotifyPropertyChanged("LocalRecord");
                });

                return (true, String.Empty, false);
            }
            catch (TaskCanceledException)
            {
                dispatcherQueue.TryEnqueue(() =>
                {
                    LocalRecord.IsDownloading = false;
                    LocalRecord.DownloadProgress = 0;
                    LocalRecord.IsDownloaded = false;
                    NotifyPropertyChanged("LocalRecord");
                });


                return (false, String.Empty, true);
            }
            catch (Exception err)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: {err.Message}");

                dispatcherQueue.TryEnqueue(() =>
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

                try
                {
                    File.Delete(tempDllFile);
                }
                catch (Exception)
                {
                    // NOOP
                }

            }
        }

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
                ZipMD5Hash = String.Empty,
                IsSignatureValid = WinTrust.VerifyEmbeddedSignature(fileName),
            };

            App.CurrentApp.LoadLocalRecordFromDLSSRecord(dlssRecord, true);

            return dlssRecord;
        }
    }
}
