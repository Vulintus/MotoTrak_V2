using MotoTrakBase;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MotoTrakCalibration
{
    /// <summary>
    /// Model class for doing pull calibrations
    /// </summary>
    public class PullCalibrationModel : NotifyPropertyChangedObject
    {
        #region Public data members

        public List<PullWeightModel> TestWeights = new List<PullWeightModel>();
        public MotorDevice PullDevice = null;
        
        #endregion

        #region Singleton Constructor

        private static PullCalibrationModel _instance = null;

        /// <summary>
        /// Standard constructor
        /// </summary>
        private PullCalibrationModel()
        {
            InitializeTestWeights();
        }

        /// <summary>
        /// Gets the singleton instance of the pull calibration model class
        /// </summary>
        public static PullCalibrationModel GetInstance ()
        {
            if (_instance == null)
            {
                _instance = new PullCalibrationModel();
            }

            return _instance;
        }

        #endregion

        #region Private methods

        private void InitializeTestWeights()
        {
            //Get all possible weights
            var all_weights = Enum.GetValues(typeof(PullWeight)).Cast<PullWeight>().ToList();

            //Add each weight to the array
            foreach (var w in all_weights)
            {
                PullWeightModel new_weight = new PullWeightModel
                {
                    Weight = w,
                    IsVoice = true
                };

                TestWeights.Add(new_weight);
            }

            //Sort the list, just to be sure
            TestWeights = TestWeights.OrderBy(x => PullWeightConverter.ConvertFromEnumeratedValueToNumerical(x.Weight)).ToList();
        }

        #endregion

        #region Private data members

        private bool _is_running_calibration = false;
        private bool _is_calibration_single_weight = false;
        private PullWeight _single_weight = PullWeight.Grams_0;
        private BackgroundWorker _background_thread = new BackgroundWorker();

        private enum CalibrationStates
        {
            SetupNextCalibration,
            RunningCountdown,
            GrabbingValues,

        }

        private CalibrationStates CurrentCalibrationState = CalibrationStates.SetupNextCalibration;

        #endregion

        #region Properties

        public bool IsCountdownOn = true;

        public bool IsRunningCalibration
        {
            get
            {
                return _is_running_calibration;
            }
            set
            {
                _is_running_calibration = value;
                NotifyPropertyChanged("IsRunningCalibration");
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Runs a new calibration session
        /// </summary>
        public void RunCalibration ()
        {
            if (!_background_thread.IsBusy)
            {
                //Set the flag indicating that a calibration is currently running
                IsRunningCalibration = true;

                //Start a background thread to run the calibration process
                _background_thread.WorkerSupportsCancellation = true;
                _background_thread.WorkerReportsProgress = true;
                _background_thread.DoWork += _background_thread_DoWork;
                _background_thread.ProgressChanged += _background_thread_ProgressChanged; ;
                _background_thread.RunWorkerCompleted += _background_thread_RunWorkerCompleted; ;
                _background_thread.RunWorkerAsync();
            }
        }

        /// <summary>
        /// Runs a calibration of a specific weight
        /// </summary>
        /// <param name="w">The weight to be calibrated</param>
        public void RunCalibration (PullWeight w)
        {
            //Indicate that the calibration that is going to be run is for a single weight
            _is_calibration_single_weight = true;
            _single_weight = w;

            //Run the calibration function
            RunCalibration();
        }

        /// <summary>
        /// Cancels a calibration session that is currently running
        /// </summary>
        public void CancelCalibration ()
        {
            if (_background_thread.IsBusy)
            {
                _background_thread.CancelAsync();
            }
        }

        #endregion

        #region Background thread methods

        private void _background_thread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //Tell the UI that we are no longer running a calibration
            IsRunningCalibration = false;

            //Reset the single weight flags if necessary
            _is_calibration_single_weight = false;
            _single_weight = PullWeight.Grams_0;
        }

        private void _background_thread_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            
        }

        private void _background_thread_DoWork(object sender, DoWorkEventArgs e)
        {
            //Make a list of weights that we will be calibrating
            List<PullWeight> weights_to_calibrate = new List<PullWeight>();
            if (_is_calibration_single_weight)
            {
                weights_to_calibrate.Add(_single_weight);
            }
            else
            {
                foreach (var w in TestWeights)
                {
                    if (w.IsVoice)
                    {
                        weights_to_calibrate.Add(w.Weight);
                    }
                }
            }

            //Create a boolean variable to track whether the calibration process is finished
            bool done = false;

            //Set the initial calibration state
            CurrentCalibrationState = CalibrationStates.SetupNextCalibration;

            //Create a variable to hold the weight currently being calibrated
            PullWeight current_weight = PullWeight.Grams_0;

            //Strings to be said during the countdown
            List<string> countdown_strings = new List<string>();

            while (!_background_thread.CancellationPending && !done)
            {
                switch (CurrentCalibrationState)
                {
                    case CalibrationStates.SetupNextCalibration:

                        if (weights_to_calibrate.Count > 0)
                        {
                            //Set the current weight to calibrate
                            current_weight = weights_to_calibrate[0];
                            weights_to_calibrate.RemoveAt(0);


                            //Create a list of strings for the countdown part
                            string current_weight_string = PullWeightConverter.ConvertFromEnumeratedValueToNumerical(current_weight).ToString();
                            countdown_strings.Clear();
                            countdown_strings.Add("Please apply " + current_weight_string + " grams and hold.");

                            if (IsCountdownOn)
                            {
                                countdown_strings.Add("3");
                                countdown_strings.Add("2");
                                countdown_strings.Add("1");
                                countdown_strings.Add("GO");
                            }

                            //Set the next state
                            CurrentCalibrationState = CalibrationStates.RunningCountdown;
                        }
                        else
                        {
                            //Set the flag indicating we are finished
                            done = true;
                        }
                        
                        break;
                    case CalibrationStates.RunningCountdown:



                        break;
                    case CalibrationStates.GrabbingValues:
                        break;
                }

                //Sleep for a little while so we don't consume the CPU
                Thread.Sleep(33);
            }
        }

        #endregion
    }
}
