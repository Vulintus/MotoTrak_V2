using MotoTrakBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MotoTrak
{
    public class USBDeviceViewModel
    {
        #region Private data members

        USBDeviceInfo _model = null;

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="model"></param>
        public USBDeviceViewModel(USBDeviceInfo model)
        {
            _model = model;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The model usb device info object
        /// </summary>
        public USBDeviceInfo Model
        {
            get
            {
                return _model;
            }
        }

        /// <summary>
        /// The name of the booth
        /// </summary>
        public string BoothName
        {
            get
            {
                if (MotoTrakConfiguration.GetInstance().BoothPairings.ContainsKey(_model.DeviceID))
                {
                    var booth_name = MotoTrakConfiguration.GetInstance().BoothPairings[_model.DeviceID];
                    if (!string.IsNullOrEmpty(booth_name))
                    {
                        return ("Booth " + booth_name);
                    }
                }

                return "Unknown booth";
            }
        }

        /// <summary>
        /// Some information about the device itself.
        /// </summary>
        public string DeviceInformation
        {
            get
            {
                string result = ("Arduino device on port " + _model.DeviceID);
                if (IsDeviceBusy)
                {
                    result += " (device busy)";
                }

                return result;
            }
        }

        /// <summary>
        /// Indicates whether the device is busy or not
        /// </summary>
        public bool IsDeviceBusy
        {
            get
            {
                return _model.IsPortBusy;
            }
        }

        /// <summary>
        /// This determines the visibility of the green check mark that denotes the availability of this com port
        /// </summary>
        public Visibility ConnectionGoodVisibility
        {
            get
            {
                if (IsDeviceBusy)
                {
                    return Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// Determines the visibility of the red X mark that marks a busy connection
        /// </summary>
        public Visibility ConnectionBadVisibility
        {
            get
            {
                if (IsDeviceBusy)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }

        #endregion
    }
}
