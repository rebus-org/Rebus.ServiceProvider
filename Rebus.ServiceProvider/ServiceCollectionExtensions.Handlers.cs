using Microsoft.Extensions.DependencyInjection;
using Rebus.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
// ReSharper disable UnusedMember.Global

namespace Rebus.ServiceProvider
{
    /// <summary>
    /// Extension methods for making it easy to register Rebus handlers in your <see cref="IServiceCollection"/>
    /// </summary>
    public static partial class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the given <typeparamref name="THandler"/> with a transient lifestyle
        /// </summary>
        public static IServiceCollection AddRebusHandler<THandler>(this IServiceCollection services) where THandler : IHandleMessages
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            RegisterType(services, typeof(THandler));

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

        static Assembly GetAssembly<THandler>() => typeof(THandler).Assembly;

        static IEnumerable<Type> GetImplementedHandlerInterfaces(Type type) =>
            type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandleMessages<>));

        static void RegisterAssembly(IServiceCollection services, Assembly assemblyToRegister)
        {
            var typesToAutoRegister = assemblyToRegister.GetTypes()
                .Where(IsClass)
                .Select(type => new
                {
                    Type = type,
                    ImplementedHandlerInterfaces = GetImplementedHandlerInterfaces(type).ToList()
                })
                .Where(a => a.ImplementedHandlerInterfaces.Any());

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
}
