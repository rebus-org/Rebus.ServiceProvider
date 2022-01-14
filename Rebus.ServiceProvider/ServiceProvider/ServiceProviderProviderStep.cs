using System;
using System.Threading.Tasks;
using Rebus.Pipeline;

namespace Rebus.ServiceProvider;

/// <summary>
/// Incoming pipeline step that makes the service provider available to the rest of the pipeline
/// </summary>
[StepDocumentation("Adds the service provider to the incoming/outgoing step context, thus making it available for the rest of the pipeline.")]
public class ServiceProviderProviderStep : IIncomingStep, IOutgoingStep
{
    readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Creates the step
    /// </summary>
    public ServiceProviderProviderStep(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    /// <summary>
    /// Saves the service provider in the pipeline's context and invokes the rest of the pipeline
    /// </summary>
    public async Task Process(IncomingStepContext context, Func<Task> next)
    {
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