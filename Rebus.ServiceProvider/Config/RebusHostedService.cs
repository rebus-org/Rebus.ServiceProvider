using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rebus.Bus;
using Rebus.Internals;
using Rebus.Pipeline;
using Rebus.ServiceProvider;

namespace Rebus.Config;

class RebusHostedService : BackgroundService
{
    readonly Func<RebusConfigurer, IServiceProvider, RebusConfigurer> _configure;
    readonly IServiceProvider _serviceProvider;
    readonly bool _isDefaultBus;

    public RebusHostedService(Func<RebusConfigurer, IServiceProvider, RebusConfigurer> configure, IServiceProvider serviceProvider, bool isDefaultBus)
    {
        _configure = configure ?? throw new ArgumentNullException(nameof(configure));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _isDefaultBus = isDefaultBus;
    }

    public bool IsStarted { get; private set; }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var loggerFactory = _serviceProvider.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger<RebusHostedService>();

        BusLifetimeEvents busLifetimeEventsHack = null;

        var rebusConfigurer = Configure
            .With(new DependencyInjectionHandlerActivator(_serviceProvider))
            .Logging(l =>
            {
                if (loggerFactory == null) return;

                l.Use(new MicrosoftExtensionsLoggingLoggerFactory(loggerFactory));
            })
            .Options(o => o.Decorate(c =>
            {
                // snatch events here
                return busLifetimeEventsHack = c.Get<BusLifetimeEvents>();
            }))
            .Options(o => o.Decorate<IPipeline>(c => new PipelineStepConcatenator(c.Get<IPipeline>())
                .OnReceive(new ServiceProviderProviderStep(_serviceProvider), PipelineAbsolutePosition.Front)
                .OnReceive(new SetBusInstanceStep(c), PipelineAbsolutePosition.Front)));

        var configurer = _configure(rebusConfigurer, _serviceProvider);

        var starter = configurer.Create();

        if (_isDefaultBus)
        {
            var defaultBusInstance = _serviceProvider.GetRequiredService<DefaultBusInstance>();

            if (defaultBusInstance.Bus != null)
            {
                throw new InvalidOperationException($"Cannot set {starter.Bus} as the default bus instance, as it seems like the bus instance {defaultBusInstance.Bus} was already configured to be it! There can only be one default bus instance in a container instance, so please remember to set isDefaultBus:true in only one of the calls to AddRebus");
            }

            logger?.LogInformation("Setting default bus instance to {busInstance}", starter.Bus);

            defaultBusInstance.Bus = starter.Bus;
            defaultBusInstance.BusLifetimeEvents = busLifetimeEventsHack;
        }

        var bus = starter.Start();

        stoppingToken.Register(() => bus.Dispose());

        IsStarted = true;

        return Task.CompletedTask;
    }
}