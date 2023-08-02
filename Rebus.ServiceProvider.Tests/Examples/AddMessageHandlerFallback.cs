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
using Rebus.Handlers;
using Rebus.Retry.Simple;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport;
using Rebus.Transport.InMem;
// ReSharper disable ClassNeverInstantiated.Local
#pragma warning disable CS1998

namespace Rebus.ServiceProvider.Tests.Examples;

[TestFixture]
public class AddMessageHandlerFallback : FixtureBase
{
    [Test]
    public async Task ItWorks()
    {
        var events = new ConcurrentQueue<string>();

        var services = new ServiceCollection();

        services.AddSingleton(events);

        services.AddRebus(
            configure => configure
                .Transport(t => t.UseInMemoryTransport(new(), "whatever"))
                .Options(o =>
                {
                    o.RetryStrategy(secondLevelRetriesEnabled: true);
                    o.MarkAsFallbackHandler<FallbackFailedMessageHandler>();
                })
        );

        services.AddRebusHandler<SomeMessageHandler>();
        services.AddRebusHandler<AnotherMessageHandler>();
        services.AddRebusHandler<FailedAnotherMessageHandler>();
        services.AddRebusHandler<FallbackFailedMessageHandler>();

        await using var provider = services.BuildServiceProvider();

        provider.StartRebus();

        var bus = provider.GetRequiredService<IBus>();

        await bus.SendLocal(new SomeMessage());
        await bus.SendLocal(new AnotherMessage());

        await events.WaitUntil(q => q.Count >= 2);

        // additional time to let additional stuff happen
        await Task.Delay(TimeSpan.FromSeconds(0.2));

        Assert.That(events, Is.EqualTo(new[]
        {
            "SomeMessage dispatched to FALLBACK message handler",
            "AnotherMessage dispatched to failed AnotherMessage message handler"
        }));
    }

    record SomeMessage;

    record AnotherMessage;

    class SomeMessageHandler : IHandleMessages<SomeMessage>
    {
        public Task Handle(SomeMessage message) => throw new AccessViolationException("sorry man");
    }

    class AnotherMessageHandler : IHandleMessages<AnotherMessage>
    {
        public Task Handle(AnotherMessage message) => throw new AccessViolationException("sorry man");
    }

    class FailedAnotherMessageHandler : IHandleMessages<IFailed<AnotherMessage>>
    {
        readonly ConcurrentQueue<string> _events;

        public FailedAnotherMessageHandler(ConcurrentQueue<string> events) => _events = events;

        public async Task Handle(IFailed<AnotherMessage> message) => _events.Enqueue("AnotherMessage dispatched to failed AnotherMessage message handler");
    }

    class FallbackFailedMessageHandler : IHandleMessages<IFailed<object>>
    {
        readonly ConcurrentQueue<string> _events;

        public FallbackFailedMessageHandler(ConcurrentQueue<string> events) => _events = events;

        public async Task Handle(IFailed<object> message) => _events.Enqueue($"{message.Message.GetType().Name} dispatched to FALLBACK message handler");
    }
}

static class FallbackMessageHandlerExtensions
{
    /// <summary>
    /// Marks handlers of type <typeparamref name="THandler"/> as a "fallback handler", which means that it will
    /// only be used in cases where there are no other handlers.
    /// </summary>
    public static void MarkAsFallbackHandler<THandler>(this OptionsConfigurer configurer) where THandler : IHandleMessages
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));

        configurer.Decorate<IHandlerActivator>(c => new HandlerInvokerRemover<THandler>(c.Get<IHandlerActivator>()));
    }

    class HandlerInvokerRemover<THandlerType> : IHandlerActivator
    {
        readonly IHandlerActivator _handlerActivator;

        public HandlerInvokerRemover(IHandlerActivator handlerActivator)
        {
            _handlerActivator = handlerActivator ?? throw new ArgumentNullException(nameof(handlerActivator));
        }

        public async Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message, ITransactionContext transactionContext)
        {
            var handlers = await _handlerActivator.GetHandlers(message, transactionContext);
            var handlersList = handlers.ToList();

            // if there's more than one handler, there's potential for having included the 
            // fallback handler without having the need for a fallback
            if (handlersList.Count > 1)
            {
                handlersList.RemoveAll(h => h is THandlerType);
            }

            return handlersList;
        }
    }
}