using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using DeathrunManager.Shared;
using DeathrunManager.Shared.DeathrunObjects;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Listeners;
using Speedometer.Extensions;
using Speedometer.Interfaces.Managers;
using Speedometer.Services;

namespace Speedometer.Managers;

internal class MapRecordsManager(
    IModSharp modSharp,
    IDeathrunManager deathrunManagerApi) : IManager, IGameListener
{
    private static MapRecordsConfig _config = null!;
    
    private static string ConnectionString { get; set; } = "";
    
    public static MapRecordHolder? CurrentMapRecordHolder = new();
    
    public bool Init()
    {
        LoadMapRecordsConfig();
        
        //build connection string
        BuildDbConnectionString();

        //create the necessary db tables
        SetupDatabaseTables();

        modSharp.InstallGameListener(this);
        
        deathrunManagerApi.Managers.GameplayManager.MapStarted += OnDeathrunMapStarted;
        deathrunManagerApi.Managers.GameplayManager.GameStarted += OnDeathrunGameStarted;
        deathrunManagerApi.Managers.GameplayManager.MapEnded += OnDeathrunMapEnded;
        deathrunManagerApi.Managers.PlayersManager.SendChatMessage += OnDeathrunPlayerSendChatMessage;

        return true;
    }
    
    public void Shutdown()
    {
        modSharp.RemoveGameListener(this);
        
        deathrunManagerApi.Managers.GameplayManager.MapStarted -= OnDeathrunMapStarted;
        deathrunManagerApi.Managers.GameplayManager.GameStarted -= OnDeathrunGameStarted;
        deathrunManagerApi.Managers.GameplayManager.MapEnded -= OnDeathrunMapEnded;
        deathrunManagerApi.Managers.PlayersManager.SendChatMessage -= OnDeathrunPlayerSendChatMessage;

        MapRecordsServices.ClearCache();
    }
    
    #region Listeners

    public void OnResourcePrecache()
    {
        modSharp.PrecacheResource("particles/digits_x/digits_x.vpcf");
    }

    #endregion
    
    #region Api listeners
    
    private static void OnDeathrunGameStarted(string mapName)
    {
        //get the map records from the database
        Task.Run(async () => await GetMapRecordsFromDatabaseAsync());
    }
    
    private static void OnDeathrunMapStarted(string mapName)
    {
        //skip precaching if the map is not a deathrun map
        if (mapName.Contains("dr_") is not true) return;
        
        Task.Run(async () =>
        {
            var mapRecord = await GetMapRecordFromDatabaseAsync(mapName);
            CurrentMapRecordHolder = mapRecord is null ? new MapRecordHolder() : MapRecordsServices.GetRecord(mapName);
            
            if (CurrentMapRecordHolder is null) return;
            
            await CacheMapRecordHolderIntoDatabaseAsync(mapName, 
                                                        CurrentMapRecordHolder.SteamId64, 
                                                        CurrentMapRecordHolder.Name, 
                                                        CurrentMapRecordHolder.Speed);
        });
        
    }
    
    private static void OnDeathrunMapEnded(string mapName)
    {
        //skip precaching if the map is not a deathrun map
        if (mapName.Contains("dr_") is not true) return;
        
        Task.Run(async () =>
        {
            if (CurrentMapRecordHolder is null || CurrentMapRecordHolder.Speed is 0) return;
            
            await CacheMapRecordHolderIntoDatabaseAsync(mapName, 
                                                        CurrentMapRecordHolder.SteamId64, 
                                                        CurrentMapRecordHolder.Name, 
                                                        CurrentMapRecordHolder.Speed);
        });
    }
    
    private void OnDeathrunPlayerSendChatMessage(IDeathrunPlayer deathrunPlayer, PlayerSendChatMessageEventArgs args)
    {
        if (deathrunPlayer.Client.SteamId != CurrentMapRecordHolder?.SteamId64) return;

        if (deathrunManagerApi.Managers.AdminManager.GetAdmin(deathrunPlayer.Client.SteamId) is null)
        {
            var message = "";
            //we are trying to send a message with the admin prefix
            if (args.Message.GetAfter("]").Contains(deathrunPlayer.Client.Name))
            {
                message = " " + "{GOLD}[Record Holder]{DEFAULT}" + args.Message.GetAfter("]");
            }
            else
            {
                message = args.Message;
            }
        
            //pass the reconstructed message to the chat
            args.Message = message.Replace($"{deathrunPlayer.Client.Name}{{DEFAULT}}:", $"{{PURPLE}}{deathrunPlayer.Client.Name}{{DEFAULT}}:{{DEFAULT}}");
        }
    }
    
    #endregion
    
    #region Async methods

    private static async Task GetMapRecordsFromDatabaseAsync()
    {
        try
        {
            await using var connection = new MySqlConnection(ConnectionString);
            await connection.OpenAsync();

            var mapsRecords = await connection.QueryAsync<(string, ulong, string, float)>(
                $"SELECT map, record_holder_steamid64, record_holder_name, record_holder_speed FROM `{_config.TableName}`"

            );

            foreach (var mapsRecord in mapsRecords)
            {
                MapRecordsServices.TryUpdateRecord(mapName: mapsRecord.Item1,
                                                   steamId64: mapsRecord.Item2,
                                                   playerName: mapsRecord.Item3,
                                                   speed: mapsRecord.Item4);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    
    private static async Task<MapRecordHolder?> GetMapRecordFromDatabaseAsync(string mapName)
    {
        try
        {
            await using var connection = new MySqlConnection(ConnectionString);
            await connection.OpenAsync();

            var mapRecord = await connection.QueryFirstOrDefaultAsync<MapRecordHolder>(
                $"SELECT record_holder_steamid64, record_holder_name, record_holder_speed FROM `{_config.TableName}` WHERE map = '{mapName}'"

            );
            
            return mapRecord;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }  
        
        return null;
    }

    private static async Task CacheMapRecordHolderIntoDatabaseAsync(string mapName, ulong steamId64, string playerName, float speed)
    {
        try
        {
            await using var connection = new MySqlConnection(ConnectionString);
            await connection.OpenAsync();
            
            var cacheMapQuery 
                = $@" INSERT INTO `{_config.TableName}` 
                      ( map,
                       record_holder_steamid64,
                       record_holder_name,
                       record_holder_speed )  
                      VALUES 
                      ( @CurrentMap, @SteamId64, @PlayerName, @Speed ) 
                      ON DUPLICATE KEY UPDATE 
                                       record_holder_steamid64  = {steamId64},
                                       record_holder_name       = '{playerName}',
                                       record_holder_speed      = {speed}
                                       
                    ";
    
            await connection.ExecuteAsync(cacheMapQuery,
                new { CurrentMap = mapName, SteamId64 = steamId64, PlayerName = playerName, Speed = speed });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    #endregion
    
    #region Config
    
    private void LoadMapRecordsConfig()
    {
        var sharpPath = Path.Combine(modSharp.GetGamePath(), "../sharp");
        var configPathConstruct = Path.GetFullPath(Path.Combine(sharpPath, "configs"));
        
        if (!Directory.Exists(configPathConstruct + "/Deathrun.Manager/modules/Speedometer")) 
            Directory.CreateDirectory(configPathConstruct + "/Deathrun.Manager/modules/Speedometer");
        
        var configPath = Path.Combine(configPathConstruct, "Deathrun.Manager/modules/Speedometer/map_records.json");
        if (!File.Exists(configPath)) CreateMapRecordsConfig(configPath);

        var config = JsonSerializer.Deserialize<MapRecordsConfig>(File.ReadAllText(configPath))!;
        _config = config;
    }
    
    private static void CreateMapRecordsConfig(string configPath)
    {
        var config = new MapRecordsConfig ();
            
        File.WriteAllText(configPath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
    }
    
    public void ReloadMapRecordsConfig() { LoadMapRecordsConfig(); }

    #endregion
    
    #region ConnectionString

    private static void BuildDbConnectionString() 
    {
        //build connection string
        ConnectionString = new MySqlConnectionStringBuilder
        {
            Database = _config.Database,
            UserID = _config.User,
            Password = _config.Password,
            Server = _config.Host,
            Port = (uint)_config.Port,
        }.ConnectionString;
    }

    #endregion
    
    #region Tables

    private static void SetupDatabaseTables()
    {
        Task.Run(() => CreateDatabaseTable($@" CREATE TABLE IF NOT EXISTS `{_config.TableName}` 
                                               (
                                                   `id` BIGINT NOT NULL AUTO_INCREMENT,
                                                   `map` TEXT NOT NULL UNIQUE,
                                                   `record_holder_steamid64` NUMERIC(50, 0) DEFAULT 0,
                                                   `record_holder_name` TEXT DEFAULT NULL,
                                                   `record_holder_speed` FLOAT DEFAULT 0,
     
                                                   PRIMARY KEY (id)
                                               )"));
    }
    
    private static async Task CreateDatabaseTable(string databaseTableStringStructure)
    {
        try
        {
            await using var dbConnection = new MySqlConnection(ConnectionString);
            dbConnection.Open();
            
            await dbConnection.ExecuteAsync(databaseTableStringStructure);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    
    #endregion

    int IGameListener.ListenerVersion => IGameListener.ApiVersion;
    int IGameListener.ListenerPriority => 0;
}

public class MapRecordsConfig
{
    public string Host { get; init; } = "localhost";
    public string Database { get; init; } = "database_name";
    public string User { get; init; } = "database_user";
    public string Password { get; init; } = "database_password";
    public int Port { get; init; } = 3306;
    public string TableName { get; init; } = "deathrun_maprecords";
}