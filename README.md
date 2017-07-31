# Rebus.ServiceProvider

[![install from nuget](https://img.shields.io/nuget/v/Rebus.ServiceProvider.svg?style=flat-square)](https://www.nuget.org/packages/Rebus.ServiceProvider)

Provides an ASP.NET Core Service Provider-based container adapter for [Rebus](https://github.com/rebus-org/Rebus).

![](https://raw.githubusercontent.com/rebus-org/Rebus/master/artwork/little_rebusbus2_copy-200x200.png)

---

## Usage

### In a Startup.cs

```c#
public void ConfigureServices(IServiceCollection services)
{
   // Register handlers 
   services.AutoRegisterHandlersFromAssemblyOf<Handler1>();

   // Configure and register Rebus
   services.AddRebus(configure => configure
       .Logging(l => l.None()))
       .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "Messages"))
       .Routing(r => r.TypeBased().MapAssemblyOf<Message1>("Messages")));
}

public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
{
     app
         .UseDeveloperExceptionPage()
         .UseRebus()
		 // or optionally act on the bus:
		 // .UseRebus(async bus => await bus.Subscribe<Message1>())
         .Run(async (context) =>
         {
             var bus = app.ApplicationServices.GetRequiredService<IBus>();

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

// Configure Rebus
services.AddRebus(configure => configure
   .Logging(l => l.ColoredConsole())
   .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "Messages"))
   .Routing(r => r.TypeBased().MapAssemblyOf<Message1>("Messages")));
                
// Potentially add more service registrations for the application, some of which
// could be required by handlers.

// Application starting...
var provider = services.BuildServiceProvider();

// Application started...

// trigger the 'start' of Rebus (the IBus is created, and will immediately start 'listening' for messages).
provider.UseRebus();
```

(See the ConsoleApp sample)


