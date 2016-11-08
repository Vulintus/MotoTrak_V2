using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakUtilities
{
    /// <summary>
    /// Static methods that are useful for operating on event handlers
    /// </summary>
    public static class EventHandlerUtilities
    {
        /// <summary>
        /// Given an event handler object and a prospective delegate, this function returns true or false indicating
        /// whether that delegate has already been assigned to the handler.
        /// </summary>
        /// <param name="handler">The event handler</param>
        /// <param name="prospectiveHandler">The prospective delegate</param>
        /// <returns>True if the delegate has been assigned to the handler, false if not</returns>
        public static bool IsEventHandlerRegistered (EventHandler handler, Delegate prospectiveHandler)
        {
            if (handler != null)
            {
                foreach (Delegate existingHandler in handler.GetInvocationList())
                {
                    if (existingHandler == prospectiveHandler)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the length of the handler's invocation list
        /// </summary>
        /// <param name="handler">The event handler</param>
        /// <returns>The number of delegates assigned to this event handler</returns>
        public static int GetDelegateCount (EventHandler handler)
        {
            if (handler != null)
            {
                return handler.GetInvocationList().Length;
            }

            return 0;
        }
    }
}
