using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MotoTrakBase
{
    /// <summary>
    /// A basic class to hold some USB device information
    /// </summary>
    public class USBDeviceInfo
    {
        #region Constructor

        public USBDeviceInfo (string device_description, string com_port)
        {
            Description = device_description;
            DeviceID = com_port;
            SerialObject = new SerialPort(DeviceID, 115200);
        }

        #endregion

        #region Public fields

        public string DeviceID = string.Empty;
        public string Description = string.Empty;
        public SerialPort SerialObject = null;

        /// <summary>
        /// Indicates whether the current serial port is busy
        /// </summary>
        public bool IsPortBusy
        {
            get
            {
                bool port_busy = true;

                if (SerialObject != null)
                {
                    try
                    {
                        //In order to truly tell whether a serial port is busy or not, we have to try and open it.
                        //This is the only way to tell if another process has a hold on the port.
                        SerialObject.Open();
                        
                        //If successful, set a flag indicating this port is not busy
                        port_busy = false;
                        
                        //Because Microsoft's API sucks, we have to sleep for a little while to allow the serial port's thread to 
                        //spin up and activate.  If we don't, then we try to call the Close() function, our program will hang.
                        //Thanks, Microsoft.
                        Thread.Sleep(500);

                        //Close the connection to the port.  We don't want to maintain it right now.
                        SerialObject.Close();
                    }
                    catch (Exception)
                    {
                        port_busy = true;
                    }
                }

                return port_busy;
            }
        }

        #endregion
        
    }
}
