using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Extensions;
using Rebus.Handlers;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport;
using Rebus.Transport.InMem;

#pragma warning disable CS1998

namespace Rebus.ServiceProvider.Tests.Examples;

[TestFixture]
public class FilterMessageHandlersDependingOnMessageHeaders : FixtureBase
{
    [Test]
    [Description("Demonstrates how IHandlerActivator can be decorated to enable filtering the list of matching handlers before dispatching to them")]
    public async Task CanDoIt()
    {
        var services = new ServiceCollection();
        var logs = new ConcurrentQueue<string>();

        services.AddSingleton(logs);
        services.AddRebusHandler<SomeKindOfHandler>();
        services.AddRebusHandler<AnotherKindOfHandler>();

        services.AddRebus(
            configure => configure
                .Transport(t => t.UseInMemoryTransport(new(), "filter-handlers"))
                .Options(o => o.Decorate<IHandlerActivator>(c => new FilteringHandlerActivator(c.Get<IHandlerActivator>())))
        );

        await using var provider = services.BuildServiceProvider();

        var bus = provider.GetRequiredService<IBus>();

        await bus.SendLocal("HEJ SomeKindOfHandler", new Dictionary<string, string> { ["routing-key"] = "some" });

        await Task.Delay(millisecondsDelay: 100);

        await bus.SendLocal("HEJ AnotherKindOfHandler", new Dictionary<string, string> { ["routing-key"] = "another" });

        await logs.WaitUntil(l => l.Count >= 2);
        // wait an additional short while to allow for additional messages to be processed
        await Task.Delay(millisecondsDelay: 200);

        Assert.That(logs, Is.EqualTo(new[]
        {
            "SomeKindOfHandler: HEJ SomeKindOfHandler",
            "AnotherKindOfHandler: HEJ AnotherKindOfHandler",
        }));
    }

    class SomeKindOfHandler : IHandleMessages<string>
    {
        readonly ConcurrentQueue<string> _logs;

        public SomeKindOfHandler(ConcurrentQueue<string> logs) => _logs = logs;

        public async Task Handle(string message) => _logs.Enqueue($"SomeKindOfHandler: {message}");
    }

    class AnotherKindOfHandler : IHandleMessages<string>
    {
        readonly ConcurrentQueue<string> _logs;

        public AnotherKindOfHandler(ConcurrentQueue<string> logs) => _logs = logs;

        public async Task Handle(string message) => _logs.Enqueue($"AnotherKindOfHandler: {message}");
    }

    class FilteringHandlerActivator : IHandlerActivator
    {
        readonly IHandlerActivator _handlerActivator;

        public FilteringHandlerActivator(IHandlerActivator handlerActivator) => _handlerActivator = handlerActivator;

        public async Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message, ITransactionContext transactionContext)
        {
            var stepContext = transactionContext.Items.GetOrThrow<IncomingStepContext>(StepContext.StepContextKey);
            var transportMessage = stepContext.Load<TransportMessage>() ?? throw new ArgumentException("Could not retrieve TransportMessage from step context");

            var handlers = await _handlerActivator.GetHandlers(message, transactionContext);

            var headers = transportMessage.Headers;
            var routingKey = headers.GetOrDefault("routing-key")?.ToLowerInvariant();

            return routingKey switch
            {
                "some" => handlers.Where(h => h is SomeKindOfHandler),
                "another" => handlers.Where(h => h is AnotherKindOfHandler),
                _ => handlers
            };
        }
    }
}