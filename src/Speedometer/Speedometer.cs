using System;
using Speedometer.Interfaces;
using DeathrunManager.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Sharp.Shared;
using Speedometer.Extensions;
using Speedometer.Interfaces.Managers;
using Speedometer.Managers;

namespace Speedometer;

public class Speedometer(ISharedSystem sharedSystem, IDeathrunManager deathrunManagerApi) : IDeathrunModule
{
    public string                           Name                   => $"Speedometer Extension";
    public string                           Author                 => "AquaVadis";
    
    public IDeathrunManager                 DeathrunManager        { get; } = deathrunManagerApi;
    public required ServiceProvider         ServiceProvider;
    
    #region IDeathrunModule
    
    public bool Init(bool hotReload)
    {
        var services = new ServiceCollection();
        services.AddSingleton(this);
        services.AddSingleton(DeathrunManager);
        services.AddSingleton(sharedSystem);
        services.AddSingleton(sharedSystem.GetModSharp());
        services.AddSingleton(sharedSystem.GetHookManager());
        services.AddSingleton(sharedSystem.GetEntityManager());
        services.AddSingleton(sharedSystem.GetClientManager());
        services.AddSingleton(sharedSystem.GetLoggerFactory());
        services.AddSingleton<IBaseInterface, IManager, MapRecordsManager>();
        services.AddSingleton<IBaseInterface, IManager, SpeedometerManager>();
        services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)));
        
        ServiceProvider = services.BuildServiceProvider();
        
        CallInit<IManager>();
        
        return true;
    }
    public void Shutdown(bool hotReload)
    {
        CallShutdown<IManager>();
        ServiceProvider.GetService<IManager>()?.Shutdown();
    }

    #endregion
    
    #region Injected Instances' Caller methods
    
    private int CallInit<T>() where T : IBaseInterface
    {
        var init = 0;

        foreach (var service in ServiceProvider.GetServices<T>())
        {
            if (!service.Init())
            {
                Log(service.GetType().Name, "Failed to Init {service}!");

                return -1;
            }

            init++;
        }

        return init;
    }

    private void CallPostInit<T>() where T : IBaseInterface
    {
        foreach (var service in ServiceProvider.GetServices<T>())
        {
            try
            {
                service.OnPostInit();
            }
            catch (Exception e)
            {
                Log(service.GetType().Name, $"An error occurred while calling PostInit | {e}");
            }
        }
    }

    private void CallShutdown<T>() where T : IBaseInterface
    {
        foreach (var service in ServiceProvider.GetServices<T>())
        {
            try
            {
                service.Shutdown();
            }
            catch (Exception e)
            {
                Log(service.GetType().Name, $"An error occurred while calling Shutdown | {e}");
            }
        }
    }

    private void CallOnAllSharpModulesLoaded<T>() where T : IBaseInterface
    {
        foreach (var service in ServiceProvider.GetServices<T>())
        {
            try
            {
                service.OnAllSharpModulesLoaded();
            }
            catch (Exception e)
            {
                
                Log(service.GetType().Name, $"An error occurred while calling OnAllSharpModulesLoaded | {e}");
            }
        }
    }

    #endregion
    
    #region ColoredLog 
    
    private static void Log(string header, string message, 
        ConsoleColor backgroundColor = ConsoleColor.DarkGray,
        ConsoleColor textColor = ConsoleColor.Black)
    {
        Console.ForegroundColor = textColor;
        Console.BackgroundColor = backgroundColor;
        Console.Write($"{header}:");
        Console.ResetColor();
        Console.Write($" {message} \n");
    }
    
    #endregion
}