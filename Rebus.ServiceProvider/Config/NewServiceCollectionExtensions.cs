using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rebus.Bus;
using Rebus.Injection;
using Rebus.Pipeline;
using Rebus.ServiceProvider;
using Rebus.Transport;
// ReSharper disable ArgumentsStyleLiteral
// ReSharper disable ArrangeModifiersOrder
// ReSharper disable SimplifyLinqExpressionUseAll

namespace Rebus.Config;

public static class NewServiceCollectionExtensions
{
    ///// <summary>
    ///// Adds an independent <see cref="IHostedService"/> to the host's container, containing its own service provider. This means that the
    ///// <paramref name="configureServices"/> callback must be used to make any container registrations necessary for the service to work.
    ///// </summary>
    ///// <param name="builder">Reference to the host builder that will host this background service</param>
    ///// <param name="configure">Configuration callback that allows for executing Rebus' configuration spell</param>
    ///// <param name="configureServices">Configuration callback that must be used to configure the container</param>
    //public static IHostBuilder AddRebusService(this IHostBuilder builder, Func<RebusConfigurer, IServiceProvider, RebusConfigurer> configure, Action<IServiceCollection> configureServices)
    //{
    //    return builder.ConfigureServices((_, hostServices) =>
    //    {
    //        hostServices.AddSingleton<IHostedService>(provider =>
    //        {
    //            void ConfigureServices(IServiceCollection services)
    //            {
    //                configureServices(services);

    //                services.AddSingleton(new RebusResolver());
    //                services.AddTransient(p => p.GetRequiredService<RebusResolver>().GetBus(p));
    //                services.AddTransient(p => p.GetRequiredService<IBus>().Advanced.SyncBus);
    //                services.AddTransient(_ => MessageContext.Current ?? throw new InvalidOperationException("Could not get current message context! The message context can only be resolved when handling a Rebus message, and it looks like this attempt was made from somewhere else."));
    //            }

    //            return new IndependentRebusHostedService(configure, ConfigureServices, provider);
    //        });
    //    });
    //}

