using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DLSS_Swapper.Data;

namespace DLSS_Swapper.UserControls;

public class MultipleDLLsFoundControlModel : ObservableObject
{
    public List<GameAsset> DLLsList { get; set; }

    public MultipleDLLsFoundControlModel(Game game, GameAssetType gameAssetType)
    {
        DLLsList = game.GameAssets.Where(x => x.AssetType == gameAssetType).ToList();
    }
}
