# Rebus.ServiceProvider

[![install from nuget](https://img.shields.io/nuget/v/Rebus.ServiceProvider.svg?style=flat-square)](https://www.nuget.org/packages/Rebus.ServiceProvider)

Provides an Microsoft.Extensions.DependencyInjection-based container adapter for [Rebus](https://github.com/rebus-org/Rebus).

![](https://raw.githubusercontent.com/rebus-org/Rebus/master/artwork/little_rebusbus2_copy-200x200.png)

---

## Intro

This container adapter is meant to be used with the generic host introduced with .NET Core 2.1, which has evolved into the ubiquitous hosting model for .NET.

### Quickstart

When you configure your services, do this to invoke the Rebus configuration spell:

```csharp
services.AddRebus(
    configure => configure
        .Transport(t => t.UseAzureServiceBus(connectionString, queueName))
);
```
and that is all there is to it! 😁 Rebus will use an `IHostedService` behind the covers to manage the Rebus instance, so it'll be started and stopped as the host conducts its background services.

If you need access to something that must be resolved from the container (e.g. configurations and stuff), there's an overload that passes the service provider to the
configurer:

```csharp
services.AddRebus(
    (configure, provider) => {
        var asb = provider.GetRequiredService<IOptions<AsbSettings>>();
        var connectionString = asb.ConnectionString;
        var queueName = asb.InputQueueName;
        
        return configure
            .Transport(t => t.UseAzureServiceBus(connectionString, queueName));
    }
);
```

If you want to subscribe to stuff at startup, use the `onCreated` callback:

```csharp
services.AddRebus(
    (configure, provider) => {
        var asb = provider.GetRequiredService<IOptions<AsbSettings>>();
        var connectionString = asb.ConnectionString;
        var queueName = asb.InputQueueName;
        
        return configure
            .Transport(t => t.UseAzureServiceBus(connectionString, queueName));
    },

    onCreated: async bus => {
        await bus.Subscribe<FirstEvent>();
        await bus.Subscribe<SecondEvent>();
    }
);
```

and that's basically it.

If you're interested in hosting multiple Rebus instances inside a single process, please read on. 🙂


### Logging

ℹ️ Please note that logging will be automatically configured  - if possible, and if you haven't configured anything yourself.

As logging is integrated with the host, Rebus will simply direct all of its logging to loggers created using
the `ILoggerFactory` provided by the host, so if you want to log by some other means (e.g. with Serilog), you can 
simply use the appropriate Serilog integration package and direct the host's logs to a Serilog sink.

It's still possible to e.g. append the usual `.Logging(l => l.Serilog())` if you want to do that, but it's often easier
to just let Rebus use the host logging mechanism, and then configure logging in the host like you would anyway.


### Hosting outside of the generic host

It can still be used outside of the generic host, but that will require usage to follow a pattern like this:

```csharp
var services = new ServiceCollection();

services.AddRebus(...);

using var provider = services.BuildServiceProvider();

// THIS 👇 will start the bus(es)  
provider.StartRebus();
```

### Hosting inside the generic host

⚠ With the generic host (which is what you're using, if you've created a console app, a background worker, or a web app), the configuration extensions
in this package rely on `IHostedService` and how the host uses these, and therefore the above call to `StartRebus()` shoule NOT be made.

ℹ With the generic host, there's two major modes of operation:

1. Starting one or more Rebus instances, using the host's container instance
1. Starting one or more Rebus instances, using one or more separate container instances

## Usage

### Starting one or more Rebus instances, using the host's container instance

When sharing the host's container instance, starting one or more Rebus instances is as easy as

```csharp
services.AddRebus(...);
```

as many times as you like. 

⚠ But please note that there can be only ONE PRIMARY Rebus instance so you'll most likely follow a pattern like this:

```csharp
// This one 👇 will be the primary bus instance
services.AddRebus(...);

services.AddRebus(isPrimaryBus: false, ...);

services.AddRebus(isPrimaryBus: false, ...);
```

if you want to add multiple Rebus instances. 

When you add a Rebus instance with `AddRebus`, you can configure it the way you're used to via its `RebusConfigurer` and its extensions, so it could be something like

```csharp
services.AddRebus(
    configure => configure
        .Transport(t => t.UseAzureServiceBus(connectionString, "my-queue-name"))
        .Serializer(s => s.UseSystemTextJson())
);
```

for a single bus instance (which is also the default), or something like

```csharp
services.AddRebus(
    configure => configure
        .Transport(t => t.UseAzureServiceBus(connectionString, "some-kind-of-processor"))
        .Serializer(s => s.UseSystemTextJson())
);

services.AddRebus(
    isPrimaryBus: false,

    configure: configure => configure
        .Transport(t => t.UseAzureServiceBus(connectionString, "some-kind-of-background-processor"))
        .Serializer(s => s.UseSystemTextJson())
);
```

to add two bus instances. If you want to subscribe to something when starting up, there's an optional `onCreated` parameter that makes this possible, e.g. like this:

```csharp
services.AddRebus(
    configure => configure
        .Transport(t => t.UseAzureServiceBus(connectionString, "my-queue-name"))
        .Serializer(s => s.UseSystemTextJson()),

    onCreated: async bus => {
        await bus.Subscribe<SomethingInterestingHappened>();
        await bus.Subscribe<AnotherInterestingThingHappened>();
    }
);
```

The callback passed as `onCreated` will be executed when the bus is created and operational, but before it has begun consuming messages.

### Starting one or more Rebus instances, using one or more separate container instances

When you want to host additional Rebus instances in the same process, but you want an extra degree of separation from your host application (e.g. if you're following the
"modular monolith" approach), you can call the `AddRebusService` on the host builder for each independent background service you would like to add.

Since the background services are separate from the host, they each have their own container instance, and so you'll need to register whatever stuff they need to work.

In your startup code, you'll most likely find something that goes like this:

```csharp
var builder = WebApplication.CreateBuilder(args);

// register stuff in builder.Services here

var app = builder.Build();

// maybe configure web app middlewares here

await app.RunAsync();
```

This builder has a `Host` property, which is where you will find the `AddRebusService` extension method:

```csharp
builder.Host.AddRebusService(
    services => (...)
);
```

It has a single callback, which is where you can configure the necessary services for the container instance dedicated to this background service. It requires that at
least ONE call be made to `AddRebus` like this:

```csharp
builder.Host.AddRebusService(
    services => services.AddRebus(...)
);
```

but otherwise it works like your normal service collection. This also means that it's totally fine to add multiple Rebus instance to it as well, if you like (but you'll
probably want to mostly stay away from that to avoid giving your team mates a headache 🤯).

A typical configuraion could look like this (assuming that you're into the aesthetics of wrapping your registrations behind `IServiceCollection` extensions):

```csharp
builder.Host.AddRebusService(
    services => {
        services.AddMyEfDatabaseContext();

        services.AddMyRepositories();
        
        services.AddMyApplicationServices();

        services.AddRebus(
            configure => configure
                .Transport(t => t.UseAzureServiceBus(connectionString, "my-queue-name"))
        );

        services.AddRebusHandler<SomeMessageHandler>();
        services.AddRebusHandler<AnotherMessageHandler>();
    }
);
```

ℹ When using the separate background service approach described here, the container will forward calls for

* `Microsoft.Extensions.Hosting.IHostApplicationLifetime`
* `Microsoft.Extensions.Logging.ILoggerFactory`

to the host's container, which essentially makes these things transparently available to the separate Rebus service.


### Primary bus instance?

When adding multiple bus instances to a single container instance, there's one big question that needs to be answered: Which bus instance will be returned if you resolve
`IBus` from it?

```csharp
var bus = serviceProvider.GetRequiredService<IBus>();

// ???
```

or if you have an `IBus` injected:

```csharp
public class SomethingPublisher
{
    public SomethingPublisher(IBus bus) => ... //< ???
}
```

This where the concept of "default bus" is relevant.

### Specific bus instances

If you need to be able to later resolve a specific bus instance from the service provider, you can register a bus with a key:
```csharp
services.AddRebus(
    configure => configure
        .Transport(t => t.UseAzureServiceBus(connectionString, queueName)),
    
    key: "my-favorite-bus"
);
```

Later, when your app is running, you will then be able to retrieve that specific bus instance via `IBusRegistry` like so:
```
var registry = provider.GetRequiredService<IBusRegistry>(); //< or have this injected

var bus = registry.GetBus("my-favorite-bus");

// voilá! 🎉
```


### Delayed start of the bus

When you configure the bus, e.g. with
```csharp
services.AddRebus(
    configure => configure
        .Transport(t => t.UseAzureServiceBus(connectionString, queueName)),

    key: "my-favorite-bus"
);
```
and then the host starts up (or you call the `StartRebus()` extension method on the service provider), the bus will automatically be started (i.e.
it will start consuming messages).

You can delay the time of when message consumption is begun by setting `startAutomatically: false` in the call to `AddRebus`:
```csharp
services.AddRebus(
    configure => configure
        .Transport(t => t.UseAzureServiceBus(connectionString, queueName)),

    key: "my-favorite-bus",
    startAutomatically: false //< the bus will be "started" with 0 workers, i.e. it will not consume anything
);
```

At a later time, when you think it's about time the bus gets to taste some sweet message goodness, you can start it via the registry:

```
var registry = provider.GetRequiredService<IBusRegistry>(); //< or have this injected

registry.StartBus("my-favorite-bus");

// voilá! 🎉
```

Since starting the bus this way requires that you retrieve it via the bus registry, it's a hard requirement that a KEY is provided when calling `AddRebus`.

