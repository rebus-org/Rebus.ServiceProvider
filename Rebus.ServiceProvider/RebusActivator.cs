using System;

namespace Rebus.ServiceProvider
{
    /// <summary>
    /// Wrapper type for the work required to start a rebus IBus instance.
    /// </summary>
    public class RebusActivator
    {
        private readonly Action _activate;

        /// <summary>
        /// Initializes a new instance of the <see cref="RebusActivator"/> class.
        /// </summary>
        /// <param name="activate">The work required to activate Rebus.</param>
        public RebusActivator(Action activate)
        {
            _activate = activate;
        }

        /// <summary>
        /// Starts a bus instance.
        /// </summary>
        public void Activate()
        {
            _activate();
        }
    }
}
