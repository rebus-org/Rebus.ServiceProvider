using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rebus.Bus;
using Rebus.Internals;

namespace Rebus.Config;

public static class NewServiceProviderExtensions
{
    public static IServiceProvider UseRebus(this IServiceProvider serviceProvider)
    {
        if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));

        static Task Noop(IBus bus) => Task.CompletedTask;

        serviceProvider.UseRebus(Noop);

        return serviceProvider;
    }

    public static IServiceProvider UseRebus(this IServiceProvider serviceProvider, Func<IBus, Task> startAction)
    {
        if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));
        if (startAction == null) throw new ArgumentNullException(nameof(startAction));

        AsyncHelpers.RunSync(async () =>
        {
            var services = serviceProvider.GetServices<IHostedService>()
                .OfType<RebusHostedService>()
                .Where(service => !service.IsStarted)
                .ToList();

            foreach (var service in services)
            {
                await service.StartAsync(CancellationToken.None);
            }

            var bus = serviceProvider.GetRequiredService<IBus>();

            await startAction(bus);

            var hostApplicationLifetime = serviceProvider.GetService<IHostApplicationLifetime>();

            if (hostApplicationLifetime != null)
            {
                services.Reverse();

                hostApplicationLifetime.ApplicationStopping.Register(() =>
                {
                    AsyncHelpers.RunSync(async () =>
                    {
                        foreach (var service in services)
                        {
                            await service.StopAsync(CancellationToken.None);
                        }
                    });
                });
            }
        });

        return serviceProvider;
    }
}