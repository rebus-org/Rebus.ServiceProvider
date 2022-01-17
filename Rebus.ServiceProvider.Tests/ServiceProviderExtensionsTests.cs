using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Tests.Contracts;

namespace Rebus.ServiceProvider.Tests;

[TestFixture]
public class ServiceProviderExtensionsTests : FixtureBase
{
    private Microsoft.Extensions.DependencyInjection.ServiceProvider _serviceProvider;
    private Mock<IBus> _busMock;
    private Mock<IBusStarter> _busStarterMock;

    public class TestMessage
    {
    }

    protected override void SetUp()
    {
        _busMock = new Mock<IBus>
        {
            DefaultValue = DefaultValue.Mock
        };

        _busStarterMock = new Mock<IBusStarter>();
        _busStarterMock.SetReturnsDefault(_busMock.Object);

        _serviceProvider = new ServiceCollection()
            .AddRebus(c => c)
            .Replace(ServiceDescriptor.Singleton(_busStarterMock.Object))
            .BuildServiceProvider();

        Using(_serviceProvider);
    }

    [Test]
    public void UseRebus_StartsBus()
    {
        // Act
        _serviceProvider.UseRebus();

        // Assert
        _busStarterMock.Verify(m => m.Start(), Times.Once);
    }

    [Test]
    public void UseRebus_WithNullProvider_Throws()
    {
        IServiceProvider provider = null;

        // Act
        void Act() => provider.UseRebus();

        // Assert
        var ex = Assert.Throws<ArgumentNullException>(Act);
        Assert.AreEqual(nameof(provider), ex.ParamName);
        _busStarterMock.Verify(m => m.Start(), Times.Never);
    }

    [Test]
    public void UseRebus_WithSyncDelegate_ExecutesAndStartsBus()
    {
        // Act
        _serviceProvider.UseRebus(async bus =>
        {
            Assert.AreEqual(_busMock.Object, bus);
            bus.Advanced.SyncBus.Subscribe<TestMessage>();
        });

        // Assert
        _busStarterMock.Verify(m => m.Start(), Times.Once);
        _busMock.Verify(m => m.Advanced.SyncBus.Subscribe<TestMessage>(), Times.Once);
    }

    [Test]
    public void UseRebus_WithSyncDelegateAndNullProvider_Throws()
    {
        IServiceProvider provider = null;
        Mock<Func<IBus, Task>> onBusStarted = new Mock<Func<IBus, Task>>();

        // Act
        void Act() => provider.UseRebus(onBusStarted.Object);

        // Assert
        var ex = Assert.Throws<ArgumentNullException>(Act);
        Assert.AreEqual(nameof(provider), ex.ParamName);
        _busStarterMock.Verify(m => m.Start(), Times.Never);
        onBusStarted.Verify(m => m.Invoke(It.IsAny<IBus>()), Times.Never);
    }

    [Test]
    public void UseRebus_WithNullSyncDelegateAndProvider_Throws()
    {
        Func<IBus, Task> onBusStarted = null;

        // Act
        void Act() => _serviceProvider.UseRebus(onBusStarted);

        // Assert
        var ex = Assert.Throws<ArgumentNullException>(Act);
        Assert.AreEqual(nameof(onBusStarted), ex.ParamName);
        _busMock.Verify(m => m.Advanced.SyncBus.Subscribe<TestMessage>(), Times.Never);
    }

    [Test]
    public void UseRebus_WithAsyncDelegate_StartsBusAndExecutes()
    {
        // Act
        _serviceProvider.UseRebus(async bus =>
        {
            Assert.AreEqual(_busMock.Object, bus);
            await bus.Subscribe<TestMessage>();
        });

        // Assert
        _busStarterMock.Verify(m => m.Start(), Times.Once);
        _busMock.Verify(m => m.Subscribe<TestMessage>(), Times.Once);
    }

    [Test]
    public void UseRebus_WithAsyncDelegateAndNullProvider_Throws()
    {
        IServiceProvider provider = null;
        Mock<Func<IBus, Task>> onBusStarted = new Mock<Func<IBus, Task>>();
        onBusStarted.SetReturnsDefault(Task.CompletedTask);

        // Act
        void Act() => provider.UseRebus(onBusStarted.Object);

        // Assert
        var ex = Assert.Throws<ArgumentNullException>(Act);
        Assert.AreEqual(nameof(provider), ex.ParamName);
        _busStarterMock.Verify(m => m.Start(), Times.Never);
        onBusStarted.Verify(m => m.Invoke(It.IsAny<IBus>()), Times.Never);
    }

    [Test]
    public void UseRebus_WithNullAsyncDelegateAndProvider_Throws()
    {
        Func<IBus, Task> onBusStarted = null;

        // Act
        void Act() => _serviceProvider.UseRebus(onBusStarted);

        // Assert
        var ex = Assert.Throws<ArgumentNullException>(Act);
        Assert.AreEqual(nameof(onBusStarted), ex.ParamName);
        _busMock.Verify(m => m.Subscribe<TestMessage>(), Times.Never);
    }
}