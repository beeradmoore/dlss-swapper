using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DLSS_Swapper.Pages;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace DLSS_Swapper.Data.GitHub
{
    internal class GitHubRelease
    {
        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; }
        // https://github.com/beeradmoore/dlss-swapper/releases/tag/v0.9.8.0

        [JsonPropertyName("tag_name")]
        public string TagName { get; set; }
        // v0.9.8.0

        [JsonPropertyName("name")]
        public string Name { get; set; }
        // v0.9.8.0

        [JsonPropertyName("draft")]
        public bool Draft { get; set; }
        // false

        [JsonPropertyName("prerelease")]
        public bool PreRelease { get; set; }
        // false

        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; }
        // 2022-01-29T04:57:30Z

        [JsonIgnore]
        public DateTime CreateAtDateTime => DateTime.Parse(CreatedAt);

        [JsonPropertyName("published_at")]
        public string PublishedAt { get; set; }
        // 2022-01-29T05:02:29Z

        public DateTime PublishedAtDateTime => DateTime.Parse(PublishedAt);

        [JsonPropertyName("body")]
        public string Body { get; set; }
        // ## What's Changed\r\n* Fixed issue where circular symbolic links would...

        internal ulong GetVersionNumber()
        {
            // Name should always start with a version, it could be in the format v1, v1.1, v1.1.1, or v1.1.1.1
            var firstPartOfName = Name?.Split(" ").FirstOrDefault()?.Trim();
            if (firstPartOfName == null || firstPartOfName.StartsWith("v", StringComparison.InvariantCultureIgnoreCase) == false)
            {
                return 0;
            }

            // This will split v1 through to v1.1.1.1 as 4 parts of the latest release version.
            ulong version = 0;
            var latestReleaseVersionParts = firstPartOfName.Substring(1).Split(".");
            if (latestReleaseVersionParts.Length >= 1)
            {
                if (ulong.TryParse(latestReleaseVersionParts[0], out ulong latestReleaseMajor) == false)
                {
                    return 0;
                }
                version += (latestReleaseMajor << 48);

                if (latestReleaseVersionParts.Length >= 2)
                {
                    if (ulong.TryParse(latestReleaseVersionParts[1], out ulong latestReleaseMinor) == false)
                    {
                        return 0;
                    }
                    version += (latestReleaseMinor << 32);

                    if (latestReleaseVersionParts.Length >= 3)
                    {
                        if (ulong.TryParse(latestReleaseVersionParts[2], out ulong latestReleaseBuild) == false)
                        {
                            return 0;
                        }
                        version += (latestReleaseBuild << 16);

                        if (latestReleaseVersionParts.Length >= 4)
                        {
                            if (ulong.TryParse(latestReleaseVersionParts[3], out ulong latestReleaseRevision) == false)
                            {
                                return 0;
                            }
                            version += latestReleaseRevision;
                        }
                    }
                }

                return version;
            }
            else
            {
                // This shouldn't be able to happen, but if our list was 0 items this will be hit.
                return 0;
            }
        }
    }
}
