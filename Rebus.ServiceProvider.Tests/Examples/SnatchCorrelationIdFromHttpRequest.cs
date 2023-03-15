using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Pipeline.Send;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable UnassignedGetOnlyAutoProperty
// ReSharper disable CollectionNeverUpdated.Local

namespace Rebus.ServiceProvider.Tests.Examples;

[TestFixture]
[Description("Demonstrates how an outgoing step can be used to attach the correlation ID of the current HTTP request to all outgoing messages")]
public class SnatchCorrelationIdFromHttpRequest : FixtureBase
{
    [Test]
    [Explicit]
    public async Task CanSnatchIt()
    {
        var services = new ServiceCollection();

        services.AddTransient<IHttpContextAccessor>();

        services.AddRebus(
            (configure, provider) => configure
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "doesn't matter"))
                .Options(o => o.AutomaticallyAddCorrelationId(provider))
        );

        await using var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<IBus>().SendLocal("hej");
    }

}

static class CorrelationIdConfigurationExtensions
{
    public static void AutomaticallyAddCorrelationId(this OptionsConfigurer configurer, IServiceProvider serviceProvider, string httpRequestHeaderKey = "CorrelationId")
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));
        if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));

        configurer.Decorate<IPipeline>(c =>
        {
            var pipeline = c.Get<IPipeline>();
            var step = new SetCorrelationIdOutgoingStep(serviceProvider, httpRequestHeaderKey);

            return new PipelineStepInjector(pipeline)
                .OnSend(step, PipelineRelativePosition.Before, typeof(SerializeOutgoingMessageStep));
        });
    }

    [StepDocumentation("Snatches the correlation ID from the current web request and attaches it to outgoing messages")]
    class SetCorrelationIdOutgoingStep : IOutgoingStep
    {
        readonly IServiceProvider _serviceProvider;
        readonly string _httpRequestHeaderKey;

        public SetCorrelationIdOutgoingStep(IServiceProvider serviceProvider, string httpRequestHeaderKey)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _httpRequestHeaderKey = httpRequestHeaderKey ?? throw new ArgumentNullException(nameof(httpRequestHeaderKey));
        }

        public async Task Process(OutgoingStepContext context, Func<Task> next)
        {
            var httpContextAccessor = _serviceProvider.GetRequiredService<IHttpContextAccessor>();
            var httpContext = httpContextAccessor.HttpContext;
            var correlationId = httpContext?.Request.Headers[_httpRequestHeaderKey].FirstOrDefault();

            if (correlationId != null)
            {
                var message = context.Load<Message>();

                message.Headers[Headers.CorrelationId] = correlationId;
            }

            await next();
        }
    }
}

/// <summary>
/// Don't want to bring in AspNetCore nuggies for this :)
/// </summary>
class IHttpContextAccessor
{
    public HttpContext HttpContext { get; } = new();
}

class HttpContext
{
    public HttpRequest Request { get; } = new();
}

class HttpRequest
{
    public Dictionary<string, IEnumerable<string>> Headers { get; } = new() { ["CorrelationId"] = new[] { "known-id" } };
}
