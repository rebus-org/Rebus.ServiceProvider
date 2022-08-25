using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rebus.Bus;
using Rebus.Pipeline;
using Rebus.ServiceProvider.Internals;
// ReSharper disable SimplifyLinqExpressionUseAll

namespace Rebus.Config;

/// <summary>
/// Extension method for registering 
/// </summary>
public static class HostBuilderExtensions
{
    /// <summary>
    /// Adds an independent <see cref="IHostedService"/> to the host's container, containing its own service provider. This means that the
    /// <paramref name="configureServices"/> callback must be used to make any container registrations necessary for the service to work.
    /// </summary>
    /// <param name="builder">Reference to the host builder that will host this background service</param>
    /// <param name="configureServices">
    /// Configuration callback that must be used to configure the container. Making at least one call to
    /// <see cref="ServiceCollectionExtensions.AddRebus(Microsoft.Extensions.DependencyInjection.IServiceCollection,System.Func{Rebus.Config.RebusConfigurer,Rebus.Config.RebusConfigurer},bool,System.Func{Rebus.Bus.IBus,System.Threading.Tasks.Task},string,bool)(IServiceCollection,Func{RebusConfigurer,RebusConfigurer},bool,Func{IBus,Task})"/>
    /// or <see cref="ServiceCollectionExtensions.AddRebus(Microsoft.Extensions.DependencyInjection.IServiceCollection,System.Func{Rebus.Config.RebusConfigurer,Rebus.Config.RebusConfigurer},bool,System.Func{Rebus.Bus.IBus,System.Threading.Tasks.Task},string,bool)(IServiceCollection,Func{RebusConfigurer,IServiceProvider,RebusConfigurer},bool,Func{IBus,Task})"/>
    /// </param>
    public static IHostBuilder AddRebusService(this IHostBuilder builder, Action<IServiceCollection> configureServices)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (configureServices == null) throw new ArgumentNullException(nameof(configureServices));

