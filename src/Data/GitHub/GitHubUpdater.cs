using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ByteSizeLib;
using CommunityToolkit.WinUI.Controls;
using DLSS_Swapper.Extensions;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.UserControls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace DLSS_Swapper.Data.GitHub;

/// <summary>
/// Helper class to be notified of updates of the app (which is Debug and Release builds)
/// </summary>
internal class GitHubUpdater
{
    /// <summary>
    /// Queries GitHub and returns the latest GitHubRelease object, or null if the request failed.
    /// </summary>
    /// <returns>Latest GitHubRelease object, or null if the request failed</returns>
    internal async Task<GitHubRelease?> FetchLatestRelease(bool forceCheck)
    {
        var shouldDownload = true;
        var releasesFile = Storage.GetReleasesPath();
        if (File.Exists(releasesFile))
        {
            var fileInfo = new FileInfo(releasesFile);
            var lastModifiedTime = DateTime.Now - fileInfo.LastWriteTime;
            if (lastModifiedTime.TotalMinutes < 30)
            {
                shouldDownload = false;

                // If we are not downloading and we are not forced to check then return the existing object.
                if (forceCheck == false)
                {
                    using (var fileStream = File.OpenRead(releasesFile))
                    {
                        var githubRelease = JsonSerializer.Deserialize(fileStream, SourceGenerationContext.Default.GitHubRelease);
                        if (githubRelease is not null)
                        {
                            return githubRelease;
                        }
                    }
                }
            }
        }

        if (shouldDownload == true || forceCheck == true)
        {
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    var fileDownloader = new FileDownloader("https://api.github.com/repos/beeradmoore/dlss-swapper/releases/latest", 0);
                    await fileDownloader.DownloadFileToStreamAsync(memoryStream).ConfigureAwait(false);

                    memoryStream.Position = 0;

                    var githubRelease = JsonSerializer.Deserialize(memoryStream, SourceGenerationContext.Default.GitHubRelease);
                    if (githubRelease is null)
                    {
                        throw new Exception("Could not load GitHub release data.");
                    }

                    memoryStream.Position = 0;

                    // If we did load the json, save it to disk.
                    using (var fileStream = File.Create(releasesFile))
                    {
                        await memoryStream.CopyToAsync(fileStream).ConfigureAwait(false);
                    }

                    return githubRelease;
                }
            }
            catch (Exception err)
            {
                // NOOP
                Logger.Error(err);
                return null;
            }
        }

        return null;
    }

    internal async Task<GitHubRelease?> GetReleaseFromTag(string tag)
    {
        try
        {
            using (var memoryStream = new MemoryStream())
            {
                var fileDownloader = new FileDownloader($"https://api.github.com/repos/beeradmoore/dlss-swapper/releases/tags/{tag}", 0);
                await fileDownloader.DownloadFileToStreamAsync(memoryStream).ConfigureAwait(false);
                memoryStream.Position = 0;
                var githubRelease = JsonSerializer.Deserialize(memoryStream, SourceGenerationContext.Default.GitHubRelease);
                if (githubRelease is null)
                {
                    throw new Exception("Could not load GitHub release data.");
                }

                return githubRelease;
            }
        }
        catch (Exception err)
        {
            // NOOP
            Logger.Error(err);
            Debugger.Break();
            return null;
        }
    }

    /// <summary>
    /// Queries GitHub and returns a GitHubRelease only if a newer version was detected, otherwise null
    /// </summary>
    /// <returns>GitHubRelease object if an update is available, otherwise null.</returns>
    internal async Task<GitHubRelease?> CheckForNewGitHubRelease(bool forceCheck)
    {
        var latestRelease = await FetchLatestRelease(forceCheck).ConfigureAwait(false);
        if (latestRelease is null)
        {
            return null;
        }

        var latestVersion = latestRelease.GetVersionNumber();
        var version = App.CurrentApp.GetVersion();
        var currentVersion = ((ulong)version.Major << 48) +
            ((ulong)version.Minor << 32) +
            ((ulong)version.Build << 16) +
            ((ulong)version.Revision);

        // New version is available.
        if (latestVersion > currentVersion)
        {
            return latestRelease;
        }

        return null;
    }


    internal bool HasPromptedBefore(GitHubRelease gitHubRelease)
    {
        var thisVersion = gitHubRelease.GetVersionNumber();
        var lastVersionPromptedFor = Settings.Instance.LastPromptWasForVersion;

        if (lastVersionPromptedFor == 0)
        {
            return false;
        }
        else if (thisVersion < lastVersionPromptedFor)
        {
            return false;
        }

        return true;
    }

    internal async Task DisplayNewUpdateDialog(GitHubRelease gitHubRelease, XamlRoot xamlRoot)
    {
        // Update settings so we won't auto prompt for this version (or lower) ever again.
        var versionNumber = gitHubRelease.GetVersionNumber();
        if (versionNumber > Settings.Instance.LastPromptWasForVersion)
        {
            Settings.Instance.LastPromptWasForVersion = versionNumber;
        }


        var currentVerion = App.CurrentApp.GetVersionString();

        var yourVersion = ResourceHelper.GetFormattedResourceTemplate("GitHubUpdater_CurrentVersionIsActualTemplate", currentVerion);
        var contentUpdate = new MarkdownTextBlock()
        {
            Text = $"{yourVersion}\n\n{gitHubRelease.Body}",
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            Config = new MarkdownConfig(),
        };

        await App.CurrentApp.RunOnUIThreadAsync(async () =>
        {
            var dialog = new EasyContentDialog(xamlRoot)
            {
                Title = $"{ResourceHelper.GetString("GitHubUpdater_UpdateAvailable")} - {gitHubRelease.Name}",
                SecondaryButtonText = ResourceHelper.GetString("GitHubUpdater_ViewUpdate"),
                DefaultButton = ContentDialogButton.Secondary,
                CloseButtonText = ResourceHelper.GetString("General_Cancel"),
                Content = new ScrollViewer()
                {
                    Content = contentUpdate,
                },
            };

            GitHubReleaseAsset? installerAsset = null;

#if PORTABLE == false
            // Only show the update button if we could fetch the update that is ready to install.
            foreach (var gitHubAsset in gitHubRelease.Assets)
            {
                // Check all the strings we want to use exist.
                if (string.IsNullOrWhiteSpace(gitHubAsset.Name) ||
                    string.IsNullOrWhiteSpace(gitHubAsset.ContentType) ||
                    string.IsNullOrWhiteSpace(gitHubAsset.State) ||
                    string.IsNullOrWhiteSpace(gitHubAsset.Digest))
                {
                    continue;
                }

                // Check that we are looking at a exe file.
                if (gitHubAsset.ContentType.Equals("application/x-msdownload", StringComparison.OrdinalIgnoreCase) == false)
                {
                    continue;
                }

                // Check that the state is uploaded.
                if (gitHubAsset.State.Equals("uploaded", StringComparison.OrdinalIgnoreCase) == false)
                {
                    continue;
                }

                // Check if we are looking at something like "DLSS.Swapper-1.2.3.2-installer.exe"
                if (gitHubAsset.Name.EndsWith("-installer.exe", StringComparison.OrdinalIgnoreCase) == false)
                {
                    continue;
                }

                if (installerAsset is not null)
                {
                    // Something happened, we found TWO installer assets. Because we don't know what one should be used we will use none and auto-update will be disabled.
                    installerAsset = null;
                    break;
                }

                installerAsset = gitHubAsset;
            }

            // If the installer asset is found we add the update button and make it the primary response.
            if (installerAsset is not null)
            {
                dialog.PrimaryButtonText = ResourceHelper.GetString("General_Update");
                dialog.DefaultButton = ContentDialogButton.Primary;
            }
#endif

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary && installerAsset is not null)
            {
                await DownloadAndInstallAsync(gitHubRelease, installerAsset, xamlRoot);
            }
            else if (result == ContentDialogResult.Secondary)
            {
                await Launcher.LaunchUriAsync(new Uri(gitHubRelease.HtmlUrl));
            }
        });
    }

    async Task DownloadAndInstallAsync(GitHubRelease gitHubRelease, GitHubReleaseAsset gitHubAsset, XamlRoot xamlRoot)
    {
#if PORTABLE
        // You should not have got here.
        return;
#endif

        var filesProgressBar = new ProgressBar()
        {
            IsIndeterminate = true
        };
        var progressTextBlock = new TextBlock()
        {
            Text = string.Empty,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        progressTextBlock.Inlines.Add(new Run()
        {
            Text = "Progress: "
        });
        var progressRun = new Run() { Text = "-" };
        progressTextBlock.Inlines.Add(progressRun);
        var progressStackPanel = new StackPanel()
        {
            Spacing = 16,
            Orientation = Orientation.Vertical,
            Children =
            {
                filesProgressBar,
                progressTextBlock,
            }
        };




        var updatesFolder = Storage.GetUpdatesFolder();
        var tempDownloadFile = Path.Combine(updatesFolder, gitHubAsset.Name);
        if (Directory.Exists(updatesFolder) == false)
        {
            Directory.CreateDirectory(updatesFolder);
        }

        var shouldDownload = true;
        if (File.Exists(tempDownloadFile))
        {
            using (FileStream fileStream = File.OpenRead(tempDownloadFile))
            {
                var hash = fileStream.GetSha256Hash();
                if (gitHubAsset.Digest.Equals($"sha256:{hash}", StringComparison.OrdinalIgnoreCase))
                {
                    shouldDownload = false;
                }
            }
        }


        if (shouldDownload)
        {
            var cancellationTokenSource = new CancellationTokenSource();

            var downloadingDialog = new EasyContentDialog(xamlRoot)
            {
                Title = "Downloading Update",
                Content = progressStackPanel,
                CloseButtonText = ResourceHelper.GetString("General_Cancel"),
            };
            downloadingDialog.CloseButtonClick += (sender, args) =>
            {
                try
                {
                    cancellationTokenSource.Cancel();
                }
                catch (Exception)
                {
                    // NOOP
                }
            };
            _ = downloadingDialog.ShowAsync();

            var totalSizeString = ByteSize.FromBytes(gitHubAsset.Size).ToString("MB", CultureInfo.CurrentCulture);
            var fileDownloader = new FileDownloader(gitHubAsset.BrowserDownloadUrl);

            try
            {
                using (var fileStream = File.Create(tempDownloadFile))
                {
                    var downloaderTask = fileDownloader.DownloadFileToStreamAsync(fileStream, cancellationTokenSource.Token, progressCallback: (downloadedBytes, totalBytes, percent) =>
                    {
                        var displayPercent = percent * 100;
                        progressRun.Text = $"{ByteSize.FromBytes(downloadedBytes).MegaBytes.ToString("F2", CultureInfo.CurrentCulture)} / {totalSizeString} ({percent:F1}%)";
                        filesProgressBar.IsIndeterminate = false;
                        filesProgressBar.Value = percent;
                    });


                    var didDownload = await downloaderTask;
                    if (didDownload == false)
                    {
                        throw new Exception("DownloadFileToStreamAsync returned false.");
                    }

                    downloadingDialog.Hide();
                }

            }
            catch (TaskCanceledException) when (cancellationTokenSource.IsCancellationRequested)
            {
                // User cancelled.
                downloadingDialog.Hide();
                return;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                downloadingDialog.Hide();

                var downloadErrorDialog = new EasyContentDialog(xamlRoot)
                {
                    Title = ResourceHelper.GetString("General_Error"),
                    Content = "Could not download update.",
                    PrimaryButtonText = ResourceHelper.GetString("GitHubUpdater_ViewUpdate"),
                    CloseButtonText = ResourceHelper.GetString("General_Cancel"),
                    DefaultButton = ContentDialogButton.Primary,
                };

                var downloadErrorResult = await downloadErrorDialog.ShowAsync();
                if (downloadErrorResult == ContentDialogResult.Primary)
                {
                    await Launcher.LaunchUriAsync(new Uri(gitHubRelease.HtmlUrl));
                }

                return;
            }
        }


        var installDialog = new EasyContentDialog(xamlRoot)
        {
            Title = "Download Complete",
            Content = "Update is now ready to install.",
            PrimaryButtonText = "Install",
            CloseButtonText = ResourceHelper.GetString("General_Cancel"),
            DefaultButton = ContentDialogButton.Primary,
        };
        var installDialogResult = await installDialog.ShowAsync();

        if (installDialogResult == ContentDialogResult.Primary)
        {

            var updatingDialog = new EasyContentDialog(xamlRoot)
            {
                Title = "Updating",
                Content = new ProgressRing() { IsIndeterminate = true },
            };
            _ = updatingDialog.ShowAsync();

            // Give the popup time to show.
            await Task.Delay(500);

            try
            {
                var processStartInfo = new ProcessStartInfo()
                {
                    FileName = tempDownloadFile,
                    UseShellExecute = true,
                };
                var installerProcess = Process.Start(processStartInfo);
                if (installerProcess is null)
                {
                    throw new Exception("Could not launch installer");
                }

                // Close DLSS Swapper so the installer can install
                Application.Current.Exit();
            }
            catch (Exception err)
            {
                Logger.Error(err);

                updatingDialog.Hide();

                var errorDialog = new EasyContentDialog(xamlRoot)
                {
                    Title = ResourceHelper.GetString("General_Error"),
                    Content = "Could not run the installer at this time. Please try again later, or manually install the update.",
                    PrimaryButtonText = ResourceHelper.GetString("GitHubUpdater_ViewUpdate"),
                    CloseButtonText = ResourceHelper.GetString("General_Cancel"),
                    DefaultButton = ContentDialogButton.Primary,
                };
                var errorDialogResult = await errorDialog.ShowAsync();

                if (errorDialogResult == ContentDialogResult.Primary)
                {
                    await Launcher.LaunchUriAsync(new Uri(gitHubRelease.HtmlUrl));
                }
            }
        }

    }

    internal async Task DisplayWhatsNewDialog(GitHubRelease gitHubRelease, XamlRoot xamlRoot)
    {
        var contentUpdate = new MarkdownTextBlock()
        {
            Text = gitHubRelease.Body,
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            Config = new MarkdownConfig(),
        };

        var dialog = new EasyContentDialog(xamlRoot)
        {
            Title = $"{ResourceHelper.GetString("GitHubUpdater_DlssSwapperUpdated")} - {gitHubRelease.Name}",
            CloseButtonText = ResourceHelper.GetString("General_Cancel"),
            DefaultButton = ContentDialogButton.Close,
            Content = new ScrollViewer()
            {
                Content = contentUpdate,
            },
        };
        await dialog.ShowAsync();
    }
}
