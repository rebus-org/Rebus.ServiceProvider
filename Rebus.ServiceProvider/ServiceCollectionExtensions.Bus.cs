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
            return AddRebus(services, (c, p) => configureRebus(c));
        }


        /// <summary>
        /// Registers and/or modifies Rebus configuration for the current service collection.
        /// </summary>
        /// <param name="services">The current message service builder.</param>
        /// <param name="configureRebus">The optional configuration actions for Rebus.</param>
        public static IServiceCollection AddRebus(this IServiceCollection services, Func<RebusConfigurer, IServiceProvider, RebusConfigurer> configureRebus)
        {
            if (configureRebus == null)
            {
                throw new ArgumentNullException(nameof(configureRebus));
            }

            var messageBusRegistration = services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(IBus));

            if (messageBusRegistration != null)
            {
                throw new InvalidOperationException("Rebus has already been configured.");
            }

            services.AddTransient(s => MessageContext.Current);
            services.AddTransient(s => s.GetService<IBus>().Advanced.SyncBus);

            // Register the Rebus Bus instance, to be created when it is first requested.
            services.AddSingleton(provider => new DependencyInjectionHandlerActivator(provider));
            services.AddSingleton(provider =>
            {
                var configurer = Configure.With(provider.GetRequiredService<DependencyInjectionHandlerActivator>());
                configureRebus(configurer, provider);

                return configurer.Start();
            });

            return services;
        }
    }
}
