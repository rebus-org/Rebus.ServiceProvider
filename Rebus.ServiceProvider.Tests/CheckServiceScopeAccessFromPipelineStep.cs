using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;

// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable ArgumentsStyleLiteral
#pragma warning disable 1998

namespace Rebus.ServiceProvider.Tests;

[TestFixture]
public class CheckServiceScopeAccessFromPipelineStep : FixtureBase
{
    [Test]
    public async Task ItWorksLikeThis()
    {
        var stringReceived = new ManualResetEvent(initialState: false);

        var services = new ServiceCollection();

        services.AddRebus(
            configure => configure
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "who-cares"))
                .Options(o => o.EnableMyCustomStep())
        );

        services.AddRebusHandler(_ => new StringHandler(stringReceived));

        var serviceProvider = Using(services.BuildServiceProvider());

        serviceProvider.StartRebus();

        var bus = serviceProvider.GetRequiredService<IBus>();

        await bus.SendLocal("hej med dig min ven!");

        stringReceived.WaitOrDie(TimeSpan.FromSeconds(3));
    }
}

class StringHandler : IHandleMessages<string>
{
    readonly ManualResetEvent _stringReceived;

    public StringHandler(ManualResetEvent stringReceived) => _stringReceived = stringReceived;

    public async Task Handle(string message) => _stringReceived.Set();
}

static class MyCustomStepExtensions
{
    public static void EnableMyCustomStep(this OptionsConfigurer configurer)
    {
        configurer.Decorate<IPipeline>(c =>
        {
            var pipeline = c.Get<IPipeline>();
            var step = new MyCustomStep();

            return new PipelineStepInjector(pipeline)
                .OnReceive(step, PipelineRelativePosition.After, typeof(ActivateHandlersStep));
        });
    }

    class MyCustomStep : IIncomingStep
    {
        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            // this is the global service provider - we can load stuff from it
            _ = context.Load<IServiceProvider>();

            // or we can create a scope and load stuff from that
            _ = context.Load<IServiceScope>();

            await next();
        }
    }
}