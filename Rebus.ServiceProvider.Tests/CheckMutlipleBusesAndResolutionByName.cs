using System;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
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
    public void CanDoIt()
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
}