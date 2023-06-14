using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Pipeline.Send;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;

namespace Rebus.ServiceProvider.Tests.Examples;

[TestFixture]
public class DigOutServiceProviderScopeInOutgoingStep : FixtureBase
{
    [Test]
    public async Task CanDigItOut()
    {
        var network = new InMemNetwork();
        network.CreateQueue("anotherqueue");

        var services = new ServiceCollection();

        services.AddRebus(
            configure => configure
                .Transport(t => t.UseInMemoryTransport(network, "mainqueue"))
                .Routing(t => t.TypeBased().Map<OutgoingMessage>("anotherqueue"))
                .Options(o => o.Decorate<IPipeline>(c => new PipelineStepInjector(c.Get<IPipeline>()).OnSend(new DigScopeOutOutgoingStep(), PipelineRelativePosition.Before, typeof(SerializeOutgoingMessageStep))))
        );

        services.AddRebusHandler<MyStringHandler>();

        await using var provider = services.BuildServiceProvider();

        var bus = provider.GetRequiredService<IBus>();

        await bus.Send(new OutgoingMessage());
        var sentOutsideScope = await network.WaitForNextMessageFrom("anotherqueue");

        await bus.SendLocal("send new OutgoingMessage() do");
        var sentInsideScope = await network.WaitForNextMessageFrom("anotherqueue");

        Assert.That(sentOutsideScope.Headers, Does.Not.ContainKey("got-the-scope"));
        Assert.That(sentInsideScope.Headers, Does.ContainKey("got-the-scope").And.ContainValue("yes"));
    }

    class DigScopeOutOutgoingStep : IOutgoingStep
    {
        public async Task Process(OutgoingStepContext context, Func<Task> next)
        {
            if (context.GetAsyncServiceScopeOrNull() != null)
            {
                context.Load<Message>().Headers["got-the-scope"] = "yes";
            }

            if (context.GetServiceScopeOrNull() != null)
            {
                context.Load<Message>().Headers["got-the-scope"] = "yes";
            }

            await next();
        }
    }

    record OutgoingMessage;

    class MyStringHandler : IHandleMessages<string>
    {
        readonly IBus _bus;

        public MyStringHandler(IBus bus) => _bus = bus;

        public async Task Handle(string message) => await _bus.Send(new OutgoingMessage());
    }
}