using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;

// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable ArgumentsStyleNamedExpression

namespace Rebus.ServiceProvider.Tests;

[TestFixture]
public class VerifyBusLifetimeEventsInContainer : FixtureBase
{
    [Test]
    public void CanAddEventListenersAfterTheFact()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddRebus(configure => configure
            .Logging(l => l.Console(minLevel: LogLevel.Debug))
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "bus lifetime events test")));

        var serviceProvider = serviceCollection.BuildServiceProvider();

        Using(serviceProvider);

        serviceProvider.StartRebus();

        var events = serviceProvider.GetRequiredService<BusLifetimeEvents>();
        var queue = new ConcurrentQueue<string>();

        events.BusDisposing += () => queue.Enqueue("BusDisposing");
        events.WorkersStopped += () => queue.Enqueue("WorkersStopped");
        events.BusDisposed += () => queue.Enqueue("BusDisposed");

        CleanUpDisposables();

        Assert.That(queue, Is.EqualTo(new[]
        {
            "BusDisposing",
            "WorkersStopped",
            "BusDisposed"
        }));
    }
}