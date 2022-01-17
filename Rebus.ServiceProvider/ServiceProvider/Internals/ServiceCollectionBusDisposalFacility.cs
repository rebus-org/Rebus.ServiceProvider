using System;
using Rebus.Bus;

namespace Rebus.ServiceProvider.Internals;

/// <summary>
/// Sweet little hack that makes it possible for us to ensure that the bus is disposed with the container
/// </summary>
class ServiceCollectionBusDisposalFacility : IDisposable
{
    readonly IBus _bus;

    public ServiceCollectionBusDisposalFacility(IBus bus) => _bus = bus ?? throw new ArgumentNullException(nameof(bus));

    public void Dispose() => _bus.Dispose();
}