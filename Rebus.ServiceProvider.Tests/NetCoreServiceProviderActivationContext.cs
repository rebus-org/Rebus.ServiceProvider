using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Extensions;
using Rebus.Handlers;
using Rebus.Tests.Contracts.Activation;

namespace Rebus.ServiceProvider.Tests
{
    public class NetCoreServiceProviderActivationContext : IActivationContext
    {
        public IHandlerActivator CreateActivator(Action<IHandlerRegistry> handlerConfig, out IActivatedContainer container)
        {
            var services = new ServiceCollection();
            handlerConfig.Invoke(new HandlerRegistry(services));

            var provider = services.BuildServiceProvider();
            container = new ActivatedContainer(provider);

            return new NetCoreServiceProviderContainerAdapter(provider);
        }

        public IBus CreateBus(Action<IHandlerRegistry> handlerConfig, Func<RebusConfigurer, RebusConfigurer> configureBus, out IActivatedContainer container)
        {
            var services = new ServiceCollection();
            handlerConfig.Invoke(new HandlerRegistry(services));

            services.AddRebus(configureBus);

            var provider = services.BuildServiceProvider();
            container = new ActivatedContainer(provider);

            return provider.UseRebus();
        }

        private class HandlerRegistry : IHandlerRegistry
        {
            private readonly IServiceCollection _services;

            public HandlerRegistry(IServiceCollection services)
            {
                _services = services;
            }

            public IHandlerRegistry Register<THandler>() where THandler : IHandleMessages
            {
                GetHandlerInterfaces(typeof(THandler))
                    .ForEach(i => _services.AddTransient(i, typeof(THandler)));

                return this;
            }
        }

        private class ActivatedContainer : IActivatedContainer
        {
            private readonly IServiceProvider _provider;

            public ActivatedContainer(IServiceProvider provider)
            {
                _provider = provider;
            }

            public IBus ResolveBus()
            {
                return _provider.GetRequiredService<IBus>();
            }

            public void Dispose()
            {
                ResolveBus().Dispose();
            }
        }

        static IEnumerable<Type> GetHandlerInterfaces(Type type)
        {
#if NETSTANDARD1_6
            return type.GetTypeInfo().GetInterfaces()
                .Where(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandleMessages<>))
                .ToArray();
#else
            return type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandleMessages<>))
                .ToArray();
#endif
        }
    }
}
