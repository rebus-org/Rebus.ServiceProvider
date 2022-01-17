using System;
using System.Threading.Tasks;
using Rebus.Bus;

namespace Rebus.Config;

public static class NewServiceProviderExtensions
{
    public static IServiceProvider UseRebus(this IServiceProvider serviceProvider)
    {
        return serviceProvider;
    }

    public static IServiceProvider UseRebus(this IServiceProvider serviceProvider, Func<IBus, Task> startAction)
    {
        return serviceProvider;
    }
}