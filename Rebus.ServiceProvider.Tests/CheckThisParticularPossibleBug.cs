using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Retry.Simple;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;

#pragma warning disable 1998

namespace Rebus.ServiceProvider.Tests;

[TestFixture]
[Description(@"

Try to reproduce issue described here:

    https://github.com/rebus-org/Rebus/issues/830

where, apparently, a message got dispatched twice to the 2nd level handler.

Could not reproduce it though.

")]
public class CheckThisParticularPossibleBug
{
    [Test]
    public async Task ItWorks()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddRebus(configure =>
            configure
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "doesn't matter'"))
                .Options(o => o.RetryStrategy(secondLevelRetriesEnabled: true, maxDeliveryAttempts: 3))
        );

        var callsToSecondLevelHandler = new ConcurrentQueue<string>();

        serviceCollection.AddSingleton(_ => callsToSecondLevelHandler);

        GenerateProductVariantEventHandler GetHandler(IServiceProvider p) => new(
            bus: p.GetRequiredService<IBus>(),
            callsToSecondLevelHandler: p.GetRequiredService<ConcurrentQueue<string>>()
        );

        serviceCollection.AddTransient<IHandleMessages<GenerateProductVariant>>(GetHandler);
        serviceCollection.AddTransient<IHandleMessages<IFailed<GenerateProductVariant>>>(GetHandler);
        //serviceCollection.AddTransient<IHandleMessages<IFailed<GenerateProductVariant>>>(GetHandler);

        await using var provider = serviceCollection.BuildServiceProvider();

        provider.StartRebus();

        var bus = provider.GetRequiredService<IBus>();

        var orderId = Guid.NewGuid().ToString();

        await bus.SendLocal(new GenerateProductVariant("WHO CARES", orderId));

        await callsToSecondLevelHandler.WaitUntil(queue => queue.Count > 0);

        await Task.Delay(TimeSpan.FromSeconds(3));

        Assert.That(callsToSecondLevelHandler.Count, Is.EqualTo(1));
    }

    public class GenerateProductVariantEventHandler :
        IHandleMessages<GenerateProductVariant>,
        IHandleMessages<IFailed<GenerateProductVariant>>
    {
        readonly IBus _bus;
        readonly ConcurrentQueue<string> _callsToSecondLevelHandler;

        public GenerateProductVariantEventHandler(IBus bus, ConcurrentQueue<string> callsToSecondLevelHandler)
        {
            _bus = bus;
            _callsToSecondLevelHandler = callsToSecondLevelHandler;
        }

        public async Task Handle(GenerateProductVariant message)
        {
            throw new ApplicationException(message.ProductVariant);
        }

        public async Task Handle(IFailed<GenerateProductVariant> messageWrapper)
        {
            await _bus.Publish(new ProductVariantGenerationFailed(
                orderId: messageWrapper.Message.OrderId,
                errorDescription: messageWrapper.ErrorDescription
            ));

            _callsToSecondLevelHandler.Enqueue($"called at {DateTime.UtcNow.Ticks} tickerinos");
        }
    }

    public class GenerateProductVariant
    {
        public GenerateProductVariant(string productVariant, string orderId)
        {
            ProductVariant = productVariant;
            OrderId = orderId;
        }

        public string ProductVariant { get; }
        public string OrderId { get; }
    }

    public class ProductVariantGenerationFailed
    {
        public string OrderId { get; }
        public string ErrorDescription { get; }

        public ProductVariantGenerationFailed(string orderId, string errorDescription)
        {
            OrderId = orderId;
            ErrorDescription = errorDescription;
        }
    }
}