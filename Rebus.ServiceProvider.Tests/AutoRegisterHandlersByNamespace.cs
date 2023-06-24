using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.ServiceProvider.Tests.NamespaceTest1;

namespace Rebus.ServiceProvider.Tests
{
    [TestFixture]
    public class AutoRegisterHandlersByNamespace
    {
        [Test]
        public void Registers_handlers_in_specific_namespace()
        {
            var services = new ServiceCollection();
            services.AutoRegisterHandlersFromAssemblyNamespaceOf<Namespace1TestHandler1>();

            var provider = services.BuildServiceProvider();
            var handlers = provider.GetServices<IHandleMessages<string>>()
                .ToList();

            // Should auto register the 2 handlers in Namespace1
            // and not register the 1 that's in Namespace2.
            Assert.AreEqual(2, handlers.Count);
            Assert.IsInstanceOf<Namespace1TestHandler1>(handlers[0]);
            Assert.IsInstanceOf<Namespace1TestHandler2>(handlers[1]);
        }
    }
}

namespace Rebus.ServiceProvider.Tests.NamespaceTest1
{
    public class Namespace1TestHandler1 : IHandleMessages<string>
    {
        public Task Handle(string message)
        {
            throw new System.NotImplementedException();
        }
    }

    public class Namespace1TestHandler2 : IHandleMessages<string>
    {
        public Task Handle(string message)
        {
            throw new System.NotImplementedException();
        }
    }
}

namespace Rebus.ServiceProvider.Tests.NamespaceTest2
{
    public class Namespace2TestHandler1 : IHandleMessages<string>
    {
        public Task Handle(string message)
        {
            throw new System.NotImplementedException();
        }
    }
}