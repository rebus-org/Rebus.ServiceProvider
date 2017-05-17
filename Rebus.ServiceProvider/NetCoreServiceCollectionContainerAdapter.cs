using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Handlers;
using Rebus.Pipeline;
using Rebus.Transport;
using Microsoft.AspNetCore.Hosting;

namespace Rebus.ServiceProvider
{
    /// <summary>
    /// Implementation of <see cref="IContainerAdapter"/> that is backed by a ServiceProvider
    /// </summary>
    /// <seealso cref="Rebus.Activation.IContainerAdapter" />
    public class NetCoreServiceCollectionContainerAdapter : IContainerAdapter
    {
        readonly IServiceCollection _services;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetCoreServiceCollectionContainerAdapter"/> class.
        /// </summary>
        /// <param name="services">The ServiceCollection.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public NetCoreServiceCollectionContainerAdapter(IServiceCollection services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));

            var serviceProvider = _services.BuildServiceProvider();
            var applicationLifetime = serviceProvider.GetService<IApplicationLifetime>();

            applicationLifetime?.ApplicationStopping.Register(DisposeBus);
        }

        /// <summary>
        /// Resolves all handlers for the given <typeparamref name="TMessage"/> message type
        /// </summary>
        public Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message, ITransactionContext transactionContext)
        {
            var resolvedHandlerInstances = GetAllHandlersInstances<TMessage>();

            transactionContext.OnDisposed(() =>
            {
                foreach (var disposableInstance in resolvedHandlerInstances.OfType<IDisposable>())
                {
                    disposableInstance.Dispose();
                }
            });

            return Task.FromResult((IEnumerable<IHandleMessages<TMessage>>)resolvedHandlerInstances);
        }

        /// <summary>
        /// Sets the bus instance that this <see cref="T:Rebus.Activation.IContainerAdapter" /> should be able to inject when resolving handler instances
        /// </summary>
        public void SetBus(IBus bus)
        {
            if (_services.Any(s => s.ServiceType == typeof(IBus)))
            {
                throw new InvalidOperationException("An IBus instance has already been registered. Please use multiple container instances if you want to host multiple Rebus instances in a single process.");
            }

            _services.AddSingleton(bus);
            _services.AddTransient(s => MessageContext.Current);
            _services.AddTransient(s => s.GetService<IBus>().Advanced.SyncBus);
        }

        void DisposeBus()
        {
            var serviceProvider = _services.BuildServiceProvider();
            var bus = serviceProvider.GetService<IBus>();

            bus.Dispose();
        }

        List<IHandleMessages<TMessage>> GetAllHandlersInstances<TMessage>()
        {
            var container = _services.BuildServiceProvider();

            var handledMessageTypes = typeof(TMessage).GetBaseTypes()
                .Concat(new[] { typeof(TMessage) });

            return handledMessageTypes
                .SelectMany(t =>
                {
                    var implementedInterface = typeof(IHandleMessages<>).MakeGenericType(t);

                    return container.GetServices(implementedInterface).Cast<IHandleMessages>();
                })
                .Cast<IHandleMessages<TMessage>>()
                .ToList();
        }
    }
}
