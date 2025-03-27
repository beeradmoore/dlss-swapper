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
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

namespace DLSS_Swapper.UserControls;

public partial class NewDLLsControlModel : ObservableObject, IDisposable
{
    public NewDLLsControlModel()
    {
        _languageManager = LanguageManager.Instance;
        _languageManager.OnLanguageChanged += OnLanguageChanged;
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
        package.SetText(Title);
        Clipboard.SetContent(package);
    }

    [RelayCommand]
    void CopyBody()
    {
        var package = new DataPackage();
        package.SetText(Body);
        Clipboard.SetContent(package);
    }

    #region LanguageProperties
    public string Title => $"{ResourceHelper.GetString("NewDllFoundOn")} {DateTime.Now.ToString("yyyy-MM-dd")}";
    public string WhileLoadingYourGameNewDllsDiscoveredHelpUsText => ResourceHelper.GetString("WhileLoadingYourGameNewDllsDiscoveredHelpUs");
    public string StepOneCreateNewIssueText => ResourceHelper.GetString("StepOneCreateNewIssue");
    public string CreateNewGithubIssueText => ResourceHelper.GetString("CreateNewGithubIssue");
    public string GithubAccountRequiredText => ResourceHelper.GetString("GithubAccountRequired");
    public string IfButtonDoesntWorkTryHereText => ResourceHelper.GetString("IfButtonDoesntWorkTryHere");
    public string StepTwoCopyTitleText => ResourceHelper.GetString("StepTwoCopyTitle");
    public string CopyText => ResourceHelper.GetString("Copy");
    public string StepThreeCopyBodyText => ResourceHelper.GetString("StepThreeCopyBody");
    public string StepFourSubmitYourIssueText => ResourceHelper.GetString("StepFourSubmitYourIssue");
    public string YouDoNotHaveToSubmitDllAutoTrackText => ResourceHelper.GetString("YouDoNotHaveToSubmitDllAutoTrack");
    public string ThanksForHelpingText => ResourceHelper.GetString("ThanksForHelping");
    #endregion

    private void OnLanguageChanged()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(WhileLoadingYourGameNewDllsDiscoveredHelpUsText));
        OnPropertyChanged(nameof(StepOneCreateNewIssueText));
        OnPropertyChanged(nameof(CreateNewGithubIssueText));
        OnPropertyChanged(nameof(GithubAccountRequiredText));
        OnPropertyChanged(nameof(IfButtonDoesntWorkTryHereText));
        OnPropertyChanged(nameof(StepTwoCopyTitleText));
        OnPropertyChanged(nameof(CopyText));
        OnPropertyChanged(nameof(StepThreeCopyBodyText));
        OnPropertyChanged(nameof(StepFourSubmitYourIssueText));
        OnPropertyChanged(nameof(YouDoNotHaveToSubmitDllAutoTrackText));
        OnPropertyChanged(nameof(ThanksForHelpingText));
    }

    public void Dispose()
    {
        _languageManager.OnLanguageChanged -= OnLanguageChanged;
    }

    ~NewDLLsControlModel()
    {
        Dispose();
    }

    private readonly LanguageManager _languageManager;
}
