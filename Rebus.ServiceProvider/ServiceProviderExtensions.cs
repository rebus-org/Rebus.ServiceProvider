using System;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Bus;

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
            provider.GetRequiredService<IBus>();
            return provider;
        }
    }
}
