using System;
using System.Globalization;
using DeathrunManager.Shared;
using DeathrunManager.Shared.DeathrunObjects;
using Speedometer.Interfaces.Managers;

namespace Speedometer.Managers;

public class SpeedometerManager(
    IDeathrunManager deathrunManagerApi) : IManager
{
    public bool Init()
    {        
        deathrunManagerApi.Managers.PlayersManager.ThinkPost += OnDeathrunPlayerThinkPost;

        return true;
    }

    public void Shutdown()
    {
        deathrunManagerApi.Managers.PlayersManager.ThinkPost -= OnDeathrunPlayerThinkPost;
    }
    
    private static void OnDeathrunPlayerThinkPost(IDeathrunPlayer deathrunPlayer)
    {
        float speedNum = 0;
        if (deathrunPlayer.PlayerPawn?.IsAlive is true)
        {
            speedNum = deathrunPlayer.PlayerPawn?.GetAbsVelocity().Length() ?? 999;
            
            if (speedNum > MapRecordsManager.CurrentMapRecordHolder?.Speed)
            {
                MapRecordsManager.CurrentMapRecordHolder.SteamId64 = deathrunPlayer.Client.SteamId;
                MapRecordsManager.CurrentMapRecordHolder.Name = deathrunPlayer.Client.Name;
                MapRecordsManager.CurrentMapRecordHolder.Speed = speedNum;
            }
        }
        else
        {
            var observedDeathrunPlayer = deathrunPlayer.ObservedDeathrunPlayer;
            if (observedDeathrunPlayer is null) return;
                
            speedNum = observedDeathrunPlayer.PlayerPawn?.GetAbsVelocity().Length() ?? 777;
        }
        
        deathrunPlayer.SetCenterMenuTopRowHtml
        (
            $"<font class='fontSize-s stratum-font fontWeight-Bold' color='#A7A7A7'>SPEED: </font>"
            + $"<font class='fontSize-sm stratum-font fontWeight-Bold' color='magenta'>{speedNum.ToString("F", CultureInfo.InvariantCulture)}</font>"
            + $"<font class='fontSize-sm stratum-font fontWeight-Bold' color='#A7A7A7'> | </font>"
            + $"<font class='fontSize-s stratum-font fontWeight-Bold' color='#A7A7A7'>RECORD: </font>"
            + $"<font class='fontSize-sm stratum-font fontWeight-Bold' color='gold'>{MapRecordsManager.CurrentMapRecordHolder?.Speed.ToString("F", CultureInfo.InvariantCulture)}"
            + $" <font class='fontSize-s stratum-font' color='#A7A7A7'>{(MapRecordsManager.CurrentMapRecordHolder?.Name.Length > 10 ? "(" + MapRecordsManager.CurrentMapRecordHolder.Name.Substring(0, 9) + "..)" : "(" + MapRecordsManager.CurrentMapRecordHolder?.Name + ")")}</font> </font>"
        );
    }

}