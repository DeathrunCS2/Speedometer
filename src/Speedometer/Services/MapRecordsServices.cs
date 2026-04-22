using System.Collections.Generic;

namespace Speedometer.Services;

public static class MapRecordsServices
{
    private static readonly Dictionary<string, MapRecordHolder> MapRecordsCache = new();
    
    public static bool TryUpdateRecord(string mapName, ulong steamId64, string playerName, float speed)
    {
        if (string.IsNullOrWhiteSpace(mapName)) return false;

        var newRecord = new MapRecordHolder
        {
            SteamId64 = steamId64,
            Name = playerName,
            Speed = speed
        };

        // Case 1: Map not recorded yet → add
        if (!MapRecordsCache.TryGetValue(mapName, out var existingRecord))
        {
            MapRecordsCache[mapName] = newRecord;
            return true; // new record set
        }

        // Case 2: Replace only if better
        if (newRecord.Speed > existingRecord.Speed)
        {
            MapRecordsCache[mapName] = newRecord;
            return true; // record beaten
        }

        return false; // no update
    }

    public static MapRecordHolder? GetRecord(string mapName) => MapRecordsCache.TryGetValue(mapName, out var record) ? record : null;
    

    public static IReadOnlyDictionary<string, MapRecordHolder> GetAllRecords() => MapRecordsCache;
    
    public static void ClearCache() => MapRecordsCache.Clear();
}

public class MapRecordHolder
{
    public ulong SteamId64 { get; set; }
    public string Name { get; set; } = "";
    public float Speed { get; set; }
}