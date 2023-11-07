using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;

namespace Rebus.ServiceProvider.Tests.Bugs;

[TestFixture]
public class HostedServiceAndOneWayClient : FixtureBase
{
    [Test]
    [Description(@"Demonstrates how Rebus can be registered and used as a one-way client in Azure Functions. 

Azure Functions have a problem, because they throw an exception if they find an IHostedService registration in the container.

This becomes Rebus' problem, because Rebus has no way of knowing if your Rebus instance is a one-way client during the registration phase, and so it's not possible to skip the IHostedService registration when it is.

Therefore, you can simply let Rebus do its things, and then you can remove all IHostedService registrations from the container afterwards.")]
    public async Task CanRegisterAndUseBusWithoutHavingHostedServiceRegistrationInContainer()
    {
        var network = new InMemNetwork();

        network.CreateQueue("some-queue");

        var services = new ServiceCollection();

        services.AddRebus(configure => configure.Transport(t => t.UseInMemoryTransportAsOneWayClient(network)));

        // here's the crucial part: leave no IHostedService registrations behind
        services
            .Where(s => s.ServiceType == typeof(IHostedService) && s.ImplementationFactory?.Method.ToString().Contains("AddRebus") == true)
            .ToList()
            .ForEach(s => services.Remove(s));

        await using var provider = services.BuildServiceProvider();

        var bus = provider.GetRequiredService<IBus>();

        await bus.Advanced.Routing.Send("some-queue", new { Text = "hello there!" });
    }
}