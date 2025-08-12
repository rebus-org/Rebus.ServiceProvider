using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Logging;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Rebus.ServiceProvider.Tests.Examples;

[TestFixture]
public class InMemoryPerformanceCheck : FixtureBase
{
    Microsoft.Extensions.DependencyInjection.ServiceProvider provider;

    protected override void SetUp()
    {
        base.SetUp();

        var services = new ServiceCollection();

        services.AddRebus(configure =>
            configure.Transport(t => t.UseInMemoryTransport(new(), "test-queue"))
                .Routing(r => r.TypeBased().Map<PerformanceData>("test-queue")) // Map all messages in the assembly of Program to the queue
                .Logging(l => l.ColoredConsole(LogLevel.Warn))
        );

        services.AddRebusHandler<PerformanceConsumer>();

        services.AddSingleton<PerformanceProducer>();

        provider = services.BuildServiceProvider();
        provider.StartHostedServices();
    }

    protected override void TearDown()
    {
        provider.Dispose();
        base.TearDown();
    }

    [Test]
    [Repeat(10)]
    public async Task RunTheTest()
    {
        var producer = provider.GetRequiredService<PerformanceProducer>();

        var elapsedMilliseconds = await producer.TestPerformance();

        Console.WriteLine($"Perf test execution time: {elapsedMilliseconds} ms");
    }

    public static class PerformanceCounter
    {
        public static int Counter = 10000;
    }

    public class PerformanceProducer(IBus bus)
    {
        public static readonly AutoResetEvent AutoResetEvent = new(initialState: false);

        public async Task<long> TestPerformance()
        {
            var stopwatch = Stopwatch.StartNew();

            var messages = Enumerable.Range(0, PerformanceCounter.Counter)
                .Select(n => new PerformanceData(n));

            foreach (var message in messages)
            {
                await bus.Send(message);
            }

            AutoResetEvent.WaitOne(); // Wait for all messages to be processed

            return stopwatch.ElapsedMilliseconds;
        }
    }

    public class PerformanceConsumer : IHandleMessages<PerformanceData>
    {
        static int counter;

        public async Task Handle(PerformanceData message)
        {
            if (counter != message.Index)
            {
                Console.WriteLine($"Expected message index {counter}, but got {message.Index}");
            }

            var result = Interlocked.Increment(ref counter);

            if (result == PerformanceCounter.Counter)
            {
                Interlocked.Exchange(ref counter, 0); // Reset counter for next performance test
                PerformanceProducer.AutoResetEvent.Set(); // Signal that all messages have been processed
            }
        }
    }

    public record PerformanceData(int Index);
}