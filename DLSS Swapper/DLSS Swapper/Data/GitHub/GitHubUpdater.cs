using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace DLSS_Swapper.Data.GitHub
{
    /// <summary>
    /// Helper class to be notified of updates in non-Windows Store builds of the app (which is Debug and Release builds)
    /// </summary>
    internal class GitHubUpdater
    {
        /// <summary>
        /// Queries GitHub and returns the latest GitHubRelease object, or null if the request failed.
        /// </summary>
        /// <returns>Latest GitHubRelease object, or null if the request failed</returns>
        internal async Task<GitHubRelease> FetchLatestRelease()
        {
            try
            {
                return await App.CurrentApp.HttpClient.GetFromJsonAsync<GitHubRelease>("https://api.github.com/repos/beeradmoore/dlss-swapper/releases/latest").ConfigureAwait(false);
            }
            catch (Exception err)
            {
                System.Diagnostics.Debug.WriteLine($"FetchLatestRelease Error: {err.Message}");
                // NOOP
                return null;
            }
        }

        internal async Task<GitHubRelease> GetReleaseFromTag(string tag)
        {
            try
            {
                return await App.CurrentApp.HttpClient.GetFromJsonAsync<GitHubRelease>($"https://api.github.com/repos/beeradmoore/dlss-swapper/releases/tags/{tag}").ConfigureAwait(false);
            }
            catch (Exception err)
            {
                System.Diagnostics.Debug.WriteLine($"GetFromTag Error: {err.Message}");
                // NOOP
                return null;
            }
        }

#if !RELEASE_WINDOWSSTORE
        /// <summary>
        /// Queries GitHub and returns a GitHubRelease only if a newer version was detected, otherwise null
        /// </summary>
        /// <returns>GitHubRelease object if an update is available, otherwise null.</returns>
        internal async Task<GitHubRelease> CheckForNewGitHubRelease()
        {
            var latestRelease = await FetchLatestRelease().ConfigureAwait(false);
            if (latestRelease == null)
            {
                return null;
            }

            var latestVersion = latestRelease.GetVersionNumber();

            var version = Windows.ApplicationModel.Package.Current.Id.Version;
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
#endif


        internal bool HasPromptedBefore(GitHubRelease gitHubRelease)
        {
            var thisVersion = gitHubRelease.GetVersionNumber();
            var lastVersionPromptedFor = Settings.LastPromptWasForVersion;

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

        internal async Task DisplayNewUpdateDialog(GitHubRelease gitHubRelease, Control rootElement)
        {
            // Update settings so we won't auto prompt for this version (or lower) ever again.
            var versionNumber = gitHubRelease.GetVersionNumber();
            if (versionNumber > Settings.LastPromptWasForVersion)
            {
                Settings.LastPromptWasForVersion = versionNumber;
            }


            var version = Windows.ApplicationModel.Package.Current.Id.Version;
            var currentVerion = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";

            var yourVersion = $"You currently have {currentVerion} installed.\n\n";
            var contentUpdate = new MarkdownTextBlock()
            {
                Text = yourVersion + gitHubRelease.Body,
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            };

            var dialog = new ContentDialog()
            {
                Title = $"Update Available - {gitHubRelease.Name}",
                PrimaryButtonText = "Update",
                CloseButtonText = "Cancel",
                Content = new ScrollViewer()
                {
                    Content = contentUpdate,
                },
                XamlRoot = rootElement.XamlRoot,
                RequestedTheme = Settings.AppTheme,
            };
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                await Launcher.LaunchUriAsync(new Uri(gitHubRelease.HtmlUrl));
            }
        }
        internal async Task DisplayWhatsNewDialog(GitHubRelease gitHubRelease, Control rootElement)
        {
            var contentUpdate = new MarkdownTextBlock()
            {
                Text = gitHubRelease.Body,
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            };

            var dialog = new ContentDialog()
            {
                Title = $"DLSS Swapper just updated - {gitHubRelease.Name}",
                PrimaryButtonText = "Cancel",
                Content = new ScrollViewer()
                {
                    Content = contentUpdate,
                },
                XamlRoot = rootElement.XamlRoot,
                RequestedTheme = Settings.AppTheme,
            };
            await dialog.ShowAsync();
        }
    }
}
