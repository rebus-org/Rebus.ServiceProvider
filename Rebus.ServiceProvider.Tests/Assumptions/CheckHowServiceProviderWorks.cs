using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;

namespace Rebus.ServiceProvider.Tests.Assumptions;

[TestFixture]
public class CheckHowServiceProviderWorks : FixtureBase
{
    [Test]
    public void Disposal()
    {
        var services = new ServiceCollection();

        services.AddRebus(c => c.Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "bimse")));
        
        using (var provider = services.BuildServiceProvider())
        {
            provider.UseRebus();
        }
    }
}