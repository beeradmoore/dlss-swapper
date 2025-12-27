using System.Threading.Tasks;
using DLSS_Swapper.Interfaces;
using SQLite;

namespace DLSS_Swapper.Data.UbisoftConnect;

[Table("ubisoft_connect_game")]
internal class UbisoftConnectGame : Game
{
    public override GameLibrary GameLibrary => GameLibrary.UbisoftConnect;

    public override bool IsReadyToPlay => true;

    [Column("remote_header_image")]
    public string RemoteHeaderImage { get; set; } = string.Empty;

    public UbisoftConnectGame()
    {

    }

    public UbisoftConnectGame(string installId)
    {
        PlatformId = installId;
        SetID();
    }

    protected override async Task UpdateCacheImageAsync()
    {
        await DownloadCoverAsync(RemoteHeaderImage).ConfigureAwait(false);
    }

    public override bool UpdateFromGame(Game game)
    {
        var didChange = ParentUpdateFromGame(game);

        if (game is UbisoftConnectGame ubisoftConnectGame)
        {
            //_localHeaderImages = xboxGame._localHeaderImages;
        }

        return didChange;
    }
}
