using Rebus.Config;
using Rebus.Transport.InMem;

var network = new InMemNetwork();

IHost host = Host
    .CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddRebus(
            configure => configure
                .Transport(t => t.UseInMemoryTransport(network, "bus1"))
        );
    })
    .Build();

await host.RunAsync();
