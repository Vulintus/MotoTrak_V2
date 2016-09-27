using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakBase
{
    /// <summary>
    /// This enumeration describes different events that can take place within a trial
    /// </summary>
    public enum MotorTrialEventType
    {
        [Description("Undefined event")]
        [MultipleEventsAllowed(true)]
        UndefinedEvent,

        [Description("Successful trial")]
        [MultipleEventsAllowed(false)]
        SuccessfulTrial,

        [Description("Hit window end")]
        [MultipleEventsAllowed(false)]
        HitWindowEnd,

        [Description("Trial initiation")]
        [MultipleEventsAllowed(false)]
        TrialInitiation,

        [Description("Trial end")]
        [MultipleEventsAllowed(false)]
        TrialEnd,

        [Description("User-defined event")]
        [MultipleEventsAllowed(true)]
        UserDefinedEvent
    }
}
