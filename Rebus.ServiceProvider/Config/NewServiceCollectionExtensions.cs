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

// ReSharper disable ArrangeModifiersOrder
// ReSharper disable SimplifyLinqExpressionUseAll

namespace Rebus.Config;

public static class NewServiceCollectionExtensions
{
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

        services.AddSingleton<IHostedService>(p => new RebusHostedService(configure, p));

        if (!services.Any(s => s.ImplementationType == typeof(RebusResolver)))
        {
            services.AddSingleton(new RebusResolver());
            services.AddTransient(p => p.GetRequiredService<RebusResolver>().GetBus(p));
        }

        if (isDefaultBus)
        {

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

    class RebusHostedService : BackgroundService
    {
        readonly Func<RebusConfigurer, IServiceProvider, RebusConfigurer> _configure;
        readonly IServiceProvider _serviceProvider;

        public RebusHostedService(Func<RebusConfigurer, IServiceProvider, RebusConfigurer> configure, IServiceProvider serviceProvider)
        {
            _configure = configure;
            _serviceProvider = serviceProvider;
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var rebusConfigurer = Configure.With(new DependencyInjectionHandlerActivator(_serviceProvider))
                .Options(o => o.Decorate<IPipeline>(c =>
                {
                    var pipeline = c.Get<IPipeline>();
                    return new PipelineStepConcatenator(pipeline)
                        .OnReceive(new ServiceProviderProviderStep(_serviceProvider), PipelineAbsolutePosition.Front)
                        .OnReceive(new SetBusInstanceStep(c), PipelineAbsolutePosition.Front);
                }));

            var configurer = _configure(rebusConfigurer, _serviceProvider);

            var starter = configurer.Create();
            var bus = starter.Start();

            stoppingToken.Register(() => bus.Dispose());
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