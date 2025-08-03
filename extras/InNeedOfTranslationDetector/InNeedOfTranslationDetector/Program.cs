
using System.Diagnostics;
using System.Text.RegularExpressions;

var srcDirectory = string.Empty;

var dir = new DirectoryInfo(AppContext.BaseDirectory);
while (dir is not null)
{
    var newDirectoryToCheck = Path.Combine(dir.FullName, "src");
    if (Directory.Exists(newDirectoryToCheck))
    {
        srcDirectory = newDirectoryToCheck;
        break;
    }
    dir = dir.Parent; // Move up one directory
}

if (string.IsNullOrWhiteSpace(srcDirectory) == true)
{
    Console.WriteLine("Unable to find DLSS Swapper src directory.");
    return 1;
}

var results = new Dictionary<string, List<string>>();
var keyResults = new Dictionary<string, List<string>>();

var filesToIgnore = new List<string>()
{
    "Converters\\DLSSStateVisibilityConverter.cs",
};


#region Xaml checking
var allXamlFiles = Directory.GetFiles(srcDirectory, "*.xaml", SearchOption.AllDirectories).Where(file => file.Contains("\\obj\\") == false && file.Contains("\\bin\\") == false).ToList();

var xamlRegexes = new List<Regex>()
{
    new Regex(@"Text=""([^""{][^""]*)"""),
    new Regex(@"Content=""([^""{][^""]*)"""),
    new Regex(@"Header=""([^""{][^""]*)"""),
    new Regex(@"ToolTip=""([^""{][^""]*)"""),
    new Regex(@"Label=""([^""{][^""]*)"""),
};

var ignoredXamlMatches = new List<string>()
{
    "Text=\"DLSS\"",
    "Text=\" / \"",
    "Text=\"DLSS: \"",
    "Text=\"DLSS Frame Generation\"",
    "Text=\"DLSS Ray Reconstruction\"",
    "Text=\"FSR 3.1 (DirectX 12)\"",
    "Text=\"FSR 3.1 (Vulkan)\"",
    "Text=\"XeSS\"",
    "Text=\"XeSS Frame Generation\"",
    "Text=\"XeLL\"",
    "Text=\"&#xE8C8;\"",
    "Text=\"DLSS Preset\""
};

foreach (var xamlFile in allXamlFiles)
{
    var relativePath = Path.GetRelativePath(srcDirectory, xamlFile);

    if (filesToIgnore.Contains(relativePath))
    {
        continue;
    }

    var fileData = File.ReadAllText(xamlFile);
    foreach (var xamlRegex in xamlRegexes)
    {
        var matches = xamlRegex.Matches(fileData);
        if (matches is null || matches.Count == 0)
        {
            continue;
        }

        foreach (Match match in matches)
        {
            if (ignoredXamlMatches.Contains(match.Value))
            {
                continue;
            }

            if (results.ContainsKey(relativePath) == false)
            {
                results[relativePath] = new List<string>();
            }

            results[relativePath].Add(match.Value);
        }
    }
}

#endregion


#region C# checking

var allCSharpFiles = Directory.GetFiles(srcDirectory, "*.cs", SearchOption.AllDirectories).Where(file => file.Contains("\\obj\\") == false && file.Contains("\\bin\\") == false).ToList();

var csharpRegexes = new List<Regex>()
{
    //new Regex(@"Text = ""([^""{][^""]*)"""),
    //new Regex(@"Content = ""([^""{][^""]*)"""),
    new Regex(@"(\s*)=(\s*)""([^""{][^""]*)"""),
    new Regex(@"(\s*)=(\s*)\$""([^""{][^""]*)"""),
    //new Regex(@"(?<![=])\s*=\s*(?![=])\s*([$@]*)""(([^""\\]|\\.|"""")*)""", RegexOptions.Compiled),
    //new Regex("\"([^\"\\\\]*(\\\\.[^\"\\\\]*)*)\"\r\n")
};

