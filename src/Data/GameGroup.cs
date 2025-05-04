using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.WinUI.Collections;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.Data;

internal class GameGroup
{
    public string Name { get; init; } = string.Empty;
    public GameLibrary? GameLibrary { get; init; }
    public AdvancedCollectionView Games { get; init; }

    public GameGroup(string name, GameLibrary? gameLibrary, AdvancedCollectionView games)
    {
        Name = name;
        GameLibrary = gameLibrary;
        Games = games;
    }
}
