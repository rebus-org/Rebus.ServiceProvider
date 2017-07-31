using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Rebus.Bus;
using Rebus.ServiceProvider;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Defines common operations for Rebus when using an <see cref="IApplicationBuilder"/>.
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Activates the Rebus engine, allowing it to start sending and receiving messages.
        /// </summary>
        /// <param name="app">The application hosting Rebus.</param>
        public static IApplicationBuilder UseRebus(this IApplicationBuilder app)
        {
            app.ApplicationServices.UseRebus();
            return app;
        }

        /// <summary>
        /// Activates the Rebus engine, allowing it to start sending and receiving messages.
        /// </summary>
        /// <param name="app">The application hosting Rebus.</param>
        /// <param name="busAction">An action to perform on the bus.</param>
        public static IApplicationBuilder UseRebus(this IApplicationBuilder app, Action<IBus> busAction)
        {
            app.ApplicationServices.UseRebus(busAction);
            return app;
        }
    }
}
