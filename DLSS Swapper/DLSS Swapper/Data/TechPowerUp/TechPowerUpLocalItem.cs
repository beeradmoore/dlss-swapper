using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DLSS_Swapper.Data.TechPowerUp
{
    public class TechPowerUpLocalItem : INotifyPropertyChanged
    {
        public TechPowerUpDownloadItem DownloadItem { get; private set; }


        double _downloadPercent;
        public double DownloadPercent
        {
            get
            {
                return _downloadPercent;
            }
            set
            {
                if (value != _downloadPercent)
                {
                    _downloadPercent = value;
                    NotifyPropertyChanged();
                }
            }
        }

        bool _isDownloaded;
        public bool IsDownloaded
        {
            get { return _isDownloaded; }
            set
            {
                if (value != _isDownloaded)
                {
                    _isDownloaded = value;
                    NotifyPropertyChanged();
                }
            }
        }

        bool _isDownloading;
        public bool IsDownloading
        {
            get { return _isDownloading; }
            set
            {
                if (value != _isDownloading)
                {
                    _isDownloading = value;
                    NotifyPropertyChanged();
                }
            }
        }


        public string LocalFile => Path.Combine(Settings.TechPowerUpDownloadsDirectory, Path.GetFileName(DownloadItem.FileName));
        

        public TechPowerUpLocalItem(TechPowerUpDownloadItem downloadItem)
        {
            DownloadItem = downloadItem;
            ValidateLocalFile();
        }

        bool ValidateLocalFile()
        {
            if (File.Exists(LocalFile))
            {
                if (GetLocalFileMD5() == DownloadItem.MD5Hash)
                {
                    ((App)Application.Current).Window.DispatcherQueue.TryEnqueue(() =>
                    {
                        IsDownloaded = true;
                    });
                    return true;
                }
                else
                {
                    // TODO: Alert that incorrect hash was discovered.
                    File.Delete(LocalFile);
                }
            }

            return false;
        }

        string GetLocalFileMD5()
        {
            if (File.Exists(LocalFile))
            {
                using var md5 = MD5.Create();
                using var stream = File.OpenRead(LocalFile);
                var md5Hash = md5.ComputeHash(stream);
                return BitConverter.ToString(md5Hash).Replace("-", "").ToUpperInvariant();
            }

            return String.Empty;
        }

        public void StartDownload(string url, IProgress<int> progress)
        {
            Task.Run(async () =>
            {
                ((App)Application.Current).Window.DispatcherQueue.TryEnqueue(() =>
                {
                    IsDownloaded = false;
                    IsDownloading = true;
                });
                var filename = Path.GetFileName(url);
                try
                {
                    using (var httpClient = new HttpClient())
                    {
                        var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                        if (response.IsSuccessStatusCode)
                        {
                            using (var stream = await response.Content.ReadAsStreamAsync())
                            {
                                using (var file = File.Create(LocalFile))
                                {
                                    var buffer = new byte[1024 * 8];
                                    int bytesRead = 0;
                                    long bytesDownloaded = 0;
                                    double contentLength = (double)response.Content.Headers.ContentLength.Value;
                                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                    {
                                        await file.WriteAsync(buffer, 0, bytesRead);

                                        bytesDownloaded += bytesRead;
                                        progress.Report((int)(bytesDownloaded / contentLength * 100.0));
                                    }
                                }
                            }

                            await ExtractDll();
                        }
                        
                        else
                        {
                            ((App)Application.Current).Window.DispatcherQueue.TryEnqueue(async () =>
                            {
                                var dialog = new ContentDialog()
                                {
                                    Title = "Error",
                                    Content = $"Unable to download {filename}",
                                    CloseButtonText = "Okay",
                                    XamlRoot = ((App)Application.Current).MainWindow.Content.XamlRoot,
                                };
                                await dialog.ShowAsync();
                            });
                        }
                    }
                }
                catch (Exception err)
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR: Could now download zip, {err.Message}");


                    ((App)Application.Current).Window.DispatcherQueue.TryEnqueue(async () =>
                    {
                        var dialog = new ContentDialog()
                        {
                            Title = "Error",
                            Content = $"Unable to download {filename}.\nMessage: {err.Message}",
                            CloseButtonText = "Okay",
                            XamlRoot = ((App)Application.Current).MainWindow.Content.XamlRoot,
                        };
                        await dialog.ShowAsync();
                    });
                }
                finally
                {
                    ((App)Application.Current).Window.DispatcherQueue.TryEnqueue(() =>
                    {
                        IsDownloading = false;
                    });
                }
            });
        }

        public async Task<bool> ExtractDll()
        {
            if (ValidateLocalFile())
            {
                TaskCompletionSource<bool> didValidateAndExtractTCS = new TaskCompletionSource<bool>();
                return await Task.Run<bool>(() =>
                {
                    var expectedDllVersion = Path.GetFileName(LocalFile).Replace("nvngx_dlss_", String.Empty).Replace(".zip", String.Empty);

                    var tempDirectory = Path.Combine(Path.GetTempPath(), "DLSS Swapper", expectedDllVersion);
                    ZipFile.ExtractToDirectory(LocalFile, tempDirectory, true);
                    var expectedDllFilePath = Path.Combine(tempDirectory, "nvngx_dlss.dll");
                    if (File.Exists(expectedDllFilePath))
                    {
                        var detectedDllVersion = FileVersionInfo.GetVersionInfo(expectedDllFilePath).FileVersion.Replace(",", ".");

                        // Eh.. detectedDllVersion would be 2.2.11.0 but expectedDllVersion is 2.2.11
                        // Should update to use hashes in the future.
                        if (detectedDllVersion.StartsWith(expectedDllVersion))
                        {
                            var targetDllDirectory = Path.Combine(Settings.DllsDirectory, detectedDllVersion);
                            if (Directory.Exists(targetDllDirectory) == false)
                            {
                                Directory.CreateDirectory(targetDllDirectory);
                                File.Copy(expectedDllFilePath, Path.Combine(targetDllDirectory, "nvngx_dlss.dll"), true);
                                return true;
                            }
                        }
                    }

                    return false;
                });
            }
            else
            {
                // TODO: Error that the file is invalid.
            }
            return false;
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
