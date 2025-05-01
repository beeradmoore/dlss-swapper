using System.Collections.Generic;
using System.IO;
using DLSS_Swapper.Data.BattleNet.Proto;

namespace DLSS_Swapper.Data.BattleNet;

internal static class BattleNetGameTitles
{
    private static readonly Dictionary<string, string> _uidTitles = new()
    {
        { "s1", "StarCraft" },
        { "s2", "StarCraft II" },
        { "wow", "World of Warcraft" },
        { "wow_classic", "World of Warcraft Classic" },
        { "pro", "Overwatch 2" },
        { "w2bn", "Warcraft II: Battle.net Edition" },
        { "w3", "Warcraft III" },
        { "hsb", "Hearthstone" },
        { "hero", "Heroes of the Storm" },
        { "d3cn", "暗黑破壞神III" },
        { "d3", "Diablo III" },
        { "fenris", "Diablo IV" },
        { "viper", "Call of Duty: Black Ops 4" },
        { "odin", "Call of Duty: Modern Warfare" },
        { "lazarus", "Call of Duty: MW2 Campaign Remastered" },
        { "zeus", "Call of Duty: Black Ops Cold War" },
        { "rtro", "Blizzard Arcade Collection" },
        { "wlby", "Crash Bandicoot 4: It's About Time" },
        { "osi", "Diablo® II: Resurrected" },
        { "fore", "Call of Duty: Vanguard" },
        { "d2", "Diablo® II" },
        { "d2LOD", "Diablo® II: Lord of Destruction®" },
        { "w3ROC", "Warcraft® III: Reign of Chaos" },
        { "w3tft", "Warcraft® III: The Frozen Throne®" },
        { "sca", "StarCraft® Anthology" },
        { "anbs", "Diablo Immortal" }
    };

    public static string GetTitle(this ProductInstall product)
    {
        if (_uidTitles.TryGetValue(product.Uid, out var title))
        {
            return title;
        }

        if (string.IsNullOrEmpty(product.Settings.InstallPath))
        {
            return product.Uid;
        }

        var dir = new DirectoryInfo(product.Settings.InstallPath);
        return dir.Name;
    }
}
