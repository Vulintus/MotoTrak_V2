using System.ComponentModel;

namespace MotoTrakBase
{
    /// <summary>
    /// This enumeration defines options for how the hit threshold changes within a session.  In other words, whether the stage
    /// being used for the session is an "adaptive" stage or a "static" stage.
    /// </summary>
    public enum MotorStageAdaptiveThresholdType
    {
        [Description("Undefined")]
        Undefined,

        [Description("Static")]
        Static,

        [Description("50th percentile")]
        Median,

        [Description("25th percentile")]
        Percentile25,

        [Description("75th percentile")]
        Percentile75,

        [Description("Linear")]
        Linear,

        [Description("Dynamic")]
        Dynamic
    }
}
