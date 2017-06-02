using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Handlers;
using Rebus.Routing.TypeBased;
using Rebus.Transport.InMem;

namespace Rebus.ServiceProvider.Tests
{
    [TestFixture]
    public partial class ServiceCollectionExtensionsTests
    {
        [Test]
        public async Task AddRebus_ConfigureRebusOnce_StartsAndConfiguresBus()
        {
            // Arrange
            var services = new ServiceCollection();
            var testHandler = new Handler1(2);

            // Act            
            services
                .AddSingleton<IHandleMessages<Message1>, Handler1>()
                .AddRebus(config => config
                    .Logging(l => l.None())
                    .Transport(t => t.UseInMemoryTransport(new InMemNetwork(false), "Messages"))
                    .Routing(r => r.TypeBased().MapAssemblyOf<Message1>("Messages")));

            var provider = services
                .BuildServiceProvider()
                .UseRebus();

            var rebus = provider.GetRequiredService<IBus>();
            await rebus.Send(new Message1());
            await rebus.Send(new Message1());

            // Assert
            await Task.WhenAny(testHandler.CountReached, Task.Delay(3000));

            (provider.GetRequiredService<IHandleMessages<Message1>>() as Handler1)
                .HandleCount.Should().Be(2);
        }

        [Test]
        public void AddRebus_ConfigureRebusManyTimes_Throws()
        {
            // Arrange
            var services = new ServiceCollection();
            var testHandler = new Handler1(2);

            // Act
            new Action(() =>
            {
                services
                    .AddSingleton<IHandleMessages<Message1>, Handler1>()
                    .AddRebus(config => config
                        .Logging(l => l.None())
                        .Transport(t => t.UseInMemoryTransport(new InMemNetwork(false), "Messages")))
                    .AddRebus(config => config
                        .Routing(r => r.TypeBased().MapAssemblyOf<Message1>("Messages")));
            }).ShouldThrow<InvalidOperationException>();
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
                .AddSingleton<IHandleMessages<Message1>>(testHandler2)
                .AddRebus(config => config
                    .Logging(l => l.None())
                    .Transport(t => t.UseInMemoryTransport(new InMemNetwork(false), "Messages"))
                    .Routing(r => r.TypeBased().MapAssemblyOf<Message1>("Messages")));

            var provider = services
                .BuildServiceProvider()
                .UseRebus();

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

        public abstract class MessageBase { }

        public class Message1 : MessageBase { }

        public abstract class TestHandlerBase
        {
            private readonly TaskCompletionSource<bool> _countReachedSource;
            private readonly int _countToWaitFor;

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
}
