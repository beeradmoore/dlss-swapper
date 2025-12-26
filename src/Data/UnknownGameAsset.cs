using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.Data;

internal class UnknownGameAsset
{
    public GameLibrary GameLibrary { get; init; }
    public string GameTitle { get; init; }
    public GameAsset GameAsset { get; init; }

    public UnknownGameAsset(GameLibrary gameLibrary, string gameTitle, GameAsset gameAsset)
    {
        GameLibrary = gameLibrary;
        GameTitle = gameTitle;
        GameAsset = gameAsset;
    }
}
