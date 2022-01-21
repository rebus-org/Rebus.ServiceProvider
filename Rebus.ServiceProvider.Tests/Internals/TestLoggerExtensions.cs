using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Rebus.ServiceProvider.Tests.Internals;

public static class TestLoggerExtensions
{
    public static ServiceCollection WithTestLogger(this ServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddConsole();

            builder.SetMinimumLevel(LogLevel.Trace);
        });
        return services;
    }
}