using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rebus.Bus;
using Rebus.Pipeline;
// ReSharper disable ArgumentsStyleLiteral
// ReSharper disable ArrangeModifiersOrder
// ReSharper disable SimplifyLinqExpressionUseAll

namespace Rebus.Config;

/// <summary>
/// Extension methods for registering Rebus in a service collection
/// </summary>
public static class NewServiceCollectionExtensions
{
    /// <summary>
    /// Adds Rebus to the service collection, invoking the <paramref name="configure"/> callback to allow for executing Rebus' configuration spell.
    /// The <paramref name="isDefaultBus"/> parameter indicates whether resolving <see cref="IBus"/> from the resulting service provider outside of a Rebus
    /// handler should yield THIS particular bus instance. Please note that there can be only 1 default bus per container instance! And please note that
    /// Rebus handlers (and any services injected into them) will always have the <see cref="IBus"/> from the current message context injected into them.
    /// </summary>
    /// <param name="services">Reference to the service collection that this extension method is invoked on</param>
    /// <param name="configure">Configuration callback that can be used to invoke the Rebus configuration spell</param>
    /// <param name="isDefaultBus">
    /// Indicates whether resolving <see cref="IBus"/> from the resulting service provider outside of a Rebus
    /// handler should yield this particular bus instance. Please note that there can be only 1 default bus per container instance! And please note that
    /// Rebus handlers (and any services injected into them) will always have the <see cref="IBus"/> from the current message context injected into them.
    /// </param>
    public static IServiceCollection AddRebus(this IServiceCollection services, Func<RebusConfigurer, RebusConfigurer> configure, bool isDefaultBus = true)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        return AddRebus(services, (configurer, _) => configure(configurer), isDefaultBus: isDefaultBus);
    }

    public static IServiceCollection AddRebus(this IServiceCollection services, Func<RebusConfigurer, IServiceProvider, RebusConfigurer> configure, bool isDefaultBus = true)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        services.AddSingleton<IHostedService>(provider => new RebusHostedService(configure, provider, isDefaultBus));

        if (isDefaultBus)
        {
            if (services.Any(s => s.ServiceType == typeof(DefaultBusInstance)))
            {
                throw new InvalidOperationException($"Detected that the service collection already contains a default bus registration - please make only one single AddRebus call with isDefaultBus:true");
            }

            services.AddSingleton(new DefaultBusInstance());
        }

        if (!services.Any(s => s.ImplementationType == typeof(RebusResolver)))
        {
            services.AddSingleton(new RebusResolver());
            services.AddTransient(p => p.GetRequiredService<RebusResolver>().GetBus(p));
            services.AddTransient(p => p.GetRequiredService<IBus>().Advanced.SyncBus);
            services.AddTransient(p => p.GetRequiredService<IBus>().Advanced.DataBus);
            services.AddTransient(_ => MessageContext.Current ?? throw new InvalidOperationException("Could not get current message context! The message context can only be resolved when handling a Rebus message, and it looks like this attempt was made from somewhere else."));
            services.AddTransient(p => p.GetRequiredService<DefaultBusInstance>().BusLifetimeEvents);
        }

        return services;
    }
}