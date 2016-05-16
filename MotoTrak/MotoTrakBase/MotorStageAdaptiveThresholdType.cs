using System.ComponentModel;

namespace MotoTrakBase
{
    /// <summary>
    /// This enumeration defines options for how the hit threshold changes within a session.  In other words, whether the stage
    /// being used for the session is an "adaptive" stage or a "static" stage.
    /// </summary>
    public enum MotorStageAdaptiveThresholdType
    {
        [Description("undefined")]
        Undefined,

        [Description("static")]
        Static,

        [Description("median")]
        Median,

        [Description("linear")]
        Linear,

        [Description("dynamic")]
        Dynamic
    }
}
