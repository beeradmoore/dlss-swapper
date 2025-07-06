using System;
using DLSS_Swapper.Data;
using DLSS_Swapper.Data.BattleNet;
using DLSS_Swapper.Data.EpicGamesStore;
using DLSS_Swapper.Data.GOG;
using DLSS_Swapper.Data.Steam;
using DLSS_Swapper.Data.UbisoftConnect;
using DLSS_Swapper.Data.Xbox;
using DLSS_Swapper.Data.ManuallyAdded;
using Nito.AsyncEx;
using SQLite;

namespace DLSS_Swapper;

internal class Database
{
    static Database? _instance;
    internal static Database Instance => _instance ??= new Database();

    internal AsyncLock Mutex { get; init; }
    internal SQLiteAsyncConnection Connection { get; init; }

    public Database()
    {
        // Use a single syncronous connection to make tables
        using (var syncConnection = new SQLiteConnection(Storage.GetDBPath()))
        {
            try
            {
                syncConnection.CreateTable<GameHistory>();
            }
            catch (Exception err)
            {
                Logger.Error(err);
            }

            try
            {
                syncConnection.CreateTable<SteamGame>();
            }
            catch (Exception err)
            {
                Logger.Error(err);
            }


            try
            {
                syncConnection.CreateTable<GOGGame>();
            }
            catch (Exception err)
            {
                Logger.Error(err);
            }


            try
            {
                syncConnection.CreateTable<EpicGamesStoreGame>();
            }
            catch (Exception err)
            {
                Logger.Error(err);
            }


            try
            {
                syncConnection.CreateTable<UbisoftConnectGame>();
            }
            catch (Exception err)
            {
                Logger.Error(err);
            }


            try
            {
                syncConnection.CreateTable<XboxGame>();
            }
            catch (Exception err)
            {
                Logger.Error(err);
            }


            try
            {
                syncConnection.CreateTable<ManuallyAddedGame>();
            }
            catch (Exception err)
            {
                Logger.Error(err);
            }

            try
            {
                syncConnection.CreateTable<BattleNetGame>();
            }
            catch (Exception err)
            {
                Logger.Error(err);
            }

            try
            {
                syncConnection.CreateTable<GameAsset>();
            }
            catch (Exception err)
            {
                Logger.Error(err);
            }

            syncConnection.Close();
        }

        Mutex = new AsyncLock();
        Connection = new SQLiteAsyncConnection(Storage.GetDBPath());
    }

    public void Init()
    {
        // If we didn't get this the database was not created as the above threw an exception
        Logger.Verbose("Database Init");
    }

}
