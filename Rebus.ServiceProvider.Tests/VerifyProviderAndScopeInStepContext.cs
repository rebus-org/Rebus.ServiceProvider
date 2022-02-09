using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Pipeline;
using Rebus.Retry.Simple;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;

// ReSharper disable ArgumentsStyleLiteral
// ReSharper disable ArgumentsStyleOther
// ReSharper disable UnusedVariable
// ReSharper disable ClassNeverInstantiated.Local
#pragma warning disable 1998

namespace Rebus.ServiceProvider.Tests;

[TestFixture]
public class VerifyProviderAndScopeInStepContext : FixtureBase
{
    [Test]
    public async Task ScopeIsReusedWhenItExists()
    {
        var serviceCollection = new ServiceCollection();
        var guidQueue = new ConcurrentQueue<Guid>();

        serviceCollection.AddSingleton(guidQueue);
        serviceCollection.AddRebusHandler<AnotherStringHandler>();
        serviceCollection.AddScoped<ScopedInstance>();

        var guidFirstEncountered = Guid.Empty;

        var step = new StepContextHooker(context =>
        {
            var serviceProvider = context.Load<IServiceProvider>();
            var scope = serviceProvider.CreateScope();

            guidFirstEncountered = scope.ServiceProvider.GetRequiredService<ScopedInstance>().Id;

            // this should make the handler activator use the same scope
            context.Save(scope);
        });

        AddRebusWithStep(serviceCollection, step);

        var provider = Using(serviceCollection.BuildServiceProvider());

        provider.StartRebus();

        await provider.GetRequiredService<IBus>().SendLocal("hej med dig din bandit!");

        await guidQueue.WaitUntil(q => q.Count == 1);

        var guidInjectedIntoHandler = guidQueue.First();

        Assert.That(guidInjectedIntoHandler, Is.EqualTo(guidFirstEncountered));
    }

    class ScopedInstance
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    class AnotherStringHandler : IHandleMessages<string>
    {
        readonly ScopedInstance _scopedInstance;
        readonly ConcurrentQueue<Guid> _guidQueue;

        public AnotherStringHandler(ScopedInstance scopedInstance, ConcurrentQueue<Guid> guidQueue)
        {
            _scopedInstance = scopedInstance;
            _guidQueue = guidQueue;
        }

        public async Task Handle(string message) => _guidQueue.Enqueue(_scopedInstance.Id);
    }

    [Test]
    public async Task ServiceProviderIsAvailableInStepContext()
    {
        var serviceCollection = new ServiceCollection();
        var handlerWasCalled = new ManualResetEvent(initialState: false);

        serviceCollection.AddSingleton(handlerWasCalled);
        serviceCollection.AddRebusHandler<StringHandler>();

        var step = new StepContextHooker(context =>
        {
            var serviceProvider = context.Load<IServiceProvider>()
                                  ?? throw new ApplicationException(
                                      "Could not find service provider in the current step context!");
        });

        AddRebusWithStep(serviceCollection, step);

        var provider = Using(serviceCollection.BuildServiceProvider());

        provider.StartRebus();

        await provider.GetRequiredService<IBus>().SendLocal("hej med dig din bandit!");

        handlerWasCalled.WaitOrDie(timeout: TimeSpan.FromSeconds(5));
    }

    static void AddRebusWithStep(IServiceCollection serviceCollection, IIncomingStep step)
    {
        serviceCollection.AddRebus(configure => configure
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "whatever"))
            .Options(o =>
            {
                o.Decorate<IPipeline>(c =>
                {
                    var pipeline = c.Get<IPipeline>();
                    return new PipelineStepInjector(pipeline)
                        .OnReceive(step, PipelineRelativePosition.After, typeof(SimpleRetryStrategyStep));
                });

                o.LogPipeline(verbose: true);
            }));
    }

    class StringHandler : IHandleMessages<string>
    {
        readonly ManualResetEvent _done;

        public StringHandler(ManualResetEvent done) => _done = done;

        public async Task Handle(string message) => _done.Set();
    }

    class StepContextHooker : IIncomingStep
    {
        readonly Action<IncomingStepContext> _contextCallback;

        public StepContextHooker(Action<IncomingStepContext> contextCallback) => _contextCallback =
            contextCallback ?? throw new ArgumentNullException(nameof(contextCallback));

        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            _contextCallback(context);

            await next();
        }
    }
}