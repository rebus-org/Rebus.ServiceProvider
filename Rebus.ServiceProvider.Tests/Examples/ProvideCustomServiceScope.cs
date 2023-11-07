using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Pipeline;
using Rebus.Retry.Simple;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;
#pragma warning disable CS1998

namespace Rebus.ServiceProvider.Tests.Examples;

[TestFixture]
[Description("Demonstrates how a user-provided scope can be used to resolve Rebus handlers")]
public class ProvideCustomServiceScope : FixtureBase
{
    [TestCase(1)]
    [TestCase(10)]
    public async Task JustInsertPipelineStepThatCreatesTheScope(int count)
    {
        using var countdown = new CountdownEvent(initialCount: count);

        var services = new ServiceCollection();

        services.AddSingleton(countdown);
        services.AddRebusHandler<MyStringHandler>();
        services.AddRebus(
            (configure, provider) => configure
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "queue-name"))
                .Options(o => o.EnableCustomScope(provider))
        );

        await using var provider = services.BuildServiceProvider();
        provider.StartRebus();

        var bus = provider.GetRequiredService<IBus>();

        var messages = Enumerable.Range(0, countdown.CurrentCount)
            .Select(n => $"MESSAGE NO {n} 🙂")
            .ToList();

        foreach (var msg in messages)
        {
            await bus.SendLocal(msg);
        }

        Assert.That(countdown.Wait(millisecondsTimeout: 1000), Is.True,
            "Countdown event was not signaled in time");

    }

    class MyStringHandler : IHandleMessages<string>
    {
        readonly CountdownEvent _countdown;

        public MyStringHandler(CountdownEvent countdown) => _countdown = countdown;

        public async Task Handle(string message) => _countdown.Signal();
    }
}

/// <summary>
/// This is the code required to implement a user-provider <see cref="IServiceScope"/> with a nifty configuration extension wrapping it
/// </summary>
static class CustomScopeExtensions
{
    public static void EnableCustomScope(this OptionsConfigurer configurer, IServiceProvider serviceProvider)
    {
        configurer.Decorate<IPipeline>(c =>
        {
            var step = new ProvideCustomScopeStep(serviceProvider);
            return new PipelineStepInjector(c.Get<IPipeline>())
                .OnReceive(step, PipelineRelativePosition.After, typeof(DefaultRetryStep));
        });
    }

    [StepDocumentation("Incoming pipeline step that uses the service provider passed to it to create an IServiceScope, which will be stashed in the incoming context for subsequent steps to pick up.")]
    class ProvideCustomScopeStep : IIncomingStep
    {
        readonly IServiceProvider _serviceProvider;

        public ProvideCustomScopeStep(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();

            context.Save<IServiceScope>(scope);

            await next();
        }
    }
}