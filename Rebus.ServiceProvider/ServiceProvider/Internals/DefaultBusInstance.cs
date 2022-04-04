using System;
using System.Threading.Tasks;
using Rebus.Bus;
// ReSharper disable ArgumentsStyleLiteral

namespace Rebus.ServiceProvider.Internals;

class DefaultBusInstance
{
    Lazy<(IBus, BusLifetimeEvents)> _instances;

    public void SetInstanceResolver(Lazy<Task<(IBus, BusLifetimeEvents)>> resolver)
    {
        if (resolver == null) throw new ArgumentNullException(nameof(resolver));

        _instances = new(() =>
        {
            var task = resolver.Value;
            var result = default((IBus, BusLifetimeEvents));

            AsyncHelpers.RunSync(async () => result = await task);

            return result;
        });
    }

    public IBus Bus => _instances.Value.Item1;

    public BusLifetimeEvents BusLifetimeEvents => _instances.Value.Item2;
}