var ignoredCSharpMatches = new List<string>()
{
    " = \"\\n\"",
    "= \"0\"",
    " = \"0\"",
    " = $\"\\\"",
    " = \"?\"",
    "=\"args\"",
    " = \"runas\"",
    " = $\"add \\\"",
    " = \"reg\"",
    " = \"cmd.exe\"",
    " = \"runas\"",
    " = \"LangResourceError\"",
    " = \"SELECT * FROM Win32_OperatingSystem\"",
    "= \"2.0\"",
    "= \"1\"",
    " = \"✅\"",
    " = \"❌\"",
    " = \"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/133.0.0.0 Safari/537.36 Edg/133.0.0.0\"",
    " = \"AppsUseLightTheme\"",
    " = \"LastUpdatedThemeId\"",
    " = \"CryptQueryObject\"",
    " = \"dlss-swapper-downloads.beeradmoore.com\"",
    " = \"00AAC56B-CD44-11d0-8CC2-00C04FC295EE\"",
    " = \"nvngx_dlss.dll\"",
    " = \"nvngx_dlssg.dll\"",
    " = \"nvngx_dlssd.dll\"",
    " = \"amd_fidelityfx_dx12.dll\"",
    " = \"amd_fidelityfx_vk.dll\"",
    "=\"dllRecords\"",
    "=\"gameAssetType\"",
    "=\"dllRecords\"",
    "=\"importedDllRecords\"",
    "=\"dllRecord\"",
    "=\"isImported\"",
    "=\"dllRecords\"",
    "=\"importedDllRecords\"",
    "=\"dllRecords\"",
    "=\"gameAssetType\"",
    "=\"records\"",
    "=\"manifestRecords\"",
    "=\"importedRecords\"",
    "=\"gameAsset\"",
    "=\"game\"",
    "= \"nvngx_dlss.dll\"",
    "= \"nvngx_dlssg.dll\"",
    "= \"nvngx_dlssd.dll\"",
    "= \"amd_fidelityfx_dx12.dll\"",
    "= \"amd_fidelityfx_vk.dll\"",
    "= \"libxess.dll\"",
    "= \"libxell.dll\"",
    "= \"libxess_fg.dll\"",
    "=\"zipArchive\"",
    "=\"dllRecord\"",
    "=\"Exception\"",
    " = \"libxess.dll\"",
    " = \"libxell.dll\"",
    " = \"libxess_fg.dll\"",
    " = \"FidelityFX_SDK\"",
    " = \"FidelityFX-SDK\"",
    " = \"SQLite_net\"",
    " = \"SQLite-net\"",
    " = \"notes.md\"",
    " = \"license.txt\"",
    " = \"\\xF0E2\"",
    " = \"\\xE8FD\"",
    " = \"MainNavigationView\"",
    " = \"AppTitleBar\"",
    " = \"DialogShowingStates\"",
    " = \"DialogShowing\"",
    " = \"DLSS_Swapper.Acknowledgements.\"",
    " = \"en-US\"",
    " = \"DieselGameBox\"",
    " = \"DieselGameBoxTall\"",
    " = \"LANG_HUNT\"",
    " = $\"gog_{gogGame.PlatformId}\"",
    " = $\"/C copy \\\"",
    " = \"Test 1\"",
    " = \"Test 2\"",
    " = \"Test 3\"",
    " = \"Test 4\"",
    " = \"Test 5\"",
    " = \"Test 6\"",
    " = \"Test 7\"",
    " = \"Test 8\"",
    " = \"Test 9\"",
    " = \"Test 10\"",
    " = \"Test 11\"",
    " = $\"v{_displayVersion}\"",
    " = \"dlss_swapper_export.zip\"",
    " = \"dlss_swapper_translation.json\"",
    " = \"dlss_swapper_published_translation.zip\"",
    " = $\"dlss_swapper_export_{dllRecord.DisplayName.Replace(\"",
    " = $\"export_{directoryInfo.Name}.zip\"",
    "=\"path\"",
    "=\"directory\"",
    "=\"settings\"",
    "=\"sender\"",
    "=\"e\"",
    "=\"dlssRecord\"",
    "= \"LANG_HUNT\"",
    "= \"FidelityFX_SDK\"",
    "= \"SQLite_net\"",
    "= \"notes.md\"",
    "= \"license.txt\"",
    " = $\"storageFile is null\"",
    " = \"PageTag_Settings\"",
    " = \"PageTag_Library\"",
    " = \"PageTag_Games\"",
    " = \"PageTag_Acknowledgements\"",
    "= \"DieselGameBoxTall\"",
    "= \"DieselGameBox\"",
    "= \"MainNavigationView\"",
    "= \"AppTitleBar\"",
    "= \"DialogShowing\"",
    "= \"DialogShowingStates\"",
    " = \"Value\"",
    "= \"en-US\"",
    " = \"General_None\"",
    " = $\"v{DisplayVersion} (v{Version})\"",
    " = $\"v{DisplayVersion} (v{dllVersion})\"",
    " = $\"v{DisplayVersion}\"",
    "=\"languageKey\""

};


