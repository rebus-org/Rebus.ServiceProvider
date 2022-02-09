using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
using Rebus.ServiceProvider.Tests.Internals;
using Rebus.Transport.InMem;

// ReSharper disable ArgumentsStyleLiteral

namespace Rebus.ServiceProvider.Tests;

[TestFixture]
public class CheckMutlipleBusesAndResolutionByName
{
    [Test]
    public void CanDoIt_CannotRegisterSameKeyTwice()
    {
        var services = new ServiceCollection();

        services.AddRebus(
            configure => configure
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "queue1")),
            key: "bus1"
        );

        services.AddRebus(
            configure => configure
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "queue2")),
            isDefaultBus: false,
            key: "bus1"
        );

        using var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<ArgumentException>(() => provider.StartRebus());

        Console.WriteLine(ex);
    }

    [Test]
    public void CanRetrieveBusesByKey()
    {
        var services = new ServiceCollection();
        var network = new InMemNetwork();

        services.AddRebus(
            configure => configure
                .Transport(t => t.UseInMemoryTransport(network, "queue1")),
            key: "bus1"
        );

        services.AddRebus(
            configure => configure
                .Transport(t => t.UseInMemoryTransport(network, "queue2")),
            isDefaultBus: false,
            key: "bus2"
        );

        using var provider = services.BuildServiceProvider();

        provider.StartRebus();

        var registry = provider.GetRequiredService<IBusRegistry>();

        var bus1 = registry.GetBus("bus1");
        var bus2 = registry.GetBus("bus2");

        Assert.That(bus1.ToString(), Is.Not.EqualTo(bus2.ToString()));
    }

    [Test]
    public void CanDelayStartingTheBus()
    {
        var services = new ServiceCollection().WithTestLogger();
        var network = new InMemNetwork();

        services.AddRebus(
            configure => configure.Transport(t => t.UseInMemoryTransport(network, "queue1")),
            key: "my-nifty-bus-key",
            startAutomatically: false
        );

        using var provider = services.BuildServiceProvider();

        provider.StartRebus();

        var registry = provider.GetRequiredService<IBusRegistry>();

        var bus = registry.GetBus("my-nifty-bus-key");

        Assert.That(bus.Advanced.Workers.Count, Is.EqualTo(0));

        registry.StartBus("my-nifty-bus-key");

        Assert.That(bus.Advanced.Workers.Count, Is.EqualTo(1));
    }

    [Test]
    public void CanGetAllKeys()
    {
        var services = new ServiceCollection();
        var network = new InMemNetwork();

        void AddRebus(ServiceCollection serviceCollection, string key)
        {
            serviceCollection.AddRebus(
                configure => configure.Transport(t => t.UseInMemoryTransport(network, $"queue-for-{key}")),
                key: key,
                startAutomatically: false,
                isDefaultBus: false
            );
        }

        AddRebus(services, "bus1");
        AddRebus(services, "bus2");
        AddRebus(services, "bus3");

        using var provider = services.BuildServiceProvider();

        provider.StartRebus();

        var registry = provider.GetRequiredService<IBusRegistry>();

        var keys = registry.GetAllKeys().OrderBy(k => k);

        Assert.That(keys, Is.EqualTo(new[] {"bus1", "bus2", "bus3"}));
    }

    [Test]
    public void CanTryGet()
    {
        var services = new ServiceCollection();
        var network = new InMemNetwork();

        void AddRebus(ServiceCollection serviceCollection, string key)
        {
            serviceCollection.AddRebus(
                configure => configure.Transport(t => t.UseInMemoryTransport(network, $"queue-for-{key}")),
                key: key,
                isDefaultBus: false
            );
        }

        AddRebus(services, "bus1");

        using var provider = services.BuildServiceProvider();

        provider.StartRebus();

        var registry = provider.GetRequiredService<IBusRegistry>();

        Assert.That(registry.TryGetBus("bus1", out var bus1), Is.True);
        Assert.That(registry.TryGetBus("bus2", out var bus2), Is.False);
        Assert.That(registry.TryGetBus("bus3", out var bus3), Is.False);

        Assert.That(bus1, Is.Not.Null);
        Assert.That(bus2, Is.Null);
        Assert.That(bus3, Is.Null);
    }
}