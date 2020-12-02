using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Pipeline;
using Rebus.ServiceProvider.Internals;

namespace Rebus.ServiceProvider
{
    public static partial class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers and/or modifies Rebus configuration for the current service collection.
        /// </summary>
        /// <param name="services">The current message service builder.</param>
        /// <param name="configure">The optional configuration actions for Rebus.</param>
        public static IServiceCollection AddRebus(this IServiceCollection services, Func<RebusConfigurer, RebusConfigurer> configure)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            return AddRebus(services, (c, p) => configure(c));
        }

        /// <summary>
        /// Registers and/or modifies Rebus configuration for the current service collection.
        /// </summary>
        /// <param name="services">The current message service builder.</param>
        /// <param name="configure">The optional configuration actions for Rebus.</param>
        public static IServiceCollection AddRebus(this IServiceCollection services, Func<RebusConfigurer, IServiceProvider, RebusConfigurer> configure)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            var busAlreadyRegistered = services.Any(descriptor => descriptor.ServiceType == typeof(IBus));

            if (busAlreadyRegistered)
            {
                throw new InvalidOperationException(@"Sorry, but it seems like Rebus has already been configured in this service collection. 

It is advised to use one container instance per bus instance, because this way it can be treated as an autonomous component with the container as the root.");
            }

            services.AddTransient(s => MessageContext.Current ?? throw new InvalidOperationException("Attempted to resolve IMessageContext outside of a Rebus handler, which is not possible. If you get this error, it's probably a sign that your service provider is being used outside of Rebus, where it's simply not possible to resolve a Rebus message context. Rebus' message context is only available to code executing inside a Rebus handler."));
            services.AddTransient(s => s.GetRequiredService<IBus>().Advanced.SyncBus);

            BusLifetimeEvents busLifetimeEvents = null;

            // Register the Rebus Bus instance, to be created when it is first requested.
            services.AddSingleton<IHandlerActivator, DependencyInjectionHandlerActivator>();

            services.AddSingleton(provider =>
            {
                var activator = provider.GetRequiredService<IHandlerActivator>();

                var configurer = Configure.With(activator);

                configure(configurer, provider);

                var starter = configurer

                    // little hack: we snatch the lifetime events here...
                    .Options(o => o.Decorate(c => busLifetimeEvents = c.Get<BusLifetimeEvents>()))

                    .Options(o => o.Decorate<IPipeline>(c =>
                    {
                        var pipeline = c.Get<IPipeline>();
                        var step = new ServiceProviderProviderStep(provider);

                        return new PipelineStepConcatenator(pipeline)
                            .OnReceive(step, PipelineAbsolutePosition.Front)
                            .OnSend(step, PipelineAbsolutePosition.Front);
                    }))
                    .Create();

                return starter;
            });

            // ...so we can install a resolver for it here:
            services.AddSingleton(provider =>
            {
                // first, ensure that the busLifetimeEventsInstance has been set
                provider.GetRequiredService<IBus>();

                // then return the instance
                return busLifetimeEvents;
            });

            services.AddSingleton(provider => provider.GetRequiredService<IBusStarter>().Bus);
            services.AddSingleton(provider => new ServiceCollectionBusDisposalFacility(provider.GetRequiredService<IBus>()));

            return services;
        }
    }
}
