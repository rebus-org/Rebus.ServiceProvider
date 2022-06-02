using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Rebus.Config;
using Rebus.Persistence.InMem;
using Rebus.Sagas;
using Rebus.Transport.InMem;

namespace Rebus.ServiceProvider.Tests.Hacks;

[TestFixture]
public class ExposeRebusServiceViaServiceProvider
{
    [Test]
    public async Task CanExposeRebusSagaStorage()
    {
        var services = new ServiceCollection();

        // first, we add a singleton instance with a public setter, that we'll use to transfer Rebus' ISagaStorage instance
        services.AddSingleton(new SagaStorageHolder());

        // next, we configure the container to resolve ISagaStorage by forwarding the call to the holder
        services.AddSingleton<ISagaStorage>(p => p.GetRequiredService<SagaStorageHolder>().SagaStorage);

        // last, we ensure that Rebus' configuration exposes its ISagaStorage instance by cheating the decorator API and snatching it!
        services.AddRebus(
            (configure, provider) => configure
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "doesn't matter"))
                .Sagas(s =>
                {
                    s.StoreInMemory();

                    s.Decorate(c =>
                    {
                        var sagaStorage = c.Get<ISagaStorage>();
                        provider.GetRequiredService<SagaStorageHolder>().SagaStorage = sagaStorage;
                        return sagaStorage;
                    });
                })
        );

        // build the provider like we would normally do
        await using var provider = services.BuildServiceProvider();

        // manually call this, because we're not in a hosting context
        provider.StartRebus();

        // now the container can deliver this:
        var storage = provider.GetRequiredService<ISagaStorage>();

        Assert.That(storage, Is.TypeOf<InMemorySagaStorage>());
        
        // and you can call stuff on it
        var saga = await storage.Find(typeof(MySagaData), "id", Guid.NewGuid());

        Assert.That(saga, Is.Null, "in this case it should be null, because there's no saga data in there");
    }

    class MySagaData : SagaData { }

    class SagaStorageHolder
    {
        public ISagaStorage SagaStorage { get; set; }
    }
}

