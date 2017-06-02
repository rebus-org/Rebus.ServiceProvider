﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Handlers;
using Rebus.Tests.Contracts.Activation;

namespace Rebus.ServiceProvider.Tests
{
    public class NetCoreServiceProviderContainerAdapterFactory : IContainerAdapterFactory
    {
        readonly IServiceCollection _serviceCollection = new ServiceCollection();
        private IServiceProvider _provider;

        public void CleanUp()
        {
            var bus = GetProvider().GetService<IBus>();

            bus.Dispose();
        }

        public IHandlerActivator GetActivator()
        {
            return new NetCoreServiceProviderContainerAdapter(GetProvider());
        }

        public IBus GetBus()
        {
            return GetProvider().GetService<IBus>();
        }

        private IServiceProvider GetProvider()
        {
            if (_provider == null)
            {
                _provider = _serviceCollection.BuildServiceProvider();
            }

            return _provider;
        }

        void IContainerAdapterFactory.RegisterHandlerType<THandler>()
        {
            GetHandlerInterfaces(typeof(THandler))
                .ForEach(i => _serviceCollection.AddTransient(i, typeof(THandler)));
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