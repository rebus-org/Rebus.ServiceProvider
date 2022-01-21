using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Bus.Advanced;

namespace Rebus.ServiceProvider.Internals;

/// <summary>
/// All TRANSIENT resolutions of IBus must be wrapped in this to avoid having the container dispose the bus prematurely!
/// </summary>
class BusWrapper : IBus
{
    readonly IBus _bus;

    public BusWrapper(IBus bus) => _bus = bus ?? throw new ArgumentNullException(nameof(bus));

    // this is the important part: do NOT forward call to wrapped bus - disposing the wrapper must never dispose the bus!
    public void Dispose()
    {
    }

    public Task SendLocal(object commandMessage, IDictionary<string, string> optionalHeaders = null) => _bus.SendLocal(commandMessage, optionalHeaders);

    public Task Send(object commandMessage, IDictionary<string, string> optionalHeaders = null) => _bus.Send(commandMessage, optionalHeaders);

    public Task DeferLocal(TimeSpan delay, object message, IDictionary<string, string> optionalHeaders = null) => _bus.DeferLocal(delay, message, optionalHeaders);

    public Task Defer(TimeSpan delay, object message, IDictionary<string, string> optionalHeaders = null) => _bus.Defer(delay, message, optionalHeaders);

    public Task Reply(object replyMessage, IDictionary<string, string> optionalHeaders = null) => _bus.Reply(replyMessage, optionalHeaders);

    public Task Subscribe<TEvent>() => _bus.Subscribe<TEvent>();

    public Task Subscribe(Type eventType) => _bus.Subscribe(eventType);

    public Task Unsubscribe<TEvent>() => _bus.Unsubscribe<TEvent>();

    public Task Unsubscribe(Type eventType) => _bus.Unsubscribe(eventType);

    public Task Publish(object eventMessage, IDictionary<string, string> optionalHeaders = null) => _bus.Publish(eventMessage, optionalHeaders);

    public IAdvancedApi Advanced => _bus.Advanced;

    public override string ToString() => _bus.ToString();
}