using Deathrun.Speedometer.Interfaces.Managers;
using Deathrun.Speedometer.Interfaces.Managers.Native;
using Deathrun.Speedometer.Managers.Native.ClientListener;
using Deathrun.Speedometer.Managers.Native.Event;
using Deathrun.Speedometer.Managers.Native.GameListener;
using Deathrun.Speedometer.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Deathrun.Speedometer.Managers;

internal static class ManagersDependencyInjection
{
    public static IServiceCollection AddManagers(this IServiceCollection services)
    {
        //Native Managers
        services.AddSingleton<IManager, IClientListenerManager, ClientListenerManager>();
        services.AddSingleton<IManager, IEventManager, EventManager>();
        services.AddSingleton<IManager, IGameListenerManager, GameListenerManager>();
        
        services.AddSingleton<IManager, ISpeedManager, SpeedManager>();
            
        return services;
    }
}
