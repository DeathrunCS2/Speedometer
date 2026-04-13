using System.Globalization;
using DeathrunManager.Shared;
using DeathrunManager.Shared.Objects;
using Microsoft.Extensions.Logging;
using Sharp.Shared;

namespace Deathrun.Speedometer;

public class Speedometer(ISharedSystem sharedSystem, IDeathrunManager deathrunManagerApi) : IDeathrunModule
{
    public string                           Name                   => $"Speedometer Extension";
    public string                           Author                 => "AquaVadis";
    
    public IDeathrunManager                 DeathrunManagerApi     { get; } = deathrunManagerApi;
    private ILogger<Speedometer>            _logger                = sharedSystem.GetLoggerFactory().CreateLogger<Speedometer>();
    
    #region IModule
    
    public bool Init(bool hotReload)
    {
        DeathrunManagerApi.Managers.PlayersManager.ThinkPost += OnDeathrunPlayerThinkPost;
        
        return true;
    }
    public void Shutdown(bool hotReload)
    {
        DeathrunManagerApi.Managers.PlayersManager.ThinkPost -= OnDeathrunPlayerThinkPost;
    }

    #endregion
    
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
            
        deathrunPlayer.SetCenterMenuMiddleRowHtml
        (
            $"<font class='fontSize-m stratum-font fontWeight-Bold' color='#A7A7A7'>Speed: </font>"
            + $"<font class='fontSize-m stratum-font fontWeight-Bold' color='magenta'>{speedNum.ToString("F", CultureInfo.InvariantCulture)}</font>"    
        );
    }
}