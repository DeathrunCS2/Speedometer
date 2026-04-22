using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Speedometer.Interfaces;
using Speedometer.Managers;

namespace Speedometer.Services;

internal class MapRecordsServices() : IBaseInterface
{
    public bool Init() => true;
    
    private static readonly ConcurrentDictionary<string, MapRecordHolder> MapRecordsCache = new(StringComparer.OrdinalIgnoreCase);

    public static bool TryUpdateRecord(string mapName, ulong steamId64, string playerName, float speed)
    {
        if (string.IsNullOrWhiteSpace(mapName)) return false;

        var newRecord = new MapRecordHolder
        {
            SteamId64 = steamId64,
            Name = playerName,
            Speed = speed
        };

        while (true)
        {
            // Case 1: Try to add if missing
            if (MapRecordsCache.TryAdd(mapName, newRecord)) return true;

            // Case 2: Already exists → get current
            if (MapRecordsCache.TryGetValue(mapName, out var existing) is not true) continue; // rare race, retry

            // Not better → stop
            if (speed <= existing.Speed) return false;

            // Try to replace it only if unchanged
            if (MapRecordsCache.TryUpdate(mapName, newRecord, existing)) return true;
        }
    }
    
    public static MapRecordHolder? GetRecord(string mapName) => MapRecordsCache.GetValueOrDefault(mapName);
    
    public static IReadOnlyDictionary<string, MapRecordHolder> GetAllRecords() => MapRecordsCache;
    
    public static void ClearCache() => MapRecordsCache.Clear();
}

public class MapRecordHolder
{
    public ulong SteamId64 { get; set; } = 0;
    public string Name { get; set; } = "";
    public float Speed { get; set; } = 0;
}