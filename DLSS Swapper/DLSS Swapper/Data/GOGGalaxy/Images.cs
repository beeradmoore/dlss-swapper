using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DLSS_Swapper.Data.GOGGalaxy
{
    internal class Images
    {
        [JsonPropertyName("background")]
        public string Background { get; set; }

        [JsonPropertyName("icon")]
        public string Icon { get; set; }

        [JsonPropertyName("logo")]
        public string Logo { get; set; }

        [JsonPropertyName("logo2x")]
        public string Logo2x { get; set; }

        [JsonPropertyName("menuNotificationAv")]
        public string MenuNotificationAv { get; set; }

        [JsonPropertyName("menuNotificationAv2")]
        public string MenuNotificationAv2 { get; set; }

        [JsonPropertyName("sidebarIcon")]
        public string SidebarIcon { get; set; }

        [JsonPropertyName("sidebarIcon2x")]
        public string SidebarIcon2x { get; set; }
    }
}
