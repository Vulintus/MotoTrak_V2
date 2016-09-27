using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakBase
{
    /// <summary>
    /// An attribute class for event types that indicates whether multiple events of a single event type are allowed within a trial
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public class MultipleEventsAllowedAttribute : System.Attribute
    {
        public bool AllowMultipleEvents = false;

        public MultipleEventsAllowedAttribute(bool allow)
        {
            AllowMultipleEvents = allow;
        }
    }
}
