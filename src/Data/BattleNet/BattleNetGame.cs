using SQLite;
using System.Threading.Tasks;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.Data.BattleNet;

[Table("battlenet_game")]
internal class BattleNetGame : Game
{
    public override GameLibrary GameLibrary => GameLibrary.BattleNet;

    [Column("state_playable")]
    public bool StatePlayable { get; set; }

    public override bool IsReadyToPlay => StatePlayable;

    [Column("remote_cover_image")]
    public string RemoteCoverImage { get; set; } = string.Empty;


    [Column("launcher_id")]
    public string LauncherId { get; set; } = string.Empty;

    public BattleNetGame()
    {

    }

    public BattleNetGame(string gameId)
    {
        PlatformId = gameId;
        SetID();
    }

    protected override async Task UpdateCacheImageAsync()
    {
        if (string.IsNullOrWhiteSpace(RemoteCoverImage) == false)
        {
            var didDownload = await DownloadCoverAsync(RemoteCoverImage).ConfigureAwait(false);
            if (didDownload)
            {
                return;
            }
        }

        // Fallback to manual downloaded images.
        {
            var didDownload = await DownloadCoverAsync($"https://dlss-swapper-downloads.beeradmoore.com/images/covers/battlenet/{PlatformId}.webp").ConfigureAwait(false);
            if (didDownload == false)
            {
                Logger.Error($"Could not load image for {PlatformId}");
            }
        }
    }

    public override bool UpdateFromGame(Game game)
    {
        var didChange = ParentUpdateFromGame(game);

        if (game is BattleNetGame battleNetGame)
        {
            if (StatePlayable != battleNetGame.StatePlayable)
            {
                StatePlayable = battleNetGame.StatePlayable;
                didChange = true;
            }

            if (RemoteCoverImage != battleNetGame.RemoteCoverImage)
            {
                RemoteCoverImage = battleNetGame.RemoteCoverImage;
                didChange = true;
            }

            if (LauncherId != battleNetGame.LauncherId)
            {
                LauncherId = battleNetGame.LauncherId;
                didChange = true;
            }
        }

        return didChange;
    }
}
