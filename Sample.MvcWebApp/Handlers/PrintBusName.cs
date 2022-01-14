using Rebus.Bus;
using Rebus.Handlers;
#pragma warning disable CS1998

namespace Sample.MvcWebApp.Handlers;

public class PrintBusName : IHandleMessages<string>
{
    readonly IBus _bus;

    public PrintBusName(IBus bus) => _bus = bus ?? throw new ArgumentNullException(nameof(bus));

    public async Task Handle(string message)
    {
        Console.WriteLine($"Message {message} handled by bus {_bus}");
    }
}