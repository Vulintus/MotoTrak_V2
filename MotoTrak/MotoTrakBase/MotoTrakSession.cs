using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
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

        private MotorStage _selectedStage = new MotorStage();
        private List<MotorStage> _allStages = new List<MotorStage>();

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

        #region Private methods

        /// <summary>
        /// Sets any streaming parameters that need to be set, as defined by the selected stage.
        /// </summary>
        private void SetStreamingParameters()
        {
            //Set the streaming period to be the sample period from the currently selected stage
            ArdyBoard.SetStreamingPeriod(SelectedStage.SamplePeriodInMilliseconds);
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

        /// <summary>
        /// The stage that has been selected for this session.
        /// </summary>
        public MotorStage SelectedStage
        {
            get
            {
                return _selectedStage;
            }
            set
            {
                _selectedStage = value;
            }
        }

        /// <summary>
        /// A list that contains all available stages from the stage file.  These stages have not been filtered
        /// based on the device currently connected to the MotoTrak booth.
        /// </summary>
        public List<MotorStage> Stages
        {
            get
            {
                return _allStages;
            }
            set
            {
                _allStages = value;
            }
        }

        /// <summary>
        /// A filtered list of available stages based on the device that is currently connected to the MotoTrak booth.
        /// </summary>
        public List<MotorStage> AvailableStages
        {
            get
            {
                //Filter the stages based on whether each stage's defined device type equals the type of device currently being used.
                var filtered_stages = Stages.Where(stage => stage.DeviceType == Device.DeviceType).ToList();
                return filtered_stages;
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

            //Now that configuration file has been loaded, and all config variables have been set, let's read in stages
            //Setting the "Stages" variable also sets the "AvailableStages" variable because of the nature of how the 
            //getters/setters are written.
            Stages = MotorStage.RetrieveAllStages();

            //Now set the selected stage to a default value
            if (AvailableStages.Count > 0)
            {
                //If there are available stages for the current device, the default stage is the first stage in the list.
                SelectedStage = AvailableStages[0];
                
                //Tell the Arduino board to stream data at the sampling rate defined in the default stage
                SetStreamingParameters();

                //Start a background worker which will continue looping and reading in data.
                backgroundLoop = new BackgroundWorker();
                backgroundLoop.WorkerSupportsCancellation = true;
                backgroundLoop.WorkerReportsProgress = true;
                backgroundLoop.DoWork += HandleStreaming;
                backgroundLoop.ProgressChanged += NotifyMainThreadOfNewData;
                backgroundLoop.RunWorkerCompleted += CloseSession;
                backgroundLoop.RunWorkerAsync();
            }
            else
            {
                //If there are no available stages for this device
                SelectedStage = null;
            }
        }

        #endregion

        #region Properties pertaining to the background worker thread, but not edited by that thread

        BackgroundWorker backgroundLoop = null;

        #endregion

        #region Private properties edited by the background worker thread

        private List<double> _monitoredSignal = new List<double>();

        #endregion

        #region Properties edited by the background worker thread
        
        /// <summary>
        /// This property contains the current "monitored" signal that gets displayed to the user.
        /// This property can ONLY be set by the background worker thread, in order to keep the application
        /// thread-safe.  This property can be read at any time by the main thread (the UI thread), but it should not
        /// be set by that thread.
        /// </summary>
        public List<double> MonitoredSignal
        {
            get
            {
                return _monitoredSignal;
            }
            private set
            {
                _monitoredSignal = value;
            }
        }

        #endregion

        #region BackgroundWorker methods

        /// <summary>
        /// This method is called by the background worker thread whenever it is cancelled.  This will occur whenever the MotoTrak window
        /// is closed, because that is the only time the thread would ever be cancelled.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseSession(object sender, RunWorkerCompletedEventArgs e)
        {
            //empty function
        }

        /// <summary>
        /// This method notifies the main thread of updates to any data that may be important for it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NotifyMainThreadOfNewData(object sender, ProgressChangedEventArgs e)
        {
            //currently empty
        }

        /// <summary>
        /// HandleStreaming is the method that runs the primary behavior loop of MotoTrak.
        /// This method reads in new data from the Arduino board and makes decisions based on that data, such
        /// as whether to feed the animal or to deliver stimulation.
        /// 
        /// This method may not WRITE to variables controlled by the main thread UNLESS those variables are 
        /// properly locked!  It may read from variables at any time.  This is essential for MotoTrak to 
        /// properly be thread-safe.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleStreaming(object sender, DoWorkEventArgs e)
        {
            //Begin periodic streaming from the Arduino board
            ArdyBoard.EnableStreaming(1);

            //Create a null trial object as a placeholder for new trials that will be created
            MotorTrial trial = null;

            //Define the buffer size for streaming data from the Arduino board
            int buffer_size = SelectedStage.TotalRecordedSamplesPerTrial;

            //Define some variables that will be needed throughout our endless loop.
            //First, we want to define an array that holds the raw stream of data
            List<List<int>> raw_stream_data = new List<List<int>>();

            //Define a list in which we will keep signal data from the device.
            List<double> raw_signal_data = new List<double>();

            //Get the index of the device signal within the data stream
            int device_signal_index = SelectedStage.DataStreamTypes.IndexOf(MotorBoardDataStreamType.DeviceValue);
            if (device_signal_index == -1)
            {
                //If no stream position was defined for the device signal for this stage
                throw new MotoTrakException("No way to tell which signal is the device signal!");
            }

            //This loop is endless, for all intents and purposes.
            //It will exit when the user closes the MotoTrak window.
            while (!backgroundLoop.CancellationPending)
            {
                //Read in new datapoints from the Arduino board
                ReadNewDataFromArduino(raw_stream_data, buffer_size);

                //Convert the raw stream data to a form that is useable by the program (transform it by the slope and baseline
                //according to the device settings.  This happens regardless of what stage has been selected).
                raw_signal_data = raw_stream_data.Select(x => (Device.Slope * (x[device_signal_index] - Device.Baseline))).ToList();

                //Now we will act upon the signal based upon what run-state we are in, and also based upon the stage selected by the user.
                switch (SessionState)
                {
                    case SessionRunState.Scan:

                        //This is essentially the "idle" state of the program, when no animal is actually running.

                        //Make a "hard" copy of the signal data and assign it to the MonitoredSignal variable, which is what the user sees
                        //on the plot.  The "ToList" function makes a copy of the data, rather than copying a pointer.  This is important!
                        MonitoredSignal = raw_signal_data.ToList();

                        break;
                    case SessionRunState.TrialWait:
                        break;
                    case SessionRunState.TrialRun:
                        break;
                    case SessionRunState.TrialEnd:
                        break;
                    case SessionRunState.TrialManualFeed:
                        break;
                    case SessionRunState.Pause:
                        break;
                }

                //After handling whatever state the program is in, notify the UI/main thread of any changes
                backgroundLoop.ReportProgress(0, null);

                //Sleep the thread for 10 milliseconds so we don't consume too much CPU time
                Thread.Sleep(10);
            }

            //If we reach this point in the code, it means that user has decided to close the MotoTrak window.  This next line of code
            //tells anyone who is listening that the background worker is being cancelled.
            e.Cancel = true;
        }

        private void ReadNewDataFromArduino ( List<List<int>> raw_stream_data, int buffer_size )
        {
            //Read in new streaming data from the Arduino board
            List<List<int>> new_data_points = ArdyBoard.ReadStream();

            //Add the new data points to our buffer
            raw_stream_data.AddRange(new_data_points);

            //Now reduce the size of raw stream data if the size has exceeded our max buffer size.
            raw_stream_data = raw_stream_data.Skip(Math.Max(0, raw_stream_data.Count - buffer_size)).Take(buffer_size).ToList();
        }

        #endregion
    }

}
