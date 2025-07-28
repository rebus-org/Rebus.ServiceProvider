using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rebus.ServiceProvider.Internals;

namespace Rebus.Config;

/// <summary>
/// Rebus-relevant helper for <see cref="IServiceProvider"/>
/// </summary>
public static class ServiceProviderExtensions
{
    /// <summary>
    /// Can be used to start registered Rebus instance(s) manually, instead of letting the hosting environment do it. This method should only
    /// be called in situations where you've called AddRebus on your service collections and you are building your service provider OUTSIDE
    /// of the generic host.
    /// </summary>
    public static IServiceProvider StartHostedServices(this IServiceProvider serviceProvider)
    {
        if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));

        async Task StartHostedServicesAsync()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(1));
            var cancellationToken = timeout.Token;

            var disposalHelper = serviceProvider.GetRequiredService<RebusDisposalHelper>();
            var services = serviceProvider.GetServices<IHostedService>().ToList();

            foreach (var service in services)
            {
                await service.StartAsync(cancellationToken);

                Task StopService() => service.StopAsync(CancellationToken.None);

                disposalHelper.Add(new DisposableCallback(() => AsyncHelpers.RunSync(StopService)));
            }
        }

        AsyncHelpers.RunSync(StartHostedServicesAsync);

        return serviceProvider;
    }

    class DisposableCallback(Action disposed) : IDisposable
    {
        public void Dispose() => disposed();
    }
}