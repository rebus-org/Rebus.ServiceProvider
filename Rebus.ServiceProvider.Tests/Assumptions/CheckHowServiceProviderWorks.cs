using System;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;

// ReSharper disable ConvertToUsingDeclaration

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
            provider.StartRebus();
        }
    }

    [Test]
    public void CanRegisterInstanceThatGetsDisposed_SingletonInstance()
    {
        var instance = new OhTellMeIfYouHaveBeenDisposedLittleInstance();
        var services = new ServiceCollection();
        services.AddSingleton(instance);

        services.BuildServiceProvider().Dispose();

        Assert.That(instance.WasDisposed, Is.False);
    }

    [Test]
    public void CanRegisterInstanceThatGetsDisposed_SingletonFactory()
    {
        var instance = new OhTellMeIfYouHaveBeenDisposedLittleInstance();
        var services = new ServiceCollection();
        services.AddSingleton(_ => instance);

        services.BuildServiceProvider().Dispose();

        Assert.That(instance.WasDisposed, Is.False);
    }

    [Test]
    public void CanRegisterInstanceThatGetsDisposed_SingletonType()
    {
        var services = new ServiceCollection();
        services.AddSingleton<OhTellMeIfYouHaveBeenDisposedLittleInstance>();

        var provider = services.BuildServiceProvider();
        var instance = provider.GetRequiredService<OhTellMeIfYouHaveBeenDisposedLittleInstance>();
        provider.Dispose();

        Assert.That(instance.WasDisposed, Is.True);
    }

    class OhTellMeIfYouHaveBeenDisposedLittleInstance : IDisposable
    {
        public bool WasDisposed { get; set; }

        public void Dispose() => WasDisposed = true;
    }
}