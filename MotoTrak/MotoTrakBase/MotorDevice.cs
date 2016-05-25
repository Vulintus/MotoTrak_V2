using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MotoTrakBase
{
    /// <summary>
    /// A class that describes a device used in the MotoTrak program
    /// </summary>
    public class MotorDevice
    {
        #region Private variables

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

        public double DegreesPerTick { get; set; }
        public double Slope { get; set; }
        public double Baseline { get; set; }
        public int DeviceIndex { get; set; }
        public MotorDeviceType DeviceType { get; set; }

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
        public static MotorDeviceType ConvertArdyDeviceValueToDeviceType(int deviceValue)
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

        private void InitializeDevice()
        {
            //Get the instance of the motor board
            MotorBoard motorBoard = MotorBoard.GetInstance();

            //Set these to some defaults
            Slope = 1;
            Baseline = 0;
            DegreesPerTick = 0;

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

