using System;

namespace DLSS_Swapper.Data.CustomDirectory;

public class CustomGame : Game
{
    public override string HeaderImage => String.Empty;

    public CustomGame(String name, String path)
    {
        Title = name;
        InstallPath = path;
        
        DetectDLSS();
    }
}
