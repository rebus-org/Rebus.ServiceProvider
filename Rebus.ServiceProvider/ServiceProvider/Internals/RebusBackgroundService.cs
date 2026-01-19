using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Rebus.Bus;
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Rebus.ServiceProvider.Internals;

class RebusBackgroundService(RebusInitializer rebusInitializer) : IHostedService
{
    readonly RebusInitializer _rebusInitializer = rebusInitializer ?? throw new ArgumentNullException(nameof(rebusInitializer));

    IBus _bus;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var (bus, _) = await _rebusInitializer._busAndEvents.Value;

        _bus = bus;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _bus?.Dispose();
    }
}