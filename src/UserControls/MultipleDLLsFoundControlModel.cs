using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLSS_Swapper.Data;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Translations.UserControls;

namespace DLSS_Swapper.UserControls;

public partial class MultipleDLLsFoundControlModel : ObservableObject
{
    public MultipleDLLsFoundControlModel(Game game, GameAssetType gameAssetType) : base()
    {
        TranslationProperties = new MultipleDLLsFoundTranslationPropertiesViewModel();
        DLLsList = game.GameAssets.Where(x => x.AssetType == gameAssetType).ToList();
    }

    [ObservableProperty]
    public partial MultipleDLLsFoundTranslationPropertiesViewModel TranslationProperties { get; private set; }

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
}
