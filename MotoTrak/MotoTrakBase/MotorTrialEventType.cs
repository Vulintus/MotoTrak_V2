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
        UndefinedEvent,

        [Description("Successful trial")]
        SuccessfulTrial,

        [Description("Hit window end")]
        HitWindowEnd,

        [Description("Trial initiation")]
        TrialInitiation,

        [Description("Trial end")]
        TrialEnd,

        [Description("User-defined event")]
        UserDefinedEvent
    }
}
