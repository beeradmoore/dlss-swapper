using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLSS_Swapper.Attributes;
using DLSS_Swapper.Data;
using DLSS_Swapper.Helpers;

namespace DLSS_Swapper.UserControls;

public partial class MultipleDLLsFoundControlModel : ObservableObject, IDisposable
{
    public MultipleDLLsFoundControlModel(Game game, GameAssetType gameAssetType)
    {
        _languageManager = LanguageManager.Instance;
        _languageManager.OnLanguageChanged += OnLanguageChanged;
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

    private void OnLanguageChanged()
    {
        Type currentClassType = GetType();
        IEnumerable<string> languageProperties = LanguageManager.GetClassLanguagePropertyNames(currentClassType);
        foreach (string propertyName in languageProperties)
        {
            OnPropertyChanged(propertyName);
        }
    }
    public void Dispose()
    {
        _languageManager.OnLanguageChanged -= OnLanguageChanged;
    }
    ~MultipleDLLsFoundControlModel()
    {
        Dispose();
    }

    private readonly LanguageManager _languageManager;
}
