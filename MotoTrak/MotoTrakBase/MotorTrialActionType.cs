using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakBase
{
    /// <summary>
    /// This enumeration represents the set of actions that MotoTrak can take towards the rat, typically in response to 
    /// a successful trial, but also in some other scenarios.
    /// </summary>
    public enum MotorTrialActionType
    {
        TriggerFeeder,
        PlaySound,
        SendStimulationTrigger,
        AdjustAutopositionerPosition,
        Unknown
    }
}
