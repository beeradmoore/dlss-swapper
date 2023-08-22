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

        var tasks = directories.Select(directory => GetGamesFromDirectoryAsync(directory, games));

        await Task.WhenAll(tasks);

        games.Sort();
        LoadedGames.AddRange(games);
        LoadedDLSSGames.AddRange(games.Where(g => g.HasDLSS));

        return games;
    }

    private static async Task GetGamesFromDirectoryAsync(string directory, ICollection<Game> games)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        var tasks = new List<Task>();

        foreach (var dir in Directory.GetDirectories(directory))
        {
            tasks.Add(Task.Run(() => games.Add(new CustomGame(Path.GetFileName(dir), dir))));
        }

        await Task.WhenAll(tasks);
    }

    public bool IsInstalled()
    {
        return Settings.Instance.Directories.Count > 0;
    }
}
