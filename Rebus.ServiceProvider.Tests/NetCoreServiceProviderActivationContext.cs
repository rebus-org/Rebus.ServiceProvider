using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
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
            services.AddSingleton<IApplicationLifetime>(new TestLifetime());

            var provider = services.BuildServiceProvider();
            
            var adapter = new NetCoreServiceProviderContainerAdapter(provider);

            container = new ActivatedContainer(provider);

            return adapter;
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

        private class HandlerRegistry : IHandlerRegistry
        {
            private readonly IServiceCollection _services;

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
                _provider.GetRequiredService<IApplicationLifetime>().StopApplication();
            }
        }

        private class TestLifetime : IApplicationLifetime
        {
            private readonly CancellationTokenSource _stoppingSource;
            private readonly CancellationTokenSource _stoppedSource;

            public TestLifetime()
            {
                var source = new CancellationTokenSource();
                ApplicationStarted = source.Token;
                source.Cancel();

                _stoppingSource = new CancellationTokenSource();
                _stoppedSource = new CancellationTokenSource();

                ApplicationStopping = _stoppingSource.Token;
                ApplicationStopped = _stoppedSource.Token;
            }

            public CancellationToken ApplicationStarted { get; }

            public CancellationToken ApplicationStopping { get; }

            public CancellationToken ApplicationStopped { get; }

            public void StopApplication()
            {
                _stoppingSource.Cancel();

                var allHandlersStopped = new CancellationTokenSource();
                _stoppedSource.Token.Register(() => allHandlersStopped.Cancel());

                _stoppedSource.Cancel();

                // make sure we block untill all the handlers have finished.
                allHandlersStopped.Token.WaitHandle.WaitOne();
            }
        }
    }
}
