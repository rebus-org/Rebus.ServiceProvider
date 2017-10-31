using System;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Rebus.Logging;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;

// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable ArgumentsStyleNamedExpression

namespace Rebus.ServiceProvider.Tests
{
    [TestFixture]
    public class CheckTheApi : FixtureBase
    {
        [Test]
        public void ThisIsHowItWorks()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddRebus(configure => configure
                .Logging(l => l.Console(minLevel: LogLevel.Debug))
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "ioc-text")));

            var serviceProvider = serviceCollection.BuildServiceProvider();

            Using((IDisposable)serviceProvider);

            serviceProvider.UseRebus();
        }
    }
}