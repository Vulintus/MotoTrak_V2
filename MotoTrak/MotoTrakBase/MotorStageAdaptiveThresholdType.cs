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
        [MotoTrak_V1_SpreadsheetColumnValue(new string[] { "undefined" })]
        Undefined,

        [Description("Static")]
        [MotoTrak_V1_SpreadsheetColumnValue(new string[] { "static" })]
        Static,

        [Description("50th percentile")]
        [MotoTrak_V1_SpreadsheetColumnValue(new string[] { "50th percentile", "50%", "median" })]
        Median,

        [Description("25th percentile")]
        [MotoTrak_V1_SpreadsheetColumnValue(new string[] { "25th percentile", "25%", "lower quartile" })]
        Percentile25,

        [Description("75th percentile")]
        [MotoTrak_V1_SpreadsheetColumnValue(new string[] { "75th percentile", "75%", "upper quartile" })]
        Percentile75,

        [Description("Linear")]
        [MotoTrak_V1_SpreadsheetColumnValue(new string[] { "linear" })]
        Linear,

        [Description("Dynamic")]
        [MotoTrak_V1_SpreadsheetColumnValue(new string[] { "dynamic" })]
        Dynamic
    }
}
