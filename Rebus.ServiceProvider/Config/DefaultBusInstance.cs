using Rebus.Bus;

namespace Rebus.Config;

class DefaultBusInstance
{
    public IBus Bus { get; set; }
    public BusLifetimeEvents BusLifetimeEvents { get; set; }
}