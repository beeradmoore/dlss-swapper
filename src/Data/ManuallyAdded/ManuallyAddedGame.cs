using System;
using System.Diagnostics.Contracts;
using DLSS_Swapper.Interfaces;
using SQLite;

namespace DLSS_Swapper.Data.CustomDirectory;

[Table("ManuallyAddedGame")]
public class ManuallyAddedGame : Game
{
    public override GameLibrary GameLibrary => GameLibrary.ManuallyAdded;

//    public override string HeaderImage => String.Empty;

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
    }
}
