using System;
using System.Collections.Generic;
using System.Linq;
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
            var services = new ServiceCollection().AddSingleton(p => new DependencyInjectionHandlerActivator(p));
            handlerConfig.Invoke(new HandlerRegistry(services));
            
            var provider = services.BuildServiceProvider();

            container = new ActivatedContainer(provider);

            return provider.GetRequiredService<DependencyInjectionHandlerActivator>();
        }

        public IBus CreateBus(Action<IHandlerRegistry> handlerConfig, Func<RebusConfigurer, RebusConfigurer> configureBus, out IActivatedContainer container)
        {
            var services = new ServiceCollection();
            handlerConfig.Invoke(new HandlerRegistry(services));

            services.AddRebus(configureBus);

            var provider = services.BuildServiceProvider();
            container = new ActivatedContainer(provider);

            provider.UseRebus();

            return container.ResolveBus();
        }

        class HandlerRegistry : IHandlerRegistry
        {
            readonly IServiceCollection _services;

            public HandlerRegistry(IServiceCollection services)
            {
                _services = services;
            }

            public IHandlerRegistry Register<THandler>() where THandler : class, IHandleMessages
            {
                GetHandlerInterfaces(typeof(THandler))
                    .ForEach(i => _services.AddTransient(i, typeof(THandler)));

                return this;
            }

            static IEnumerable<Type> GetHandlerInterfaces(Type type)
            {
                return type.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandleMessages<>))
                    .ToArray();
            }
        }

        class ActivatedContainer : IActivatedContainer
        {
            readonly Microsoft.Extensions.DependencyInjection.ServiceProvider _provider;

            public ActivatedContainer(Microsoft.Extensions.DependencyInjection.ServiceProvider provider)
            {
                _provider = provider;
            }

            public IBus ResolveBus()
            {
                return _provider.GetRequiredService<IBus>();
            }

            public void Dispose()
            {
               _provider.Dispose();
            }
        }
    }
}
