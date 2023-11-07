using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Rebus.Handlers;

namespace Sample.WebApp;

public class Handler1 : IHandleMessages<Message1>
{
    readonly ILogger _logger;

    public Handler1(ILogger<Handler1> logger)
    {
        _logger = logger;
    }

    public Task Handle(Message1 message)
    {
        _logger.LogInformation("Handler1 received : {message}", message);

        return Task.CompletedTask;
    }
}