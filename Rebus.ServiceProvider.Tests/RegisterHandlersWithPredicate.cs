using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.ServiceProvider.Tests.Predicates;

namespace Rebus.ServiceProvider.Tests
{
    [TestFixture]
    public class RegisterHandlersWithPredicate
    {
        [Test]
        public void Registers_handler_matching_predicate_1()
        {
            var services = new ServiceCollection();
            services.AutoRegisterHandlersFromAssemblyOf<TestHandler1>(x => x == typeof(TestHandler1));
            

            var provider = services.BuildServiceProvider();
            var handlers = provider.GetServices<IHandleMessages<string>>()
                .ToList();

            Assert.AreEqual(1, handlers.Count);
            Assert.IsInstanceOf<TestHandler1>(handlers[0]);
        }
        
        [Test]
        public void Registers_handler_matching_predicate_2()
        {
            var services = new ServiceCollection();
            services.AutoRegisterHandlersFromAssembly(typeof(TestHandler1).Assembly, x => x == typeof(TestHandler1));
            

            var provider = services.BuildServiceProvider();
            var handlers = provider.GetServices<IHandleMessages<string>>()
               .ToList();

            Assert.AreEqual(1, handlers.Count);
            Assert.IsInstanceOf<TestHandler1>(handlers[0]);
        }
    }
}

namespace Rebus.ServiceProvider.Tests.Predicates
{
    public class TestHandler1 : IHandleMessages<string>
    {
        public Task Handle(string message)
        {
            throw new System.NotImplementedException();
        }
    }

    public class TestHandler2 : IHandleMessages<string>
    {
        public Task Handle(string message)
        {
            throw new System.NotImplementedException();
        }
    }
}
