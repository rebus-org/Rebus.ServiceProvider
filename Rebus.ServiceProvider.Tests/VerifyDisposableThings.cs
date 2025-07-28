using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
// ReSharper disable AccessToDisposedClosure

#pragma warning disable CS1998

namespace Rebus.ServiceProvider.Tests;

[TestFixture]
public class VerifyDisposableThings : FixtureBase
{
    [Test]
    public async Task WorksAsItShould()
    {
        var services = new ServiceCollection();

        services.AddRebus(configure => configure.Transport(t => t.UseInMemoryTransport(new(), "doesn't matter")));

        using var disposableWasDisposed = new ManualResetEvent(initialState: false);
        using var asyncDisposableWasDisposed = new ManualResetEvent(initialState: false);

        services.AddTransient(_ => new SomethingDisposable(disposableWasDisposed));
        services.AddTransient(_ => new SomethingAsyncDisposable(asyncDisposableWasDisposed));

        services.AddRebusHandler<StringHandler>();

        await using var provider = services.BuildServiceProvider();

        provider.StartRebus();

        var bus = provider.GetRequiredService<IBus>();

        await bus.SendLocal("HEJ SØDE VEN 🙂");

        disposableWasDisposed.WaitOrDie(TimeSpan.FromSeconds(5));
        asyncDisposableWasDisposed.WaitOrDie(TimeSpan.FromSeconds(5));
    }

    class StringHandler(SomethingDisposable _, SomethingAsyncDisposable __) : IHandleMessages<string>
    {
        public async Task Handle(string message)
        {
        }
    }

    record SomethingDisposable : IDisposable
    {
        readonly ManualResetEvent _disposed;

        public SomethingDisposable(ManualResetEvent disposed) => _disposed = disposed;

        public void Dispose() => _disposed.Set();
    }

    record SomethingAsyncDisposable : IAsyncDisposable
    {
        readonly ManualResetEvent _disposed;

        public SomethingAsyncDisposable(ManualResetEvent disposed) => _disposed = disposed;

        public async ValueTask DisposeAsync() => _disposed.Set();
    }
}