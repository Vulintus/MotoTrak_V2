using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakBase
{
    /// <summary>
    /// This enumerated type defines the kinds of data streams that can come from the MotoTrak controller board.
    /// </summary>
    public enum MotorBoardDataStreamType
    {
        [Description("Unknown stream type")]
        Unknown,

        [Description("Timestamp")]
        Timestamp,

        [Description("Device signal")]
        DeviceValue,

        [Description("IR sensor signal")]
        IRSensorValue
    }
}
