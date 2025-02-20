using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;
#pragma warning disable CS1998

namespace Rebus.ServiceProvider.Tests.Examples;

#if NET8_0_OR_GREATER
[TestFixture]
[Description("Demonstrates how a service key can be used to resolve Rebus handlers")]
public class KeyedServices : FixtureBase
{
    [Test]
    public async Task ItWorks()
    {
        var services = new ServiceCollection();

        var bus1Key = "bus1";
        var bus2Key = "bus2";

        var log = new List<string>();

        services.AddRebusHandler<Handler1>(bus1Key, (serviceProvider, serviceKey) => new((string)serviceKey, log));
        services.AddRebusHandler<Handler2>(bus2Key, (serviceProvider, serviceKey) => new((string)serviceKey, log));

        services.AddRebus(
            configure => configure.Transport(t => t.UseInMemoryTransport(new InMemNetwork(), $"queue-{Guid.NewGuid():N}")),
            isDefaultBus: true, 
            key: bus1Key,
            serviceKey: bus1Key);

        services.AddRebus(
            configure => configure.Transport(t => t.UseInMemoryTransport(new InMemNetwork(), $"queue-{Guid.NewGuid():N}")),
            isDefaultBus: false,
            key: bus2Key,
            serviceKey: bus2Key);

        await using var provider = services.BuildServiceProvider();
        provider.StartRebus();

        var bus1 = provider.GetRequiredService<IBusRegistry>().GetBus(bus1Key);
        var bus2 = provider.GetRequiredService<IBusRegistry>().GetBus(bus2Key);

        await bus1.SendLocal("Hej!");
        await bus2.SendLocal("Hej!");

        await Task.Delay(TimeSpan.FromSeconds(3));

        CollectionAssert.AreEquivalent(new[] { bus1Key, bus2Key }, log);
    }

    class Handler1(string serviceKey, List<string> log) : IHandleMessages<string>
    {
        public async Task Handle(string _) => log.Add(serviceKey);
    }

    class Handler2(string serviceKey, List<string> log) : IHandleMessages<string>
    {
        public async Task Handle(string _) => log.Add(serviceKey);
    }
}
#endif