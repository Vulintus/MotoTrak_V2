using System.ComponentModel;

namespace MotoTrakBase
{
    /// <summary>
    /// This enumeration defines the different types of hit thresholds that exist in MotoTrak.
    /// </summary>
    public enum MotorTaskTypeV1
    {
        [Description("Undefined")]
        Undefined,

        [Description("grams (peak)")]
        PeakForce,

        [Description("grams (sustained)")]
        SustainedForce,

        [Description("degrees (total)")]
        TotalDegrees
    }
}
