using System;
using Microsoft.Extensions.DependencyInjection;

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
            var activator = provider.GetRequiredService<RebusActivator>();
            activator.Activate();
            return provider;
        }
    }
}
