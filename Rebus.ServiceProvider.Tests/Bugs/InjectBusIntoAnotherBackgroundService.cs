using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArgumentsStyleLiteral

namespace Rebus.ServiceProvider.Tests.Bugs;

[TestFixture]
public class InjectBusIntoAnotherBackgroundService : FixtureBase
{
    [Test]
    public async Task CanDoIt()
    {
        using var otherBackgroundServiceWasStarted = new ManualResetEvent(initialState: false);

        var services = new ServiceCollection();

        services.AddRebus(
            configure => configure
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "bim"))
        );
        services.AddHostedService<TestService>();
        services.AddSingleton(otherBackgroundServiceWasStarted);

        await using var provider = services.BuildServiceProvider();

        provider.StartRebus();

        otherBackgroundServiceWasStarted.WaitOrDie(timeout: TimeSpan.FromSeconds(5));
    }

    class TestService : BackgroundService
    {
        readonly ManualResetEvent _wasExecuted;

        public TestService(IBus bus, ManualResetEvent wasExecuted) => _wasExecuted = wasExecuted;

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _wasExecuted.Set();
            return Task.CompletedTask;
        }
    }
}