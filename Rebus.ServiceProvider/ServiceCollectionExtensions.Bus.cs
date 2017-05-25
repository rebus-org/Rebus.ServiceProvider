using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Bus;
using Rebus.Config;

namespace Rebus.ServiceProvider
{
    public static partial class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers and/or modifies Rebus configuration for the current service collection.
        /// </summary>
        /// <param name="services">The current message service builder.</param>
        /// <param name="configureRebus">The optional configuration actions for Rebus.</param>
        public static IServiceCollection AddRebus(this IServiceCollection services, Func<RebusConfigurer, RebusConfigurer> configureRebus = null)
        {
            var messageBusRegistration = services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(IBus));

            if (messageBusRegistration == null) // not yet registered
            {
                var adapter = new ServiceProviderContainerAdapter(services);
                var configurer = Configure.With(adapter);
                services.AddSingleton(configurer);

                // Register the Rebus Bus instance, to be created when it is first requested.
                services.AddSingleton(provider =>
                {
                    // Apply any configuration in the order in which they were specified during startup
                    foreach (var configAction in provider.GetServices<RebusConfigAction>())
                    {
                        configAction.Action(configurer);
                    }

                    return configurer.Start();
                });

                // Register the bus activator, to be 'activated' when it is first requested.
                services.AddSingleton(provider => new RebusActivator(() =>
                {
                    // give our adapter the application DI container
                    adapter.Activate(provider);

                    // trigger the configurer.Start() we registered above to activate the bus, which can immediately
                    // start processing messages.  Our adapter is ready to server handlers from the app DI container, so all
                    // is good.
                    var bus = provider.GetRequiredService<IBus>();
                }));
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
