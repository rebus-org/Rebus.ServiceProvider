using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Rebus.Handlers;
using Rebus.Transport;

namespace Rebus.ServiceProvider.Tests;

[TestFixture]
public class CheckResolutionSpeed
{
    /*

Initial:
100000 resolutions took 3,3 s - that's 30014,5 /s
100000 resolutions took 3,3 s - that's 30416,5 /s
100000 resolutions took 3,3 s - that's 30472,4 /s

Cache generic types:
100000 resolutions took 0,9 s - that's 114627,3 /s
100000 resolutions took 0,9 s - that's 115224,8 /s

After updating some package versions:
100000 resolutions took 0,9 s - that's 116519,3 /s

     */
    [TestCase(100000)]
    [TestCase(1000000)]
    [Description("A little bit of a funny test case, because System.String implements SO many things (e.g. IComparable, etc.), which means that the resolver has ~40 types to look up(!)")]
    public async Task JustResolveManyTimes_String(int count)
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddTransient<IHandleMessages<string>>(_ => new StringHandler());

        serviceCollection.AddSingleton(p => new DependencyInjectionHandlerActivator(p));

        await using var provider = serviceCollection.BuildServiceProvider();

        var handlerActivator = provider.GetRequiredService<DependencyInjectionHandlerActivator>();

        var stopwatch = Stopwatch.StartNew();

        for (var counter = 0; counter < count; counter++)
        {
            using var scope = new RebusTransactionScope();

            await handlerActivator.GetHandlers("this is my message", scope.TransactionContext);
        }

        var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;

        Console.WriteLine($"{count} resolutions took {elapsedSeconds:0.0} s - that's {count / elapsedSeconds:0.0} /s");
    }

    [TestCase(100000)]
    [TestCase(1000000)]
    public async Task JustResolveManyTimes_OwnMessageType(int count)
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddTransient<IHandleMessages<SomeMessage>>(_ => new SomeMessageHandler());

        serviceCollection.AddSingleton(p => new DependencyInjectionHandlerActivator(p));

        await using var provider = serviceCollection.BuildServiceProvider();

        var handlerActivator = provider.GetRequiredService<DependencyInjectionHandlerActivator>();

        var stopwatch = Stopwatch.StartNew();

        for (var counter = 0; counter < count; counter++)
        {
            using var scope = new RebusTransactionScope();

            await handlerActivator.GetHandlers(new SomeMessage(), scope.TransactionContext);
        }

        var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;

        Console.WriteLine($"{count} resolutions took {elapsedSeconds:0.0} s - that's {count / elapsedSeconds:0.0} /s");
    }

    record SomeMessage;

    class SomeMessageHandler : IHandleMessages<SomeMessage>
    {
        public Task Handle(SomeMessage message)
        {
            throw new System.NotImplementedException();
        }
    }

    class StringHandler : IHandleMessages<string>
    {
        public Task Handle(string message)
        {
            throw new System.NotImplementedException();
        }
    }
}