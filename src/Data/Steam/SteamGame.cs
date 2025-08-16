using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using CommunityToolkit.Mvvm.ComponentModel;
using DLSS_Swapper.Data.Steam.SteamAPI;
using DLSS_Swapper.Interfaces;
using SQLite;

namespace DLSS_Swapper.Data.Steam;

[Table("SteamGame")]
internal partial class SteamGame : Game
{
    public override GameLibrary GameLibrary => GameLibrary.Steam;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReadyToPlay))]
    [Column("state_flags")]
    public partial SteamStateFlag StateFlags { get; set; }

    public override bool IsReadyToPlay
    {
        get
        {
            const SteamStateFlag allowedFlags = SteamStateFlag.StateFullyInstalled | SteamStateFlag.StateAppRunning;
            return StateFlags != 0 && (StateFlags & ~allowedFlags) == 0;
        }
    }

    public SteamGame()
    {

    }

    public SteamGame(string appId)
    {
        PlatformId = appId;
        SetID();
    }

    protected override async Task UpdateCacheImageAsync()
    {
        // Try get image from the local disk first.
        var localHeaderImagePath = Path.Combine(SteamLibrary.GetInstallPath(), "appcache", "librarycache", $"{PlatformId}_library_600x900.jpg");
        if (File.Exists(localHeaderImagePath))
        {
            using (var fileStream = File.Open(localHeaderImagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                await ResizeCoverAsync(fileStream).ConfigureAwait(false);
            }
            return;
        }

        // Special case for Steamworks redistributable. 
        if (PlatformId == "228980")
        {
            await DownloadCoverAsync($"https://steamcdn-a.akamaihd.net/steam/apps/{PlatformId}/header.jpg").ConfigureAwait(false);
            return;            
        }

        // If it doesn't exist, load from web.
        var didDownload = await DownloadCoverAsync($"https://steamcdn-a.akamaihd.net/steam/apps/{PlatformId}/library_600x900_2x.jpg").ConfigureAwait(false);

        // If we couldn't download the cover try getting it from the IStoreBrowseService.
        if (didDownload == false)
        {
            try
            {
                var getItemsInput = new GetItemsInput();
                getItemsInput.Ids.Add(new StoreItemId() { AppId = Int32.Parse(PlatformId, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture) });
                getItemsInput.DataRequest.IncludeAssets = true;

                var jsonPayload = JsonSerializer.Serialize(getItemsInput);
                var payloadUrlEncoded = HttpUtility.UrlEncode(jsonPayload);

                using (var steamApiResponse = await App.CurrentApp.HttpClient.GetAsync($"https://api.steampowered.com/IStoreBrowseService/GetItems/v1/?input_json={payloadUrlEncoded}", System.Net.Http.HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                {
                    if (steamApiResponse.IsSuccessStatusCode == false)
                    {
                        Logger.Error($"Failed to load Steam cover for {PlatformId} from IStoreBrowseService. Status code: {steamApiResponse.StatusCode}");
                        return;
                    }

                    using (var responseStream = await steamApiResponse.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    {
                        var response = JsonSerializer.Deserialize<SteamAPIResponse<GetItemsResponse>>(responseStream, SourceGenerationContext.Default.SteamAPIResponseGetItemsResponse);
                        if (response?.Response?.StoreItems.Any() == true)
                        {
                            // We are only doing one search, so we likely only care for the first item.
                            var storeItem = response.Response.StoreItems[0];

                            if (storeItem.Assets is null)
                            {
                                Logger.Error($"No Assets found for {PlatformId} in the response from IStoreBrowseService.");
                                return;
                            }

                            if (string.IsNullOrWhiteSpace(storeItem.Assets.AssetUrlFormat))
                            {
                                Logger.Error($"No AssetUrlFormat found for {PlatformId} in the response from IStoreBrowseService.");
                                return;
                            }

                            // We are only checking LibraryCapsule2x, hopefully it exists for all games
                            if (string.IsNullOrWhiteSpace(storeItem.Assets.LibraryCapsule2x) == false)
                            {
                                // There are 3 different CDNs, I don't lknow what one they will use, so lets try all of them?
                                var cdns = new[]
                                {
                                    "https://shared.fastly.steamstatic.com",
                                    "https://shared.steamstatic.com",
                                    "https://shared.akamai.steamstatic.com"
                                };

                                foreach (var cdn in cdns)
                                {
                                    var coverUrl = $"{cdn}/store_item_assets/{storeItem.Assets.AssetUrlFormat.Replace("${FILENAME}", storeItem.Assets.LibraryCapsule2x)}";
                                    var didDownloadCover = await DownloadCoverAsync(coverUrl).ConfigureAwait(false);
                                    if (didDownloadCover)
                                    {
                                        return;
                                    }
                                    Logger.Error($"Could not download cover \"{storeItem.Assets.LibraryCapsule2x}\" with CDN {cdn} so trying next.");
                                }
                            }
                        }
                        else
                        {
                            Logger.Error($"No store items found for {PlatformId} in the response from IStoreBrowseService.");
                        }
                    }
                }

                Logger.Error($"Tried all known methods to get Steam cover for {PlatformId}, but all had failed.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to load Steam cover for {PlatformId} from IStoreBrowseService.");
            }
        }
    }

    public override bool UpdateFromGame(Game game)
    {
        var didChange = ParentUpdateFromGame(game);

        if (game is SteamGame steamGame)
        {
            if (StateFlags != steamGame.StateFlags)
            {
                StateFlags = steamGame.StateFlags;
                didChange = true;
            }
        }

        return didChange;
    }
}
