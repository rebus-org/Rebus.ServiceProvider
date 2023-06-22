using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rebus.Bus;
using Rebus.Handlers;
using Rebus.Pipeline;
using Rebus.ServiceProvider;
using Rebus.ServiceProvider.Internals;

// ReSharper disable SimplifyLinqExpressionUseAll
// ReSharper disable UnusedMember.Global

namespace Rebus.Config;

/// <summary>
/// Extension methods for registering Rebus and message handlers in your <see cref="IServiceCollection"/>
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Rebus to the service collection, invoking the <paramref name="configure"/> callback to allow for executing Rebus' configuration spell.
    /// The <paramref name="isDefaultBus"/> parameter indicates whether resolving <see cref="IBus"/> from the resulting service provider outside of a Rebus
    /// handler should yield THIS particular bus instance. Please note that there can be only 1 default bus per container instance! And please note that
    /// Rebus handlers (and any services injected into them) will always have the <see cref="IBus"/> from the current message context injected into them.
    /// </summary>
    /// <param name="services">
    /// Reference to the service collection that this extension method is invoked on
    /// </param>
    /// <param name="configure">
    /// Configuration callback that can be used to invoke the Rebus configuration spell
    /// </param>
    /// <param name="isDefaultBus">
    /// Indicates whether resolving <see cref="IBus"/> from the resulting service provider outside of a Rebus
    /// handler should yield this particular bus instance. Please note that there can be only 1 default bus per container instance! And please note that
    /// Rebus handlers (and any services injected into them) will always have the <see cref="IBus"/> from the current message context injected into them.
    /// </param>
    /// <param name="onCreated">
    /// Optionally provides an asynchronous callback, which will be executed once the bus is operational, but before it has been started (i.e. begun receiving messages). This is a good place to establish any subscriptions required for the bus.
    /// </param>
    /// <param name="key">
    /// Optional key for the bus, which enables later retrieval of this specific bus instance by resolving <see cref="IBusRegistry"/> and calling <see cref="IBusRegistry.GetBus"/>
    /// </param>
    /// <param name="startAutomatically">
    /// Configures whether this bus should be started automatically (i.e. whether message consumption should begin) when the host starts up (or when StartRebus() is called on the service provider).
    /// Setting this to false should be combined with providing a <paramref name="key"/>, because the bus can then be started by resolving <see cref="IBusRegistry"/> and calling <see cref="IBusRegistry.StartBus"/> on it.
    /// </param>
    public static IServiceCollection AddRebus(this IServiceCollection services, Func<RebusConfigurer, RebusConfigurer> configure, bool isDefaultBus = true, Func<IBus, Task> onCreated = null, string key = null, bool startAutomatically = true)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        return AddRebus(services, (configurer, _) => configure(configurer), isDefaultBus: isDefaultBus, onCreated: onCreated, key: key, startAutomatically: startAutomatically);
    }

    /// <summary>
    /// Adds Rebus to the service collection, invoking the <paramref name="configure"/> callback to allow for executing Rebus' configuration spell.
    /// The <paramref name="isDefaultBus"/> parameter indicates whether resolving <see cref="IBus"/> from the resulting service provider outside of a Rebus
    /// handler should yield THIS particular bus instance. Please note that there can be only 1 default bus per container instance! And please note that
    /// Rebus handlers (and any services injected into them) will always have the <see cref="IBus"/> from the current message context injected into them.
    /// </summary>
    /// <param name="services">
    /// Reference to the service collection that this extension method is invoked on
    /// </param>
    /// <param name="configure">
    /// Configuration callback that can be used to invoke the Rebus configuration spell
    /// </param>
    /// <param name="isDefaultBus">
    /// Indicates whether resolving <see cref="IBus"/> from the resulting service provider outside of a Rebus
    /// handler should yield this particular bus instance. Please note that there can be only 1 default bus per container instance! And please note that
    /// Rebus handlers (and any services injected into them) will always have the <see cref="IBus"/> from the current message context injected into them.
    /// </param>
    /// <param name="onCreated">
    /// Optionally provides an asynchronous callback, which will be executed once the bus is operational, but before it has been started (i.e. begun receiving messages). This is a good place to establish any subscriptions required for the bus.
    /// </param>
    /// <param name="key">
    /// Optional key for the bus, which enables later retrieval of this specific bus instance by resolving <see cref="IBusRegistry"/> and calling <see cref="IBusRegistry.GetBus"/>
    /// </param>
    /// <param name="startAutomatically">
    /// Configures whether this bus should be started automatically (i.e. whether message consumption should begin) when the host starts up (or when StartRebus() is called on the service provider).
    /// Setting this to false should be combined with providing a <paramref name="key"/>, because the bus can then be started by resolving <see cref="IBusRegistry"/> and calling <see cref="IBusRegistry.StartBus"/> on it.
    /// </param>
    public static IServiceCollection AddRebus(this IServiceCollection services, Func<RebusConfigurer, IServiceProvider, RebusConfigurer> configure, bool isDefaultBus = true, Func<IBus, Task> onCreated = null, string key = null, bool startAutomatically = true)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        if (!startAutomatically && key == null)
        {
            throw new ArgumentException($"Cannot add bus with startAutomatically=false and key=null. When configuring the bus to not automatically be started, a key must be provided, so that it's possible to look up the bus instance later on via IBusRegistry");
        }

        if (!services.Any(s => s.ServiceType == typeof(RebusDisposalHelper)))
        {
            // important to create this one in a way where the container assumes responsibility of disposing it
            services.AddSingleton<RebusDisposalHelper>();
            // NOTE: this was added to support disposal in scenarios where Rebus is hosted with a service provider OUTSIDE of the generic host
        }

        if (isDefaultBus)
        {
            if (services.Any(s => s.ServiceType == typeof(DefaultBusInstance)))
            {
                throw new InvalidOperationException("Detected that the service collection already contains a default bus registration - please make only one single AddRebus call with isDefaultBus:true");
            }

            services.AddSingleton(p => new RebusInitializer(startAutomatically, key, configure, onCreated, p, isDefaultBus, p.GetService<IHostApplicationLifetime>()));
            services.AddSingleton(p =>
                                  {
                                      var defaultBusInstance = new DefaultBusInstance();
                                      defaultBusInstance.SetInstanceResolver(p.GetRequiredService<RebusInitializer>()._busAndEvents);
                                      return defaultBusInstance;
                                  });
            services.AddSingleton<IHostedService>(p => new RebusBackgroundService(p.GetRequiredService<RebusInitializer>()));
        }
        else
        {
            services.AddSingleton<IHostedService>(p => new RebusBackgroundService(new RebusInitializer(startAutomatically, key, configure, onCreated, p, isDefaultBus, p.GetService<IHostApplicationLifetime>())));
        }

        if (!services.Any(s => s.ImplementationType == typeof(RebusResolver)))
        {
            services.AddSingleton(new RebusResolver());
            services.AddSingleton(new ServiceProviderBusRegistry());
            services.AddSingleton<IBusRegistry>(p => p.GetRequiredService<ServiceProviderBusRegistry>());
            services.AddTransient(p => p.GetRequiredService<RebusResolver>().GetBus(p));
            services.AddTransient(p => p.GetRequiredService<IBus>().Advanced.SyncBus);
            services.AddTransient(p => p.GetRequiredService<IBus>().Advanced.DataBus);
            services.AddTransient(_ => MessageContext.Current ?? throw new InvalidOperationException("Could not get current message context! The message context can only be resolved when handling a Rebus message, and it looks like this attempt was made from somewhere else."));
            services.AddTransient(p => p.GetRequiredService<DefaultBusInstance>().BusLifetimeEvents);
        }

        return services;
    }

    /// <summary>
    /// Registers the given <typeparamref name="THandler"/> with a transient lifestyle
    /// </summary>
    public static IServiceCollection AddRebusHandler<THandler>(this IServiceCollection services) where THandler : IHandleMessages
    {
        return AddRebusHandler(services, typeof(THandler));
    }

    /// <summary>
    /// Register the given <paramref name="typeToRegister"/> with a transient lifestyle
    /// </summary>
    public static IServiceCollection AddRebusHandler(this IServiceCollection services, Type typeToRegister)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (typeToRegister == null) throw new ArgumentNullException(nameof(typeToRegister));

        RegisterType(services, typeToRegister);

        return services;
    }

    /// <summary>
    /// Registers the given <typeparamref name="THandler"/> with a transient lifestyle
    /// </summary>
    public static IServiceCollection AddRebusHandler<THandler>(this IServiceCollection services, Func<IServiceProvider, THandler> factory) where THandler : IHandleMessages
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        RegisterFactory(services, typeof(THandler), provider => factory(provider));

        return services;
    }

    /// <summary>
    /// Automatically picks up all handler types from the assembly containing <typeparamref name="THandler"/> and registers them in the container
    /// </summary>
    /// <typeparam name="THandler">The type of the handler.</typeparam>
    /// <param name="services">The services.</param>
    /// <exception cref="System.ArgumentNullException"></exception>
    public static IServiceCollection AutoRegisterHandlersFromAssemblyOf<THandler>(this IServiceCollection services)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        var assemblyToRegister = GetAssembly<THandler>();

        RegisterAssembly(services, assemblyToRegister);

        return services;
    }

    /// <summary>
    /// Automatically picks up all handler types from the specified assembly and registers them in the container
    /// </summary>
    /// <param name="services">The services</param>
    /// <param name="assembly">The assembly to scan</param>
    public static IServiceCollection AutoRegisterHandlersFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (assembly == null) throw new ArgumentNullException(nameof(assembly));

        RegisterAssembly(services, assembly);

        return services;
    }

    /// <summary>
    /// Automatically picks up all handler types from the specified assembly and registers them in the container
    /// </summary>
    /// <param name="services">The services</param>
    /// <param name="assemblyString">The long name of the assembly</param>
    public static IServiceCollection AutoRegisterHandlersFromAssembly(this IServiceCollection services, string assemblyString)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (string.IsNullOrEmpty(assemblyString)) throw new ArgumentNullException(nameof(assemblyString));

        var assemblyName = new AssemblyName(assemblyString);
        var assembly = Assembly.Load(assemblyName);

        RegisterAssembly(services, assembly);

        return services;
    }

    /// <summary>
    /// Registers all Rebus message handler types found in the assembly of <typeparamref name="T"/> under the namespace that type lives
    /// under. So all types within the same namespace will get mapped as handlers, but not types under other namespaces. This allows
    /// you to separate messages for specific queues by namespace and register them all in one go.
    /// </summary>
    public static IServiceCollection AutoRegisterHandlersFromAssemblyNamespaceOf<T>(this IServiceCollection services)
    {
        return services.AutoRegisterHandlersFromAssemblyNamespaceOf(typeof(T));
    }

    /// <summary>
    /// Registers all Rebus message handler types found in the assembly of <paramref name="handlerType"/> under the namespace that type lives
    /// under. So all types within the same namespace will get mapped as handlers, but not types under other namespaces. This allows
    /// you to separate messages for specific queues by namespace and register them all in one go.
    /// </summary>
    public static IServiceCollection AutoRegisterHandlersFromAssemblyNamespaceOf(this IServiceCollection services, Type handlerType)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (handlerType == null) throw new ArgumentNullException(nameof(handlerType));

        RegisterAssembly(services, handlerType.Assembly, handlerType.Namespace);

        return services;
    }

    static Assembly GetAssembly<THandler>() => typeof(THandler).Assembly;

    static IEnumerable<Type> GetImplementedHandlerInterfaces(Type type) =>
        type.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandleMessages<>));

    static void RegisterAssembly(IServiceCollection services, Assembly assemblyToRegister, string namespaceFilter = null)
    {
        var typesToAutoRegister = assemblyToRegister.GetTypes()
            .Where(IsClass)
            .Select(type => new
            {
                Type = type,
                ImplementedHandlerInterfaces = GetImplementedHandlerInterfaces(type).ToList()
            })
            .Where(a => a.ImplementedHandlerInterfaces.Any());

        if (!string.IsNullOrEmpty(namespaceFilter))
        {
            typesToAutoRegister = typesToAutoRegister.Where(a =>
                a.Type.Namespace != null && a.Type.Namespace.StartsWith(namespaceFilter));
        }

        foreach (var type in typesToAutoRegister)
        {
            RegisterType(services, type.Type);
        }
    }

    static bool IsClass(Type type) => !type.IsInterface && !type.IsAbstract;

    static void RegisterFactory(IServiceCollection services, Type typeToRegister, Func<IServiceProvider, object> factory)
    {
        var implementedHandlerInterfaces = GetImplementedHandlerInterfaces(typeToRegister).ToArray();

        foreach (var handlerInterface in implementedHandlerInterfaces)
        {
            services.AddTransient(handlerInterface, factory);
        }
    }

    static void RegisterType(IServiceCollection services, Type typeToRegister)
    {
        var implementedHandlerInterfaces = GetImplementedHandlerInterfaces(typeToRegister).ToArray();

        foreach (var handlerInterface in implementedHandlerInterfaces)
        {
            services.AddTransient(handlerInterface, typeToRegister);
        }
    }
}