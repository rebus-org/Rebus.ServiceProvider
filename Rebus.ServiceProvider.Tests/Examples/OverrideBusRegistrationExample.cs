using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Bus.Advanced;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;

namespace Rebus.ServiceProvider.Tests.Examples;

[TestFixture]
public class OverrideBusRegistrationExample : FixtureBase
{
    [TestCase(true)]
    [TestCase(false)]
    public void WeCanDoIt(bool testMode)
    {
        var services = new ServiceCollection();

        var logger = new ListLoggerFactory();

        services.AddRebus(
            configure => configure
                .Logging(l => l.Use(logger))
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "whateverman"))
        );

        if (testMode)
        {
            // disable all hosted services
            services.RemoveAll<IHostedService>();

            // if this is too harsh, one can cherry-pick the ones to remove:
            //var toRemove = services
            //    .Where(s => s.ServiceType == typeof(IHostedService) && Equals(s.ImplementationFactory?.Method.DeclaringType?.Assembly, typeof(ServiceProviderExtensions).Assembly))
            //    .ToList();
            //foreach (var descriptor in toRemove)
            //{
            //    services.Remove(descriptor);
            //}

            // replace the main IBus registration with the fake
            services.Replace(ServiceDescriptor.Singleton<IBus>(new FakeBus()));
        }

        using var provider = services.BuildServiceProvider();

        provider.StartRebus();

        var bus = provider.GetRequiredService<IBus>();

        provider.Dispose();

        if (testMode)
        {
            Assert.That(bus, Is.TypeOf<FakeBus>(), "Expected the resolved IBus to be a FakeBus");
            Assert.That(logger.Count(), Is.EqualTo(0), "Bus should never get to log anything");
        }
        else
        {
            Assert.That(bus, Is.Not.TypeOf<FakeBus>(), "Pretty sure this isn't going to happen 😉");
            Assert.That(logger.Count(), Is.GreaterThan(0), "This is the real bus, so it logs what it's doing");
        }
    }

    class FakeBus : IBus
    {
        public void Dispose() => throw new NotImplementedException();
        public Task SendLocal(object commandMessage, IDictionary<string, string> optionalHeaders = null) => throw new NotImplementedException();
        public Task Send(object commandMessage, IDictionary<string, string> optionalHeaders = null) => throw new NotImplementedException();
        public Task DeferLocal(TimeSpan delay, object message, IDictionary<string, string> optionalHeaders = null) => throw new NotImplementedException();
        public Task Defer(TimeSpan delay, object message, IDictionary<string, string> optionalHeaders = null) => throw new NotImplementedException();
        public Task Reply(object replyMessage, IDictionary<string, string> optionalHeaders = null) => throw new NotImplementedException();
        public Task Subscribe<TEvent>() => throw new NotImplementedException();
        public Task Subscribe(Type eventType) => throw new NotImplementedException();
        public Task Unsubscribe<TEvent>() => throw new NotImplementedException();
        public Task Unsubscribe(Type eventType) => throw new NotImplementedException();
        public Task Publish(object eventMessage, IDictionary<string, string> optionalHeaders = null) => throw new NotImplementedException();
        public IAdvancedApi Advanced { get; }
    }
}