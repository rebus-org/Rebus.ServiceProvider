using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Rebus.Bus;

namespace Rebus.ServiceProvider.Internals;

class ServiceProviderBusRegistry : IBusRegistry
{
    readonly ConcurrentDictionary<string, IBus> _instances = new();

    public IBus GetBus(string key)
    {
        return _instances.TryGetValue(key, out var result)
            ? result
            : throw new KeyNotFoundException($"Registry did not contain a bus instance with key '{key}'. The key is configured by calling AddRebus(...,  key: \"your-key\") when adding a bus to the container. Also, it's required that the host has been started (or that StartRebus() has been called on the service provider), because the bus instances do not exist before that.");
    }

    public bool TryGetBus(string key, out IBus bus) => _instances.TryGetValue(key, out bus);

    public void AddBus(IBus bus, string key)
    {
        if (_instances.TryAdd(key, bus)) return;

        throw new ArgumentException($"Cannot add bus {bus} with key '{key}' to the registry, because the bus {_instances[key]} was registered under that key");
    }
}