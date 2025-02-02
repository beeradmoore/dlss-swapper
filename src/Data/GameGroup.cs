using CommunityToolkit.WinUI.Collections;

namespace DLSS_Swapper.Data;

internal class GameGroup
{
    public string Name { get; init; } = string.Empty;
    public AdvancedCollectionView Games { get; init; }

    public GameGroup(string name, AdvancedCollectionView games)
    {
        Name = name;
        Games = games;
    }
}
