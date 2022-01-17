using System;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Injection;
using Rebus.Pipeline;

namespace Rebus.ServiceProvider;

/// <summary>
/// Incoming pipeline step that makes the bus instance and the service provider available to the rest of the pipeline
/// </summary>
[StepDocumentation("Adds the service provider to the incoming/outgoing step context, thus making it available for the rest of the pipeline.")]
public class ServiceProviderProviderStep : IIncomingStep, IOutgoingStep
{
    readonly IServiceProvider _serviceProvider;
    readonly Lazy<IBus> _bus;

    /// <summary>
    /// Creates the step
    /// </summary>
    public ServiceProviderProviderStep(IServiceProvider serviceProvider, IResolutionContext resolutionContext)
    {
        if (resolutionContext == null) throw new ArgumentNullException(nameof(resolutionContext));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _bus = new(resolutionContext.Get<IBus>);
    }

    /// <summary>
    /// Saves the service provider in the pipeline's context and invokes the rest of the pipeline
    /// </summary>
    public async Task Process(IncomingStepContext context, Func<Task> next)
    {
        context.Save(_bus.Value);
        context.Save(_serviceProvider);
        await next();
    }

    /// <summary>
    /// Saves the service provider in the pipeline's context and invokes the rest of the pipeline
    /// </summary>
    public async Task Process(OutgoingStepContext context, Func<Task> next)
    {
        context.Save(_serviceProvider);
        await next();
    }
}