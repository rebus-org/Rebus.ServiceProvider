using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Pipeline;

namespace Rebus.ServiceProvider.Internals;

class RebusBackgroundService : BackgroundService
{
    readonly Func<RebusConfigurer, IServiceProvider, RebusConfigurer> _configure;
    readonly IServiceProvider _serviceProvider;
    readonly Func<IBus, Task> _onCreated;
    readonly bool _startAutomatically;
    readonly bool _isDefaultBus;
    readonly string _key;
    readonly Lazy<Task<(IBus, BusLifetimeEvents)>> _busInitializer;

    public RebusBackgroundService(Func<RebusConfigurer, IServiceProvider, RebusConfigurer> configure,
        IServiceProvider serviceProvider, bool isDefaultBus, Func<IBus, Task> onCreated, DefaultBusInstance defaultBusInstance, string key = null,
        bool startAutomatically = true)
    {
        _configure = configure ?? throw new ArgumentNullException(nameof(configure));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _isDefaultBus = isDefaultBus;
        _onCreated = onCreated;
        _key = key;
        _startAutomatically = startAutomatically;

        _busInitializer = new Lazy<Task<(IBus, BusLifetimeEvents)>>(async () => await InitializeBus(CancellationToken.None));

        if (isDefaultBus)
        {
            if (defaultBusInstance == null)
            {
                throw new InvalidOperationException(
                    $"The {nameof(isDefaultBus)} = true paramater said to configure this Rebus instance to be the default bus, but the {nameof(defaultBusInstance)} parameter was NULL!");
            }

            defaultBusInstance.SetInstanceResolver(_busInitializer);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var (bus, _) = await _busInitializer.Value;

        stoppingToken.Register(bus.Dispose);
    }

    async Task<(IBus, BusLifetimeEvents)> InitializeBus(CancellationToken stoppingToken)
    {
        var loggerFactory = _serviceProvider.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger<RebusBackgroundService>();

        BusLifetimeEvents busLifetimeEventsHack = null;

        var rebusConfigurer = Configure
            .With(new DependencyInjectionHandlerActivator(_serviceProvider))
            .Options(o => o.Decorate(c =>
            {
                // snatch events here
                return busLifetimeEventsHack = c.Get<BusLifetimeEvents>();
            }))
            .Options(o => o.Decorate<IPipeline>(context =>
            {
                var pipeline = context.Get<IPipeline>();
                var serviceProviderProviderStep = new ServiceProviderProviderStep(_serviceProvider, context);
                return new PipelineStepConcatenator(pipeline)
                    .OnReceive(serviceProviderProviderStep, PipelineAbsolutePosition.Front);
            }));

        var configurer = _configure(rebusConfigurer, _serviceProvider);

        var starter = configurer
            .Logging(l =>
            {
                if (loggerFactory == null) return;

                try
                {
                    // wild hack: Reflect into Injectionist to see, if a logger factory has already been registered
                    if (l.ReflectWhetherItHasRegistration<IRebusLoggerFactory>())
                    {
                        return;
                    }

                    l.Use(new MicrosoftExtensionsLoggingLoggerFactory(loggerFactory));
                }
                catch (InvalidOperationException)
                {
                    // ignore this exception, because it'll simply mean that a logger factory has already been configured,
                    // and so we should ignore that... 
                }
            })
            .Create();

        var bus = starter.Bus;

        logger?.LogInformation("Successfully created bus instance {busInstance} (isDefaultBus: {flag})", bus, _isDefaultBus);

        // stopping the bus here will ensure that we've finished executing all message handlers when the container is disposed
        stoppingToken.Register(() =>
        {
            logger?.LogDebug("Stopping token signaled - disposing bus instance {busInstance}", bus);
            bus.Dispose();
            logger?.LogInformation("Bus instance {busInstance} successfully disposed", bus);
        });

        if (_key != null)
        {
            var registry = _serviceProvider.GetRequiredService<ServiceProviderBusRegistry>();
            registry.AddBus(bus, starter, _key);
            logger?.LogDebug("Bus instance {busInstance} was registered in IBusRegistry with key {key}", bus, _key);
        }

        if (_onCreated != null)
        {
            logger?.LogDebug("Invoking onCreated callback on bus instance {busInstance}", bus);
            await _onCreated(bus);
        }

        if (_startAutomatically)
        {
            logger?.LogDebug("Starting bus instance {busInstance}", bus);
            starter.Start();
        }
        else
        {
            logger?.LogDebug("NOT starting bus instance {busInstance}, because it has been configured with startAutomatically:false", bus);
        }

        return (bus, busLifetimeEventsHack);
    }
}