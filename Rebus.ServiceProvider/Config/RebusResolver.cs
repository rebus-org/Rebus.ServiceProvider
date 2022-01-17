using System;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Bus;
using Rebus.Pipeline;

namespace Rebus.Config;

class RebusResolver
{
    public IBus GetBus(IServiceProvider serviceProvider)
    {
        var messageContext = MessageContext.Current;

        if (messageContext == null)
        {
            try
            {
                return serviceProvider.GetRequiredService<DefaultBusInstance>().Bus
                    ?? throw new InvalidOperationException("No default bus configured");
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

        return bus;
    }
}