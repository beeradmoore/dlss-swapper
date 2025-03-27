using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using DLSS_Swapper.Attributes;
using DLSS_Swapper.Data;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.UserControls;

public partial class MultipleDLLsFoundControlModel : LocalizedViewModelBase
{
    public MultipleDLLsFoundControlModel(Game game, GameAssetType gameAssetType) : base()
    {
        DLLsList = game.GameAssets.Where(x => x.AssetType == gameAssetType).ToList();
    }

    public List<GameAsset> DLLsList { get; init; }

    [RelayCommand]
    void OpenDLLPath(GameAsset gameAsset)
    {
        try
        {
            if (File.Exists(gameAsset.Path))
            {
                Process.Start("explorer.exe", $"/select,{gameAsset.Path}");
            }
            else
            {
                var dllPath = Path.GetDirectoryName(gameAsset.Path) ?? string.Empty;
                if (Directory.Exists(dllPath))
                {
                    Process.Start("explorer.exe", dllPath);
                }
                else
                {
                    throw new Exception(ResourceHelper.GetFormattedResourceTemplate("CouldNotFindGameInstallPathTemplate", gameAsset.Path));
                }
            }
        }
        catch (Exception err)
        {
            Logger.Error(err);
        }
    }


    #region LanguageProperties
    [LanguageProperty] public string BelowMultipleDllFoundYouWillBeAbleToSwapInfo => ResourceHelper.GetString("BelowMultipleDllFoundYouWillBeAbleToSwapInfo");
    #endregion
}
