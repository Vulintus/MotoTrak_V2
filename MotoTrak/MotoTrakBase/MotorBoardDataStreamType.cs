using System;
using System.Collections.Generic;
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
        Timestamp,
        DeviceValue,
        IRSensorValue
    }
}
