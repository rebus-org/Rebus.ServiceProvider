using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.Transport.InMem;

namespace Sample.WebApp;

public class Startup
{
    readonly ILoggerFactory _loggerFactory;
    public Startup(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }
    public void ConfigureServices(IServiceCollection services)
    {
        // Register handlers 
        services.AutoRegisterHandlersFromAssemblyOf<Handler1>();

        // Configure and register Rebus
        services.AddRebus(configure => configure
            //.Logging(l => ...) //< do not configure logging - it will automatically use the host's logging
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "Messages"))
            .Routing(r => r.TypeBased().MapAssemblyOf<Message1>("Messages")));
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.Run(async (context) =>
        {
            var bus = app.ApplicationServices.GetRequiredService<IBus>();
            var logger = _loggerFactory.CreateLogger<Startup>();

            logger.LogInformation("Publishing {MessageCount} messages", 10);

            await Task.WhenAll(
                Enumerable.Range(0, 10)
                    .Select(i => new Message1())
                    .Select(message => bus.Send(message)));

            await context.Response.WriteAsync("Rebus sent another 10 messages!");
        });
    }
}