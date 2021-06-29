using System;
using System.Linq;
using System.Threading;
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
using Rebus.ServiceProvider;
using Rebus.Transport.InMem;

namespace Sample.WebApp
{
    public class Startup
    {
        private readonly ILoggerFactory _loggerFactory;
        public Startup(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }
        public void ConfigureServices(IServiceCollection services)
        {
            // Register handlers 
            services.AutoRegisterHandlersFromAssemblyOf<Handler1>();

            // when added here, the bus will be NOT have been disposed when the StopAsync method gets called
            services.AddHostedService<BackgroundServiceExample>();

            // Configure and register Rebus
            services.AddRebus(configure => configure
                .Logging(l => l.Use(new MSLoggerFactoryAdapter(_loggerFactory)))
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "Messages"))
                .Routing(r => r.TypeBased().MapAssemblyOf<Message1>("Messages")));
        
            // when added here, the bus will be disposed when the StopAsync method gets called
            //services.AddHostedService<BackgroundServiceExample>();
        }

        class BackgroundServiceExample : IHostedService
        {
            readonly BusLifetimeEvents _busLifetimeEvents;
            readonly ILogger<BackgroundServiceExample> _logger;

            bool _disposed;

            public BackgroundServiceExample(BusLifetimeEvents busLifetimeEvents, ILogger<BackgroundServiceExample> logger)
            {
                _busLifetimeEvents = busLifetimeEvents ?? throw new ArgumentNullException(nameof(busLifetimeEvents));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            }

            public async Task StartAsync(CancellationToken cancellationToken)
            {
                _busLifetimeEvents.BusDisposed += () => _disposed = true;

                _logger.LogInformation("BackgroundServiceExample is started");
            }

            public async Task StopAsync(CancellationToken cancellationToken)
            {
                var busDisposed = _disposed;

                _logger.LogInformation("BackgroundServiceExample is stopped - bus disposed={busDisposed}", busDisposed);
            }
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.ApplicationServices.UseRebus();
            //or optionally act on the bus
            //app.ApplicationServices.UseRebus(async bus => await bus.Subscribe<Message1>());

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
}
