# Changelog

## 2.0.0-a1
* Test release

## 2.0.0-b01
* Test release

## 2.0.0
* Release 2.0.0

## 3.0.0
* Update to Rebus 3

## 4.0.0
* Update to Rebus 4
* Add .NET Core support (netstandard 1.6)
* Additional assembly scanning registration method - thanks [komainu85]
* Update contracts dep - thanks [trevorreeves]
* Add usage sample and bus callback for startup actions - thanks [trevorreeves]
* Pass provider to configuration action - thanks [mattwhetton]
* Fix `AddScoped` - thanks [rosieks]


## 4.0.1
* Remove type constraint on the `AutoRegisterHandlersFromAssemblyOf` extension

## 4.0.2
* Make `AutoRegister(...)` methods return `IServiceCollection` for the sake of the builder pattern - thanks [weisro]

## 5.0.0
* Remove ASP.NET Core dependency and make it into an adapter for Microsoft.Extensions.DependencyInjection (the way it's supposed to be) - thanks [Hawxy]
* Speed up resolution by a factor of 3 to 4
* Cleaner separation between registration (stuff that happens to the service collection) and resolution (stuff that happens to the service provider)
* Detect `ObjectDisposedException` when resolving handlers and interpret that as we're being shut down
* Update to Rebus 6 - thanks [Slettan]

## 5.0.1
* Leave disposal of `ILifetimeScope` to creator, when a custom scope instance was provided by an incoming step

## 5.0.2
* Fix polymorphic resolution of handlers compatible with `IFailed<TMessage>` - thanks [Tsjunne]

## 5.0.3
* Duplicate handler resolution when using Lamar - thanks [jorenp]

## 5.0.4
* Additional handler registration overload

## 5.0.5
* Provide access to `BusLifetimeEvents` through container

## 5.0.6
* Reference abstractions library containing `IServiceCollection` instead of implementation library - thanks [jorenp]

## 6.0.0
* Update Microsoft.Extensions.DependencyInjection.Abstractions dependency to 3.0.0
* Resolve handler activator as `IHandlerActivator` from container to enable decoration, replacement, etc. - thanks [skwasjer]
* Add `AddRebusHandler` overload that accepts a `Type` parameter instead of a type argument via generics - thanks [zlepper]

## 6.1.0

* Resolve handler activator as IHandlerActivator from container to enable decoration, replacement, etc. - thanks [skwasjer]

## 6.2.0
* Add additional targets for .NET Standard 2.1 and .NET 5 - thanks [dariogriffo]

## 6.3.0
* Add async overload of `UseRebus`, so you can `serviceProvider.UseRebus(async bus => /* do async stuff */)` - thanks [skwasjer]

## 6.3.1
* Make version range more tolerant, so .NET Standard 2.0 package can use the latest abstractions - thanks [zlepper]

## 6.4.0
* Better handling of polymorphic dispatch when using generics at multiple levels - thanks [TobiasNissen]

## 6.4.1
* Minor bugfix for constrained covariant type parameters - thanks [TobiasNissen]

## 7.0.0
* Depend on Microsoft.Extensions.Hosting.Abstractions instead, and take advantage of background services to dispose the bus properly
* Add target for .NET 6.0 - thanks [meronz]

## 8.0.0-b03
* First attempt at implementing multi-hosting support: Now multiple calls can be made to `AddRebus`, just remember to pass `isDefaultBus:false` to all but one of them. The default is that `AddRebus` results in the registration of a primary bus to keep behavior as true to the original behavior as possible.
* Add ability to add a key to each bus instance via `key: "whatever"` when calling `AddRebus`, allowing for later retrieval via `IBusRegistry`
* Add ability to not automatically start a bus instance when starting up, thus deferring to when `StartBus(key)` is called on `IBusRegistry`

## 8.0.0-b04
* Improved error messages when no default bus is registered, bus not started, etc.

## 8.0.0-b05
* Move initialization of Rebus instances to a `Lazy<Task<>>`, thus making initialization oblivious of whether it was initiated by the background service hosting the instance, or because it was to be injected into something else (e.g. another background service)

## 8.0.0-b06
* Fix bug that would prevent `IBus` from being resolved in certain situations - thanks [AYss]

## 8.0.0
* Publish real version (no changes compared to b06)

## 8.1.0
* Add `AddRebusService` overload that accepts list of types to forward to the host container - thanks [sbyse]

## 8.2.0
* Add `AddRebusService` overloads that accept callbacks have `HostBuilderContext` passed to them - thanks [SamPersson]

## 8.3.0
*  Make ServiceProvider available in outgoing steps again - thanks [SamPersson]

## 8.3.1
* Fix host container type forwarding to work also in cases where a type has multiple implementations - thanks [mattwcole]

## 8.4.0
* Add ability to target .NET 7 by extending allowed version ranges of dependencies - thanks [SbiCA]

## 9.0.0-b01
* Target .NET Standard and .NET 6 to be able to support async scopes (and thus disposing `IAsyncDisposable` things)

[AYss]: https://github.com/AYss
[dariogriffo]: https://github.com/dariogriffo
[Hawxy]: https://github.com/Hawxy
[jorenp]: https://github.com/jorenp
[komainu85]: https://github.com/komainu85
[mattwcole]: https://github.com/mattwcole
[mattwhetton]: https://github.com/mattwhetton
[meronz]: https://github.com/meronz
[rosieks]: https://github.com/rosieks
[SamPersson]: https://github.com/SamPersson
[SbiCA]: https://github.com/SbiCA
[sbyse]: https://github.com/sbyse
[skwasjer]: https://github.com/skwasjer
[Slettan]: nhttps://github.com/Slettan
[skwasjer]: https://github.com/skwasjer
[TobiasNissen]: https://github.com/TobiasNissen
[trevorreeves]: https://github.com/trevorreeves
[Tsjunne]: https://github.com/Tsjunne
[weisro]: https://github.com/weisro
[zlepper]: https://github.com/zlepper
