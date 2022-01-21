using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Rebus.Bus;
using Rebus.Config;

namespace Rebus.ServiceProvider.Internals;

class ServiceProviderBusRegistry : IBusRegistry
{
    readonly ConcurrentDictionary<string, IBusStarter> _starters = new();
    readonly ConcurrentDictionary<string, IBus> _buses = new();

    public IBus GetBus(string key)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        return _buses.TryGetValue(key, out var result)
            ? result
            : throw GetKeyNotFoundException(key);
    }

    public bool TryGetBus(string key, out IBus bus)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        return _buses.TryGetValue(key, out bus);
    }

    public bool ContainsKey(string key) => _buses.ContainsKey(key);

    public IBus StartBus(string key)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        var starter = _starters.TryGetValue(key, out var result)
            ? result
            : throw GetKeyNotFoundException(key);

        return starter.Start();
    }

    public IReadOnlyList<string> GetAllKeys() => _buses.Keys.ToList().AsReadOnly();

    public void AddBus(IBus bus, IBusStarter busStarter, string key)
    {
        if (!_buses.TryAdd(key, bus))
        {
            throw new ArgumentException($"Cannot add bus {bus} with key '{key}' to the registry, because the bus {_buses[key]} was registered under that key");
        }

        _starters[key] = busStarter;
    }

    static KeyNotFoundException GetKeyNotFoundException(string key)
    {
        return new KeyNotFoundException($"Registry did not contain a bus instance with key '{key}'. The key is configured by calling AddRebus(...,  key: \"your-key\") when adding a bus to the container. Also, it's required that the host has been started (or that StartRebus() has been called on the service provider), because the bus instances do not exist before that.");
    }
}