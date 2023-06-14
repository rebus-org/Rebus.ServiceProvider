using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Retry;
using Rebus.Retry.Simple;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Transport;
using Rebus.Transport.InMem;

// ReSharper disable UnusedTypeParameter

namespace Rebus.ServiceProvider.Tests;

/// <summary>
///     Testing the polymorphic message handler activation.
/// </summary>
[TestFixture]
public class PolymorphicMessageHandlerActivation : FixtureBase
{
    IHandlerActivator Setup(IHandleMessages testHandler, Type messageHandlerType)
    {
        var services = new ServiceCollection();

        services
            .AddSingleton(messageHandlerType, testHandler)
            .AddRebus(config => config
                .Logging(l => l.None())
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "Messages"))
                .Routing(r => r.TypeBased().MapAssemblyOf<Parent>("Messages")));

        var provider = Using(services.BuildServiceProvider()).StartRebus();

        return new DependencyInjectionHandlerActivator(provider);
    }

    [Test]
    public async Task Handlers_ShouldHandleSameType()
    {
        var activator = Setup(new ChildMessageHandler(), typeof(IHandleMessages<Child>));

        using var scope = new RebusTransactionScope();

        var handlers = await activator.GetHandlers(new Child(), scope.TransactionContext);

        handlers.Should().HaveCount(1);
    }

    [Test]
    public async Task Handlers_ShouldHandleSubtype()
    {
        var activator = Setup(new ParentMessageHandler(), typeof(IHandleMessages<Parent>));

        using var scope = new RebusTransactionScope();

        var handlers = await activator.GetHandlers(new Child(), scope.TransactionContext);

        handlers.Should().HaveCount(1);
    }

    [Test]
    public async Task Handlers_ShouldNotHandleSupertype()
    {
        var activator = Setup(new ChildMessageHandler(), typeof(IHandleMessages<Child>));

        using var scope = new RebusTransactionScope();

        var handlers = await activator.GetHandlers(new Parent(), scope.TransactionContext);

        handlers.Should().HaveCount(0);
    }

    [Test]
    public async Task Handlers_ShouldHandleCovariantSubtype()
    {
        var activator = Setup(new CovariantGenericParentMessageHandler(),
            typeof(IHandleMessages<ICovariantGeneric<Parent>>));

        using var scope = new RebusTransactionScope();

        var handlers = await activator.GetHandlers(new ConcreteCovariantGeneric<Child>(), scope.TransactionContext);

        handlers.Should().HaveCount(1);
    }

    [Test]
    public async Task Handlers_ShouldHandleSameCovariantType()
    {
        var activator = Setup(new CovariantGenericParentMessageHandler(),
            typeof(IHandleMessages<ICovariantGeneric<Parent>>));

        using var scope = new RebusTransactionScope();

        var handlers = await activator.GetHandlers(new ConcreteCovariantGeneric<Parent>(), scope.TransactionContext);

        handlers.Should().HaveCount(1);
    }

    [Test]
    public async Task Handlers_ShouldHandleMultipleCovariantTypeParameters()
    {
        var activator = Setup(new DoubleCovariantMixedMessageHandler(),
            typeof(IHandleMessages<IDoubleCovariantGeneric<Child, Parent>>));

        using var scope = new RebusTransactionScope();

        var handlers = await activator.GetHandlers(new ConcreteDoubleCovariantGeneric<Child, Child>(),
            scope.TransactionContext);

        handlers.Should().HaveCount(1);
    }

    [Test]
    public async Task Handlers_ShouldHandleMultipleCovariantTypeParametersWithSameType()
    {
        var activator = Setup(new DoubleCovariantSameMessageHandler(),
            typeof(IHandleMessages<IDoubleCovariantGeneric<Child, Child>>));

        using var scope = new RebusTransactionScope();

        var handlers = await activator.GetHandlers(new ConcreteDoubleCovariantGeneric<Child, Child>(),
            scope.TransactionContext);

        handlers.Should().HaveCount(1);
    }

    [Test]
    public async Task Handlers_ShouldNotGetBaseTypesOfRegularTypeParameter()
    {
        var activator = Setup(new GenericParentMessageHandler(), typeof(IHandleMessages<IGeneric<Parent>>));

        using var scope = new RebusTransactionScope();

        var handlers = await activator.GetHandlers(new ConcreteGeneric<Child>(), scope.TransactionContext);

        handlers.Should().HaveCount(0);
    }

    [Test]
    public async Task Handlers_ShouldHandleIFailedMessages()
    {
        var activator = Setup(new FailedMessageHandler(), typeof(IHandleMessages<IFailed<object>>));

        using var scope = new RebusTransactionScope();

        var handlers = await activator.GetHandlers(new FailedMessage<Child>(), scope.TransactionContext);

        handlers.Should().HaveCount(1);
    }

    [Test]
    public async Task Handlers_ShouldHandleCovariantTypeParametersWithConstraints()
    {
        var activator = Setup(new ConstrainedCovariantMessageHandler(),
            typeof(IHandleMessages<IConstrainedCovariant<Parent>>));

        using var scope = new RebusTransactionScope();

        var handlers = await activator.GetHandlers(new ConcreteConstrainedCovariant(), scope.TransactionContext);

        handlers.Should().HaveCount(1);
    }
}

public class Parent
{
}

public class Child : Parent
{
}

public class ParentMessageHandler : IHandleMessages<Parent>
{
    public Task Handle(Parent message) => Task.CompletedTask;
}

public class ChildMessageHandler : IHandleMessages<Child>
{
    public Task Handle(Child message) => Task.CompletedTask;
}

public interface ICovariantGeneric<out T>
{
}

public class ConcreteCovariantGeneric<T> : ICovariantGeneric<T>
{
}

public class CovariantGenericParentMessageHandler : IHandleMessages<ICovariantGeneric<Parent>>
{
    public Task Handle(ICovariantGeneric<Parent> message) => Task.CompletedTask;
}

public interface IGeneric<T>
{
}

public class ConcreteGeneric<T> : IGeneric<T>
{
}

public class GenericParentMessageHandler : IHandleMessages<IGeneric<Parent>>
{
    public Task Handle(IGeneric<Parent> message) => Task.CompletedTask;
}

public interface IDoubleCovariantGeneric<out T, out S>
{
}

public class ConcreteDoubleCovariantGeneric<T, S> : IDoubleCovariantGeneric<T, S>
{
}

public class DoubleCovariantMixedMessageHandler : IHandleMessages<IDoubleCovariantGeneric<Child, Parent>>
{
    public Task Handle(IDoubleCovariantGeneric<Child, Parent> message) => Task.CompletedTask;
}

public class DoubleCovariantSameMessageHandler : IHandleMessages<IDoubleCovariantGeneric<Child, Child>>
{
    public Task Handle(IDoubleCovariantGeneric<Child, Child> message) => Task.CompletedTask;
}

public class FailedMessage<T> : IFailed<T>
{
    public T Message { get; }
    public string ErrorDescription { get; }
    public Dictionary<string, string> Headers { get; }
    public IEnumerable<ExceptionInfo> Exceptions { get; }
}

public class FailedMessageHandler : IHandleMessages<IFailed<object>>
{
    public Task Handle(IFailed<object> message) => Task.CompletedTask;
}

public interface IConstrainedCovariant<out T> where T : Parent
{
}

public class ConcreteConstrainedCovariant : IConstrainedCovariant<Child>
{
}

public class ConstrainedCovariantMessageHandler : IHandleMessages<IConstrainedCovariant<Parent>>
{
    public Task Handle(IConstrainedCovariant<Parent> message) => Task.CompletedTask;
}