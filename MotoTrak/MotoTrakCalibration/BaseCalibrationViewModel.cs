using MotoTrakBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                _booth_label = value;
                NotifyPropertyChanged("BoothLabel");
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
                return DeviceModel.Slope.ToString("0.0000");
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
    }
}
