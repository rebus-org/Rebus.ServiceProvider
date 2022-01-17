using Rebus.Bus;
using Rebus.Handlers;
#pragma warning disable CS1998

namespace Sample.MvcWebApp.Handlers;

public class PrintReceivedMessageTogetherWithBusName : IHandleMessages<string>
{
    readonly IBus _bus;

    public PrintReceivedMessageTogetherWithBusName(IBus bus) => _bus = bus ?? throw new ArgumentNullException(nameof(bus));

    public async Task Handle(string message) => Console.WriteLine($"The string '{message}' was received by bus instance {_bus}");
}