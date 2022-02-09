using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;

// ReSharper disable AccessToDisposedClosure
#pragma warning disable CS1998

namespace Rebus.ServiceProvider.Tests.Bugs;

[TestFixture]
public class SendAndReceiveBunchOfMessages : FixtureBase
{
    [TestCase(10)]
    public async Task ItWorks(int count)
    {
        using var counter = new SharedCounter(initialValue: count);

        var services = new ServiceCollection();

        services
            .AddRebus(
                configure => configure
                    .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "who-cares"))
            )
            .AddRebusHandler<DecrementCounterHandler>()
            .AddSingleton(counter);

        await using var provider = services.BuildServiceProvider();

        provider.StartRebus();

        await Task.WhenAll(Enumerable.Range(0, count)
            .Select(async _ =>
            {
                using var scope = provider.CreateScope();

                var bus = scope.ServiceProvider.GetRequiredService<IBus>();

                await bus.SendLocal(new DecrementCounter());
            }));

        counter.WaitForResetEvent(timeoutSeconds: 2);
    }

    record DecrementCounter;

    class DecrementCounterHandler : IHandleMessages<DecrementCounter>
    {
        readonly SharedCounter _counter;

        public DecrementCounterHandler(SharedCounter counter) => _counter = counter;

        public async Task Handle(DecrementCounter message) => _counter.Decrement();
    }
}