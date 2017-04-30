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
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Automatically picks up all handler types from the assembly containing <typeparamref name="THandler"/> and registers them in the container
        /// </summary>
        /// <typeparam name="THandler">The type of the handler.</typeparam>
        /// <param name="services">The services.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static void AutoRegisterHandlersFromAssemblyOf<THandler>(this IServiceCollection services) where THandler : IHandleMessages
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var assemblyToRegister = GetAssembly<THandler>();

            RegisterAssembly(services, assemblyToRegister);
        }

        static Assembly GetAssembly<THandler>() where THandler : IHandleMessages
        {
#if NETSTANDARD1_6
            return typeof(THandler).GetTypeInfo().Assembly;
#else
            return typeof(THandler).Assembly;
#endif
        }

        static IEnumerable<Type> GetImplementedHandlerInterfaces(Type type)
        {
#if NETSTANDARD1_6
            return type.GetTypeInfo().GetInterfaces()
                .Where(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandleMessages<>));
#else
            return type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandleMessages<>));
#endif
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
#if NETSTANDARD1_6
            return !type.GetTypeInfo().IsInterface && !type.GetTypeInfo().IsAbstract;
#else
            return !type.IsInterface && !type.IsAbstract;
#endif
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
