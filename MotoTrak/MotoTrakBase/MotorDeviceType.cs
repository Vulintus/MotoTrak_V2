using System.ComponentModel;

namespace MotoTrakBase
{
    /// <summary>
    /// An enumerated type for each kind of device we could possibly use in the program
    /// </summary>
    public enum MotorDeviceType
    {
        [Description("Unknown")]
        [Units("Unknown")]
        Unknown,

        [Description("Knob")]
        [Units("degrees")]
        Knob,

        [Description("Pull")]
        [Units("grams")]
        Pull,

        [Description("Lever")]
        [Units("degrees")]
        Lever
    }
}