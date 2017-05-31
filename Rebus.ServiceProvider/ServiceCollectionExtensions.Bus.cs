using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Pipeline;

namespace Rebus.ServiceProvider
{
    public static partial class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers and/or modifies Rebus configuration for the current service collection.
        /// </summary>
        /// <param name="services">The current message service builder.</param>
        /// <param name="configureRebus">The optional configuration actions for Rebus.</param>
        public static IServiceCollection AddRebus(this IServiceCollection services, Func<RebusConfigurer, RebusConfigurer> configureRebus)
        {
            var messageBusRegistration = services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(IBus));

            if (messageBusRegistration == null) // not yet registered
            {
                services.AddTransient(s => MessageContext.Current);
                services.AddTransient(s => s.GetService<IBus>().Advanced.SyncBus);

                // Register the Rebus Bus instance, to be created when it is first requested.
                services.AddSingleton(provider =>
                {
                    var adapter = new ServiceProviderContainerAdapter(provider);

                    // Apply any configuration in the order in which they were specified during startup
                    var configurer = Configure.With(adapter);
                    foreach (var configAction in provider.GetServices<RebusConfigAction>())
                    {
                        configAction.Action(configurer);
                    }

                    return configurer.Start();
                });
            }

            // Now register this configure action, as potentially one of many from different components...
            if (configureRebus != null)
            {
                services.AddSingleton(new RebusConfigAction(configureRebus));
            }

            return services;
        }
    }
}
