using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DLSS_Swapper.Helpers;

public partial class FileDownloader : ObservableObject
{
    /// <summary>
    /// Percent, 0.0 to 100.0
    /// </summary>
    [ObservableProperty]
    public partial double Percent { get; set; } = 0.0;

    /// <summary>
    /// Used to indicate there is download progress or that download progress can be tracked
    /// </summary>
    [ObservableProperty]
    public partial bool IsIndeterminate { get; set; } = true;

    /// <summary>
    /// Used to report the current bytes downloaded.
    /// </summary>
    [ObservableProperty]
    public partial long DownloadedBytes { get; set; } = 0L;

    /// <summary>
    /// Used to report the total bytes to download. 
    /// </summary>
    [ObservableProperty]
    public partial long TotalBytesToDownload { get; set; } = 0L;

    public Guid Guid { get; } = Guid.NewGuid();

    public const int BufferSize = 65536;

    string _url;

    public string LogPrefix { get; set; } = string.Empty;

    int _timerInterval = 0;
    public FileDownloader(string url, int timerInterval = 100)
    {
        _url = url;
        _timerInterval = timerInterval;
    }

    public async Task<bool> DownloadFileToStreamAsync(Stream outputStream, CancellationToken cancellationToken = default(CancellationToken), Action<HttpStatusCode>? statusCodeCallback = null, Action<long, long, double>? progressCallback = null)
    {
        var totalBytesRead = 0L;
        var lastReportedPercent = 0.0;
        var lastReportedBytes = 0L;
        var contentLength = -1L;

        System.Timers.Timer? uiUpdateTimer = null;

        if (string.IsNullOrWhiteSpace(LogPrefix))
        {
            LogPrefix = $"{Guid.ToString("D")} - ";
        }

        if (_timerInterval > 0)
        {
            uiUpdateTimer = new System.Timers.Timer(_timerInterval)
            {
                AutoReset = true,
            };
            uiUpdateTimer.Elapsed += (sender, e) =>
            {
                var shouldUpdate = false;
                var newPercent = 0.0;
                // If we don't have a content length we don't calculate percent, but we do calculate bytes changed
                if (contentLength == -1)
                {
                    if (totalBytesRead != lastReportedBytes)
                    {
                        lastReportedBytes = totalBytesRead;
                        shouldUpdate = true;
                    }
                }
                else
                {
                    newPercent = (totalBytesRead / (double)TotalBytesToDownload) * 100.0;
                    if (lastReportedPercent != newPercent)
                    {
                        lastReportedPercent = newPercent;
                        shouldUpdate = true;
                    }
                }

                if (shouldUpdate)
                {
                    App.CurrentApp.RunOnUIThread(() =>
                    {
                        DownloadedBytes = totalBytesRead;
                        Percent = newPercent;

                        progressCallback?.Invoke(DownloadedBytes, TotalBytesToDownload, Percent);
                    });
                }
            };
        }

        // 64kb buffer
        var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        try
        {
            Logger.Verbose($"{LogPrefix}Starting download of {_url}");

            using (var response = await App.CurrentApp.HttpClient.GetAsync(_url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
            {
                Logger.Verbose($"{LogPrefix}Status Code: {response.StatusCode}");
                statusCodeCallback?.Invoke(response.StatusCode);

                response.EnsureSuccessStatusCode();

                if (Settings.Instance.LoggingLevel == LoggingLevel.Verbose)
                {
                    Logger.Verbose($"{LogPrefix}HTTP Version: {response.Version}");
                    if (response.RequestMessage?.Headers is not null)
                    {
                        foreach (var header in response.RequestMessage.Headers)
                        {
                            Logger.Verbose($"{LogPrefix}Request Header {header.Key}: {string.Join(", ", header.Value)}");
                        }
                    }

                    if (response.Headers is not null)
                    {
                        foreach (var header in response.Headers)
                        {
                            Logger.Verbose($"{LogPrefix}Response Header {header.Key}: {string.Join(", ", header.Value)}");
                        }
                    }

                    if (response.Content.Headers is not null)
                    {
                        foreach (var header in response.Content.Headers)
                        {
                            Logger.Verbose($"{LogPrefix}Content Header {header.Key}: {string.Join(", ", header.Value)}");
                        }
                    }
                }

                contentLength = response.Content.Headers?.ContentLength ?? -1;

                App.CurrentApp.RunOnUIThread(() =>
                {
                    Percent = 0.0;
                    // Stay on IsIndeterminate if there is no content length
                    IsIndeterminate = contentLength == -1;
                    TotalBytesToDownload = contentLength;
                });

                uiUpdateTimer?.Start();

                using (var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                {
                    var bytesRead = 0;
                    while ((bytesRead = await responseStream.ReadAsync(buffer.AsMemory(0, BufferSize), cancellationToken).ConfigureAwait(false)) > 0)
                    {
                        await outputStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                        //Interlocked.Add(ref totalBytesRead, bytesRead);
                        totalBytesRead += bytesRead;
                    }
                }
            }

            App.CurrentApp.RunOnUIThread(() =>
            {
                Percent = 100.0;
            });

            Logger.Verbose($"{LogPrefix}Complete. Read {totalBytesRead} bytes from {_url}");
            return true;
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested == false)
        {
            throw;
        }
        catch (Exception err)
        {
            Logger.Error(err, $"{LogPrefix} could not download {_url}");
            throw;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            uiUpdateTimer?.Stop();
        }
    }
}
