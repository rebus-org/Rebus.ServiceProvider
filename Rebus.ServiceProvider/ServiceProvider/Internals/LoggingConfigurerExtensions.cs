using System;
using System.Linq;
using System.Reflection;
using Rebus.Config;
using Rebus.Injection;

namespace Rebus.ServiceProvider.Internals;

static class LoggingConfigurerExtensions
{
    /// <summary>
    /// Optimistic hack where we go hunting for the <see cref="Injectionist"/> inside the <see cref="RebusConfigurer"/>
    /// so see if something has been registered.
    /// </summary>
    public static bool ReflectWhetherItHasRegistration<T>(this RebusLoggingConfigurer configurer)
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));

        var injectionistField = configurer.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .FirstOrDefault(f => f.FieldType == typeof(Injectionist));
        
        if (injectionistField == null) return false;

        var injectionist = injectionistField.GetValue(configurer) as Injectionist;
        
        if (injectionist == null) return false;

        return injectionist.Has<T>();
    }
}