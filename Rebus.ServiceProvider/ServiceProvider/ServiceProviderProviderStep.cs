using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Bus;
using Rebus.Injection;
using Rebus.Pipeline;
using Rebus.Transport;

namespace Rebus.ServiceProvider;

/// <summary>
/// Incoming pipeline step that makes the bus instance and the service provider available to the rest of the pipeline
/// </summary>
[StepDocumentation("Adds the service provider to the incoming/outgoing step context, thus making it available for the rest of the pipeline.")]
public class ServiceProviderProviderStep : IIncomingStep, IOutgoingStep
{
    readonly IServiceProvider _serviceProvider;
    readonly Lazy<IBus> _bus;
    readonly bool _injectRootServiceProvider;

    /// <summary>
    /// Creates the step
    /// </summary>
    public ServiceProviderProviderStep(IServiceProvider serviceProvider, IResolutionContext resolutionContext, bool injectRootServiceProvider = true)
    {
        if (resolutionContext == null) throw new ArgumentNullException(nameof(resolutionContext));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _bus = new(resolutionContext.Get<IBus>);
        _injectRootServiceProvider = injectRootServiceProvider;
    }

    /// <summary>
    /// Saves the service provider in the pipeline's context and invokes the rest of the pipeline
    /// </summary>
    public async Task Process(IncomingStepContext context, Func<Task> next)
    {
        if (_injectRootServiceProvider)
        {
            context.Save(_serviceProvider);
        }
        else
        {
            var transactionContext = context.Load<ITransactionContext>();

            var scope = _serviceProvider.CreateScope();
            transactionContext.OnDisposed(_ => scope.Dispose());

            context.Save(scope);
        }
        context.Save(_bus.Value);
        await next();
    }

    /// <summary>
    /// Saves the service provider in the pipeline's context and invokes the rest of the pipeline
    /// </summary>
    public async Task Process(OutgoingStepContext context, Func<Task> next)
    {
        if (_injectRootServiceProvider)
        {
            context.Save(_serviceProvider);
        }
        else
        {
            var transactionContext = context.Load<ITransactionContext>();

            var scope = _serviceProvider.CreateScope();
            transactionContext.OnDisposed(_ => scope.Dispose());

            context.Save(scope);
        }

        context.Save(_bus.Value);
        await next();
    }
}