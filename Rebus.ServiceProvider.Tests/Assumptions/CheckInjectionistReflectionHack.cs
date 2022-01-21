using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Logging;
using Rebus.ServiceProvider.Internals;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;

namespace Rebus.ServiceProvider.Tests.Assumptions;

[TestFixture]
public class CheckInjectionistReflectionHack : FixtureBase
{
    [Test]
    public void ToAvoidAnExceptionWeCanMaybeReflectOurWayToCheckIfInjectionistHasThisParticularRegistration()
    {
        using var activator = new BuiltinHandlerActivator();

        var configurer = Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "doesn't matter"))
            .Logging(l => l.ColoredConsole());

        var hasLoggerFactoryRegistration = false;

        configurer.Logging(l =>
        {
            hasLoggerFactoryRegistration = l.ReflectWhetherItHasRegistration<IRebusLoggerFactory>();
        });

        configurer.Start();

        Assert.That(hasLoggerFactoryRegistration, Is.True);
    }
}