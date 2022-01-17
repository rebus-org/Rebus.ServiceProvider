using Rebus.Bus;

namespace Rebus.ServiceProvider.Internals;

class DefaultBusInstance
{
    public IBus Bus { get; set; }
    public BusLifetimeEvents BusLifetimeEvents { get; set; }
}