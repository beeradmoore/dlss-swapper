using System;
using SQLite;

namespace DLSS_Swapper.Data;

public enum GameHistoryEventType
{
    Unknown,
    DLLSwapped,
    DLLReset,
    DLLDetected,
    DLLChangedExternally,
    DLLBackupRemoved,
}

public class GameHistory
{
    [Indexed]
    [Column("game_id")]
    public string GameId { get; set; } = string.Empty;

    [Column("event_type")]
    public GameHistoryEventType EventType { get; set; } = GameHistoryEventType.Unknown;

    [Column("asset_type")]
    public GameAssetType? AssetType { get; set; }

    [Column("asset_path")]
    public string? AssetPath { get; set; }

    [Ignore]
    public string AssetTypeName
    {
        get
        {
            if (AssetType is null)
            {
                return string.Empty;
            }

            return DLLManager.Instance.GetAssetTypeName(AssetType.Value);
        }
    }

    [Column("event_time")]
    public DateTime EventTime { get; set; } = DateTime.MinValue;

    [Column("asset_version")]
    public string? AssetVersion { get; set; }
}
