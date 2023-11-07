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

class RebusInitializer
{
    public readonly Lazy<Task<(IBus, BusLifetimeEvents)>> _busAndEvents;

    readonly bool _startAutomatically;
    readonly string _key;
    readonly Func<RebusConfigurer, IServiceProvider, RebusConfigurer> _configure;
    readonly Func<IBus, Task> _onCreated;
    readonly IServiceProvider _serviceProvider;
    readonly bool _isDefaultBus;
    readonly CancellationToken? _cancellationToken;

    public RebusInitializer(
        bool startAutomatically,
        string key,
        Func<RebusConfigurer, IServiceProvider, RebusConfigurer> configure,
        Func<IBus, Task> onCreated,
        IServiceProvider serviceProvider,
        bool isDefaultBus,
        IHostApplicationLifetime lifetime)
    {
        _startAutomatically = startAutomatically;
        _key = key;
        _configure = configure;
        _onCreated = onCreated;
        _serviceProvider = serviceProvider;
        _isDefaultBus = isDefaultBus;
        _cancellationToken = lifetime?.ApplicationStopping;

        _busAndEvents = GetLazyInitializer();
    }
    
    public Lazy<Task<(IBus, BusLifetimeEvents)>> GetLazyInitializer()
    {
        return new Lazy<Task<(IBus, BusLifetimeEvents)>>(async () =>
        {
            var stoppingToken = _cancellationToken ?? CancellationToken.None;
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
                        .OnReceive(serviceProviderProviderStep, PipelineAbsolutePosition.Front)
                        .OnSend(serviceProviderProviderStep, PipelineAbsolutePosition.Front);
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

            logger?.LogInformation("Successfully created bus instance {busInstance} (isDefaultBus: {flag})", bus,
                _isDefaultBus);

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
                logger?.LogDebug(
                    "NOT starting bus instance {busInstance}, because it has been configured with startAutomatically:false",
                    bus);
            }

            return (bus, busLifetimeEventsHack);
        });
    }
}