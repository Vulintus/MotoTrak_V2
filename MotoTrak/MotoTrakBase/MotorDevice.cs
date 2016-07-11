using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MotoTrakBase
{
    /// <summary>
    /// A class that describes a device used in the MotoTrak program
    /// </summary>
    public class MotorDevice : NotifyPropertyChangedObject
    {
        #region Private constants

        //Constants used to identify devices
        private const int LEVERHD_MIN_VALUE = 50;
        private const int LEVERHD_MAX_VALUE = 99;

        private const int KNOB_MIN_VALUE = 100;
        private const int KNOB_MAX_VALUE = 449;
        private const int PULL_MIN_VALUE = 450;
        private const int PULL_MAX_VALUE = 550;

        private const int LEVER_MIN_VALUE = 800;
        private const int LEVER_MAX_VALUE = 949;
        private const int WHEEL_MIN_VALUE = 950;
        private const int WHEEL_MAX_VALUE = 1023;

        #endregion

        #region Private variables

        private MotorDeviceType _device_type = MotorDeviceType.Unknown;
        private List<double> _coefficients = new List<double>();
        private int _device_index = 0;
        
        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a motor device with an unknown type.
        /// </summary>
        public MotorDevice()
        {
            DeviceType = MotorDeviceType.Unknown;
            DeviceIndex = 0;
            InitializeDevice();
        }

        /// <summary>
        /// Constructs a motor device with a specified type.
        /// </summary>
        /// <param name="type">A MotorDeviceType that defines the kind of device used.</param>
        /// <param name="index">The device index.  This is mainly to satisfy old versions of the motor board with multiple device ports. This can
        /// typically be set to 0, unless it is needed.</param>
        public MotorDevice(MotorDeviceType type, int index)
        {
            DeviceType = type;
            DeviceIndex = index;
            InitializeDevice();
        }

        /// <summary>
        /// Constructs a motor device with a specified type.
        /// If this constructor is called, the user supplies the coefficients, and the motor controller board is NOT CONTACTED to obtain them.
        /// This constructor is useful if a MotorDevice object is being created when loading in a data file from a previous session, rather
        /// than creating a device object for a new session that is about to be run.
        /// </summary>
        /// <param name="type">The device type</param>
        /// <param name="index">The index of the device on the motor board</param>
        /// <param name="coeffs">The coefficients for the device calibration</param>
        public MotorDevice(MotorDeviceType type, int index, List<double> coeffs)
        {
            DeviceType = type;
            DeviceIndex = index;
            Coefficients = coeffs;
        }

        #endregion

        #region Destructors

        ~MotorDevice()
        {
            if (DeviceType == MotorDeviceType.Knob)
            {
                //Turn off SPI communication for the knob.
                MotorBoard board = MotorBoard.GetInstance();
                board.KnobToggle(0);
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// The list of coefficients for this device.
        /// Index 0 = y-intercept (baseline)
        /// Index 1 = slope
        /// </summary>
        public List<double> Coefficients
        {
            get
            {
                return _coefficients;
            }
            set
            {
                _coefficients = value;
            }
        }
        
        /// <summary>
        /// The same as Coefficients[0].  The calibrated baseline of the device.
        /// </summary>
        public double Baseline
        {
            get
            {
                return Coefficients[0];
            }
            set
            {
                Coefficients[0] = value;
            }
        }

        /// <summary>
        /// The same as Coefficients[1].  The calibrated slope of the device.
        /// </summary>
        public double Slope
        {
            get
            {
                return Coefficients[1];
            }
            set
            {
                Coefficients[1] = value;
            }
        }
        
        /// <summary>
        /// The index of this device.  This indicates what position the device is in if the motor controller board
        /// has multiple devices connected to it.
        /// </summary>
        public int DeviceIndex
        {
            get
            {
                return _device_index;
            }
            set
            {
                _device_index = value;
            }
        }

        /// <summary>
        /// The type of this device
        /// </summary>
        public MotorDeviceType DeviceType
        {
            get
            {
                return _device_type;
            }
            set
            {
                _device_type = value;
            }
        }

        /// <summary>
        /// The name of this device.
        /// </summary>
        public string DeviceName
        {
            get
            {
                return MotorDeviceTypeConverter.ConvertToDescription(this.DeviceType);
            }
        }

        #endregion

        #region Methods
        
        /// <summary>
        /// Given a value returned from the Arduino with the GetDevice() function on ArdyMotor board, this converts
        /// that value to a MotorDeviceType.
        /// </summary>
        public static MotorDeviceType ConvertAnalogDeviceValueToDeviceType(int deviceValue)
        {
            //Find out what device is there
            if (deviceValue >= KNOB_MIN_VALUE && deviceValue <= KNOB_MAX_VALUE)
            {
                //It's a knob
                return MotorDeviceType.Knob;
            }
            else if (deviceValue >= PULL_MIN_VALUE && deviceValue <= PULL_MAX_VALUE)
            {
                //It's a pull
                return MotorDeviceType.Pull;
            }
            else
            {
                //It's undefined
                return MotorDeviceType.Unknown;
            }
        }

        /// <summary>
        /// Converts a device type to a range of possible analog values that could correspond to that device
        /// when reading the device type on the motor controller board.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Tuple<int, int> ConvertDeviceTypeToAnalogValueRange(MotorDeviceType type)
        {
            switch (type)
            {
                case MotorDeviceType.Pull:
                    return new Tuple<int, int>(PULL_MIN_VALUE, PULL_MAX_VALUE);
                case MotorDeviceType.Knob:
                    return new Tuple<int, int>(KNOB_MIN_VALUE, KNOB_MAX_VALUE);
            }

            return new Tuple<int, int>(0, 0);
        }

        /// <summary>
        /// This function is called by the constructor
        /// </summary>
        private void InitializeDevice()
        {
            //Get the instance of the motor board
            MotorBoard motorBoard = MotorBoard.GetInstance();

            //Set these to some defaults
            Slope = 1;
            Baseline = 0;

            switch (DeviceType)
            {
                case MotorDeviceType.Knob:

                    //Toggle the knob as being turned on.  This is important because it initiates SPI communication, which is important
                    //for the knob to work properly.
                    motorBoard.KnobToggle(1);

                    //The slope is set to a default of 0.25 for the Knob task.  This is due to the specific kind of rotary encoders we are using.
                    Slope = 0.25;

                    //The baseline level should be set to what the device value currently is.  This means the device should not be in use when
                    //the program is initializing.
                    Baseline = motorBoard.ReadDevice();
                    
                    break;
                case MotorDeviceType.Pull:
                    //Set the baseline of the pull apparatus
                    Baseline = motorBoard.GetBaseline();

                    //Set the slope of the pull apparatus
                    Slope = motorBoard.CalGrams();
                    Slope /= motorBoard.NPerCalGrams();

                    break;
                default:
                    break;
            }
        }

        #endregion
    }
}

