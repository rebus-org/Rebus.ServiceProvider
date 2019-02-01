using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Extensions;
using Rebus.Handlers;

namespace Rebus.ServiceProvider
{
    /// <summary>
    /// Extension methods for making it easy to register Rebus handlers in your <see cref="IServiceCollection"/>
    /// </summary>
    public static partial class ServiceCollectionExtensions
    {
        /// <summary>
        /// Automatically picks up all handler types from the assembly containing <typeparamref name="THandler"/> and registers them in the container
        /// </summary>
        /// <typeparam name="THandler">The type of the handler.</typeparam>
        /// <param name="services">The services.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static IServiceCollection AutoRegisterHandlersFromAssemblyOf<THandler>(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var assemblyToRegister = GetAssembly<THandler>();

            RegisterAssembly(services, assemblyToRegister);
            return services;
        }
        
        /// <summary>
        /// Automatically picks up all handler types from the specified assembly and registers them in the container
        /// </summary>
        /// <param name="services">The services</param>
        /// <param name="assemblyString">The long name of the assembly</param>
        public static IServiceCollection AutoRegisterHandlersFromAssembly(this IServiceCollection services, string assemblyString)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (string.IsNullOrEmpty(assemblyString))
                throw new ArgumentNullException(nameof(assemblyString));

            var assemblyName = new AssemblyName(assemblyString);

            var assembly = Assembly.Load(assemblyName);

            RegisterAssembly(services, assembly);

            return services;
        }

        static Assembly GetAssembly<THandler>()
        {
            return typeof(THandler).Assembly;
        }

        static IEnumerable<Type> GetImplementedHandlerInterfaces(Type type)
        {
            return type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandleMessages<>));

        }

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
                RegisterType(services, type.Type, true);
            }
        }

        static bool IsClass(Type type)
        {
            return !type.IsInterface && !type.IsAbstract;
        }

        static void RegisterType(IServiceCollection services, Type typeToRegister, bool auto)
        {
            var implementedHandlerInterfaces = GetImplementedHandlerInterfaces(typeToRegister).ToArray();

            if (!implementedHandlerInterfaces.Any())
                return;

            implementedHandlerInterfaces
                .ForEach(i => services.AddTransient(i, typeToRegister));
        }
    }
}
