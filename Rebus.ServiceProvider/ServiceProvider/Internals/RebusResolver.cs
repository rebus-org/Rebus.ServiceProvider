using System;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Bus;
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
                var defaultBusInstance = serviceProvider.GetService<DefaultBusInstance>()
                    ?? throw new InvalidOperationException("No default bus seems to have been configured! One (and only one!) of the calls to AddRebus must specify isDefaultBus:true to make that bus registration the default bus.");

                var busToReturn = defaultBusInstance.Bus
                    ?? throw new InvalidOperationException("Could not resolve default bus. Found that a bus registration was marked with isDefaultBus:true, but the bus instance could not be found, which indicates that the bus might not have been started! Please remember to call provider.StartRebus() after building the service provider, before trying to resolve the default bus.");

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
}