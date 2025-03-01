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

    public Guid Guid { get; } = Guid.CreateVersion7();

    public const int BufferSize = 65536;

    string _url;

    public FileDownloader(string url)
    {
        _url = url;
    }

    public async Task<bool> DownloadFileToStreamAsync(Stream outputStream, CancellationToken cancellationToken = default(CancellationToken), bool withTimer = true)
    {
        var totalBytesRead = 0L;
        var lastReportedPercent = 0.0;
        var lastReportedBytes = 0L;
        var contentLength = -1L;

        System.Timers.Timer? uiUpdateTimer = null;
        var requestId = Guid.ToString("D");

        if (withTimer == true)
        {
            uiUpdateTimer = new System.Timers.Timer(100)
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
                    });
                }
            };
        }

        // 64kb buffer
        var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        try
        {
            Logger.Verbose($"{requestId} - Starting download of {_url}");

            using (var response = await App.CurrentApp.HttpClient.GetAsync(_url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();

                if (Settings.Instance.LoggingLevel == LoggingLevel.Verbose)
                {
                    Logger.Verbose($"{requestId} - HTTP Version: {response.Version}");
                    if (response.RequestMessage?.Headers is not null)
                    {
                        foreach (var header in response.RequestMessage.Headers)
                        {
                            Logger.Verbose($"{requestId} - Request Header {header.Key}: {string.Join(", ", header.Value)}");
                        }
                    }

                    if (response.Headers is not null)
                    {
                        foreach (var header in response.Headers)
                        {
                            Logger.Verbose($"{requestId} - Response Header {header.Key}: {string.Join(", ", header.Value)}");
                        }
                    }

                    if (response.Content.Headers is not null)
                    {
                        foreach (var header in response.Content.Headers)
                        {
                            Logger.Verbose($"{requestId} - Content Header {header.Key}: {string.Join(", ", header.Value)}");
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
                    while ((bytesRead = await responseStream.ReadAsync(buffer, 0, BufferSize, cancellationToken).ConfigureAwait(false)) > 0)
                    {
                        await outputStream.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
                        //Interlocked.Add(ref totalBytesRead, bytesRead);
                        totalBytesRead += bytesRead;
                    }
                }
            }
            

            App.CurrentApp.RunOnUIThread(() =>
            {
                Percent = 100.0;
            });

            return true;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            uiUpdateTimer?.Stop();
        }
    }

    private void UiUpdateTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        throw new NotImplementedException();
    }
}
