
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

#region Xaml checking
var allXamlFiles = Directory.GetFiles(srcDirectory, "*.xaml", SearchOption.AllDirectories).Where(file => file.Contains("\\obj\\") == false && file.Contains("\\bin\\") == false).ToList();

var xamlRegexes = new List<Regex>()
{
    new Regex(@"Text=""([^""{][^""]*)"""),
    new Regex(@"Content=""([^""{][^""]*)"""),
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
};

foreach (var xamlFile in allXamlFiles)
{
    var relativePath = Path.GetRelativePath(srcDirectory, xamlFile);
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
    new Regex(@"Text = ""([^""{][^""]*)"""),
    new Regex(@"Content = ""([^""{][^""]*)"""),
};

var ignoredCSharpMatches = new List<string>()
{
    "Text = \"\\n\"",
    "Text = \"0\"",
};

foreach (var csharpFiles in allCSharpFiles)
{
    var relativePath = Path.GetRelativePath(srcDirectory, csharpFiles);
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

            if (results.ContainsKey(relativePath) == false)
            {
                results[relativePath] = new List<string>();
            }

            results[relativePath].Add(match.Value);
        }
    }
}
#endregion

foreach ((var file, var matches) in results)
{
    Console.WriteLine($"File: {file}");
    Console.WriteLine("Matches:");
    foreach (var match in matches)
    {
        Console.WriteLine($"  {match}");
    }
    Console.WriteLine();
}



return 0;