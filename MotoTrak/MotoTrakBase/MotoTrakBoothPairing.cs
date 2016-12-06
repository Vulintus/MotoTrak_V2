using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakBase
{
    /// <summary>
    /// A simple class that represents a MotoTrak booth pairing with a com port
    /// </summary>
    public class MotoTrakBoothPairing
    {
        #region Fields

        public string BoothLabel = string.Empty;
        public string ComPort = string.Empty;
        public MotorDeviceType DeviceConnected = MotorDeviceType.Unknown;
        public DateTime LastUpdated = DateTime.MinValue;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public MotoTrakBoothPairing()
        {
            //empty
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public MotoTrakBoothPairing(string booth_label, string com_port, MotorDeviceType device_type, DateTime last_update)
        {
            BoothLabel = booth_label;
            ComPort = com_port;
            DeviceConnected = device_type;
            LastUpdated = last_update;
        }

        #endregion
    }
}