        return builder.AddRebusService((_, hostServices) => configureServices(hostServices));
    }

    /// <summary>
    /// Adds an independent <see cref="IHostedService"/> to the host's container, containing its own service provider. This means that the
    /// <paramref name="configureServices"/> callback must be used to make any container registrations necessary for the service to work.
    /// </summary>
    /// <param name="builder">Reference to the host builder that will host this background service</param>
    /// <param name="configureServices">
    /// Configuration callback that must be used to configure the container. Making at least one call to
    /// <see cref="ServiceCollectionExtensions.AddRebus(Microsoft.Extensions.DependencyInjection.IServiceCollection,System.Func{Rebus.Config.RebusConfigurer,Rebus.Config.RebusConfigurer},bool,System.Func{Rebus.Bus.IBus,System.Threading.Tasks.Task},string,bool)(IServiceCollection,Func{RebusConfigurer,RebusConfigurer},bool,Func{IBus,Task})"/>
    /// or <see cref="ServiceCollectionExtensions.AddRebus(Microsoft.Extensions.DependencyInjection.IServiceCollection,System.Func{Rebus.Config.RebusConfigurer,Rebus.Config.RebusConfigurer},bool,System.Func{Rebus.Bus.IBus,System.Threading.Tasks.Task},string,bool)(IServiceCollection,Func{RebusConfigurer,IServiceProvider,RebusConfigurer},bool,Func{IBus,Task})"/>
    /// </param>
    public static IHostBuilder AddRebusService(this IHostBuilder builder, Action<HostBuilderContext, IServiceCollection> configureServices)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (configureServices == null) throw new ArgumentNullException(nameof(configureServices));
        
        return builder.AddRebusService(configureServices, typeof(IHostApplicationLifetime), typeof(ILoggerFactory));
    }

    /// <summary>
    /// Adds an independent <see cref="IHostedService"/> to the host's container, containing its own service provider. This means that the
    /// <paramref name="configureServices"/> callback must be used to make any container registrations necessary for the service to work.
    /// </summary>
    /// <param name="builder">Reference to the host builder that will host this background service</param>
    /// <param name="configureServices">
    /// Configuration callback that must be used to configure the container. Making at least one call to
    /// <see cref="ServiceCollectionExtensions.AddRebus(Microsoft.Extensions.DependencyInjection.IServiceCollection,System.Func{Rebus.Config.RebusConfigurer,Rebus.Config.RebusConfigurer},bool,System.Func{Rebus.Bus.IBus,System.Threading.Tasks.Task},string,bool)(IServiceCollection,Func{RebusConfigurer,RebusConfigurer},bool,Func{IBus,Task})"/>
    /// or <see cref="ServiceCollectionExtensions.AddRebus(Microsoft.Extensions.DependencyInjection.IServiceCollection,System.Func{Rebus.Config.RebusConfigurer,Rebus.Config.RebusConfigurer},bool,System.Func{Rebus.Bus.IBus,System.Threading.Tasks.Task},string,bool)(IServiceCollection,Func{RebusConfigurer,IServiceProvider,RebusConfigurer},bool,Func{IBus,Task})"/>
    /// </param>
    /// <param name="forwardedSingletonTypes">
    /// Types available from the host's service provider which should be forwarded into the hosted service's container.
    /// Please note that the registration will be made with the SINGLETON lifestyle, which in turn means that the target registration must also be a singleton, otherwise
    /// things will get messy.
    /// </param>
    public static IHostBuilder AddRebusService(this IHostBuilder builder, Action<IServiceCollection> configureServices, params Type[] forwardedSingletonTypes)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (configureServices == null) throw new ArgumentNullException(nameof(configureServices));
        if (forwardedSingletonTypes == null) throw new ArgumentNullException(nameof(forwardedSingletonTypes));

        return builder.AddRebusService((_, hostServices) => configureServices(hostServices), forwardedSingletonTypes);
    }

    /// <summary>
    /// Adds an independent <see cref="IHostedService"/> to the host's container, containing its own service provider. This means that the
    /// <paramref name="configureServices"/> callback must be used to make any container registrations necessary for the service to work.
    /// </summary>
    /// <param name="builder">Reference to the host builder that will host this background service</param>
    /// <param name="configureServices">
    /// Configuration callback that must be used to configure the container. Making at least one call to
    /// <see cref="ServiceCollectionExtensions.AddRebus(Microsoft.Extensions.DependencyInjection.IServiceCollection,System.Func{Rebus.Config.RebusConfigurer,Rebus.Config.RebusConfigurer},bool,System.Func{Rebus.Bus.IBus,System.Threading.Tasks.Task},string,bool)(IServiceCollection,Func{RebusConfigurer,RebusConfigurer},bool,Func{IBus,Task})"/>
    /// or <see cref="ServiceCollectionExtensions.AddRebus(Microsoft.Extensions.DependencyInjection.IServiceCollection,System.Func{Rebus.Config.RebusConfigurer,Rebus.Config.RebusConfigurer},bool,System.Func{Rebus.Bus.IBus,System.Threading.Tasks.Task},string,bool)(IServiceCollection,Func{RebusConfigurer,IServiceProvider,RebusConfigurer},bool,Func{IBus,Task})"/>
    /// </param>
    /// <param name="forwardedSingletonTypes">
    /// Types available from the host's service provider which should be forwarded into the hosted service's container.
    /// Please note that the registration will be made with the SINGLETON lifestyle, which in turn means that the target registration must also be a singleton, otherwise
    /// things will get messy.
    /// </param>
    public static IHostBuilder AddRebusService(this IHostBuilder builder, Action<HostBuilderContext, IServiceCollection> configureServices, params Type[] forwardedSingletonTypes)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (configureServices == null) throw new ArgumentNullException(nameof(configureServices));
        if (forwardedSingletonTypes == null) throw new ArgumentNullException(nameof(forwardedSingletonTypes));
        
        return builder.ConfigureServices((hostBuilderContext, hostServices) =>
        {
            hostServices.AddSingleton<IHostedService>(hostProvider =>
            {
                void ConfigureServices(IServiceCollection services)
                {
                    // add forwards to host service provider
                    foreach (var forwardedType in forwardedSingletonTypes)
                    {
                        services.AddSingletonForward(forwardedType, hostProvider);
                    }

                    // configure user's services
                    configureServices(hostBuilderContext, services);

                    // ensure that at least one bus instance was registered
                    if (!services.Any(s => s.ServiceType == typeof(IBus)))
                    {
                        throw new InvalidOperationException("No Rebus instances were registered in the service collection - please remember to call services.AddRebus(...) on the configuration callback.");
                    }

                    // make Rebus base registrations
                    services.AddSingleton(new RebusResolver());
                    services.AddTransient(provider => provider.GetRequiredService<RebusResolver>().GetBus(provider));
                    services.AddTransient(provider => provider.GetRequiredService<IBus>().Advanced.SyncBus);
                    services.AddTransient(_ => MessageContext.Current ?? throw new InvalidOperationException("Could not get current message context! The message context can only be resolved when handling a Rebus message, and it looks like this attempt was made from somewhere else."));
                }

                return new IndependentRebusHostedService(ConfigureServices);
            });
        });
    }

    static void AddSingletonForward(this IServiceCollection services, Type forwardedType, IServiceProvider hostProvider)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (forwardedType == null) throw new ArgumentNullException(nameof(forwardedType));
        if (hostProvider == null) throw new ArgumentNullException(nameof(hostProvider));

        services.AddSingleton(forwardedType, _ => hostProvider.GetRequiredService(forwardedType));
    }
}