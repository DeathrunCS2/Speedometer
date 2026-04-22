using System;
using System.Globalization;
using Speedometer.Interfaces;
using DeathrunManager.Shared;
using DeathrunManager.Shared.DeathrunObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Sharp.Shared;
using Speedometer.Extensions;
using Speedometer.Interfaces.Managers;
using Speedometer.Services;

namespace Speedometer;

public class Speedometer(ISharedSystem sharedSystem, IDeathrunManager deathrunManagerApi) : IDeathrunModule
{
    public string                           Name                   => $"Speedometer Extension";
    public string                           Author                 => "AquaVadis";
    
    public IDeathrunManager                 DeathrunManager        { get; } = deathrunManagerApi;
    public required ServiceProvider         ServiceProvider;
    
    #region IModule
    
    public bool Init(bool hotReload)
    {
        DeathrunManager.Managers.PlayersManager.ThinkPost += OnDeathrunPlayerThinkPost;
        
        var services = new ServiceCollection();
        services.AddSingleton(this);
        services.AddSingleton(DeathrunManager);
        services.AddSingleton(sharedSystem);
        services.AddSingleton(sharedSystem.GetModSharp());
        services.AddSingleton(sharedSystem.GetHookManager());
        services.AddSingleton(sharedSystem.GetEntityManager());
        services.AddSingleton(sharedSystem.GetClientManager());
        services.AddSingleton(sharedSystem.GetLoggerFactory());
        services.AddSingleton<IBaseInterface, IMapRecords, MapRecords>();
        services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)));
        
        ServiceProvider = services.BuildServiceProvider();
        
        ServiceProvider.GetService<IMapRecords>()?.Init();
        
        return true;
    }
    public void Shutdown(bool hotReload)
    {
        DeathrunManager.Managers.PlayersManager.ThinkPost -= OnDeathrunPlayerThinkPost;
        
        ServiceProvider.GetService<IMapRecords>()?.Shutdown();
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