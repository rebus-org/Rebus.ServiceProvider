using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;

// ReSharper disable ArgumentsStyleLiteral
// ReSharper disable UnusedTypeParameter
// ReSharper disable ClassNeverInstantiated.Local

namespace Rebus.ServiceProvider.Tests;

[TestFixture]
[Description("Experiment using strongly typed keys to manage multiple bus instances")]
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
        services.AddTransient<IBus<SecondaryBus>>(p =>
            new TypedBus<SecondaryBus>(p.GetRequiredService<IBusRegistry>().GetBus("bus2")));

        await using var provider = services.BuildServiceProvider();

        provider.StartRebus();

        var bus1 = provider.GetRequiredService<IBus<MainBus>>();
        var bus2 = provider.GetRequiredService<IBus<SecondaryBus>>();

        Assert.That(bus1.Instance.ToString(), Is.EqualTo("RebusBus mainbus"));
        Assert.That(bus2.Instance.ToString(), Is.EqualTo("RebusBus secondarybus"));
    }

    /// <summary>
    /// Marker interface for a bus key
    /// </summary>
    interface IBusKey { }

    /// <summary>
    /// Define key of main bus
    /// </summary>
    record MainBus : IBusKey;

    /// <summary>
    /// Define key of secondary bus
    /// </summary>
    record SecondaryBus : IBusKey;

    /// <summary>
    /// Define keyed bus instance holder
    /// </summary>
    interface IBus<TKey> where TKey : IBusKey
    {
        IBus Instance { get; }
    }

    /// <summary>
    /// Concrete keyed bus instance holder
    /// </summary>
    class TypedBus<TKey> : IBus<TKey> where TKey : IBusKey
    {
        public TypedBus(IBus bus) => Instance = bus ?? throw new ArgumentNullException(nameof(bus));

        public IBus Instance { get; }
    }
}