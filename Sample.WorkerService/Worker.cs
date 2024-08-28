using Rebus.Bus;
using Sample.WorkerService.Messages;

namespace Sample.WorkerService
{
    public class Worker(IBus bus) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
                await bus.SendLocal(new PrintCurrentTime());
            }
        }
    }
}
