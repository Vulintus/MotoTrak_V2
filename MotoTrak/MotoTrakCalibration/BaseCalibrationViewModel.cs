using MotoTrakBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace MotoTrakCalibration
{
    /// <summary>
    /// A base view-model used in all the calibration of all devices
    /// </summary>
    public class BaseCalibrationViewModel : NotifyPropertyChangedObject
    {
        protected string _booth_label = string.Empty;
        protected string _com_port = string.Empty;
        protected MotorDevice _device_model = null;

        public BaseCalibrationViewModel(string booth_label, string com_port, MotorDevice device_model)
        {
            DeviceModel = device_model;
            BoothLabel = booth_label;
            PortName = com_port;
        }

        /// <summary>
        /// The device model
        /// </summary>
        public MotorDevice DeviceModel
        {
            get
            {
                return _device_model;
            }
            set
            {
                _device_model = value;
                NotifyPropertyChanged("DeviceModel");
            }
        }

        /// <summary>
        /// The booth label
        /// </summary>
        public string BoothLabel
        {
            get
            {
                return _booth_label;
            }
            set
            {
                string temp = value;
                temp = BaseCalibrationViewModel.CleanInput(temp).Trim();

                int booth_number = 0;
                bool success = Int32.TryParse(temp, out booth_number);

                if (success)
                {
                    //Set the local string label
                    _booth_label = booth_number.ToString();

                    //Send the new booth label to the board
                    MotorBoard.GetInstance().SetBoothNumber(booth_number);
                }

                NotifyPropertyChanged("BoothLabel");
            }
        }

        private static string CleanInput(string strIn)
        {
            // Replace invalid characters with empty strings.
            try
            {
                return Regex.Replace(strIn, @"[^\w\-]", "",
                                     RegexOptions.None, TimeSpan.FromSeconds(1.5));
            }
            // If we timeout when replacing invalid characters, 
            // we should return Empty.
            catch (RegexMatchTimeoutException)
            {
                return String.Empty;
            }
        }

        /// <summary>
        /// The port name
        /// </summary>
        public string PortName
        {
            get
            {
                return _com_port;
            }
            set
            {
                _com_port = value;
                NotifyPropertyChanged("PortName");
            }
        }

        /// <summary>
        /// The baseline
        /// </summary>
        public string BaselineValue
        {
            get
            {
                return Convert.ToInt32(Math.Round(DeviceModel.Baseline)).ToString();
            }
        }

        /// <summary>
        /// The slope
        /// </summary>
        public string SlopeValue
        {
            get
            {
                if (DeviceModel.DeviceType == MotorDeviceType.Knob)
                {
                    return "1.0";
                }
                else
                {
                    return DeviceModel.Slope.ToString("0.0000");
                }
            }
        }        

        /// <summary>
        /// The slope units
        /// </summary>
        public string SlopeUnits
        {
            get
            {
                if (DeviceModel != null)
                {
                    if (DeviceModel.DeviceType == MotorDeviceType.Pull)
                    {
                        return "g/tick";
                    }
                    else if (DeviceModel.DeviceType == MotorDeviceType.Lever)
                    {
                        return "deg/tick";
                    }
                    else if (DeviceModel.DeviceType == MotorDeviceType.Knob)
                    {
                        return "deg/tick";
                    }
                }

                return string.Empty;
            }
        }
        
        /// <summary>
        /// The name of the connected device
        /// </summary>
        public string DeviceName
        {
            get
            {
                return "Device detected: " + DeviceModel.DeviceName;
            }
        }

        /// <summary>
        /// Visibility of the pull calibration UI elements
        /// </summary>
        public Visibility PullCalibrationControlVisible
        {
            get
            {
                if (DeviceModel.DeviceType == MotorDeviceType.Pull)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// Visibility of the lever calibration UI elements
        /// </summary>
        public Visibility LeverCalibrationControlVisible
        {
            get
            {
                if (DeviceModel.DeviceType == MotorDeviceType.Lever)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// Visibility of the knob calibration UI elements
        /// </summary>
        public Visibility KnobCalibrationControlVisible
        {
            get
            {
                if (DeviceModel.DeviceType == MotorDeviceType.Knob)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }
    }
}
