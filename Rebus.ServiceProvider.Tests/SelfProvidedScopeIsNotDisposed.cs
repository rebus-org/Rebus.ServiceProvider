using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Logging;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;

// ReSharper disable ArgumentsStyleLiteral
// ReSharper disable ArgumentsStyleOther
// ReSharper disable ArgumentsStyleStringLiteral
// ReSharper disable ClassNeverInstantiated.Local
#pragma warning disable 1998

namespace Rebus.ServiceProvider.Tests;

[TestFixture]
[Description(
    "When the scope is provided by custom incoming step, it should also be disposed by that step, and not automatically")]
public class SelfProvidedScopeIsNotDisposed : FixtureBase
{
    [Test]
    public async Task VerifyIt()
    {
        var gotTheMessage = new ManualResetEvent(initialState: false);
        var services = new ServiceCollection();
        var loggerFactory = new ListLoggerFactory(outputToConsole: true, detailed: true);

        services.AddSingleton(gotTheMessage);

        services.AddScoped<SomethingDisposable>();

        services.AddRebusHandler<StringHandler>();

        services.AddRebus(configure => configure
            .Logging(l => l.Use(loggerFactory))
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "who cares"))
            .Options(o => o.Decorate<IPipeline>(c =>
            {
                var pipeline = c.Get<IPipeline>();

                return new PipelineStepInjector(pipeline)
                    .OnReceive(new MyScopeStep(), PipelineRelativePosition.Before,
                        typeof(DeserializeIncomingMessageStep));
            })));

        var provider = Using(services.BuildServiceProvider());

        provider.StartRebus();

        await provider.GetRequiredService<IBus>().SendLocal("hej søde ven!");

        gotTheMessage.WaitOrDie(
            timeout: TimeSpan.FromSeconds(3),
            errorMessage:
            "Message was not received within 3 s timeout, which means that an exception must have occurred somewhere");

        await Task.Delay(TimeSpan.FromSeconds(2));

        var foundWarningOrError = loggerFactory.Any(log => log.Level > LogLevel.Info);

        Assert.That(foundWarningOrError, Is.False,
            "The log contained one or more warnings/errors, which is an indication that something went wrong when dispatching the message");
    }

    class SomethingDisposable : IDisposable
    {
        public bool HasBeenDisposed { get; private set; }

        public void Dispose() => HasBeenDisposed = true;
    }

    class StringHandler : IHandleMessages<string>
    {
        readonly ManualResetEvent _gotTheMessage;
        readonly SomethingDisposable _somethingDisposable;
        readonly IMessageContext _messageContext;

        public StringHandler(ManualResetEvent gotTheMessage, SomethingDisposable somethingDisposable,
            IMessageContext messageContext)
        {
            _gotTheMessage = gotTheMessage;
            _somethingDisposable = somethingDisposable;
            _messageContext = messageContext;
        }

        public async Task Handle(string message)
        {
            _messageContext.IncomingStepContext.Save(_somethingDisposable);

            _gotTheMessage.Set();
        }
    }

    class MyScopeStep : IIncomingStep
    {
        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var serviceProvider = context.Load<IServiceProvider>();

            SomethingDisposable somethingDisposable;

            using (var scope = serviceProvider.CreateScope())
            {
                context.Save(scope);

                somethingDisposable = scope.ServiceProvider.GetRequiredService<SomethingDisposable>();

                Assert.That(somethingDisposable.HasBeenDisposed, Is.False);

                var somethingDisposableResolvedAgain = scope.ServiceProvider.GetRequiredService<SomethingDisposable>();

                Assert.That(somethingDisposableResolvedAgain, Is.SameAs(somethingDisposable));

                await next();

                var somethingDisposableInjectedIntoMessageHandler = context.Load<SomethingDisposable>();

                Assert.That(somethingDisposableInjectedIntoMessageHandler, Is.SameAs(somethingDisposable));

                Assert.That(somethingDisposable.HasBeenDisposed, Is.False);
            }

            Assert.That(somethingDisposable.HasBeenDisposed, Is.True);
        }
    }
}