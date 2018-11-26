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


[komainu85]: https://github.com/komainu85
[mattwhetton]: https://github.com/mattwhetton
[rosieks]: https://github.com/rosieks
[trevorreeves]: https://github.com/trevorreeves
[weisro]: https://github.com/weisro