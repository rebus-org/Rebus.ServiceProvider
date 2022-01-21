using Rebus.Bus;

namespace Rebus.ServiceProvider;

public interface IBusRegistry
{
    IBus GetBus(string key);
}