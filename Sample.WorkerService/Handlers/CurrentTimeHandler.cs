using Rebus.Handlers;
using Sample.WorkerService.Messages;
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Sample.WorkerService.Handlers;

public class CurrentTimeHandler(ILogger<CurrentTimeHandler> logger) : IHandleMessages<PrintCurrentTime>
{
    public async Task Handle(PrintCurrentTime message) => logger.LogInformation("The time is {time}", DateTimeOffset.Now);
}