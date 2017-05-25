using Microsoft.Extensions.DependencyInjection;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.ServiceProvider;
using Rebus.Transport.InMem;

namespace Sample.ConsoleApp
{
    public class Program
    {
        static void Main(string[] args)
        {
            // 1. Perform service registrations
            var services = new ServiceCollection();
            services.AutoRegisterHandlersFromAssemblyOf<Handler1>();
            services.AddSingleton<Producer>();

            // 2. Configure and initialise Rebus... it's now listening & handling messages!
            var adapter = new NetCoreServiceCollectionContainerAdapter(services);
            Configure.With(adapter)
                .Logging(l => l.ColoredConsole())
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "Messages"))
                .Routing(r => r.TypeBased().MapAssemblyOf<Message1>("Messages"))
                .Start();

            // 3. Potentially add more service registrations for the application, some of which
            //    could be required by handlers.

            // 4. Application starting...
            var provider = services.BuildServiceProvider();

            // 5. Application started...
            var producer = provider.GetRequiredService<Producer>();
            producer.Produce();
        }
    }
}