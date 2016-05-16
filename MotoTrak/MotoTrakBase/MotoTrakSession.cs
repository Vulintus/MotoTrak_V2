using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakBase
{
    public class MotoTrakSession : NotifyPropertyChangedObject
    {
        #region Private enumerated types

        /// <summary>
        /// This enumerates the possible states a session could be in.
        /// </summary>
        private enum SessionRunState
        {
            Scan,                   //This is the idle state
            TrialWait,
            TrialRun,
            TrialEnd,
            TrialManualFeed,
            Pause
        }

        #endregion

        #region Private data members

        private SessionRunState _sessionState = SessionRunState.Scan;
        private MotorBoard _ardy = MotorBoard.GetInstance();

        private int _boothNumber = int.MinValue;
        private MotorDevice _device = new MotorDevice();
        private string _ratName = string.Empty;

        #endregion

        #region Private properties

        private MotorBoard ArdyBoard
        {
            get { return _ardy; }
            set { _ardy = value; }
        }

        private SessionRunState SessionState
        {
            get { return _sessionState; }
            set
            {
                _sessionState = value;
                NotifyPropertyChanged("SessionState");
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new MotoTrak session.
        /// </summary>
        public MotoTrakSession()
        {
            //empty constructor
        }

        #endregion
        
        #region Properties

        /// <summary>
        /// The booth in which this session is taking place.
        /// </summary>
        public int BoothNumber
        {
            get { return _boothNumber; }
            set
            {
                _boothNumber = value;
            }
        }

        /// <summary>
        /// The device currently being used in this session.
        /// </summary>
        public MotorDevice Device
        {
            get { return _device; }
            set
            {
                _device = value;
            }
        }

        /// <summary>
        /// The name of the rat being run in this session.
        /// </summary>
        public string RatName
        {
            get { return _ratName; }
            set
            {
                _ratName = value;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// This method initializes a MotoTrak session with a specified MotoTrak controller.
        /// It does NOT start a session (in the sense of actively collecting data).  
        /// The session run state at the end of this method will be "Scan" (meaning "idle").
        /// </summary>
        /// <param name="comPort">The serial port to connect to</param>
        public void InitializeSession(string comPort)
        {
            /*
             * Flow of this function:
             *      - Connect to the MotoTrak board
             *      - Verify that the MotoTrak board is running the minimum board version allowed for communication with this program
             *      - Read the MotoTrak config file to determine whether we are using a Google spreadsheet stage list, or local stage files
             *      - Load the stages that exist for the current device
             *      - Initialize streaming on the Arduino board.
             *      - Start a background worker thread to handle streaming input from the Arduino board.
             */

            //Connect to the motortrak board
            ArdyBoard.ConnectToArduino(comPort);
            if (!ArdyBoard.IsSerialConnectionValid)
            {
                throw new MotoTrakException("Unable to connect to the MotoTrak controller!");
            }

            //Check the board version
            if (!ArdyBoard.DoesSketchMeetMinimumRequirements())
            {
                throw new MotoTrakException("MotoTrak controller does not mean minimum requirements for continuing.");
            }

            //Gather information about the booth and what devices are connected to it
            BoothNumber = ArdyBoard.GetBoothNumber();
            Device = ArdyBoard.GetMotorDevice();

            //If no device was found, or if the device is unknown, throw an error.
            if (Device == null || Device.DeviceType == MotorDeviceType.Unknown)
            {
                throw new MotoTrakException("No recognized device is connected to the MotoTrak controller!");
            }

            //At this point, we need to read the MotoTrak configuration file to determine how to load in stages
            MotoTrakConfiguration config = MotoTrakConfiguration.GetInstance();
            config.ReadConfigurationFile();
            
        }

        #endregion
    }
}
