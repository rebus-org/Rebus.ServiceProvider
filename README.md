# Rebus.ServiceProvider

[![install from nuget](https://img.shields.io/nuget/v/Rebus.ServiceProvider.svg?style=flat-square)](https://www.nuget.org/packages/Rebus.ServiceProvider)

Provides an Microsoft.Extensions.DependencyInjection-based container adapter for [Rebus](https://github.com/rebus-org/Rebus).

![](https://raw.githubusercontent.com/rebus-org/Rebus/master/artwork/little_rebusbus2_copy-200x200.png)

---

## Usage

### In ASP.NET Core 2.0+ Startup.cs

```c#
public void ConfigureServices(IServiceCollection services)
{
    // Register handlers 
    services.AutoRegisterHandlersFromAssemblyOf<Handler1>();

    // Configure and register Rebus
    services.AddRebus(configure => configure
        .Logging(l => l.Use(new MSLoggerFactoryAdapter(_loggerFactory)))
        .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "Messages"))
        .Routing(r => r.TypeBased().MapAssemblyOf<Message1>("Messages")));
}
       
public void Configure(IApplicationBuilder app, IHostingEnvironment env)
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
```

(See the WebApp sample)

### A vanilla console app

```c#
var services = new ServiceCollection();

// Automatically register all handlers from the assembly of a given type...
services.AutoRegisterHandlersFromAssemblyOf<Handler1>();

//Configure Rebus
services.AddRebus(configure => configure
    .Logging(l => l.ColoredConsole())
    .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "Messages"))
    .Routing(r => r.TypeBased().MapAssemblyOf<Message1>("Messages")));

// Potentially add more service registrations for the application, some of which
// could be required by handlers.

// Make sure we correctly dispose of the provider (and therefore the bus) on application shutdown
using (var provider = services.BuildServiceProvider())
{
    // Application starting...

    // Now application is running, lets trigger the 'start' of Rebus.
    provider.UseRebus();
    
    //optionally...
    //provider.UseRebus(async bus => await bus.Subscribe<Message1>());
}
```

(See the ConsoleApp sample)
