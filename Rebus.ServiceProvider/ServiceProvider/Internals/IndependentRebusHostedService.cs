using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Rebus.ServiceProvider.Internals;

class IndependentRebusHostedService : IHostedService
{
    readonly ConcurrentStack<IHostedService> _hostedServices = new();
    readonly Action<IServiceCollection> _configureServices;

    IServiceProvider _serviceProvider;
    ILogger<IndependentRebusHostedService> _logger;

    public IndependentRebusHostedService(Action<IServiceCollection> configureServices) => _configureServices = configureServices ?? throw new ArgumentNullException(nameof(configureServices));

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var services = new ServiceCollection();

        _configureServices(services);

        _serviceProvider = services.BuildServiceProvider();

        _logger = _serviceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger<IndependentRebusHostedService>();

        try
        {
            _logger.LogDebug("Resolving hosted services");

            var hostedServices = _serviceProvider.GetServices<IHostedService>();

            foreach (var hostedService in hostedServices)
            {
                _logger.LogDebug("Starting {hostedServiceType}", hostedService.GetType());

                try
                {
                    await hostedService.StartAsync(cancellationToken);

                    _logger.LogInformation("Hosted service {hostedServiceType} successfully started", hostedService.GetType());
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // we're on our way out
                    _logger.LogDebug("Hosted service {hostedServiceType} startup cancelled", hostedService.GetType());
                    throw;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Error when starting hosted service {hostedServiceType}", hostedService.GetType());
                    throw;
                }

                _hostedServices.Push(hostedService);
            }
        }
        catch
        {
            try
            {
                // maybe pass another cancellation token here, because this could cause shutdowns to be cancelled
                // even for properly started services
                await StopStartedServices(cancellationToken);
            }
            finally
            {
                if (_serviceProvider is IDisposable disposable)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch
                    {
                        // ignored to avoid exception thrown by pre v6 versions of service provider if it has async disposables in it
                    }
                }
            }

            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await StopStartedServices(cancellationToken);
    }

    async Task StopStartedServices(CancellationToken cancellationToken)
    {
        while (_hostedServices.TryPop(out var hostedService))
        {
            try
            {
                _logger.LogDebug("Stopping {hostedServiceType}", hostedService.GetType());

                await hostedService.StopAsync(cancellationToken);

                _logger.LogInformation("Hosted service {hostedServiceType} successfully stopped", hostedService.GetType());
            }
            catch (Exception exception)
            {
                _logger?.LogError(exception, "Error when stopping hosted service {hostedServiceType}", hostedService.GetType());
            }
        }
    }
}