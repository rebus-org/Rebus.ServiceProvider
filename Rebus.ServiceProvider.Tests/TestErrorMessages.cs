using System;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;

// ReSharper disable ArgumentsStyleLiteral

namespace Rebus.ServiceProvider.Tests;

[TestFixture]
public class TestErrorMessages : FixtureBase
{
    [Test]
    public void RegisterDefaultBusTwice_Explicit()
    {
        var services = new ServiceCollection();

        services.AddRebus(
            configure => configure
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "whatever")),
            
            isDefaultBus: true
        );

        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            services.AddRebus(
                configure => configure
                    .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "whatever")),
                isDefaultBus: true
            );
        });

        Console.WriteLine(exception);
    }

    [Test]
    public void RegisterDefaultBusTwice_Implicit()
    {
        var services = new ServiceCollection();

        services.AddRebus(
            configure => configure
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "whatever"))
        );

        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            services.AddRebus(
                configure => configure
                    .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "whatever"))
            );
        });

        Console.WriteLine(exception);
    }

    [Test]
    public void ResolveDefaultBus_ThereIsNoDefault_BusNotStarted()
    {
        var services = new ServiceCollection();

        services.AddRebus(
            configure => configure
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "whatever")),
            isDefaultBus: false
        );

        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IBus>());

        Console.WriteLine(exception);
    }

    [Test]
    public void ResolveDefaultBus_ThereIsNoDefault_BusStarted()
    {
        var services = new ServiceCollection();

        services.AddRebus(
            configure => configure
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "whatever")),
            isDefaultBus: false
        );

        using var provider = services.BuildServiceProvider();

        provider.StartRebus();

        var exception = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IBus>());

        Console.WriteLine(exception);
    }

    [Test]
    public void ResolveDefaultBus_HasDefault_BusNotStarted()
    {
        var services = new ServiceCollection();

        services.AddRebus(
            configure => configure
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "whatever"))
        );

        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IBus>());

        Console.WriteLine(exception);
    }

    [Test]
    public void ResolveDefaultBus_HasDefault_BusStarted()
    {
        var services = new ServiceCollection();

        services.AddRebus(
            configure => configure
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "whatever"))
        );

        using var provider = services.BuildServiceProvider();

        provider.StartRebus();

        provider.GetRequiredService<IBus>();
    }
}