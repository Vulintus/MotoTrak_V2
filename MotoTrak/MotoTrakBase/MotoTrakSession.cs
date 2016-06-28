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
            TrialSetup,
            TrialWait,
            TrialRun,
            TrialEnd,
            TrialManualFeed
        }

        #endregion

        #region Private data members

        private SessionRunState _sessionState = SessionRunState.SessionNotRunning;
        private TrialRunState _trialState = TrialRunState.Idle;
        private bool _triggerManualFeed = false;
        
        private MotorBoard _ardy = MotorBoard.GetInstance();

        private int _boothNumber = int.MinValue;
        private MotorDevice _device = new MotorDevice();
        private string _ratName = string.Empty;

        private MotorStage _selectedStage = new MotorStage();
        private List<MotorStage> _allStages = new List<MotorStage>();

        private Object session_state_lock = new Object();
        private Object trial_state_lock = new Object();
        private Object manual_feed_lock = new object();
        
        #endregion

        #region Private properties

        /// <summary>
        /// The motor controller board object
        /// </summary>
        private MotorBoard ArdyBoard
        {
            get { return _ardy; }
            set { _ardy = value; }
        }

        /// <summary>
        /// The state of the session: whether it is running, not running, beginning, ending, or paused.
        /// </summary>
        private SessionRunState SessionState
        {
            get { return _sessionState; }
            set
            {
                //The session state can be modified by both the UI thread and the background thread
                //So we need to make sure that it is within a semaphore.
                lock (session_state_lock)
                {
                    _sessionState = value;
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
            get { return _trialState; }
            set
            {
                lock(trial_state_lock)
                {
                    _trialState = value;
                }

                NotifyPropertyChanged("TrialState");
            }
        }

        /// <summary>
        /// Whether or not a manual feed should be triggered ASAP.
        /// </summary>
        private bool IsTriggerManualFeed
        {
            get { return _triggerManualFeed; }
            set
            {
                lock(manual_feed_lock)
                {
                    _triggerManualFeed = value;
                }
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
                NotifyPropertyChanged("BoothNumber");
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
                NotifyPropertyChanged("Device");
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
                NotifyPropertyChanged("RatName");
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
                NotifyPropertyChanged("SelectedStage");
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
                var filtered_stages = Stages.Where(stage => stage.DeviceType == Device.DeviceType).ToList();
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
                return (SessionState == SessionRunState.SessionRunning);
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

        #region Methods that alter the session state, called by the UI thread, affecting the background worker thread

        /// <summary>
        /// Starts a new MotoTrak session.
        /// </summary>
        public void StartSession ()
        {
            SessionState = SessionRunState.SessionBegin;
        }

        /// <summary>
        /// Stops the session that is currently running.
        /// </summary>
        public void StopSession ()
        {
            SessionState = SessionRunState.SessionEnd;
        }

        /// <summary>
        /// Pauses the session that is currently running
        /// </summary>
        /// <param name="pause">True if the user wants to pause, false to unpause</param>
        public void PauseSession (bool pause)
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
        /// Triggers a manual feed
        /// </summary>
        public void TriggerManualFeed ()
        {
            IsTriggerManualFeed = true;
        }

        /// <summary>
        /// This function is called when the user closes MotoTrak
        /// </summary>
        public void CancelBackgroundLoop ()
        {
            if (backgroundLoop != null)
            {
                backgroundLoop.CancelAsync();
            }
        }

        #endregion

        #region Properties pertaining to the background worker thread, but not edited by that thread

        BackgroundWorker backgroundLoop = null;

        #endregion

        #region Private properties edited by the background worker thread

        private SynchronizedCollection<double> _monitoredSignal = new SynchronizedCollection<double>();
        private List<MotorTrial> _trials = new List<MotorTrial>();
        private MotorTrial _currentTrial = null;
        private ObservableCollection<Tuple<MotoTrakMessageType, string>> _messages = new ObservableCollection<Tuple<MotoTrakMessageType, string>>();
        private int fps = 0;

        #endregion

        #region Properties edited by the background worker thread

        /// <summary>
        /// This property contains the current "monitored" signal that gets displayed to the user.
        /// This property can ONLY be set by the background worker thread, in order to keep the application
        /// thread-safe.  This property can be read at any time by the main thread (the UI thread), but it should not
        /// be set by that thread.
        /// </summary>
        public SynchronizedCollection<double> MonitoredSignal
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
        /// This property contains the set of all trials for the session that is currently running.
        /// This property can ONLY be set by the background worker thread, in order to keep the application
        /// thread-safe.  This property can be read at any time by the main thread (the UI thread), but it should not
        /// be set by that thread.
        /// </summary>
        public List<MotorTrial> Trials
        {
            get
            {
                return _trials;
            }
            private set
            {
                _trials = value;
                BackgroundPropertyChanged("Trials");
            }
        }

        /// <summary>
        /// A MotorTrial object representing the current trial that is taking place (if one is taking place).
        /// </summary>
        public MotorTrial CurrentTrial
        {
            get
            {
                return _currentTrial;
            }
            private set
            {
                _currentTrial = value;
                BackgroundPropertyChanged("CurrentTrial");
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
            ArdyBoard.EnableStreaming(1);

            //Create a null trial object as a placeholder for new trials that will be created
            CurrentTrial = null;

            //Define the buffer size for streaming data from the Arduino board
            int buffer_size = SelectedStage.TotalRecordedSamplesPerTrial;

            //Define some variables that will be needed throughout our endless loop.
            //First, we want to define an array that holds the raw stream of data
            List<List<int>> stream_data_raw = new List<List<int>>();

            List<int> empty_sample_test = Enumerable.Repeat<int>(0, SelectedStage.TotalDataStreams).ToList();
            empty_sample_test[SelectedStage.DataStreamTypes.IndexOf(MotorBoardDataStreamType.DeviceValue)] = Convert.ToInt32(Device.Baseline);
            stream_data_raw = Enumerable.Repeat<List<int>>(empty_sample_test, buffer_size).ToList();

            //Define a list in which we will keep signal data from the device.
            List<double> device_signal_transformed = new List<double>();

            //Define a list that will be used as the buffer for the device signal during the trial.
            List<double> trial_device_signal = new List<double>();

            //Define an integer that will be used to keep track of the index at which a hit occurred, if so.
            //int TrialHitSampleIndex = -1;

            //Get the index of the device signal within the data stream
            int device_signal_index = SelectedStage.DataStreamTypes.IndexOf(MotorBoardDataStreamType.DeviceValue);
            if (device_signal_index == -1)
            {
                //If no stream position was defined for the device signal for this stage
                Messages.Add(new Tuple<MotoTrakMessageType, string>(MotoTrakMessageType.Error,
                    "The device signal is undefined!  Please select a stage that properly defines the device signal."));
            }
            
            Messages.Add(new Tuple<MotoTrakMessageType, string>(MotoTrakMessageType.Normal, "Welcome to MotoTrak!"));

            //Set up a stopwatch timer to track frame rate
            Stopwatch stop_watch = new Stopwatch();
            stop_watch.Start();
            int frames = 0;

            //This loop is endless, for all intents and purposes.
            //It will exit when the user closes the MotoTrak window.
            while (!backgroundLoop.CancellationPending)
            {
                //Report progress at the beginning of each loop iteration
                backgroundLoop.ReportProgress(0, null);

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
                
                //Read in new datapoints from the Arduino board
                int number_of_new_data_points = ReadNewDataFromArduino(stream_data_raw, buffer_size);

                //Now reduce the size of raw stream data if the size has exceeded our max buffer size.
                stream_data_raw = stream_data_raw.Skip(Math.Max(0, stream_data_raw.Count - buffer_size)).Take(buffer_size).ToList();

                //Convert the raw stream data to a form that is useable by the program (transform it by the slope and baseline
                //according to the device settings.  This happens regardless of what stage has been selected).
                device_signal_transformed = stream_data_raw.Select(x => (Device.Slope * (x[device_signal_index] - Device.Baseline))).ToList();

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
                        Trials = Enumerable.Empty<MotorTrial>().ToList();

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
                        MonitoredSignal = new SynchronizedCollection<double>(new object(), device_signal_transformed.ToList());
                        
                        break;
                    case TrialRunState.TrialSetup:

                        //Reset the raw stream data buffer for the next trial
                        List<int> empty_sample = Enumerable.Repeat<int>(0, SelectedStage.TotalDataStreams).ToList();
                        empty_sample[SelectedStage.DataStreamTypes.IndexOf(MotorBoardDataStreamType.DeviceValue)] = Convert.ToInt32(Device.Baseline);
                        //stream_data_raw = Enumerable.Repeat<List<int>>(empty_sample, buffer_size).ToList();

                        //Wait for a new trial to begin.
                        TrialState = TrialRunState.TrialWait;

                        break;
                    case TrialRunState.TrialWait:

                        //Run code specific to this stage to check for trial initiation
                        int trial_initiation_index = SelectedStage.StageImplementation.CheckSignalForTrialInitiation(device_signal_transformed, number_of_new_data_points, SelectedStage);
                        
                        //Handle the case in which a trial was initiated
                        if (trial_initiation_index > -1)
                        {
                            //Create a new motor trial
                            CurrentTrial = new MotorTrial();

                            //Initiate a new trial.
                            //The following method basically just fills the "trial_device_signal" object with the data that occured up until the device value
                            //broke the trial initiation threshold.  It also fills the same data into the "trial" object, but in raw form.
                            HandleTrialInitiation(buffer_size, trial_initiation_index, stream_data_raw, device_signal_transformed, CurrentTrial, out trial_device_signal);

                            //Unmark the previous trial's "hit sample index" if need be
                            CurrentTrial.HitIndex = -1;

                            //Change the session state
                            TrialState = TrialRunState.TrialRun;
                        }

                        //Make a "hard" copy of the signal data and assign it to the MonitoredSignal variable, which is what the user sees
                        //on the plot.  The "ToList" function makes a copy of the data, rather than copying a pointer.  This is important!
                        //MonitoredSignal = device_signal_transformed.ToList();
                        MonitoredSignal = new SynchronizedCollection<double>(new object(), device_signal_transformed.ToList());

                        break;
                    case TrialRunState.TrialRun:

                        //If we are in this state, it indicates that a new trial has already begun.  We are currently in the midst of running
                        //that trial.  Therefore, in this state we should take any new data that is coming in, and process that data 
                        //according to how the selected stage dictates that it should be processed.  If it is deemed a successful trial,
                        //then we should follow the procedure for a successful trial based on the stage settings (typically a feed, and also
                        //maybe a VNS stimulus or other output).  After the trial time has expired, we should move on to the next state.
                        //Trial success does NOT immediately move us to the next state.  ONLY TIME.

                        //First, we must grab any new stream data from the MotoTrak controller board.
                        var new_data = device_signal_transformed.GetRange(device_signal_transformed.Count - number_of_new_data_points, number_of_new_data_points);

                        //Add the new data to the trial device signal, starting at the end of the current data that we already have.
                        trial_device_signal.ReplaceRange(new_data, CurrentTrial.TrialData.Count);

                        //Add the new raw data to the trial object to be saved to disk later.
                        CurrentTrial.TrialData.AddRange(stream_data_raw.GetRange(stream_data_raw.Count - number_of_new_data_points, number_of_new_data_points));

                        //Check to see if the animal has succeeded up until this point in the trial, based on this stage's criterion for success.
                        if (CurrentTrial.Result == MotorTrialResult.Unknown)
                        {
                            //Check to see whether the trial was a success, based on the criterion defined by the stage definition.
                            var success = SelectedStage.StageImplementation.CheckForTrialSuccess(trial_device_signal, SelectedStage);
                            
                            if (success.Item1 == MotorTrialResult.Hit)
                            {
                                //Record the results.
                                CurrentTrial.Result = success.Item1;
                                CurrentTrial.HitTime = DateTime.Now;
                                CurrentTrial.HitIndex = success.Item2;

                                //If the trial was successful, take certain actions as described by the stage definition.
                                var success_actions = SelectedStage.StageImplementation.ReactToTrialSuccess(trial_device_signal, SelectedStage);
                                TakeAction(success_actions);
                            }
                        }

                        //Perform any necessary actions that need to be taken according to the stage parameters that are unrelated to
                        //actions that are taken given the success of a trial.
                        var actions = SelectedStage.StageImplementation.PerformActionDuringTrial(trial_device_signal, SelectedStage);
                        TakeAction(actions);

                        //Check to see if this trial has finished
                        if (CurrentTrial.TrialData.Count >= SelectedStage.TotalRecordedSamplesPerTrial)
                        {
                            //Change the session state to end this trial.
                            TrialState = TrialRunState.TrialEnd;
                        }

                        //Report progress on this trial to the main UI thread
                        //MonitoredSignal = trial_device_signal;
                        MonitoredSignal = new SynchronizedCollection<double>(new object(), trial_device_signal);

                        break;
                    case TrialRunState.TrialEnd:

                        //In this state, we finalize a trial and save it to the disk.
                        
                        //Create an end of trial message
                        string msg = SelectedStage.StageImplementation.CreateEndOfTrialMessage(CurrentTrial.Result == MotorTrialResult.Hit,
                            Trials.Count + 1, trial_device_signal, SelectedStage);
                        Messages.Add(new Tuple<MotoTrakMessageType, string>(MotoTrakMessageType.Normal, msg));
                        
                        //First, add the trial to our collection of total trials for the currently running session.
                        Trials.Add(CurrentTrial);

                        //Adjust the hit threshold for adaptive stages
                        SelectedStage.StageImplementation.AdjustDynamicHitThreshold(Trials, trial_device_signal, SelectedStage);

                        //Set the current trial to null.  This will subsequently send notifications up to the UI,
                        //telling the UI that there is not currently a trial taking place.
                        CurrentTrial = null;

                        //Tell the program to wait for another trial to begin
                        TrialState = TrialRunState.TrialSetup;

                        break;
                    case TrialRunState.TrialManualFeed:

                        //Trigger a manual feed
                        ArdyBoard.TriggerFeeder();

                        //Set up a new trial
                        TrialState = TrialRunState.TrialSetup;

                        break;
                }
                
                //Sleep the thread for 12 milliseconds so we don't consume too much CPU time
                //Thread.Sleep(12);
            }

            //If we reach this point in the code, it means that user has decided to close the MotoTrak window.  This next line of code
            //tells anyone who is listening that the background worker is being cancelled.
            e.Cancel = true;
        }
        
        private int ReadNewDataFromArduino ( List<List<int>> stream_data_raw, int buffer_size )
        {
            //Read in new streaming data from the Arduino board
            List<List<int>> new_data_points = ArdyBoard.ReadStream();
            int number_of_new_data_points = new_data_points.Count;

            //Add the new data points to our buffer
            stream_data_raw.AddRange(new_data_points);
            
            return number_of_new_data_points;
        }

        private void HandleTrialInitiation ( int buffer_size, int trial_initiation_index, List<List<int>> stream_data_raw, List<double> device_signal_transformed, 
            MotorTrial trial, out List<double> trial_device_signal )
        {
            //From the point where threshold was broken, we want to go back and grab X seconds of data from before the initiation
            //event, depending on how much data this stage asks for.
            int point_to_start_keeping_data = trial_initiation_index - SelectedStage.TotalRecordedSamplesBeforeHitWindow;
            if (point_to_start_keeping_data < 0)
            {
                point_to_start_keeping_data = 0;
            }

            //Now that we know when threshold was broken, transfer all the data that pertains to the actual trial over to the trial variables
            trial.TrialData = stream_data_raw.GetRange(point_to_start_keeping_data, trial_initiation_index - point_to_start_keeping_data + 1);
            if (trial.TrialData.Count < SelectedStage.TotalRecordedSamplesBeforeHitWindow)
            {
                //If not enough samples were in the buffer to fill out the necessary number of samples needed, we will simply zero-pad the 
                //beginning of the signal.
                int number_of_samples_needed = SelectedStage.TotalRecordedSamplesBeforeHitWindow - trial.TrialData.Count;

                //Reset the raw stream data buffer for the next trial
                List<int> empty_sample = Enumerable.Repeat<int>(0, SelectedStage.TotalDataStreams).ToList();
                List<List<int>> samples_to_prepend = Enumerable.Repeat<List<int>>(empty_sample, number_of_samples_needed).ToList();

                samples_to_prepend.AddRange(trial.TrialData);
                trial.TrialData = samples_to_prepend;
            }

            //Do the same for the transformed device signal
            var pre_trial_device_signal = device_signal_transformed.GetRange(point_to_start_keeping_data,
                trial_initiation_index - point_to_start_keeping_data + 1);
            if (pre_trial_device_signal.Count < SelectedStage.TotalRecordedSamplesBeforeHitWindow)
            {
                //Zero-pad if needed.  Same as above.
                int number_of_samples_needed = SelectedStage.TotalRecordedSamplesBeforeHitWindow - pre_trial_device_signal.Count;
                List<double> samples_to_prepend = Enumerable.Repeat<double>(0, number_of_samples_needed).ToList();
                samples_to_prepend.AddRange(pre_trial_device_signal);
                pre_trial_device_signal = samples_to_prepend;
            }

            //Now add the device signal from the X seconds before the hit window to our buffer in which we keep the whole trial device signal
            trial_device_signal = Enumerable.Repeat<double>(0, buffer_size).ToList();
            trial_device_signal.ReplaceRange(pre_trial_device_signal, 0);
        }

        private void TakeAction (List<MotorTrialAction> actions)
        {
            //Go through the list of actions to take, and perform each of them.
            foreach (MotorTrialAction a in actions)
            {
                switch (a)
                {
                    case MotorTrialAction.TriggerFeeder:
                        ArdyBoard.TriggerFeeder();
                        break;
                    case MotorTrialAction.PlaySound:
                        //this needs to be implemented
                        break;
                    case MotorTrialAction.SendStimulationTrigger:
                        ArdyBoard.TriggerStim();
                        break;
                }
            }
        }

        #endregion

        #region Background property changed

        private List<string> propertyNames = new List<string>();
        private object propertyNamesLock = new object();

        private void BackgroundPropertyChanged (string name)
        {
            lock (propertyNamesLock)
            {
                propertyNames.Add(name);
            }
        }

        #endregion
    }

}
