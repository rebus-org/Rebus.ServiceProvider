using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;

// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable ArgumentsStyleNamedExpression

namespace Rebus.ServiceProvider.Tests;

[TestFixture]
public class CheckTheApi : FixtureBase
{
    [Test]
    public async Task ThisIsHowItWorks()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddRebus(configure => configure
            .Logging(l => l.Console(minLevel: LogLevel.Debug))
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "ioc-test")));

        var serviceProvider = serviceCollection.BuildServiceProvider();

        Using(serviceProvider);

        serviceProvider.StartRebus();

        await Task.Delay(TimeSpan.FromSeconds(2));
    }
}