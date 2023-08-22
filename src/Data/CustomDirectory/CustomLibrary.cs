using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.Data.CustomDirectory;

public class CustomLibrary : IGameLibrary
{
    public GameLibrary GameLibrary => GameLibrary.CustomDirectories;
    public string Name => "Custom Directories";

    public List<Game> LoadedGames { get; } = new List<Game>();

    public List<Game> LoadedDLSSGames { get; } = new List<Game>();

    public async Task<List<Game>> ListGamesAsync()
    {
        var games = new List<Game>();
        var directories = Settings.Instance.Directories;

        var tasks = directories.Select(dir => Task.Run(() => GetGamesFromDirectory(dir)));

        foreach (var gameDirectory in await Task.WhenAll(tasks))
        {
            games.AddRange(gameDirectory);
        }

        games.Sort();
        LoadedGames.AddRange(games);
        LoadedDLSSGames.AddRange(games.Where(g => g.HasDLSS));

        return games;
    }
    
    private IEnumerable<Game> GetGamesFromDirectory(string directory)
    {
        var games = new List<Game>();

        if (!Directory.Exists(directory))
        {
            return games;
        }

        foreach (var gameDirectory in Directory.GetDirectories(directory))
        {
            games.Add(new CustomGame(Path.GetFileName(gameDirectory), gameDirectory));
        }

        return games;
    }

    public bool IsInstalled()
    {
        return Settings.Instance.Directories.Count > 0;
    }
}
