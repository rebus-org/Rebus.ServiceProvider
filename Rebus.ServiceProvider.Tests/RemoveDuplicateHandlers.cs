using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Rebus.Handlers;
using Rebus.Routing.TypeBased;
using Rebus.Transport;
using Rebus.Transport.InMem;
using System.Threading.Tasks;
using Rebus.Config;

namespace Rebus.ServiceProvider.Tests;

[TestFixture]
public class RemoveDuplicateHandlers
{
    [Test]
    public async Task PolymorphicMessageHandling_RemoveDuplicateHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        var testHandler = new Message1Handler();

        // Act            
        services
            .AddSingleton<IHandleMessages<Message1>>(testHandler)
            .AddSingleton<IHandleMessages<IMessage1>>(testHandler)
            .AddRebus(config => config
                .Logging(l => l.None())
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(outputEventsToConsole: false), "Messages"))
                .Routing(r => r.TypeBased().MapAssemblyOf<Message1>("Messages")));

        var provider = services
            .BuildServiceProvider()
            .StartRebus();

        var activator = new DependencyInjectionHandlerActivator(provider);

        // Assert
        using var scope = new RebusTransactionScope();

        var handlers = await activator.GetHandlers(new Message1(), scope.TransactionContext);

        handlers.Should().HaveCount(1);
    }

    public interface IMessage1
    {
    }

    public class Message1 : IMessage1
    {
    }

    public class Message1Handler : IHandleMessages<IMessage1>
    {
        public Task Handle(IMessage1 message) => Task.CompletedTask;
    }
}