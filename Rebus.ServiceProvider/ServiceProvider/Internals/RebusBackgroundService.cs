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
    readonly Lazy<Task<(IBus, BusLifetimeEvents)>> _busInitializer;

    CancellationToken? _cancellationToken;

    public RebusBackgroundService(Func<RebusConfigurer, IServiceProvider, RebusConfigurer> configure,
        IServiceProvider serviceProvider, bool isDefaultBus, Func<IBus, Task> onCreated,
        DefaultBusInstance defaultBusInstance, IHostApplicationLifetime hostApplicationLifetime, string key = null,
        bool startAutomatically = true)
    {
        // try snatching this
        _cancellationToken = hostApplicationLifetime?.ApplicationStopping;

        // defer initialization to this lazy boy, because it'll make it possible for whoever calls us first to cause the bus to be initialized
        _busInitializer = new Lazy<Task<(IBus, BusLifetimeEvents)>>(() => InitializeBus(
            startAutomatically: startAutomatically,
            key: key,
            configure: configure,
            onCreated: onCreated,
            serviceProvider: serviceProvider,
            isDefaultBus: isDefaultBus
        ));

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
        _cancellationToken = stoppingToken;

        var (bus, _) = await _busInitializer.Value;

        stoppingToken.Register(bus.Dispose);
    }

    async Task<(IBus, BusLifetimeEvents)> InitializeBus(bool startAutomatically, string key,
        Func<RebusConfigurer, IServiceProvider, RebusConfigurer> configure,
        Func<IBus, Task> onCreated, IServiceProvider serviceProvider, bool isDefaultBus)
    {
        var stoppingToken = _cancellationToken ?? CancellationToken.None;
        var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger<RebusBackgroundService>();

        BusLifetimeEvents busLifetimeEventsHack = null;

        var rebusConfigurer = Configure
            .With(new DependencyInjectionHandlerActivator(serviceProvider))
            .Options(o => o.Decorate(c =>
            {
                // snatch events here
                return busLifetimeEventsHack = c.Get<BusLifetimeEvents>();
            }))
            .Options(o => o.Decorate<IPipeline>(context =>
            {
                var pipeline = context.Get<IPipeline>();
                var serviceProviderProviderStep = new ServiceProviderProviderStep(serviceProvider, context);
                return new PipelineStepConcatenator(pipeline)
                    .OnReceive(serviceProviderProviderStep, PipelineAbsolutePosition.Front);
            }));

        var configurer = configure(rebusConfigurer, serviceProvider);

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

        logger?.LogInformation("Successfully created bus instance {busInstance} (isDefaultBus: {flag})", bus, isDefaultBus);

        // stopping the bus here will ensure that we've finished executing all message handlers when the container is disposed
        stoppingToken.Register(() =>
        {
            logger?.LogDebug("Stopping token signaled - disposing bus instance {busInstance}", bus);
            bus.Dispose();
            logger?.LogInformation("Bus instance {busInstance} successfully disposed", bus);
        });

        if (key != null)
        {
            var registry = serviceProvider.GetRequiredService<ServiceProviderBusRegistry>();
            registry.AddBus(bus, starter, key);
            logger?.LogDebug("Bus instance {busInstance} was registered in IBusRegistry with key {key}", bus, key);
        }

        if (onCreated != null)
        {
            logger?.LogDebug("Invoking onCreated callback on bus instance {busInstance}", bus);
            await onCreated(bus);
        }

        if (startAutomatically)
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