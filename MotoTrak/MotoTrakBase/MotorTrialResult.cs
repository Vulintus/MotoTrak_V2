using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakBase
{
    /// <summary>
    /// An enumerated type that describes all possible result types of a MotoTrak trial
    /// </summary>
    public enum MotorTrialResult
    {
        [Description("Unknown")]
        [TrialResultCode(0)]
        Unknown,

        [Description("Hit")]
        [TrialResultCode(72)]
        Hit,

        [Description("Miss")]
        [TrialResultCode(77)]
        Miss,

        [Description("Manual feed")]
        [TrialResultCode(70)]
        ManualFeed,

        [Description("Pause")]
        [TrialResultCode(80)]
        Pause
    }
}
