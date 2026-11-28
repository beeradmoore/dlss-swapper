using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DLSS_Swapper.Data.EpicGamesStore;

// Represents %LOCALAPPDATA%\com.epicgames.eglv2\cache\prod\catalog-cache.json from
// the new (eglv2 / "beta") Epic Games Launcher.
internal class LauncherCatalogCache
{
    [JsonPropertyName("items")]
    public Dictionary<string, LauncherCatalogCacheEntry> Items { get; set; } = new Dictionary<string, LauncherCatalogCacheEntry>();
}

internal class LauncherCatalogCacheEntry
{
    [JsonPropertyName("item")]
    public CacheItem? Item { get; set; }
}
