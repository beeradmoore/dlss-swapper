using System.Threading.Tasks;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.Data.BattleNet;

internal class BattleNetGame : Game
{
    public override GameLibrary GameLibrary => GameLibrary.BattleNet;

    public bool StatePlayable { get; set; }

    public override bool IsReadyToPlay => StatePlayable;

    public BattleNetGame()
    {

    }

    public BattleNetGame(string gameId)
    {
        PlatformId = gameId;
        SetID();
    }

    protected override Task UpdateCacheImageAsync()
    {
        return Task.CompletedTask;
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
        }

        return didChange;
    }
}
