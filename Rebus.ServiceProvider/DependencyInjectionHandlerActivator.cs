using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Activation;
using Rebus.Extensions;
using Rebus.Handlers;
using Rebus.Transport;
// ReSharper disable UnusedMember.Global

namespace Rebus.ServiceProvider
{
    /// <summary>
    /// Implementation of <see cref="IHandlerActivator"/> that is backed by a ServiceProvider
    /// </summary>
    /// <seealso cref="IHandlerActivator" />
    public class DependencyInjectionHandlerActivator : IHandlerActivator
    {
        readonly ConcurrentDictionary<Type, Type[]> _typesToResolveByMessage = new ConcurrentDictionary<Type, Type[]>();
        readonly IServiceProvider _provider;

        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyInjectionHandlerActivator"/> class.
        /// </summary>
        /// <param name="provider">The service provider used to yield handler instances.</param>
        public DependencyInjectionHandlerActivator(IServiceProvider provider)
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

        List<IHandleMessages<TMessage>> GetMessageHandlersForMessage<TMessage>(IServiceScope scope)
        {
            var typesToResolve = _typesToResolveByMessage.GetOrAdd(typeof(TMessage), FigureOutTypesToResolve);

            return typesToResolve
                .SelectMany(type => scope.ServiceProvider.GetServices(type).Cast<IHandleMessages>())
                .Cast<IHandleMessages<TMessage>>()
                .ToList();
        }

        static Type[] FigureOutTypesToResolve(Type messageType)
        {
            var handledMessageTypes = new[] {messageType}.Concat(messageType.GetBaseTypes());

            return handledMessageTypes
                .Select(t => typeof(IHandleMessages<>).MakeGenericType(t))
                .ToArray();
        }
    }
}