    /// <summary>
    /// Adds an independent <see cref="IHostedService"/> to the host's container, containing its own service provider. This means that the
    /// <paramref name="configureServices"/> callback must be used to make any container registrations necessary for the service to work.
    /// </summary>
    /// <param name="builder">Reference to the host builder that will host this background service</param>
    /// <param name="configure">Configuration callback that allows for executing Rebus' configuration spell</param>
    /// <param name="configureServices">Configuration callback that must be used to configure the container</param>
    public static IHostBuilder AddRebusService(this IHostBuilder builder, Action<IServiceCollection> configureServices)
    {
        return builder.ConfigureServices((_, hostServices) =>
        {
            hostServices.AddSingleton<IHostedService>(provider =>
            {
                void ConfigureServices(IServiceCollection services)
                {
                    configureServices(services);

                    services.AddSingleton(new RebusResolver());
                    services.AddTransient(p => p.GetRequiredService<RebusResolver>().GetBus(p));
                    services.AddTransient(p => p.GetRequiredService<IBus>().Advanced.SyncBus);
                    services.AddTransient(_ => MessageContext.Current ?? throw new InvalidOperationException("Could not get current message context! The message context can only be resolved when handling a Rebus message, and it looks like this attempt was made from somewhere else."));
                }

                return new IndependentRebusHostedService(configure, ConfigureServices, provider);
            });
        });
    }

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
    public static IServiceCollection AddRebus(this IServiceCollection services, Func<RebusConfigurer, RebusConfigurer> configure, bool isDefaultBus = false)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        return AddRebus(services, (configurer, _) => configure(configurer), isDefaultBus: isDefaultBus);
    }

    public static IServiceCollection AddRebus(this IServiceCollection services, Func<RebusConfigurer, IServiceProvider, RebusConfigurer> configure, bool isDefaultBus = true)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        services.AddSingleton<IHostedService>(p => new RebusHostedService(configure, new(() => p), disposeServiceProvider: false));

        if (!services.Any(s => s.ImplementationType == typeof(RebusResolver)))
        {
            services.AddSingleton(new RebusResolver());
            services.AddTransient(p => p.GetRequiredService<RebusResolver>().GetBus(p));
            services.AddTransient(p => p.GetRequiredService<IBus>().Advanced.SyncBus);
            services.AddTransient(_ => MessageContext.Current ?? throw new InvalidOperationException("Could not get current message context! The message context can only be resolved when handling a Rebus message, and it looks like this attempt was made from somewhere else."));
        }

        return services;
    }

    class RebusResolver
    {
        public IBus GetBus(IServiceProvider serviceProvider)
        {
            var messageContext = MessageContext.Current;

            if (messageContext == null)
            {
                try
                {
                    return serviceProvider.GetRequiredService<IBus>();
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException("Error when trying to resolve default bus instance! No current message context was found (i.e. we're not currently handling a message), so a bus was requested from the service provider. If you'd like to use IBus outside of message handlers, please remember to mark one of the bus registrations as being the default bus instance.", exception);
                }
            }

            var incomingStepContext = messageContext.IncomingStepContext;
            var bus = incomingStepContext.Load<IBus>();

            if (bus == null)
            {
                throw new ApplicationException("Couldn't find IBus in the incoming step context. This is a sign that the SetBusInstanceStep was not executed as expected for the incoming message, which in turn is a sign that something is very wrong.");
            }

            return bus;
        }
    }

    class IndependentRebusHostedService : RebusHostedService
    {
        public IndependentRebusHostedService(Func<RebusConfigurer, IServiceProvider, RebusConfigurer> configure, Action<IServiceCollection> configureServices, IServiceProvider hostServiceProvider)
            : base(configure, new(() => BuildServiceProvider(configureServices, hostServiceProvider)), disposeServiceProvider: true)
        {
        }

        static IServiceProvider BuildServiceProvider(Action<IServiceCollection> configureServices, IServiceProvider hostServiceProvider)
        {
            var services = new ServiceCollection();

            services.AddTransient(_ => hostServiceProvider.GetService<IHostApplicationLifetime>());

            configureServices(services);

            return services.BuildServiceProvider();
        }
    }

    class RebusHostedService : BackgroundService
    {
        readonly Func<RebusConfigurer, IServiceProvider, RebusConfigurer> _configure;
        readonly Lazy<IServiceProvider> _serviceProvider;
        readonly bool _disposeServiceProvider;

        public RebusHostedService(Func<RebusConfigurer, IServiceProvider, RebusConfigurer> configure, Lazy<IServiceProvider> serviceProvider, bool disposeServiceProvider)
        {
            _configure = configure;
            _serviceProvider = serviceProvider;
            _disposeServiceProvider = disposeServiceProvider;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var serviceProvider = _serviceProvider.Value;

            try
            {
                var rebusConfigurer = Configure.With(new DependencyInjectionHandlerActivator(serviceProvider))
                    .Options(o => o.Decorate<IPipeline>(c =>
                    {
                        var pipeline = c.Get<IPipeline>();
                        return new PipelineStepConcatenator(pipeline)
                            .OnReceive(new ServiceProviderProviderStep(serviceProvider),
                                PipelineAbsolutePosition.Front)
                            .OnReceive(new SetBusInstanceStep(c), PipelineAbsolutePosition.Front);
                    }));

                var configurer = _configure(rebusConfigurer, serviceProvider);

                var starter = configurer.Create();
                var bus = starter.Start();

                stoppingToken.Register(() =>
                {
                    bus.Dispose();

                    if (_disposeServiceProvider && serviceProvider is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                });
            }
            catch when (_disposeServiceProvider && serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
                throw;
            }

            return Task.CompletedTask;
        }
    }

    class SetBusInstanceStep : IIncomingStep
    {
        readonly IResolutionContext _resolutionContext;

        public SetBusInstanceStep(IResolutionContext resolutionContext)
        {
            _resolutionContext = resolutionContext;
        }

        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            Console.WriteLine("Getting bus from resolution context");
            var bus = _resolutionContext.Get<IBus>();
            Console.WriteLine("Saving bus to incoming step context");
            context.Save(bus);
            Console.WriteLine("Calling the rest of the pipeline");
            await next();
        }
    }
}