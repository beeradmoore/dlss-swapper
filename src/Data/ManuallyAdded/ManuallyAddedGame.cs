using System;
using System.Diagnostics.Contracts;
using DLSS_Swapper.Interfaces;
using SQLite;

namespace DLSS_Swapper.Data.CustomDirectory;

[Table("ManuallyAddedGame")]
public class ManuallyAddedGame : Game
{
    public override GameLibrary GameLibrary => GameLibrary.ManuallyAdded;

//    public override string HeaderImage => string.Empty;

    public ManuallyAddedGame()
    {

    }
    public ManuallyAddedGame(string id)
    {
        PlatformId = id;
        SetID();
    }

    public void ImportCoverImage(string imagePath)
    {
        ResizeCover(imagePath);
    }

    protected override void UpdateCacheImage()
    {
        // NOOP, the image is manually managed by the user.
        CoverImage = string.Empty;
    }

    public override bool UpdateFromGame(Game game)
    {
        var didChange = ParentUpdateFromGame(game);

        //Debugger.Break();

        if (game is ManuallyAddedGame manuallyAddedGame)
        {
            //_localHeaderImages = xboxGame._localHeaderImages;
        }

        return didChange;
    }
}
