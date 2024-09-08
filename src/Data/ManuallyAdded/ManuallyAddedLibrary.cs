﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DLSS_Swapper.Interfaces;

namespace DLSS_Swapper.Data.CustomDirectory;

public class ManuallyAddedLibrary : IGameLibrary
{
    public GameLibrary GameLibrary => GameLibrary.ManuallyAdded;
    public string Name => "Manually Added";

    public List<Game> LoadedGames { get; } = new List<Game>();

    public List<Game> LoadedDLSSGames { get; } = new List<Game>();

    public Type GameType => typeof(ManuallyAddedGame);


    static ManuallyAddedLibrary instance = null;
    public static ManuallyAddedLibrary Instance => instance ??= new ManuallyAddedLibrary();

    private ManuallyAddedLibrary()
    {

    }

    public async Task<List<Game>> ListGamesAsync()
    {
        LoadedGames.Clear();
        LoadedDLSSGames.Clear();

        var games = new List<Game>();

        var dbGames = await App.CurrentApp.Database.QueryAsync<ManuallyAddedGame>("SELECT * FROM ManuallyAddedGame");
        foreach (var dbGame in dbGames)
        {
            dbGame.ProcessGame();
            games.Add(dbGame);
        }

        games.Sort();
        LoadedGames.AddRange(games);
        LoadedDLSSGames.AddRange(games.Where(g => g.HasDLSS));
        
        return games;
    }

    public bool IsInstalled()
    {
        return true;
    }

    public async Task<List<Game>> LoadFromCacheAsync()
    {
        try
        {
            var games = await App.CurrentApp.Database.Table<ManuallyAddedGame>().ToListAsync();
            return games.ToList<Game>();
        }
        catch (Exception err)
        {
            Logger.Error(err.Message);
        }
        return new List<Game>();
    }

    public async Task LoadGamesAsync()
    {
        await Task.Delay(1);
    }

    public async Task LoadGamesFromCacheAsync()
    {
        try
        {
            var games = await App.CurrentApp.Database.Table<ManuallyAddedGame>().ToArrayAsync();
            foreach (var game in games)
            {
                GameManager.Instance.AddGame(game);
            }
        }
        catch (Exception err)
        {
            Logger.Error(err.Message);
        }
    }
}