using System;
using Rebus.Config;

namespace Rebus.ServiceProvider
{
    /// <summary>
    /// Wrapper type for some Rebus configuration work.
    /// </summary>
    public class RebusConfigAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RebusConfigAction"/> class.
        /// </summary>
        /// <param name="action">The configuration work to be performed.</param>
        public RebusConfigAction(Func<RebusConfigurer, RebusConfigurer> action)
        {
            Action = action;
        }

        /// <summary>
        /// The configuration work to be performed.
        /// </summary>
        public Func<RebusConfigurer, RebusConfigurer> Action { get; }
    }
}
