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
        }
        else
        {
            var observedDeathrunPlayer = deathrunPlayer.ObservedDeathrunPlayer;
            if (observedDeathrunPlayer is null) return;
                
            speedNum = observedDeathrunPlayer.PlayerPawn?.GetAbsVelocity().Length() ?? 777;
        }
        
        var randomNum = Random.Shared.Next(0, 1);
        
        deathrunPlayer.SetCenterMenuTopRowHtml
        (
            randomNum is not 0 ?
                $"<font class='fontSize-sm stratum-font fontWeight-Bold' color='#A7A7A7'>Map's Speed record: </font>"
                + $"<font class='fontSize-m stratum-font fontWeight-Bold' color='lightred'>1964.2</font>"
                + $"<font class='fontSize-m stratum-font fontWeight-Bold' color='magenta'> | </font>"
                + $"<font class='fontSize-sm stratum-font fontWeight-Bold' color='#A7A7A7'>Record holder: </font>"
                + $"<img src='https://i.ibb.co/Jwv5Bz6S/maprecordholdericon.png' width='13' height='13' />"
                + $"<font class='fontSize-m stratum-font fontWeight-Bold' color='gold'>AquaVadis</font>" 
                :
                $"<font class='fontSize-m stratum-font fontWeight-Bold' color='#A7A7A7'>Speed: </font>"
                + $"<font class='fontSize-m stratum-font fontWeight-Bold' color='magenta'>{speedNum.ToString("F", CultureInfo.InvariantCulture)}</font>"
        );
    }

}