using System.ComponentModel;

namespace MotoTrakBase
{
    /// <summary>
    /// An enumerated type for each kind of device we could possibly use in the program
    /// </summary>
    public enum MotorDeviceType
    {
        [Description("Unknown")]
        Unknown,

        [Description("Knob")]
        Knob,

        [Description("Pull")]
        Pull,

        [Description("Lever")]
        Lever
    }
}