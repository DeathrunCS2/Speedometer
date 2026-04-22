using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Sharp.Shared;
using Speedometer.Interfaces.Managers;
using MySqlConnector;
using Dapper;

namespace Speedometer.Services;

internal class MapRecords(
    IModSharp modSharp) : IMapRecords
{
    public static MapRecordsConfig Config = null!;
    
    private static string ConnectionString { get; set; } = "";
    
    public bool Init()
    {
        LoadMapRecordsConfig();
        
        //build connection string
        BuildDbConnectionString();

        //create the necessary db tables
        SetupDatabaseTables();
        
        return true;
    }
    
    public void Shutdown()
    {
    }
    
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
        Config = config;
    }
    
    private static void CreateMapRecordsConfig(string configPath)
    {
        var config = new MapRecordsConfig ();
            
        File.WriteAllText(configPath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
    }
    
    public void ReloadMapRecordsConfig() { LoadMapRecordsConfig(); }

    public class MapRecordsConfig
    {
        public string Host { get; init; } = "localhost";
        public string Database { get; init; } = "database_name";
        public string User { get; init; } = "database_user";
        public string Password { get; init; } = "database_password";
        public int Port { get; init; } = 3306;
        public string TableName { get; init; } = "deathrun_maprecords";
    }

    #endregion
    
    #region ConnectionString

    private static void BuildDbConnectionString() 
    {
        //build connection string
        ConnectionString = new MySqlConnectionStringBuilder
        {
            Database = Config.Database,
            UserID = Config.User,
            Password = Config.Password,
            Server = Config.Host,
            Port = (uint)Config.Port,
        }.ConnectionString;
    }

    #endregion
    
    #region Tables

    private static void SetupDatabaseTables()
    {
        Task.Run(() => CreateDatabaseTable($@" CREATE TABLE IF NOT EXISTS `{Config.TableName}` 
                                               (
                                                   `id` BIGINT NOT NULL AUTO_INCREMENT,
                                                   `map` TEXT NOT NULL UNIQUE,
                                                   `record_holder_steamid64` BIGINT(255) NOT NULL,
                                                   `record_holder_name` TEXT DEFAULT NULL,
                                                   `record_holder_speed` TEXT NOT NULL,
     
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
}