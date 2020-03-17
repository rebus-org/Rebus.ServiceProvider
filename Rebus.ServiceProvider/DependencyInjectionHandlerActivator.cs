using Microsoft.Extensions.DependencyInjection;
using Rebus.Activation;
using Rebus.Extensions;
using Rebus.Handlers;
using Rebus.Pipeline;
using Rebus.Retry.Simple;
using Rebus.Transport;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
// ReSharper disable UnusedMember.Global
#pragma warning disable 1998

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
        public DependencyInjectionHandlerActivator(IServiceProvider provider) => _provider = provider ?? throw new ArgumentNullException(nameof(provider));

        /// <summary>
        /// Resolves all handlers for the given <typeparamref name="TMessage"/> message type
        /// </summary>
        public async Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message, ITransactionContext transactionContext)
        {
            try
            {
                var scope = GetOrCreateScope(transactionContext);

                var resolvedHandlerInstances = GetMessageHandlersForMessage<TMessage>(scope);

                return resolvedHandlerInstances.ToArray();
            }
            catch (ObjectDisposedException exception)
            {
                throw new OperationCanceledException("Handler resolution aborted, because the bus is shutting down", exception);
            }
        }

        IServiceScope GetOrCreateScope(ITransactionContext transactionContext)
        {
            var stepContext = transactionContext.GetOrNull<IncomingStepContext>(StepContext.StepContextKey);

            // can't think of any situations when there would NOT be an incoming step context in the transaction context, except in tests.... so...
            if (stepContext == null) return _provider.CreateScope();

            IServiceScope CreateAndInitializeNewScope()
            {
                var scope = _provider.CreateScope();
                transactionContext.OnDisposed(ctx => scope.Dispose());
                return stepContext.Save(scope);
            }

            return stepContext.Load<IServiceScope>() ?? CreateAndInitializeNewScope();
        }

        List<IHandleMessages<TMessage>> GetMessageHandlersForMessage<TMessage>(IServiceScope scope)
        {
            var typesToResolve = _typesToResolveByMessage.GetOrAdd(typeof(TMessage), FigureOutTypesToResolve);

            return typesToResolve
                .SelectMany(type => scope.ServiceProvider.GetServices(type).Cast<IHandleMessages>())
                .Distinct(new TypeEqualityComparer())
                .Cast<IHandleMessages<TMessage>>()
                .ToList();
        }

        static Type[] FigureOutTypesToResolve(Type messageType)
        {
            IEnumerable<Type> handledMessageTypes;

            if (messageType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IFailed<>)))
            {
                var actualMessageType = messageType.GetGenericArguments()[0];
                handledMessageTypes = new[] { actualMessageType }.Concat(actualMessageType.GetBaseTypes()).Select(t => typeof(IFailed<>).MakeGenericType(t));
            }
            else
            {
                handledMessageTypes = new[] { messageType }.Concat(messageType.GetBaseTypes());
            }

            return handledMessageTypes
                .Select(t => typeof(IHandleMessages<>).MakeGenericType(t))
                .ToArray();
        }
    }
}
