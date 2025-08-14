using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Labs.WinUI.MarkdownTextBlock;
using CommunityToolkit.WinUI;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.UserControls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace DLSS_Swapper.Data.GitHub
{
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
                    PrimaryButtonText = ResourceHelper.GetString("General_Update"),
                    CloseButtonText = ResourceHelper.GetString("General_Cancel"),
                    DefaultButton = ContentDialogButton.Primary,
                    Content = new ScrollViewer()
                    {
                        Content = contentUpdate,
                    },
                };
                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    await Launcher.LaunchUriAsync(new Uri(gitHubRelease.HtmlUrl));
                }
            });
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
}
