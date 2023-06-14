using System;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Pipeline;
using Rebus.Transport;

namespace Rebus.ServiceProvider;

public static class ServiceProviderScopeStepContextExtensions
{
    /// <summary>
    /// Digs out the current <see cref="AsyncServiceScope"/> from Rebus' incoming step context. Is safe to call from an outgoing step,
    /// thus making it possible to access the scope associated with the message currently being handled, simply returning NULL when called outside
    /// of a Rebus handler.
    /// </summary>
    public static AsyncServiceScope? GetAsyncServiceScopeOrNull(this OutgoingStepContext outgoingStepContext)
    {
        if (outgoingStepContext == null) throw new ArgumentNullException(nameof(outgoingStepContext));
        var transactionContext = outgoingStepContext.Load<ITransactionContext>();

        return transactionContext?.Items.TryGetValue(StepContext.StepContextKey, out var value) == true && value is IncomingStepContext incomingStepContext
            ? incomingStepContext.Load<AsyncServiceScope?>()
            : default;
    }

    /// <summary>
    /// Digs out the current <see cref="IServiceScope"/> from Rebus' incoming step context. Is safe to call from an outgoing step,
    /// thus making it possible to access the scope associated with the message currently being handled, simply returning NULL when called outside
    /// of a Rebus handler.
    /// </summary>
    public static IServiceScope GetServiceScopeOrNull(this OutgoingStepContext outgoingStepContext)
    {
        if (outgoingStepContext == null) throw new ArgumentNullException(nameof(outgoingStepContext));
        var transactionContext = outgoingStepContext.Load<ITransactionContext>();

        return transactionContext?.Items.TryGetValue(StepContext.StepContextKey, out var value) == true && value is IncomingStepContext incomingStepContext
            ? incomingStepContext.Load<IServiceScope>()
            : default;
    }

}