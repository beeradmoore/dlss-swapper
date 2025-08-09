using System.Collections.Generic;
using System.Text.Json.Serialization;
using DLSS_Swapper.Data.Steam.SteamAPI;
using Microsoft.UI.Windowing;

namespace DLSS_Swapper;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Data.GitHub.GitHubRelease))]
[JsonSerializable(typeof(Data.EpicGamesStore.CacheItem[]))]
[JsonSerializable(typeof(Data.EpicGamesStore.ManifestFile))]
[JsonSerializable(typeof(Data.GOG.LimitedDetail.LimitedDetailImages))]
[JsonSerializable(typeof(Data.GOG.GamePiece.GamePieceOriginalImages))]
[JsonSerializable(typeof(Data.GOG.ResourceImages))]
[JsonSerializable(typeof(Data.GOG.GOGEmbedFilteredResponse))]
[JsonSerializable(typeof(Data.Manifest))]
[JsonSerializable(typeof(Data.DLLRecord))]
[JsonSerializable(typeof(Settings))]
[JsonSerializable(typeof(Data.WindowPositionRect))]
[JsonSerializable(typeof(OverlappedPresenterState))]
[JsonSerializable(typeof(Data.HashedKnownDLL))]
[JsonSerializable(typeof(Data.GameLibrarySettings))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(Data.Steam.SteamAPI.SteamAPIResponse<GetItemsResponse>))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}
