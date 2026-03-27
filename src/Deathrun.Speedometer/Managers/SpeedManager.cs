using System.Globalization;
using Deathrun.Speedometer.Interfaces.Managers;
using DeathrunManager.Shared.Objects;
using Sharp.Shared;
using Sharp.Shared.Listeners;
using Sharp.Shared.Objects;

namespace Deathrun.Speedometer.Managers;

internal class SpeedManager(
    IModSharp modSharp) : ISpeedManager, IGameListener
{
    private static IGlobalVars? _globalVars = null;
    
    #region IModule
    
    public bool Init()
    {
        modSharp.InstallGameListener(this);
        
        return true;
    }

    public static void OnPostInit() { }

    public void OnAllSharpModulesLoaded()
    {
        if (Speedometer.DeathrunManagerApi?.Instance is not { } deathrunManagerApi) return;

        deathrunManagerApi.Managers.PlayersManager.DeathrunPlayerThinkPost += OnDeathrunPlayerThinkPost;
    }
    
    public void Shutdown()
    {
        modSharp.RemoveGameListener(this);
        
        if (Speedometer.DeathrunManagerApi?.Instance is not { } deathrunManagerApi) return;

        deathrunManagerApi.Managers.PlayersManager.DeathrunPlayerThinkPost -= OnDeathrunPlayerThinkPost;
    }

    #endregion

    #region API Listeners

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
    
    #endregion
    
    #region Listeners

    public void OnServerInit() => _globalVars = modSharp.GetGlobals();
    
    #endregion
    
    int IGameListener.ListenerVersion => IGameListener.ApiVersion;
    int IGameListener.ListenerPriority => 8;
}




