using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Handlers;
using Rebus.Transport;

namespace Rebus.ServiceProvider
{
    /// <summary>
    /// Implementation of <see cref="IContainerAdapter"/> that is backed by a ServiceProvider
    /// </summary>
    /// <seealso cref="Rebus.Activation.IContainerAdapter" />
    public class NetCoreServiceProviderContainerAdapter : IContainerAdapter
    {
        readonly IServiceProvider _provider;

        IBus _bus;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetCoreServiceProviderContainerAdapter"/> class.
        /// </summary>
        /// <param name="provider">The service provider used to yield handler instances.</param>
        public NetCoreServiceProviderContainerAdapter(IServiceProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>
        /// Resolves all handlers for the given <typeparamref name="TMessage"/> message type
        /// </summary>
        /// <exception cref="System.InvalidOperationException"></exception>
        public Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message, ITransactionContext transactionContext)
        {
            var scope = _provider.CreateScope();

            var resolvedHandlerInstances = GetMessageHandlersForMessage<TMessage>(scope);

            transactionContext.OnDisposed(scope.Dispose);

            return Task.FromResult((IEnumerable<IHandleMessages<TMessage>>)resolvedHandlerInstances.ToArray());
        }

        /// <summary>
        /// Sets the bus instance associated with this <see cref="T:Rebus.Activation.IContainerAdapter" />.
        /// </summary>
        /// <param name="bus"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void SetBus(IBus bus)
        {
            if (_bus != null)
            {
                throw new InvalidOperationException("Cannot set the bus instance more than once on the container adapter.");
            }

            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        private List<IHandleMessages<TMessage>> GetMessageHandlersForMessage<TMessage>(IServiceScope scope)
        {
            var handledMessageTypes = typeof(TMessage).GetBaseTypes()
                .Concat(new[] { typeof(TMessage) });

            return handledMessageTypes
                .SelectMany(t =>
                {
                    var implementedInterface = typeof(IHandleMessages<>).MakeGenericType(t);

                    return scope.ServiceProvider.GetServices(implementedInterface).Cast<IHandleMessages>();
                })
                .Cast<IHandleMessages<TMessage>>()
                .ToList();
        }

        #region IDisposable Support

        private bool disposedValue = false;

        /// <summary>
        /// Disposes of the bus.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _bus?.Dispose();
                }

                disposedValue = true;
            }
        }

        /// <summary>
        /// Disposes of the bus.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
