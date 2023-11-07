using Microsoft.Extensions.DependencyInjection;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.Transport.InMem;

namespace Sample.ConsoleApp;

public class Program
{
    static void Main(string[] args)
    {
        // 1. Service registration pipeline...
        var services = new ServiceCollection();
        services.AutoRegisterHandlersFromAssemblyOf<Handler1>();
        services.AddSingleton<Producer>();

        // 1.1. Configure Rebus
        services.AddRebus(configure => configure
            .Logging(l => l.ColoredConsole())
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "Messages"))
            .Routing(r => r.TypeBased().MapAssemblyOf<Message1>("Messages")));

        // 1.2. Potentially add more service registrations for the application, some of which
        //      could be required by handlers.

        // 2. Application starting pipeline...
        // Make sure we correctly dispose of the provider (and therefore the bus) on application shutdown
        using (var provider = services.BuildServiceProvider())
        {
            // 3. Application started pipeline...

            // 3.1. Now application is running, lets trigger the 'start' of Rebus.
            // PLEASE NOTE THAT StartRebus SHOULD ONLY BE CALLED WHEN YOU'RE BUILDING THE SERVICE PROVIDER YOURSELF
            provider.StartRebus();
                
            // 3.2. Begin the domain work for the application
            var producer = provider.GetRequiredService<Producer>();
            producer.Produce();
        }
    }
}