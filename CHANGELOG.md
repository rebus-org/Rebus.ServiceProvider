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

[Hawxy]: https://github.com/Hawxy
[komainu85]: https://github.com/komainu85
[mattwhetton]: https://github.com/mattwhetton
[rosieks]: https://github.com/rosieks
[Slettan]: nhttps://github.com/Slettan
[trevorreeves]: https://github.com/trevorreeves
[Tsjunne]: https://github.com/Tsjunne
[weisro]: https://github.com/weisro