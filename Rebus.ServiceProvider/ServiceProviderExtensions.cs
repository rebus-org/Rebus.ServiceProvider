using System;
using System.Threading.Tasks;
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
        /// <exception cref="ArgumentNullException">Thrown when the service provider is null.</exception>
        public static IServiceProvider UseRebus(this IServiceProvider provider)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));

            return UseRebus(provider, _ => { });
        }

        /// <summary>
        /// Activates the Rebus engine, allowing it to start sending and receiving messages.
        /// </summary>
        /// <param name="provider">The service provider configured for Rebus.</param>
        /// <param name="onBusStarted">An action to perform immediately after the bus has started.</param>
        /// <exception cref="ArgumentNullException">Thrown when the service provider or action is null.</exception>
        public static IServiceProvider UseRebus(this IServiceProvider provider, Action<IBus> onBusStarted)
        {
            if (onBusStarted == null) throw new ArgumentNullException(nameof(onBusStarted));

            var bus = StartBus(provider);
            onBusStarted(bus);
            return provider;
        }

        /// <summary>
        /// Activates the Rebus engine, allowing it to start sending and receiving messages.
        /// </summary>
        /// <param name="provider">The service provider configured for Rebus.</param>
        /// <param name="onBusStarted">A function returning an asynchronous task, to perform immediately after the bus has started.</param>
        /// <exception cref="ArgumentNullException">Thrown when the service provider or delegate is null.</exception>
        public static IServiceProvider UseRebus(this IServiceProvider provider, Func<IBus, Task> onBusStarted)
        {
            if (onBusStarted == null) throw new ArgumentNullException(nameof(onBusStarted));

            var bus = StartBus(provider);
            AsyncHelpers.RunSync(() => onBusStarted(bus));
            return provider;
        }

        private static IBus StartBus(IServiceProvider provider)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));

            provider.GetRequiredService<ServiceCollectionBusDisposalFacility>();

            return provider.GetRequiredService<IBusStarter>().Start();
        }
    }
}
