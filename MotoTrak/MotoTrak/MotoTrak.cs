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
    public class MotoTrak : NotifyPropertyChangedObject
    {
        #region Singleton

        private static MotoTrak _instance = null;

        private MotoTrak()
        {
            //empty
        }

        /// <summary>
        /// Get the MotoTrak class instance
        /// </summary>
        /// <returns>Returns the MotoTrak instance</returns>
        public static MotoTrak GetInstance()
        {
            if (_instance == null)
            {
                _instance = new MotoTrak();
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
                var filtered_stages = Stages.Where(stage => stage.DeviceType == CurrentSession.Device.DeviceType).ToList();
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
        /// This observable collection contains a set of tuples that define messages for the user.
        /// These messages have a message "type" associated with each one (the first item of the tuple), and a string that contains
        /// the actual message text (the second item of the tuple).
        /// </summary>
        public ObservableCollection<Tuple<MotoTrakMessageType, string>> Messages
        {
            get
            {
                return _messages;
            }
            set
            {
                _messages = value;
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
            ControllerBoard.ConnectToArduino(comPort);
            if (!ControllerBoard.IsSerialConnectionValid)
            {
                throw new MotoTrakException(MotoTrakExceptionType.UnableToConnectToControllerBoard, string.Empty);
            }

            //Check the board version
            if (!ControllerBoard.DoesSketchMeetMinimumRequirements())
            {
                throw new MotoTrakException(MotoTrakExceptionType.ControllerBoardNotCompatible, string.Empty);
            }

            //Gather information about the booth and what devices are connected to it
            BoothLabel = ControllerBoard.GetBoothNumber().ToString();
            CurrentDevice = ControllerBoard.GetMotorDevice();
            
            //If no device was found, or if the device is unknown, throw an error.
            if (CurrentSession.Device == null || CurrentSession.Device.DeviceType == MotorDeviceType.Unknown)
            {
                throw new MotoTrakException(MotoTrakExceptionType.UnrecognizedDevice, string.Empty);
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
        private ObservableCollection<Tuple<MotoTrakMessageType, string>> _messages = new ObservableCollection<Tuple<MotoTrakMessageType, string>>();
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
            //empty function
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

            //Get the index of the device signal within the data streams
            var stream_types = CurrentSession.SelectedStage.StreamParameters.Select(x => x.StreamType).ToList();
            int device_signal_index = stream_types.IndexOf(MotorBoardDataStreamType.DeviceValue);
            if (device_signal_index == -1)
            {
                //If no stream position was defined for the device signal for this stage
                Messages.Add(new Tuple<MotoTrakMessageType, string>(MotoTrakMessageType.Error,
                    "The device signal is undefined!  Please select a stage that properly defines the device signal."));
            }

            //Define the buffer size for streaming data from the Arduino board
            int buffer_size = CurrentSession.SelectedStage.TotalRecordedSamplesPerTrial;

            //Define some variables that will be needed throughout our endless loop.
            //First, we want to define an array that holds the raw stream of data
            List<List<int>> stream_data_raw = new List<List<int>>();

            //Create an example fake empty sample from the MotoTrak controller that we will use to initially fill the buffers with
            List<int> fake_empty_sample = Enumerable.Repeat<int>(0, CurrentSession.SelectedStage.TotalDataStreams).ToList();

            //Create an empty array to hold our transformed stream data
            List<List<double>> stream_data_transformed = new List<List<double>>();

            //Create an array to hold transformed trial data
            List<List<double>> current_trial_data_transformed = new List<List<double>>();

            //Find the index of the data stream that holds the data coming in from the device (within our fake empty sample), and set its
            //value to be the baseline device value rather than the default value of 0.
            fake_empty_sample[device_signal_index] = Convert.ToInt32(CurrentDevice.Baseline);

            //Initially fill the raw data stream with our fake empty samples.
            stream_data_raw = Enumerable.Repeat<List<int>>(fake_empty_sample, buffer_size).ToList();
            
            //Welcome the user to MotoTrak
            Messages.Add(new Tuple<MotoTrakMessageType, string>(MotoTrakMessageType.Normal, "Welcome to MotoTrak!"));

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
                int number_of_new_data_points = new_data_points.Count;

                //Make a transposed copy of the new data
                var transposed_data_copy = MotorMath.Transpose(new_data_points);
                
                //Perform transformations on the new data
                var transformed_new_data = CurrentSession.SelectedStage.StageImplementation.TransformSignals(transposed_data_copy,
                    CurrentSession.SelectedStage, CurrentSession.Device);

                //Add the raw data to the stream_data_raw variable
                stream_data_raw.AddRange(new_data_points);

                //Add the transformed data to the stream_data_transformed variable
                stream_data_transformed.AddRange(transformed_new_data);

                //Now reduce the size of raw stream data if the size has exceeded our max buffer size.
                stream_data_raw = stream_data_raw.Skip(Math.Max(0, stream_data_raw.Count - buffer_size)).Take(buffer_size).ToList();

                //Do the same for the transformed data (these are in transposed form, so it has to be done a bit differently)
                foreach (var s in stream_data_transformed)
                {
                    s.Skip(Math.Max(0, stream_data_raw.Count - buffer_size)).Take(buffer_size).ToList();
                }
                
                //Set these properties for debugging purposes
                DeviceAnalogValue = stream_data_raw[stream_data_raw.Count - 1][device_signal_index];
                DeviceCalibratedValue = Convert.ToInt32(stream_data_transformed[device_signal_index][stream_data_transformed[device_signal_index].Count - 1]);

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
                        Messages.Clear();

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

                        //Grab the data that is currently in the buffer and take the mean of it.
                        var device_signal_raw = stream_data_raw.Select(x => x[device_signal_index]).ToList();
                        int mean_signal_value = Convert.ToInt32(Math.Round(device_signal_raw.Average()));

                        //Set the baseline on the controller board.
                        ControllerBoard.SetBaseline(mean_signal_value);

                        //Set the local device baseline to the same value
                        CurrentDevice.Baseline = mean_signal_value;

                        //Note that the device baseline has changed in the background
                        BackgroundPropertyChanged("Device");

                        //Reset the trial state to be idle after the baseline has been reset
                        TrialState = TrialRunState.Idle;

                        break;
                    case TrialRunState.TrialSetup:
                        
                        //Do any necessary work to set up a new trial here

                        //Wait for a new trial to begin.
                        TrialState = TrialRunState.TrialWait;

                        break;
                    case TrialRunState.TrialWait:

                        //Run code specific to this stage to check for trial initiation
                        int trial_initiation_index = CurrentSession.SelectedStage.StageImplementation.CheckSignalForTrialInitiation(stream_data_transformed, 
                            number_of_new_data_points, CurrentSession.SelectedStage);

                        //Handle the case in which a trial was initiated
                        if (trial_initiation_index > -1)
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
                        CurrentTrial.TrialData.AddRange(stream_data_raw.GetRange(stream_data_raw.Count - number_of_new_data_points, number_of_new_data_points));

                        //First, we must grab any new stream data from the MotoTrak controller board.
                        var new_data = stream_data_transformed.GetRange(stream_data_transformed.Count - number_of_new_data_points, number_of_new_data_points);

                        //Add the new data to the transformed trial signal, starting at the end of the current data that we already have.
                        //trial_device_signal.ReplaceRange(new_data, CurrentTrial.TrialData.Count);

                        //Check to see if the animal has succeeded up until this point in the trial, based on this stage's criterion for success.
                        if (CurrentTrial.Result == MotorTrialResult.Unknown)
                        {
                            //Check to see whether the trial was a success, based on the criterion defined by the stage definition.
                            var events_found = CurrentSession.SelectedStage.StageImplementation.CheckForTrialEvent(current_trial_data_transformed, CurrentSession.SelectedStage);

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
                            var event_actions = CurrentSession.SelectedStage.StageImplementation.ReactToTrialEvents(events_found, current_trial_data_transformed, CurrentSession.SelectedStage);
                            
                            //Perform each action
                            foreach (var action in event_actions)
                            {
                                action.ExecuteAction();
                            }
                        }

                        //Perform any necessary actions that need to be taken according to the stage parameters that are unrelated to
                        //actions that are taken given the success of a trial.
                        var actions = CurrentSession.SelectedStage.StageImplementation.PerformActionDuringTrial(current_trial_data_transformed, CurrentSession.SelectedStage);
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
                        string msg = CurrentSession.SelectedStage.StageImplementation.CreateEndOfTrialMessage(CurrentTrial.Result == MotorTrialResult.Hit,
                            CurrentSession.Trials.Count + 1, current_trial_data_transformed, CurrentSession.SelectedStage);
                        Messages.Add(new Tuple<MotoTrakMessageType, string>(MotoTrakMessageType.Normal, msg));

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

        private void CopyDataToMonitoredSignal (List<List<double>> data)
        {
            MonitoredSignal.Clear();
            foreach (var sample in data)
            {
                var new_sync_sample = new SynchronizedCollection<double>(new object(), sample);
                MonitoredSignal.Add(new_sync_sample);
            }
        }

        private List<List<int>> ReadNewDataFromArduino()
        {
            //Read in new streaming data from the Arduino board
            List<List<int>> new_data_points = ControllerBoard.ReadStream();
            return new_data_points;
        }

        private void HandleTrialInitiation(int buffer_size, int trial_initiation_index, List<List<int>> stream_data_raw, List<List<double>> stream_data_transformed,
            MotorTrial trial, out List<List<double>> trial_data_transformed)
        {
            List<int> empty_sample = Enumerable.Repeat<int>(0, CurrentSession.SelectedStage.TotalDataStreams).ToList();
            List<double> empty_transformed_sample = Enumerable.Repeat<double>(0, CurrentSession.SelectedStage.TotalDataStreams).ToList();

            //From the point where threshold was broken, we want to go back and grab X seconds of data from before the initiation
            //event, depending on how much data this stage asks for.
            int point_to_start_keeping_data = trial_initiation_index - CurrentSession.SelectedStage.TotalRecordedSamplesBeforeHitWindow;
            if (point_to_start_keeping_data < 0)
            {
                point_to_start_keeping_data = 0;
            }

            //Now that we know when threshold was broken, transfer all the data that pertains to the actual trial over to the trial variables
            trial.TrialData = stream_data_raw.GetRange(point_to_start_keeping_data, trial_initiation_index - point_to_start_keeping_data + 1);
            if (trial.TrialData.Count < CurrentSession.SelectedStage.TotalRecordedSamplesBeforeHitWindow)
            {
                //If not enough samples were in the buffer to fill out the necessary number of samples needed, we will simply zero-pad the 
                //beginning of the signal.
                int number_of_samples_needed = CurrentSession.SelectedStage.TotalRecordedSamplesBeforeHitWindow - trial.TrialData.Count;

                //Reset the raw stream data buffer for the next trial
                List<List<int>> samples_to_prepend = Enumerable.Repeat<List<int>>(empty_sample, number_of_samples_needed).ToList();

                samples_to_prepend.AddRange(trial.TrialData);
                trial.TrialData = samples_to_prepend;
            }

            //Do the same for the transformed device signal
            var pre_trial_device_signal = stream_data_transformed.GetRange(point_to_start_keeping_data,
                trial_initiation_index - point_to_start_keeping_data + 1);
            if (pre_trial_device_signal.Count < CurrentSession.SelectedStage.TotalRecordedSamplesBeforeHitWindow)
            {
                //Zero-pad if needed.  Same as above.
                int number_of_samples_needed = CurrentSession.SelectedStage.TotalRecordedSamplesBeforeHitWindow - pre_trial_device_signal.Count;
                
                List<List<double>> samples_to_prepend = Enumerable.Repeat<List<double>>(empty_transformed_sample, number_of_samples_needed).ToList();

                samples_to_prepend.AddRange(pre_trial_device_signal);
                pre_trial_device_signal = samples_to_prepend;
            }

            //Now add the device signal from the X seconds before the hit window to our buffer in which we keep the whole trial device signal
            trial_data_transformed = Enumerable.Repeat<List<double>>(empty_transformed_sample, buffer_size).ToList();
            trial_data_transformed.ReplaceRange(pre_trial_device_signal, 0);
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