var getStringRegexes = new List<Regex>()
{
    new Regex(@"ResourceHelper.GetString\(""(?<key>[^""]*)""\)", RegexOptions.Compiled),
    new Regex(@"ResourceHelper.GetFormattedResourceTemplate\(""(?<key>[^""]*)""", RegexOptions.Compiled),    
};

var ignoredStringKeyPrefixes = new List<string>()
{
    "General_",
    "SettingsPage_",
    "LibraryPage_",
    "TranslationToolsPage_",
    "NetworkTesterPage_",
    "MainWindow_",
    "FailedToLaunchPage_",
    "DiagnosticsPage_",
    "ApplicationTitle",
    "GamesPage_Title",
    "AcknowledgementsPage_",
    "DllRecord_",
    "Game_",
    "DllManager_",
    "GameLibrary_",
    "GamePage_",
    "GamesPage_",
    "GitHubUpdater_",
    "TranslationToolboxPage_",
    "GameHistoryEventType_",
    "GameHistoryControl_",
};

foreach (var csharpFiles in allCSharpFiles)
{
    var relativePath = Path.GetRelativePath(srcDirectory, csharpFiles);

    if (filesToIgnore.Contains(relativePath))
    {
        continue;
    }

    var fileData = File.ReadAllText(csharpFiles);
    foreach (var csharpRegex in csharpRegexes)
    {
        var matches = csharpRegex.Matches(fileData);
        if (matches is null || matches.Count == 0)
        {
            continue;
        }

        foreach (Match match in matches)
        {
            if (ignoredCSharpMatches.Contains(match.Value))
            {
                continue;
            }

            if (match.Value.StartsWith(" = \"https:"))
            {
                continue;
            }

            if (match.Value.StartsWith(" = $\"https:"))
            {
                continue;
            }

            if (match.Value.StartsWith("=\"));"))
            {
                continue;
            }

            if (results.ContainsKey(relativePath) == false)
            {
                results[relativePath] = new List<string>();
            }

            results[relativePath].Add(match.Value);
        }
    }

    foreach (var getStringRegex in getStringRegexes)
    {
        var matches = getStringRegex.Matches(fileData);
        if (matches is null || matches.Count == 0)
        {
            continue;
        }

        foreach (Match match in matches)
        {
            var key = match.Groups["key"].Value;
            var validKey = false;
            foreach (var ignoredStringKeyPrefix in ignoredStringKeyPrefixes)
            {
                if (key.StartsWith(ignoredStringKeyPrefix))
                {
                    validKey = true;
                    break;
                }
            }

            if (validKey == false)
            {
                if (keyResults.ContainsKey(relativePath) == false)
                {
                    keyResults[relativePath] = new List<string>();
                }

                keyResults[relativePath].Add(key);                
            };
        }
    }
}
#endregion

var xamlFilesCount = 0;
var xamlInstancesCount = 0;
var csharpFilesCount = 0;
var csharpInstancesCount = 0;
var unprefixedKeysCount = 0;

foreach ((var file, var matches) in results)
{
    Console.WriteLine($"File: {file}");
    var isXamlFile = false;
    var isCSharpFile = false;
    if (file.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
    {
        xamlFilesCount += matches.Count;
        isXamlFile = true;
    }
    else if (file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
    {
        csharpFilesCount += matches.Count;
        isCSharpFile = true;
    }

    Console.WriteLine("Matches:");
    foreach (var match in matches)
    {
        Console.WriteLine($"  {match}");
        if (isXamlFile)
        {
            ++xamlInstancesCount;
        }
        else if (isCSharpFile)
        {
            ++csharpInstancesCount;
        }
    }
    Console.WriteLine();
}

Console.WriteLine();
Console.WriteLine("Unprefixed keys:");
Console.WriteLine();

foreach ((var file, var matches) in keyResults)
{
    Console.WriteLine($"File: {file}");

    Console.WriteLine("Matches:");
    foreach (var match in matches)
    {
        ++unprefixedKeysCount;
        Console.WriteLine($"  {match}");
    }
    Console.WriteLine();
}


Console.WriteLine($"Found {xamlInstancesCount} xaml instances across {xamlFilesCount} files.");
Console.WriteLine($"Found {csharpInstancesCount} cs instances across {csharpFilesCount} files.");
Console.WriteLine($"Found {unprefixedKeysCount} unprefixed keys.");


return 0;