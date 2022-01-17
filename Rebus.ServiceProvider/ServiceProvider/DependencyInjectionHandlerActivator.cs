using Microsoft.Extensions.DependencyInjection;
using Rebus.Activation;
using Rebus.Extensions;
using Rebus.Handlers;
using Rebus.Pipeline;
using Rebus.Transport;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Rebus.ServiceProvider.Internals;

// ReSharper disable UnusedMember.Global
#pragma warning disable 1998

namespace Rebus.ServiceProvider;

/// <summary>
/// Implementation of <see cref="IHandlerActivator"/> that is backed by a ServiceProvider
/// </summary>
/// <seealso cref="IHandlerActivator" />
public class DependencyInjectionHandlerActivator : IHandlerActivator
{
    readonly ConcurrentDictionary<Type, Type[]> _typesToResolveByMessage = new();
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

            return GetMessageHandlersForMessage<TMessage>(scope);
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
            transactionContext.OnDisposed(_ => scope.Dispose());
            return stepContext.Save(scope);
        }

        return stepContext.Load<IServiceScope>() ?? CreateAndInitializeNewScope();
    }

    IReadOnlyList<IHandleMessages<TMessage>> GetMessageHandlersForMessage<TMessage>(IServiceScope scope)
    {
        var typesToResolve = _typesToResolveByMessage.GetOrAdd(typeof(TMessage), FigureOutTypesToResolve);

        var serviceProvider = scope.ServiceProvider;

        return typesToResolve
            .SelectMany(type => serviceProvider.GetServices(type).Cast<IHandleMessages>())
            .Distinct(new TypeEqualityComparer())
            .Cast<IHandleMessages<TMessage>>()
            .ToList();
    }

    static Type[] FigureOutTypesToResolve(Type messageType)
    {
        var handledMessageTypes = new HashSet<Type>(messageType.GetBaseTypes()) { messageType };

        var compatibleMessageTypes = new HashSet<Type>();
        foreach (var type in handledMessageTypes)
        {
            compatibleMessageTypes.UnionWith(GetCompatibleMessageHandlerTypes(type));
        }
        handledMessageTypes.UnionWith(compatibleMessageTypes);

        return handledMessageTypes
            .Select(t => typeof(IHandleMessages<>).MakeGenericType(t))
            .ToArray();
    }

    /**
         * Returns all compatible message handler types,
         * which a message with the given type should be dispatched to.
         * Covariant interfaces are taken into account.
         */
    static IEnumerable<Type> GetCompatibleMessageHandlerTypes(Type type)
    {
        if (type.IsGenericType)
        {
            var genericDefinition = type.GetGenericTypeDefinition();
            var combinations = genericDefinition.GetGenericArguments()
                .Zip(type.GetGenericArguments(), GenericTypePair.Create)
                .Select(GetBaseTypes)
                .CartesianProduct();
            return combinations.Select(types => genericDefinition.MakeGenericType(types.ToArray()));
        }

        return type.GetBaseTypes();
    }

    /// <summary>
    ///     Returns the base types that can be constructed from
    ///     the given type pair, taking parameter constraints into account.
    /// </summary>
    static IEnumerable<Type> GetBaseTypes(GenericTypePair typePair)
    {
        IEnumerable<Type> result = new[] { typePair.ActualType };

        if (IsCovariant(typePair.GenericType))
        {
            var parameterConstraints = typePair.GenericType.GetGenericParameterConstraints();
            var validBaseTypes = typePair.ActualType.GetBaseTypes()
                .Where(baseType => SatisfiesParameterConstraints(baseType, parameterConstraints));
            return result.Concat(validBaseTypes);
        }

        return result;
    }

    /// <summary>
    ///     Returns true iff the given type satisfies the given parameter constraints.
    /// </summary>
    static bool SatisfiesParameterConstraints(Type type, IEnumerable<Type> parameterConstraints)
    {
        var implementedTypes = new HashSet<Type>(type.GetBaseTypes()) { type };
        return parameterConstraints.All(constraint => implementedTypes.Contains(constraint));
    }

    /// <summary>
    ///     Returns true iff the given type parameter is covariant.
    /// </summary>
    static bool IsCovariant(Type type)
    {
        return type.GenericParameterAttributes.HasFlag(GenericParameterAttributes.Covariant);
    }

    /// <summary>
    ///     Represents a generic type argument and its corresponding actual type.
    /// </summary>
    class GenericTypePair
    {
        public Type GenericType { get; private set; }
        public Type ActualType { get; private set; }

        public static GenericTypePair Create(Type genericType, Type actualType)
        {
            return new()
            {
                GenericType = genericType,
                ActualType = actualType
            };
        }
    }
}