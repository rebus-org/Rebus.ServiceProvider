using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;
// ReSharper disable ArgumentsStyleLiteral

namespace Rebus.ServiceProvider.Tests;

[TestFixture]
public class TypedBusRegistrationsCheck : FixtureBase
{
    [Test]
    public async Task ItWorks()
    {
        var services = new ServiceCollection();

        services.AddRebus(
            configure => configure
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "bus1"))
                .Options(o => o.SetBusName("mainbus")),

            isDefaultBus: true
        );

        services.AddRebus(
            configure => configure
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "bus2"))
                .Options(o => o.SetBusName("secondarybus")),

            isDefaultBus: false,
            key: "bus2"
        );

        services.AddTransient<IBus<MainBus>>(p => new TypedBus<MainBus>(p.GetRequiredService<IBus>()));
        services.AddTransient<IBus<SecondaryBus>>(p => new TypedBus<SecondaryBus>(p.GetRequiredService<IBusRegistry>().GetBus("bus2")));

        await using var provider = services.BuildServiceProvider();

        var bus1 = provider.GetRequiredService<IBus<MainBus>>();
        var bus2 = provider.GetRequiredService<IBus<SecondaryBus>>();

        Assert.That(bus1.ToString(), Is.EqualTo("mainbus"));
        Assert.That(bus2.ToString(), Is.EqualTo("secondarybus"));
    }

    record MainBus : IBusKey;

    record SecondaryBus : IBusKey;

    interface IBus<TKey> where TKey : IBusKey
    {
        IBus Bus { get; }
    }

    interface IBusKey
    {
    }

    class TypedBus<TKey> : IBus<TKey> where TKey : IBusKey
    {
        public TypedBus(IBus bus) => Bus = bus ?? throw new ArgumentNullException(nameof(bus));

        public IBus Bus { get; }
    }
}

