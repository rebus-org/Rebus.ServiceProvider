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
    public static IHostBuilder AddRebus(this IHostBuilder builder, Func<RebusConfigurer, IServiceProvider, RebusConfigurer> configure, Action<IServiceCollection> configureServices)
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

    public static void AddRebusNew(this IServiceCollection services, Func<RebusConfigurer, RebusConfigurer> configure, bool isDefaultBus = false)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        AddRebusNew(services, (configurer, _) => configure(configurer), isDefaultBus: isDefaultBus);
    }

    public static void AddRebusNew(this IServiceCollection services, Func<RebusConfigurer, IServiceProvider, RebusConfigurer> configure, bool isDefaultBus = false)
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
        : base(configure, new(() => BuildServiceProvider(configureServices)), disposeServiceProvider: true)
        {
        }

        static IServiceProvider BuildServiceProvider(Action<IServiceCollection> configureServices)
        {
            var services = new ServiceCollection();

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