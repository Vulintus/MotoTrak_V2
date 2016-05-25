using MotoTrakUtilities;
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
            List<List<int>> stream_data_raw = new List<List<int>>();

            //Define a list in which we will keep signal data from the device.
            List<double> device_signal_transformed = new List<double>();

            //Define a list that will be used as the buffer for the device signal during the trial.
            List<double> trial_device_signal = new List<double>();

            //Define an integer that will be used to keep track of the index at which a hit occurred, if so.
            int TrialHitSampleIndex = -1;

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
                int number_of_new_data_points = ReadNewDataFromArduino(stream_data_raw, buffer_size);

                //Convert the raw stream data to a form that is useable by the program (transform it by the slope and baseline
                //according to the device settings.  This happens regardless of what stage has been selected).
                device_signal_transformed = stream_data_raw.Select(x => (Device.Slope * (x[device_signal_index] - Device.Baseline))).ToList();

                //Now we will act upon the signal based upon what run-state we are in, and also based upon the stage selected by the user.
                switch (SessionState)
                {
                    case SessionRunState.Scan:

                        //This is essentially the "idle" state of the program, when no animal is actually running.

                        //Make a "hard" copy of the signal data and assign it to the MonitoredSignal variable, which is what the user sees
                        //on the plot.  The "ToList" function makes a copy of the data, rather than copying a pointer.  This is important!
                        MonitoredSignal = device_signal_transformed.ToList();

                        break;
                    case SessionRunState.TrialWait:

                        //Run code specific to this stage to check for trial initiation
                        int trial_initiation_index = SelectedStage.StageImplementation.CheckSignalForTrialInitiation(device_signal_transformed, SelectedStage);
                        
                        //Handle the case in which a trial was initiated
                        if (trial_initiation_index > -1)
                        {
                            //Create a new motor trial
                            trial = new MotorTrial();

                            //Initiate a new trial.
                            //The following method basically just fills the "trial_device_signal" object with the data that occured up until the device value
                            //broke the trial initiation threshold.  It also fills the same data into the "trial" object, but in raw form.
                            HandleTrialInitiation(buffer_size, trial_initiation_index, stream_data_raw, device_signal_transformed, trial, trial_device_signal);

                            //Unmark the previous trial's "hit sample index" if need be
                            TrialHitSampleIndex = -1;

                            //Change the session state
                            SessionState = SessionRunState.TrialRun;
                        }

                        break;
                    case SessionRunState.TrialRun:

                        //If we are in this state, it indicates that a new trial has already begun.  We are currently in the midst of running
                        //that trial.  Therefore, in this state we should take any new data that is coming in, and process that data 
                        //according to how the selected stage dictates that it should be processed.  If it is deemed a successful trial,
                        //then we should follow the procedure for a successful trial based on the stage settings (typically a feed, and also
                        //maybe a VNS stimulus or other output).  After the trial time has expired, we should move on to the next state.
                        //Trial success does NOT immediately move us to the next state.  ONLY TIME.

                        //First, we must grab any new stream data from the MotoTrak controller board.
                        var new_data = device_signal_transformed.GetRange(device_signal_transformed.Count - number_of_new_data_points, number_of_new_data_points);

                        //Add the new data to the trial device signal, starting at the end of the current data that we already have.
                        trial_device_signal.ReplaceRange(new_data, trial.TrialData.Count);

                        //Add the new raw data to the trial object to be saved to disk later.
                        trial.TrialData.AddRange(stream_data_raw.GetRange(stream_data_raw.Count - number_of_new_data_points, number_of_new_data_points));

                        //Check to see if the animal has succeeded up until this point in the trial, based on this stage's criterion for success.
                        if (trial.Result == MotorTrialResult.Unknown)
                        {
                            //Check to see whether the trial was a success, based on the criterion defined by the stage definition.
                            var success = SelectedStage.StageImplementation.CheckForTrialSuccess(trial_device_signal, SelectedStage);
                            
                            if (success.Item1 == MotorTrialResult.Hit)
                            {
                                //Record the results.
                                trial.Result = success.Item1;
                                trial.HitTime = DateTime.Now;
                                TrialHitSampleIndex = success.Item2;

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
                        if (trial.TrialData.Count >= SelectedStage.TotalRecordedSamplesPerTrial)
                        {
                            //Change the session state to end this trial.
                            SessionState = SessionRunState.TrialEnd;
                        }

                        //Report progress on this trial to the main UI thread
                        MonitoredSignal = trial_device_signal;
                        backgroundLoop.ReportProgress(0, null);

                        break;
                    case SessionRunState.TrialEnd:
                        break;
                    case SessionRunState.TrialManualFeed:
                        break;
                    case SessionRunState.Pause:

                        //In this state we should discard all data and never update the UI.  Animals will not get any pellets
                        //for any work performed, and no new trials will be initiated.

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
        
        private int ReadNewDataFromArduino ( List<List<int>> raw_stream_data, int buffer_size )
        {
            //Read in new streaming data from the Arduino board
            List<List<int>> new_data_points = ArdyBoard.ReadStream();
            int number_of_new_data_points = new_data_points.Count;

            //Add the new data points to our buffer
            raw_stream_data.AddRange(new_data_points);

            //Now reduce the size of raw stream data if the size has exceeded our max buffer size.
            raw_stream_data = raw_stream_data.Skip(Math.Max(0, raw_stream_data.Count - buffer_size)).Take(buffer_size).ToList();

            return number_of_new_data_points;
        }

        private void HandleTrialInitiation ( int buffer_size, int trial_initiation_index, List<List<int>> stream_data_raw, List<double> device_signal_transformed, 
            MotorTrial trial, List<double> trial_device_signal )
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
                List<List<int>> samples_to_prepend = Enumerable.Repeat<List<int>>(new List<int>() { 0, 0, 0 }, number_of_samples_needed).ToList();
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
    }

}
