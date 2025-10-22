using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Rebus.ServiceProvider.Internals;

class RebusBackgroundService(RebusInitializer rebusInitializer) : BackgroundService
{
    readonly RebusInitializer _rebusInitializer = rebusInitializer ?? throw new ArgumentNullException(nameof(rebusInitializer));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var (bus, _) = await _rebusInitializer._busAndEvents.Value;
        stoppingToken.Register(bus.Dispose);
    }
}