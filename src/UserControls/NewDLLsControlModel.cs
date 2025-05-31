using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLSS_Swapper.Data;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;
using DLSS_Swapper.Translations.UserControls;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

namespace DLSS_Swapper.UserControls;

public partial class NewDLLsControlModel : ObservableObject
{
    public NewDLLsControlModel() : base()
    {
        var unknownGameAssets = GameManager.Instance.GetUnknownGameAssets();
        var gameAssetsLibraryGroup = new Dictionary<GameLibrary, Dictionary<string, List<UnknownGameAsset>>>();
        foreach (var unknownGameAsset in unknownGameAssets)
        {
            if (gameAssetsLibraryGroup.ContainsKey(unknownGameAsset.GameLibrary) == false)
            {
                gameAssetsLibraryGroup[unknownGameAsset.GameLibrary] = new Dictionary<string, List<UnknownGameAsset>>();
            }

            if (gameAssetsLibraryGroup[unknownGameAsset.GameLibrary].ContainsKey(unknownGameAsset.GameTitle) == false)
            {
                gameAssetsLibraryGroup[unknownGameAsset.GameLibrary][unknownGameAsset.GameTitle] = new List<UnknownGameAsset>();
            }

            gameAssetsLibraryGroup[unknownGameAsset.GameLibrary][unknownGameAsset.GameTitle].Add(unknownGameAsset);
        }

        var stringBuilder = new StringBuilder();
        foreach (var gameLibrayKeyPair in gameAssetsLibraryGroup)
        {
            stringBuilder.AppendLine($"{ResourceHelper.GetString("Library")}: {gameLibrayKeyPair.Key}");

            var libraryDicionary = gameLibrayKeyPair.Value as Dictionary<string, List<UnknownGameAsset>>;
            foreach (var gameAssetsDictionary in libraryDicionary)
            {
                stringBuilder.AppendLine($"- {ResourceHelper.GetString("Game")}: {gameAssetsDictionary.Key}");
                foreach (var unknownGameAsset in gameAssetsDictionary.Value)
                {
                    stringBuilder.AppendLine($"-- {Path.GetFileName(unknownGameAsset.GameAsset.Path)}, {ResourceHelper.GetString("Version")}: {unknownGameAsset.GameAsset.Version}, {ResourceHelper.GetString("Hash")}: {unknownGameAsset.GameAsset.Hash}");
                }
                stringBuilder.AppendLine();
            }
            stringBuilder.AppendLine();
        }
        Body = stringBuilder.ToString();
    }

    public NewDLLsTranslationPropertiesViewModel TranslationProperties { get; } = new NewDLLsTranslationPropertiesViewModel();

    public string Body { get; init; }

    [RelayCommand]
    async Task OpenGitHubIssueAsync()
    {
        var url = "https://github.com/beeradmoore/dlss-swapper-manifest-builder/issues/new?template=new_dlls_discovered.yml";
        await Launcher.LaunchUriAsync(new Uri(url));
    }

    [RelayCommand]
    void CopyTitle()
    {
        var package = new DataPackage();
        package.SetText(TranslationProperties.Title);
        Clipboard.SetContent(package);
    }

    [RelayCommand]
    void CopyBody()
    {
        var package = new DataPackage();
        package.SetText(Body);
        Clipboard.SetContent(package);
    }
}
