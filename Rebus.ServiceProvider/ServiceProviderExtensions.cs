using System;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Bus;
using Rebus.Config;
using Rebus.ServiceProvider.Internals;

// ReSharper disable UnusedMember.Global

namespace Rebus.ServiceProvider
{
    /// <summary>
    /// Defines common operations for Rebus the use an <see cref="IServiceProvider"/>.
    /// </summary>
    public static class ServiceProviderExtensions
    {
        /// <summary>
        /// Activates the Rebus engine, allowing it to start sending and receiving messages.
        /// </summary>
        /// <param name="provider">The service provider configured for Rebus.</param>
        public static IServiceProvider UseRebus(this IServiceProvider provider)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));

            return UseRebus(provider, _ => { });
        }

        /// <summary>
        /// Activates the Rebus engine, allowing it to start sending and receiving messages.
        /// </summary>
        /// <param name="provider">The service provider configured for Rebus.</param>
        /// <param name="busAction">An action to perform on the bus.</param>
        public static IServiceProvider UseRebus(this IServiceProvider provider, Action<IBus> busAction)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (busAction == null) throw new ArgumentNullException(nameof(busAction));

            provider.GetRequiredService<ServiceCollectionBusDisposalFacility>();

            var bus = provider.GetRequiredService<IBusStarter>().Start();
            
            busAction(bus);

            return provider;
        }
    }
}
