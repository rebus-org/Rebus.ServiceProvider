# Rebus.ServiceProvider

[![install from nuget](https://img.shields.io/nuget/v/Rebus.ServiceProvider.svg?style=flat-square)](https://www.nuget.org/packages/Rebus.ServiceProvider)

Provides an Microsoft.Extensions.DependencyInjection-based container adapter for [Rebus](https://github.com/rebus-org/Rebus).

![](https://raw.githubusercontent.com/rebus-org/Rebus/master/artwork/little_rebusbus2_copy-200x200.png)

---

## Usage

This container adapter is meant to be used with the generic host introduced with .NET Core 2.1, which has evolved into the ubiquitous hosting model for .NET.

It can still be used outside of the generic host, but that will require usage to follow a pattern like this:

```csharp
var services = new ServiceCollection();

services.AddRebus(...);

using var provider = services.BuildServiceProvider();

// THIS 👇 will start the bus(es)  
provider.StartRebusManually();
```

⚠ With the generic host (which is what you're using, if you've created a console app, a background worker, or a web app), the configuration extensions
in this package rely on `IHostedService` and how the host uses these, and therefore the above call to `StartRebusManually` shoule NOT be called.

ℹ With the generic host, there's two major modes of operation:

1. Starting one or more Rebus instances, using the host's container instance
1. Starting one or more Rebus instances, using one or more separate container instances

