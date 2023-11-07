using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;

#pragma warning disable 1998

namespace Rebus.ServiceProvider.Tests;

[TestFixture]
public class ServiceCollectionExtensionsTests
{
    [Test]
    public async Task AddRebus_ConfigureRebusOnce_StartsAndConfiguresBus()
    {
        var handledMessages = new ConcurrentQueue<string>();

        // Arrange
        var services = new ServiceCollection();
        var testHandler = new Handler1(2);

        // Act            
        services
            .AddSingleton(handledMessages)
            //.AddSingleton<IHandleMessages<Message1>>(testHandler)
            .AddTransient<IHandleMessages<TextMessage>, TextMessageHandler>()
            .AddRebus(config => config
                .Logging(l => l.None())
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "Messages"))
                .Routing(r => r.TypeBased().MapAssemblyOf<Message1>("Messages")));

        var provider = services
            .BuildServiceProvider()
            .StartRebus();

        var rebus = provider.GetRequiredService<IBus>();

        await rebus.Send(new TextMessage("HEJ"));
        await rebus.Send(new TextMessage("MED"));
        await rebus.Send(new TextMessage("DIG"));

        // Assert
        await handledMessages.WaitUntil(q => q.Count >= 3);

        await Task.Delay(TimeSpan.FromSeconds(0.1));

        Assert.That(handledMessages, Is.EqualTo(new[] {"HEJ", "MED", "DIG"}));
    }

    class TextMessageHandler : IHandleMessages<TextMessage>
    {
        readonly ConcurrentQueue<string> _receivedMessages;

        public TextMessageHandler(ConcurrentQueue<string> receivedMessages) => _receivedMessages = receivedMessages;

        public async Task Handle(TextMessage message) => _receivedMessages.Enqueue(message.Text);
    }

    class TextMessage
    {
        public string Text { get; }

        public TextMessage(string text) => Text = text;
    }

    [Test]
    public void AddRebus_ConfigureRebusManyTimes_Throws()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();

        serviceCollection
            .AddSingleton<IHandleMessages<Message1>, Handler1>()
            .AddRebus(config => config
                .Logging(l => l.None())
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "Messages")));

        // Act
        var invalidOperationException = Assert.Throws<InvalidOperationException>(() =>
        {
            serviceCollection
                .AddRebus(config => config
                    .Routing(r => r.TypeBased().MapAssemblyOf<Message1>("Messages")));
        });

        Console.WriteLine(invalidOperationException);
    }

    [Test]
    public async Task AddRebus_ConfigurePolymorphicMessageHandling_AllMessagesAreHandled()
    {
        // Arrange
        var services = new ServiceCollection();
        var testHandler1 = new Handler1(2);
        var testHandler2 = new Handler2(2);

        // Act            
        services
            .AddSingleton<IHandleMessages<Message1>>(testHandler1)
            .AddSingleton<IHandleMessages<MessageBase>>(testHandler2)
            .AddRebus(config => config
                .Logging(l => l.None())
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(false), "Messages"))
                .Routing(r => r.TypeBased().MapAssemblyOf<Message1>("Messages")));

        var provider = services
            .BuildServiceProvider()
            .StartRebus();

        var rebus = provider.GetRequiredService<IBus>();
        await rebus.Send(new Message1());
        await rebus.Send(new Message1());

        // Assert
        await Task.WhenAny(testHandler1.CountReached, Task.Delay(3000));
        await Task.WhenAny(testHandler2.CountReached, Task.Delay(3000));

        (provider.GetRequiredService<IHandleMessages<Message1>>() as Handler1)
            .HandleCount.Should().Be(2);

        (provider.GetRequiredService<IHandleMessages<MessageBase>>() as Handler2)
            .HandleCount.Should().Be(2);
    }

    public abstract class MessageBase
    {
    }

    public class Message1 : MessageBase
    {
    }

    public abstract class TestHandlerBase
    {
        readonly TaskCompletionSource<bool> _countReachedSource;
        readonly int _countToWaitFor;

        public TestHandlerBase(int countToWaitFor)
        {
            _countToWaitFor = countToWaitFor;
            _countReachedSource = new TaskCompletionSource<bool>();
        }

        public Task CountReached => _countReachedSource.Task;

        public int HandleCount { get; private set; }

        protected void RegisterHandle()
        {
            HandleCount++;

            if (HandleCount == _countToWaitFor) _countReachedSource.SetResult(true);
        }
    }

    public class Handler1 : TestHandlerBase, IHandleMessages<Message1>
    {
        public Handler1(int countToWaitFor) : base(countToWaitFor)
        {
        }

        public Task Handle(Message1 message)
        {
            RegisterHandle();
            return Task.FromResult(true);
        }
    }

    public class Handler2 : TestHandlerBase, IHandleMessages<MessageBase>
    {
        public Handler2(int countToWaitFor) : base(countToWaitFor)
        {
        }

        public Task Handle(MessageBase message)
        {
            RegisterHandle();
            return Task.FromResult(true);
        }
    }
}