using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DLSS_Swapper.Data.BattleNet;

internal class Aggregate
{
    [JsonPropertyName("installed")]
    public List<AggregateItem> Installed { get; set; } = [];
}
