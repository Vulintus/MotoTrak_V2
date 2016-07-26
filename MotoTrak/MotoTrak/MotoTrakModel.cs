using MotoTrakBase;
using MotoTrakUtilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MotoTrak
{
    /// <summary>
    /// This class is the core class that runs MotoTrak.  It is a singleton class.
    /// </summary>
    public class MotoTrakModel : NotifyPropertyChangedObject
    {
        #region Singleton

        private static MotoTrakModel _instance = null;

        private MotoTrakModel()
        {
            //empty
        }

        /// <summary>
        /// Get the MotoTrak class instance
        /// </summary>
        /// <returns>Returns the MotoTrak instance</returns>
        public static MotoTrakModel GetInstance()
        {
            if (_instance == null)
            {
                _instance = new MotoTrakModel();
            }

            return _instance;
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Sets any streaming parameters that need to be set, as defined by the selected stage.
        /// </summary>
        private void SetStreamingParameters()
        {
            //Set the streaming period to be the sample period from the currently selected stage
            ControllerBoard.SetStreamingPeriod(CurrentSession.SelectedStage.SamplePeriodInMilliseconds);
        }

        #endregion

        #region Enumerated types for session and trial states

        /// <summary>
        /// This enumerates the possible states a session could be in.
        /// </summary>
        private enum SessionRunState
        {
            SessionBegin,
            SessionRunning,
            SessionEnd,
            SessionNotRunning,
            SessionPaused,
            SessionUnpaused,
        }

        /// <summary>
        /// Enumerates the possible states of a trial within a session.
        /// "Idle" indicates that no trial is currently happening.
        /// </summary>
        private enum TrialRunState
        {
            Idle,
            ResetBaseline,
            TrialSetup,
            TrialWait,
            TrialRun,
            TrialEnd,
            TrialManualFeed
        }

        #endregion

        #region Private data members

        private MotorBoard _ardy = MotorBoard.GetInstance();
        private MotoTrakSession _current_session = null;
        private MotorTrial _current_trial = null;
        private MotorDevice _current_device = null;
        private string _current_booth_label = string.Empty;

        private List<MotorStage> _all_stages = new List<MotorStage>();

        private SessionRunState _session_state = SessionRunState.SessionNotRunning;
        private TrialRunState _trial_state = TrialRunState.Idle;

        private bool _trigger_manual_feed = false;

        private BackgroundWorker _background_thread = null;

        #endregion

        #region Data locks for multithreading

        private Object session_state_lock = new Object();
        private Object trial_state_lock = new Object();
        private Object manual_feed_lock = new Object();

        #endregion

        #region Public properties

        /// <summary>
        /// The motor controller board object
        /// </summary>
        private MotorBoard ControllerBoard
        {
            get { return _ardy; }
            set { _ardy = value; }
        }

        /// <summary>
        /// The current session being run.
        /// </summary>
        public MotoTrakSession CurrentSession
        {
            get
            {
                return _current_session;
            }
            set
            {
                _current_session = value;
                NotifyPropertyChanged("CurrentSession");
            }
        }

        /// <summary>
        /// The current trial being run for this session.
        /// </summary>
        public MotorTrial CurrentTrial
        {
            get
            {
                return _current_trial;
            }
            set
            {
                _current_trial = value;
                NotifyPropertyChanged("CurrentTrial");
            }
        }

        /// <summary>
        /// The device currently connected to the MotoTrak controller
        /// </summary>
        public MotorDevice CurrentDevice
        {
            get
            {
                return _current_device;
            }
            set
            {
                _current_device = value;
                NotifyPropertyChanged("CurrentDevice");
            }
        }

        /// <summary>
        /// The label for the booth that we are currently connected to
        /// </summary>
        public string BoothLabel
        {
            get
            {
                return _current_booth_label;
            }
            set
            {
                _current_booth_label = value;
                NotifyPropertyChanged("BoothLabel");
            }
        }

        /// <summary>
        /// The state of the session: whether it is running, not running, beginning, ending, or paused.
        /// </summary>
        private SessionRunState SessionState
        {
            get { return _session_state; }
            set
            {
                //The session state can be modified by both the UI thread and the background thread
                //So we need to make sure that it is within a semaphore.
                lock (session_state_lock)
                {
                    _session_state = value;
                }

                NotifyPropertyChanged("SessionState");
                NotifyPropertyChanged("IsSessionRunning");
                NotifyPropertyChanged("IsSessionPaused");
                NotifyPropertyChanged("TrialState");
            }
        }

        /// <summary>
        /// The state of the current trial within the session.  If this is set to "Idle", it means that no session
        /// is running, and no trial is running.  Therefore MotoTrak is in idle mode.  When a session is running,
        /// this should never be set to idle.
        /// </summary>
        private TrialRunState TrialState
        {
            get { return _trial_state; }
            set
            {
                lock (trial_state_lock)
                {
                    _trial_state = value;
                }

                NotifyPropertyChanged("TrialState");
            }
        }

        /// <summary>
        /// Whether or not a manual feed should be triggered ASAP.
        /// </summary>
        private bool IsTriggerManualFeed
        {
            get { return _trigger_manual_feed; }
            set
            {
                lock (manual_feed_lock)
                {
                    _trigger_manual_feed = value;
                }
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
                return _all_stages;
            }
            set
            {
                _all_stages = value;
                NotifyPropertyChanged("Stages");
                NotifyPropertyChanged("AvailableStages");
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
                var filtered_stages = Stages.Where(stage => stage.DeviceType == CurrentDevice.DeviceType).ToList();
                return filtered_stages;
            }
        }

        /// <summary>
        /// A boolean value indicating whether the session is currently idle or running.
        /// </summary>
        public bool IsSessionRunning
        {
            get
            {
                return (SessionState != SessionRunState.SessionNotRunning);
            }
        }

        /// <summary>
        /// Indicates whether the session is currently paused
        /// </summary>
        public bool IsSessionPaused
        {
            get
            {
                return (SessionState == SessionRunState.SessionPaused);
            }
        }

        /// <summary>
        /// This property contains the current "monitored" signal that gets displayed to the user.
        /// This property can ONLY be set by the background worker thread, in order to keep the application
        /// thread-safe.  This property can be read at any time by the main thread (the UI thread), but it should not
        /// be set by that thread.
        /// </summary>
        public SynchronizedCollection<SynchronizedCollection<double>> MonitoredSignal
        {
            get
            {
                return _monitoredSignal;
            }
            private set
            {
                _monitoredSignal = value;
                BackgroundPropertyChanged("MonitoredSignal");
            }
        }

        /// <summary>
        /// This is meant more as a debugging property.  It allows us to see how fast our program can loop and process data.
        /// This property is set by the HandleStreaming method.
        /// </summary>
        public int FramesPerSecond
        {
            get
            {
                return fps;
            }
            set
            {
                fps = value;
                BackgroundPropertyChanged("FramesPerSecond");
            }
        }

        /// <summary>
        /// The most recent analog value on the device
        /// </summary>
        public int DeviceAnalogValue
        {
            get
            {
                return _device_analog_value;
            }
            private set
            {
                _device_analog_value = value;
                BackgroundPropertyChanged("DeviceAnalogValue");
            }
        }

        /// <summary>
        /// The most recent calibrated value read from the device.
        /// </summary>
        public int DeviceCalibratedValue
        {
            get
            {
                return _device_calibrated_value;
            }
            private set
            {
                _device_calibrated_value = value;
                BackgroundPropertyChanged("DeviceCalibratedValue");
            }
        }


        #endregion

        #region Public methods that are called based on user interactions with the GUI

        /// <summary>
        /// This method initializes MotoTrak with a specified controller board.
        /// It does NOT start a session (in the sense of actively collecting data).  
        /// The session run state at the end of this method will be "SessionNotRunning".
        /// </summary>
        /// <param name="comPort">The serial port to connect to</param>
        public void InitializeMotoTrak (string comPort)
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
            bool success = ControllerBoard.ConnectToArduino(comPort);
            if (!success || !ControllerBoard.IsSerialConnectionValid)
            {
                MotoTrakMessaging.GetInstance().AddMessage("Unable to connect to MotoTrak controller board!");
            }

            //Check the board version
            if (!ControllerBoard.DoesSketchMeetMinimumRequirements())
            {
                MotoTrakMessaging.GetInstance().AddMessage("The controller board that is connected is not compatible with this version of MotoTrak.");
            }

            //Gather information about the booth and what devices are connected to it
            BoothLabel = ControllerBoard.GetBoothLabel();
            CurrentDevice = ControllerBoard.GetMotorDevice();
            
            //If no device was found, or if the device is unknown, throw an error.
            if (CurrentDevice == null || CurrentDevice.DeviceType == MotorDeviceType.Unknown)
            {
                MotoTrakMessaging.GetInstance().AddMessage("We are unable to recognize the device that is attached to the MotoTrak controller board.");
            }
            
            //At this point, we need to read the MotoTrak configuration file to determine how to load in stages
            MotoTrakConfiguration config = MotoTrakConfiguration.GetInstance();
            config.ReadConfigurationFile();

            //Now that configuration file has been loaded, and all config variables have been set, let's read in stages
            //Setting the "Stages" variable also sets the "AvailableStages" variable because of the nature of how the 
            //getters/setters are written.
            Stages = MotorStage.RetrieveAllStages();

            //Create a brand new session to be run as our current session
            CurrentSession = new MotoTrakSession();
            CurrentSession.BoothLabel = BoothLabel;
            CurrentSession.Device = CurrentDevice;

            //Now set the selected stage to a default value
            if (AvailableStages.Count > 0)
            {
                //If there are available stages for the current device, the default stage is the first stage in the list.
                CurrentSession.SelectedStage = AvailableStages[0];

                //Tell the Arduino board to stream data at the sampling rate defined in the default stage
                SetStreamingParameters();

                //Start a background worker which will continue looping and reading in data.
                _background_thread = new BackgroundWorker();
                _background_thread.WorkerSupportsCancellation = true;
                _background_thread.WorkerReportsProgress = true;
                _background_thread.DoWork += HandleStreaming;
                _background_thread.ProgressChanged += NotifyMainThreadOfNewData;
                _background_thread.RunWorkerCompleted += CloseBackgroundThread;
                _background_thread.RunWorkerAsync();
            }
            else
            {
                //If there are no available stages for this device
                CurrentSession.SelectedStage = null;
            }
        }

        /// <summary>
        /// This function is called when the user closes MotoTrak
        /// </summary>
        public void ShutdownMotoTrak ()
        {
            //Cancel the background thread that is reading data from the MotoTrak controller board.
            if (_background_thread != null)
            {
                _background_thread.CancelAsync();
            }
        }
        
        /// <summary>
        /// Starts a new MotoTrak session.
        /// </summary>
        public void StartSession()
        {
            SessionState = SessionRunState.SessionBegin;
        }

        /// <summary>
        /// Stops the session that is currently running.
        /// </summary>
        public void StopSession()
        {
            SessionState = SessionRunState.SessionEnd;
        }

        /// <summary>
        /// Pauses the session that is currently running
        /// </summary>
        /// <param name="pause">True if the user wants to pause, false to unpause</param>
        public void PauseSession(bool pause)
        {
            if (pause)
            {
                SessionState = SessionRunState.SessionPaused;
            }
            else
            {
                SessionState = SessionRunState.SessionUnpaused;
            }
        }

        /// <summary>
        /// Resets the baseline on the controller board
        /// </summary>
        public void ResetBaseline()
        {
            //This should only be done if the session is not currently running
            if (this.SessionState == SessionRunState.SessionNotRunning)
            {
                //Set the trial state to reset the baseline.  After it finishes,
                //it will automatically reset itself to the Idle state.
                TrialState = TrialRunState.ResetBaseline;
            }
        }

        /// <summary>
        /// Triggers a manual feed
        /// </summary>
        public void TriggerManualFeed()
        {
            IsTriggerManualFeed = true;
        }

        #endregion

        #region Private properties edited by the background worker thread

        private SynchronizedCollection<SynchronizedCollection<double>> _monitoredSignal = new SynchronizedCollection<SynchronizedCollection<double>>();
        private int fps = 0;
        private int _device_analog_value = 0;
        private int _device_calibrated_value = 0;

        #endregion

        #region BackgroundWorker methods

        /// <summary>
        /// This method is called by the background worker thread whenever it is cancelled.  This will occur whenever the MotoTrak window
        /// is closed, because that is the only time the thread would ever be cancelled.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseBackgroundThread(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MotoTrakMessaging.GetInstance().AddMessage("The MotoTrak background thread shut down unexpectedly. This is a fatal error, and you must restart MotoTrak. Details of the error have been saved the error data file.");
                ErrorLoggingService.GetInstance().LogExceptionError(e.Error);
            }
        }

        /// <summary>
        /// This method notifies the main thread of updates to any data that may be important for it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NotifyMainThreadOfNewData(object sender, ProgressChangedEventArgs e)
        {
            lock (propertyNamesLock)
            {
                foreach (var name in propertyNames)
                {
                    NotifyPropertyChanged(name);
                }

                propertyNames.Clear();
            }
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
            ControllerBoard.EnableStreaming(1);

            //Create a null trial object as a placeholder for new trials that will be created
            CurrentTrial = null;

            //Subscribe to changes from the MotoTrakMessaging system that we can pass up to the UI
            MotoTrakMessaging.GetInstance().PropertyChanged += BackgroundThread_ReactToMessagingSystemUpdate;

            //Get the index of the device signal within the data streams
            var stream_types = CurrentSession.SelectedStage.DataStreamTypes;
            int device_signal_index = stream_types.IndexOf(MotorBoardDataStreamType.DeviceValue);
            if (device_signal_index == -1)
            {
                //If no stream position was defined for the device signal for this stage
                MotoTrakMessaging.GetInstance().AddMessage(
                    "No device signal was defined!  Please select a stage that properly defines the device signal.");

                //Close the background thread and return
                e.Cancel = true;
                return;
            }

            //Define the buffer size for streaming data from the Arduino board
            int buffer_size = CurrentSession.SelectedStage.TotalRecordedSamplesPerTrial;

            /*
             * The following section of code will simply create some variables that we will need throughout 
             * our endless loop.
             */

            /*
             * First, we need to handle the raw data stream. We will populate it with default values for each stream.
             */
            
            //First, define a 2D array to hold each stream
            List<List<int>> stream_data_raw = new List<List<int>>();
            for (int i = 0; i < CurrentSession.SelectedStage.TotalDataStreams; i++)
            {
                //Now iterate over all existing streams, and add a 1D array to our 2D array to hold each stream
                int default_value = 0;
                if (i == device_signal_index)
                {
                    default_value = Convert.ToInt32(Math.Floor(CurrentDevice.Baseline));
                }

                var new_stream = Enumerable.Repeat<int>(default_value, buffer_size).ToList();
                stream_data_raw.Add(new_stream);
            }
            
            /*
             * Now, let's create a variable in which we will store a version of the stream data 
             * that has been transformed from its raw format to something more useful.
             */

            //Create a 2D array to hold transformed stream data
            List<List<double>> stream_data_transformed = new List<List<double>>();
            for (int i = 0; i < CurrentSession.SelectedStage.TotalDataStreams; i++)
            {
                //Add a list of floating-point values for each stream we will be handling
                var new_stream = Enumerable.Repeat<double>(0, buffer_size).ToList();
                stream_data_transformed.Add(new_stream);
            }

            /*
             * Finally, let's create another variable to hold data from the current trial.
             * This is separate from the CurrentTrial object.
             */

            //Create an array to hold transformed trial data
            List<List<double>> current_trial_data_transformed = new List<List<double>>();
            for (int i = 0; i < CurrentSession.SelectedStage.TotalDataStreams; i++)
            {
                //Add empty arrays for each stream since no trial has actually happened yet
                current_trial_data_transformed.Add(new List<double>());
            }
            
            /*
             * Now we will move on from declaring variables...
             */

            //Welcome the user to MotoTrak
            MotoTrakMessaging.GetInstance().AddMessage("Welcome to MotoTrak!");

            //Set up a stopwatch timer to track frame rate
            Stopwatch stop_watch = new Stopwatch();
            stop_watch.Start();
            int frames = 0;

            //This loop is endless, for all intents and purposes.
            //It will exit when the user closes the MotoTrak window.
            while (!_background_thread.CancellationPending)
            {
                //Report progress at the beginning of each loop iteration
                _background_thread.ReportProgress(0, null);

                //Handle the "frames-per-second" measurement for debugging purposes.
                if (stop_watch.ElapsedMilliseconds >= 1000)
                {
                    FramesPerSecond = frames;
                    frames = 0;
                    stop_watch.Reset();
                    stop_watch.Start();
                }
                else
                {
                    frames++;
                }

                //Run the autopositioner
                MotoTrakAutopositioner.GetInstance().RunAutopositioner();

                //Read in new datapoints from the Arduino board
                var new_data_points = ReadNewDataFromArduino();

                //If the number of new data points exceeds the buffer size, reduce the number of new data points and only keep the most recent
                if (new_data_points.Count > buffer_size)
                {
                    new_data_points = new_data_points.GetRange(new_data_points.Count - buffer_size, buffer_size).ToList();
                }

                //Set a local variable indicating the number of new data points we have
                int number_of_new_data_points = new_data_points.Count;

                //Make a transposed copy of the new data
                var transposed_new_data = MotorMath.Transpose(new_data_points);
                
                if (transposed_new_data != null && transposed_new_data.Count > 0)
                {
                    //Add the raw data to the stream_data_raw variable
                    for (int i = 0; i < stream_data_raw.Count; i++)
                    {
                        stream_data_raw[i].AddRange(transposed_new_data[i]);
                    }
                }
                
                try
                {
                    //Perform transformations on the new data
                    var transformed_new_data = CurrentSession.SelectedStage.StageImplementation.TransformSignals(transposed_new_data,
                        CurrentSession.SelectedStage, CurrentSession.Device);

                    //Add the transformed data to the stream_data_transformed variable
                    for (int stream_index = 0; stream_index < transformed_new_data.Count; stream_index++)
                    {
                        stream_data_transformed[stream_index].AddRange(transformed_new_data[stream_index]);
                    }
                }
                catch
                {
                    MotoTrakMessaging.GetInstance().AddMessage("Unable to transform signal data!");
                }

                /*
                 * At this point, we have read in new data and transformed it.
                 * Now we need to reduce the size of both the RAW buffer and the TRANSFORMED buffer to make sure they 
                 * stay within the limits of the defined buffer size. 
                 */

                //Now reduce the size of raw stream data if the size has exceeded our max buffer size.
                stream_data_raw = stream_data_raw.Select((x, index) => 
                    x.Skip(Math.Max(0, x.Count - buffer_size)).Take(buffer_size).ToList()).ToList();

                //Do the same for the transformed data (these are in transposed form, so it has to be done a bit differently)
                stream_data_transformed = stream_data_transformed.Select((x, index) => 
                    x.Skip(Math.Max(0, x.Count - buffer_size)).Take(buffer_size).ToList()).ToList();
                
                //Set these properties for debugging purposes
                try
                {
                    DeviceAnalogValue = stream_data_raw[device_signal_index][stream_data_raw[device_signal_index].Count - 1];
                }
                catch
                {
                    DeviceAnalogValue = 0;
                }
                
                try
                {
                    DeviceCalibratedValue = Convert.ToInt32(stream_data_transformed[device_signal_index][stream_data_transformed[device_signal_index].Count - 1]);
                }
                catch
                {
                    DeviceCalibratedValue = 0;
                }
                
                //Check to see if a manual feed needs to be triggered
                if (IsTriggerManualFeed)
                {
                    TrialState = TrialRunState.TrialManualFeed;
                    IsTriggerManualFeed = false;
                }

                //Act on any changes to the session state
                switch (SessionState)
                {
                    case SessionRunState.SessionBegin:

                        //Create an empty list of trials for the new session.
                        CurrentSession.Trials = Enumerable.Empty<MotorTrial>().ToList();

                        //Clear all messages for the new session to begin.
                        MotoTrakMessaging.GetInstance().ClearMessages();

                        //Set the session state to be running
                        SessionState = SessionRunState.SessionRunning;

                        //Set the trial state
                        TrialState = TrialRunState.TrialSetup;

                        break;
                    case SessionRunState.SessionEnd:

                        //Do any work to finish up a session here.

                        //Set the trial state to idle
                        TrialState = TrialRunState.Idle;

                        //Set the session state to "not running"
                        SessionState = SessionRunState.SessionNotRunning;

                        break;
                    case SessionRunState.SessionPaused:

                        //In the case that the session is paused, the trial state will be set to "Idle".
                        //This is the ONLY time in which the trial state is "Idle" while a session is running.
                        //This will allow the pull handle output to still be viewed on the window, but trials
                        //cannot be initiated.
                        TrialState = TrialRunState.Idle;

                        break;
                    case SessionRunState.SessionUnpaused:

                        //In the case that the user has unpaused the session, we simply set the trial state to allow
                        //trials to begin again
                        TrialState = TrialRunState.TrialSetup;

                        //Now set the session state to be running
                        SessionState = SessionRunState.SessionRunning;

                        break;
                }

                //Perform actions based on which trial state we are in
                switch (TrialState)
                {
                    case TrialRunState.Idle:

                        //This is essentially the "idle" state of the program, when no animal is actually running.

                        //Make a "hard" copy of the signal data and assign it to the MonitoredSignal variable, which is what the user sees
                        //on the plot.  The "ToList" function makes a copy of the data, rather than copying a pointer.  This is important!
                        CopyDataToMonitoredSignal(stream_data_transformed);
                        
                        break;
                    case TrialRunState.ResetBaseline:

                        try
                        {
                            //Grab the data that is currently in the buffer and take the mean of it.
                            var device_signal_raw = stream_data_raw[device_signal_index];
                            int mean_signal_value = Convert.ToInt32(Math.Round(device_signal_raw.Average()));

                            //Set the baseline on the controller board.
                            ControllerBoard.SetBaseline(mean_signal_value);

                            //Set the local device baseline to the same value
                            CurrentDevice.Baseline = mean_signal_value;

                            //Set the baseline of the device object being stored in the current session
                            CurrentSession.Device.Baseline = mean_signal_value;

                            //Note that the device baseline has changed in the background
                            BackgroundPropertyChanged("CurrentDevice");
                        }
                        catch
                        {
                            //If an error occurs, log it
                            MotoTrakMessaging.GetInstance().AddMessage("Error while resetting baseline!");
                        }
                        
                        //Reset the trial state to be idle after the baseline has been reset
                        TrialState = TrialRunState.Idle;

                        break;
                    case TrialRunState.TrialSetup:
                        
                        //Do any necessary work to set up a new trial here

                        //Wait for a new trial to begin.
                        TrialState = TrialRunState.TrialWait;

                        break;
                    case TrialRunState.TrialWait:

                        //Set the trial initiation index to a default value
                        int trial_initiation_index = -1;

                        try
                        {
                            //Run code specific to this stage to check for trial initiation
                            trial_initiation_index = CurrentSession.SelectedStage.StageImplementation.CheckSignalForTrialInitiation(stream_data_transformed,
                                number_of_new_data_points, CurrentSession.SelectedStage);
                        }
                        catch
                        {
                            //If an error occurred within the stage implementation's code, log it:
                            MotoTrakMessaging.GetInstance().AddMessage("Error encountered while attempting to check for trial initiation");
                        }

                        //Handle the case in which a trial was initiated
                        if (trial_initiation_index > -1)
                        {
                            try
                            {
                                //Create a new motor trial
                                CurrentTrial = new MotorTrial();

                                //Initiate a new trial.
                                //The following method basically just fills the "trial_device_signal" object with the data that occured up until the device value
                                //broke the trial initiation threshold.  It also fills the same data into the "trial" object, but in raw form.
                                HandleTrialInitiation(buffer_size, trial_initiation_index, stream_data_raw,
                                    stream_data_transformed, CurrentTrial, out current_trial_data_transformed);

                                //Change the session state
                                TrialState = TrialRunState.TrialRun;
                            }
                            catch
                            {
                                //In the event of an error while handling a trial initation...
                                MotoTrakMessaging.GetInstance().AddMessage("Error encountered while initiating a new trial");
                            }
                        }

                        //Make a "hard" copy of the signal data and assign it to the MonitoredSignal variable, which is what the user sees
                        //on the plot.  The "ToList" function makes a copy of the data, rather than copying a pointer.  This is important!
                        //MonitoredSignal = device_signal_transformed.ToList();
                        CopyDataToMonitoredSignal(stream_data_transformed);
                        
                        break;
                    case TrialRunState.TrialRun:

                        //If we are in this state, it indicates that a new trial has already begun.  We are currently in the midst of running
                        //that trial.  Therefore, in this state we should take any new data that is coming in, and process that data 
                        //according to how the selected stage dictates that it should be processed.  If it is deemed a successful trial,
                        //then we should follow the procedure for a successful trial based on the stage settings (typically a feed, and also
                        //maybe a VNS stimulus or other output).  After the trial time has expired, we should move on to the next state.
                        //Trial success does NOT immediately move us to the next state.  ONLY TIME.

                        //Add the new raw data to the trial object to be saved to disk later.
                        for (int i = 0; i < CurrentTrial.TrialData.Count; i++)
                        {
                            var this_stream = stream_data_raw[i];
                            var data_to_add = this_stream.GetRange(this_stream.Count - number_of_new_data_points, number_of_new_data_points);
                            CurrentTrial.TrialData[i].AddRange(data_to_add);
                        }
                        
                        //First, we must grab any new stream data from the MotoTrak controller board.
                        var new_data = stream_data_transformed.Select(x => x.GetRange(x.Count - number_of_new_data_points, number_of_new_data_points).ToList()).ToList();

                        //Add the new data to the transformed trial signal, starting at the end of the current data that we already have.
                        current_trial_data_transformed = current_trial_data_transformed.Select((x, index) => x.ReplaceRange(new_data[index], CurrentTrial.TrialData.Count).ToList()).ToList();

                        //Check to see if the animal has succeeded up until this point in the trial, based on this stage's criterion for success.
                        if (CurrentTrial.Result == MotorTrialResult.Unknown)
                        {
                            //Check to see whether the trial was a success, based on the criterion defined by the stage definition.
                            List<Tuple<MotorTrialEventType, int>> events_found = null;

                            try
                            {
                                events_found = CurrentSession.SelectedStage.StageImplementation.CheckForTrialEvent(current_trial_data_transformed, CurrentSession.SelectedStage);
                            }
                            catch
                            {
                                //Make this an empty list in the event of an error in the stage implementation's code
                                events_found = new List<Tuple<MotorTrialEventType, int>>();

                                //Log the error
                                MotoTrakMessaging.GetInstance().AddMessage("Error while checking for trial events within stage implementation");
                            }
                            
                            //Go through the events that were found
                            foreach (var cur_event in events_found)
                            {
                                //Record a successful trial
                                if (cur_event.Item1 == MotorTrialEventType.SuccessfulTrial)
                                {
                                    CurrentTrial.Result = MotorTrialResult.Hit;
                                    CurrentTrial.HitTimes.Add(DateTime.Now);
                                    CurrentTrial.HitIndices.Add(cur_event.Item2);
                                }
                            }

                            //Check to see what actions we need to take based on the events that occurred
                            List<MotorTrialAction> event_actions = null;

                            try
                            {
                                event_actions = CurrentSession.SelectedStage.StageImplementation.ReactToTrialEvents(events_found, current_trial_data_transformed, CurrentSession.SelectedStage);
                            }
                            catch
                            {
                                //In the event of an error within the stage implementation's code, make event_actions an empty list
                                event_actions = new List<MotorTrialAction>();

                                //Log the error
                                MotoTrakMessaging.GetInstance().AddMessage("Error while attempting to react to trial events within stage implementation");
                            }
                            
                            //Perform each action
                            foreach (var action in event_actions)
                            {
                                action.ExecuteAction();
                            }
                        }

                        //Perform any necessary actions that need to be taken according to the stage parameters that are unrelated to
                        //actions that are taken given the success of a trial.
                        List<MotorTrialAction> actions = null;

                        try
                        {
                            actions = CurrentSession.SelectedStage.StageImplementation.PerformActionDuringTrial(current_trial_data_transformed, CurrentSession.SelectedStage);
                        }
                        catch
                        {
                            //Set actions to be an empty list
                            actions = new List<MotorTrialAction>();

                            //Log the error
                            MotoTrakMessaging.GetInstance().AddMessage("Error in stage implementation: PerformActionDuringTrial function");
                        }

                        foreach (var action in actions)
                        {
                            action.ExecuteAction();
                        }

                        //Check to see if this trial has finished
                        if (CurrentTrial.TrialData.Count >= CurrentSession.SelectedStage.TotalRecordedSamplesPerTrial)
                        {
                            //Set the trial result if it hasn't yet been set
                            if (CurrentTrial.Result == MotorTrialResult.Unknown)
                            {
                                CurrentTrial.Result = MotorTrialResult.Miss;
                            }
                            
                            //Change the session state to end this trial.
                            TrialState = TrialRunState.TrialEnd;
                        }

                        //Report progress on this trial to the main UI thread
                        CopyDataToMonitoredSignal(current_trial_data_transformed);

                        break;
                    case TrialRunState.TrialEnd:

                        //In this state, we finalize a trial and save it to the disk.

                        //Create an end of trial message
                        try
                        {
                            string msg = CurrentSession.SelectedStage.StageImplementation.CreateEndOfTrialMessage(CurrentTrial.Result == MotorTrialResult.Hit,
                                CurrentSession.Trials.Count + 1, current_trial_data_transformed, CurrentSession.SelectedStage);
                            MotoTrakMessaging.GetInstance().AddMessage(msg);
                        }
                        catch
                        {
                            //If an error was encountered, log it
                            MotoTrakMessaging.GetInstance().AddMessage("Error in stage implementation: CreateEndOfTrialMessage function");
                        }
                        
                        //First, add the trial to our collection of total trials for the currently running session.
                        CurrentSession.Trials.Add(CurrentTrial);

                        //Adjust the hit threshold for adaptive stages
                        CurrentSession.SelectedStage.StageImplementation.AdjustDynamicStageParameters(CurrentSession.Trials, 
                            current_trial_data_transformed, CurrentSession.SelectedStage);

                        //Set the current trial to null.  This will subsequently send notifications up to the UI,
                        //telling the UI that there is not currently a trial taking place.
                        CurrentTrial = null;

                        //Tell the program to wait for another trial to begin
                        TrialState = TrialRunState.TrialSetup;

                        break;
                    case TrialRunState.TrialManualFeed:

                        //Trigger a manual feed
                        ControllerBoard.TriggerFeeder();

                        //Set up a new trial
                        TrialState = TrialRunState.TrialSetup;

                        break;
                }

                //Sleep the thread for 30 milliseconds so we don't consume too much CPU time
                Thread.Sleep(30);
            }

            //If we reach this point in the code, it means that user has decided to close the MotoTrak window.  This next line of code
            //tells anyone who is listening that the background worker is being cancelled.
            e.Cancel = true;
        }

        private void BackgroundThread_ReactToMessagingSystemUpdate(object sender, PropertyChangedEventArgs e)
        {
            //Call BackgroundPropertyChanged for the property name that has been passed in.
            BackgroundPropertyChanged(e.PropertyName);
        }

        private void CopyDataToMonitoredSignal (List<List<double>> data)
        {
            MonitoredSignal.Clear();
            foreach (var stream in data)
            {
                var new_sync_stream = new SynchronizedCollection<double>(new object(), stream);
                MonitoredSignal.Add(new_sync_stream);
            }

            BackgroundPropertyChanged("MonitoredSignal");
        }

        private List<List<int>> ReadNewDataFromArduino()
        {
            //Read in new streaming data from the Arduino board
            List<List<int>> new_data_points = ControllerBoard.ReadStream();
            return new_data_points;
        }

        /// <summary>
        /// This function does some simple manipulation of the stream data upon a recognized trial initiation.
        /// If this function is called, it means a trial has been initiated.  
        /// 
        /// The function receives a parameter
        /// buffer_size which is how much data we retain in both the raw and transformed buffers.
        /// 
        /// trial_initiation_index is the index into the buffer at which the trial initiation occurred.
        /// 
        /// stream_data_raw is the raw streaming data, in the format: [ [a1 ... a_n] [b1 ... b_n] [c1 ... c_n] ]
        /// 
        /// stream_data_transformed is the transformed streaming data, in the format: [ [a1 ... a_n] [b1 ... b_n] [c1 ... c_n] ]
        /// 
        /// trial is the MotorTrial object that has been created to store the brand new trial
        /// 
        /// trial_data_transformed is the data for the trial so far (only up through the trial initiation).  This is calculated
        /// by this function and returned as a reference parameter.
        /// </summary>
        private void HandleTrialInitiation(int buffer_size, int trial_initiation_index, 
            List<List<int>> stream_data_raw, List<List<double>> stream_data_transformed,
            MotorTrial trial, out List<List<double>> trial_data_transformed)
        {
            //Do some bounds checking of the trial initiation index
            trial_initiation_index = Math.Max(0, Math.Min(stream_data_raw.Count, trial_initiation_index));

            //From the point where threshold was broken, we want to go back and grab X seconds of data from before the initiation
            //event, depending on how much data this stage asks for.
            int point_to_start_keeping_data = trial_initiation_index - CurrentSession.SelectedStage.TotalRecordedSamplesBeforeHitWindow;

            //Do some bounds checking
            point_to_start_keeping_data = Math.Max(0, Math.Min(stream_data_raw.Count, point_to_start_keeping_data));

            //Now that we know when threshold was broken, transfer all the data that pertains to the actual trial over to the trial variables
            trial.TrialData = stream_data_raw.Select((x, index) =>
                x.GetRange(point_to_start_keeping_data, trial_initiation_index - point_to_start_keeping_data + 1).ToList()).ToList();
            for (int i = 0; i < trial.TrialData.Count; i++)
            {
                var data_from_stream = trial.TrialData[i];

                int default_value = 0;
                if (CurrentSession.SelectedStage.DataStreamTypes[i] == MotorBoardDataStreamType.DeviceValue)
                {
                    default_value = Convert.ToInt32(Math.Floor(CurrentDevice.Baseline));
                }

                if (data_from_stream.Count < CurrentSession.SelectedStage.TotalRecordedSamplesBeforeHitWindow)
                {
                    //Zero-pad if needed.
                    int number_of_samples_needed = CurrentSession.SelectedStage.TotalRecordedSamplesBeforeHitWindow - data_from_stream.Count;
                    List<int> samples_to_prepend = Enumerable.Repeat<int>(default_value, number_of_samples_needed).ToList();
                    samples_to_prepend.AddRange(data_from_stream);
                    data_from_stream = samples_to_prepend;
                    trial.TrialData[i] = data_from_stream;
                }    
            }
            
            //Do the same for the transformed device signal
            List<List<double>> pre_trial_device_signal = new List<List<double>>();
            foreach (var stream in stream_data_transformed)
            {
                var data_from_stream = stream.GetRange(point_to_start_keeping_data, trial_initiation_index - point_to_start_keeping_data + 1);
                
                if (data_from_stream.Count < CurrentSession.SelectedStage.TotalRecordedSamplesBeforeHitWindow)
                {
                    //Zero-pad if needed.  Same as above.
                    int number_of_samples_needed = CurrentSession.SelectedStage.TotalRecordedSamplesBeforeHitWindow - data_from_stream.Count;
                    List<double> samples_to_prepend = Enumerable.Repeat<double>(0, number_of_samples_needed).ToList();
                    samples_to_prepend.AddRange(data_from_stream);
                    data_from_stream = samples_to_prepend;
                }

                pre_trial_device_signal.Add(data_from_stream);
            }

            trial_data_transformed = pre_trial_device_signal;
        }

        #endregion

        #region Background property changed

        private List<string> propertyNames = new List<string>();
        private object propertyNamesLock = new object();

        private void BackgroundPropertyChanged(string name)
        {
            lock (propertyNamesLock)
            {
                propertyNames.Add(name);
            }
        }

        #endregion

    }
}
