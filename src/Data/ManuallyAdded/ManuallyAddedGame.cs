using System;
using System.Diagnostics.Contracts;

namespace DLSS_Swapper.Data.CustomDirectory;

public class ManuallyAddedGame : Game
{
    public override string HeaderImage => String.Empty;

    public override string ID => "manual_X";

    public ManuallyAddedGame()
    {

    }

    public ManuallyAddedGame(String name, String path)
    {
        Title = name;
        InstallPath = path;
        
        DetectDLSS();
    }
}
