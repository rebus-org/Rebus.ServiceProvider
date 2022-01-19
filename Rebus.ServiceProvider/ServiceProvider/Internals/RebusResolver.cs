using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Bus;
using Rebus.Bus.Advanced;
using Rebus.Pipeline;

namespace Rebus.ServiceProvider.Internals;

class RebusResolver
{
    public IBus GetBus(IServiceProvider serviceProvider)
    {
        var messageContext = MessageContext.Current;

        if (messageContext == null)
        {
            try
            {
                var busToReturn = serviceProvider.GetService<DefaultBusInstance>()?.Bus ?? throw new InvalidOperationException("No default bus configured");
                
                return new BusWrapper(busToReturn);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("Error when trying to resolve default bus instance! No current message context was found (i.e. we're not currently handling a message), so the default bus was requested from the service provider (via DefaultBusInstance). If you'd like to use IBus outside of message handlers, please remember to mark one of the bus registrations as being the default bus instance by setting isDefaultBus:true in one of the calls to AddRebus.", exception);
            }
        }

        var incomingStepContext = messageContext.IncomingStepContext;
        var bus = incomingStepContext.Load<IBus>();

        if (bus == null)
        {
            throw new ApplicationException("Couldn't find IBus in the incoming step context. This is a sign that the SetBusInstanceStep was not executed as expected for the incoming message, which in turn is a sign that something is very wrong.");
        }

        return new BusWrapper(bus);
    }

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
}