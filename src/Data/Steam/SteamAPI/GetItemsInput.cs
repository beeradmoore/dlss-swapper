using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DLSS_Swapper.Data.Steam.SteamAPI;
internal class GetItemsInput
{
    [JsonPropertyName("ids")]
    public List<StoreItemId> Ids { get; set; } = new List<StoreItemId>();

    [JsonPropertyName("context")]
    public StoreBrowseContext Context { get; set; } = new StoreBrowseContext();

    [JsonPropertyName("data_request")]
    public StoreBrowseItemDataRequest DataRequest { get; set; } = new StoreBrowseItemDataRequest();
}
