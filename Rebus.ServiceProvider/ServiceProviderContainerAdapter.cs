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
    public class ServiceProviderContainerAdapter : IContainerAdapter
    {
        private readonly IServiceCollection _services;

        private HandlerServiceProvider _handlerProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceProviderContainerAdapter"/> class.
        /// </summary>
        /// <param name="services">The ServiceCollection.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public ServiceProviderContainerAdapter(IServiceCollection services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));

            _services.AddTransient(s => MessageContext.Current);
            _services.AddTransient(s => s.GetService<IBus>().Advanced.SyncBus);
        }

        /// <summary>
        /// Gets whether or not the adapter has been activated yet with a service provider.  I.E. Is it ready
        /// to start resolving handler instances.
        /// </summary>
        public bool IsActivated => _handlerProvider != null;

        /// <summary>
        /// Activates the adapter with the supplied service provider.
        /// </summary>
        /// <param name="provider"></param>
        /// <exception cref="System.InvalidOperationException"></exception>
        public void Activate(IServiceProvider provider)
        {
            if (_handlerProvider != null)
            {
                throw new InvalidOperationException("The adapter has already been activated.");
            }

            _handlerProvider = new HandlerServiceProvider(provider);
        }

        /// <summary>
        /// Resolves all handlers for the given <typeparamref name="TMessage"/> message type
        /// </summary>
        /// <exception cref="System.InvalidOperationException"></exception>
        public Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message, ITransactionContext transactionContext)
        {
            if (_handlerProvider == null)
            {
                throw new InvalidOperationException($"Rebus messaging engine has not been activated yet, no messages can be processed.");
            }

            return _handlerProvider.GetHandlers<TMessage>(message, transactionContext);
        }

        /// <summary>
        /// This will either get called too early (the bus is started before the container is created/app started) or
        /// too late (the app has started so the bus cannot be added to service collection). This method doesn't perform any work.
        /// </summary>
        /// <param name="bus"></param>
        public void SetBus(IBus bus)
        {

        }

        private class HandlerServiceProvider : IDisposable
        {
            private readonly IServiceProvider _provider;
            private IBus _bus;

            public HandlerServiceProvider(IServiceProvider provider)
            {
                _provider = provider;

                var applicationLifetime = _provider.GetService<IApplicationLifetime>();

                applicationLifetime?.ApplicationStopping.Register(Dispose);
            }

            private IBus Bus
            {
                get
                {
                    if (_bus == null)
                    {
                        _bus = _provider.GetRequiredService<IBus>();
                    }

                    return _bus;
                }
            }

            public Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message, ITransactionContext transactionContext)
            {
                var resolvedHandlerInstances = GetMessageHandlersForMessage<TMessage>();

                transactionContext.OnDisposed(() =>
                {
                    foreach (var disposableInstance in resolvedHandlerInstances.OfType<IDisposable>())
                    {
                        disposableInstance.Dispose();
                    }
                });

                return Task.FromResult((IEnumerable<IHandleMessages<TMessage>>)resolvedHandlerInstances.ToArray());
            }

            List<IHandleMessages<TMessage>> GetMessageHandlersForMessage<TMessage>()
            {
                var handledMessageTypes = typeof(TMessage).GetBaseTypes()
                    .Concat(new[] { typeof(TMessage) });

                return handledMessageTypes
                    .SelectMany(t =>
                    {
                        var implementedInterface = typeof(IHandleMessages<>).MakeGenericType(t);

                        return _provider.GetServices(implementedInterface).Cast<IHandleMessages>();
                    })
                    .Cast<IHandleMessages<TMessage>>()
                    .ToList();
            }

            #region IDisposable Support
            private bool disposedValue = false;

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

            public void Dispose()
            {
                Dispose(true);
            }
            #endregion
        }
    }
}
