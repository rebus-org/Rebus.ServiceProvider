using System;
using System.Linq;
using System.Threading.Tasks;
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
    /// <see cref="ServiceCollectionExtensions.AddRebus(IServiceCollection,Func{RebusConfigurer,RebusConfigurer},bool,Func{IBus,Task})"/>
    /// or <see cref="ServiceCollectionExtensions.AddRebus(IServiceCollection,Func{RebusConfigurer,IServiceProvider,RebusConfigurer},bool,Func{IBus,Task})"/>
    /// from this callback is mandatory. 
    /// </param>
    public static IHostBuilder AddRebusService(this IHostBuilder builder, Action<IServiceCollection> configureServices)
    {
        return builder.ConfigureServices((_, hostServices) =>
        {
            hostServices.AddSingleton<IHostedService>(provider =>
            {
                void ConfigureServices(IServiceCollection services)
                {
                    // add forwards to host service provider
                    services.AddTransientForward<IHostApplicationLifetime>(provider);
                    services.AddTransientForward<ILoggerFactory>(provider);

                    // configure user's services
                    configureServices(services);

                    // ensure that at least one bus instance was registered
                    if (!services.Any(s => s.ServiceType == typeof(IBus)))
                    {
                        throw new InvalidOperationException("No Rebus instances were registered in the service collection - please remember to call services.AddRebus(...) on the configuration callback.");
                    }

                    // make Rebus base registrations
                    services.AddSingleton(new RebusResolver());
                    services.AddTransient(p => p.GetRequiredService<RebusResolver>().GetBus(p));
                    services.AddTransient(p => p.GetRequiredService<IBus>().Advanced.SyncBus);
                    services.AddTransient(_ => MessageContext.Current ?? throw new InvalidOperationException("Could not get current message context! The message context can only be resolved when handling a Rebus message, and it looks like this attempt was made from somewhere else."));
                }

                return new IndependentRebusHostedService(ConfigureServices);
            });
        });
    }

    static void AddTransientForward<TService>(this IServiceCollection services, IServiceProvider provider) where TService : class => services.AddTransient(_ => provider.GetService<TService>());
}