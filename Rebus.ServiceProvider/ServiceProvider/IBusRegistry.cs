using System.Collections.Generic;
using Rebus.Bus;

namespace Rebus.ServiceProvider;

/// <summary>
/// Registry of bus instances in a service provider instance. Holds bus instance that have been added to the container by calling
/// AddRebus(..., key: "some-key"), thus making it possible to later retrieve that particular bus instance.
/// </summary>
public interface IBusRegistry
{
    /// <summary>
    /// Gets the bus instance with the given <paramref name="key"/>. Throws a <see cref="KeyNotFoundException"/> if no such bus instance was registered
    /// </summary>
    IBus GetBus(string key);

    /// <summary>
    /// Tries to get the bus instance with the given <paramref name="key"/>. Returns true and sets the <paramref name="bus"/> out parameter if the bus was found,
    /// return false otherwise.
    /// </summary>
    bool TryGetBus(string key, out IBus bus);
    
    /// <summary>
    /// Gets whether a bus instance with the given key has been registered.
    /// </summary>
    bool ContainsKey(string key);
    
    /// <summary>
    /// Starts the bus instance with the given key. Can be used in cases where the bus has been added by calling AddRebus(..., startAutomatically: false) to deliberately
    /// delay the time when the bus starts consuming messages.
    /// </summary>
    IBus StartBus(string key);

    /// <summary>
    /// Gets the keys of all bus instances in this registry.
    /// </summary>
    /// <returns></returns>
    IReadOnlyList<string> GetAllKeys();
}