using MotoTrakBase;
using MotoTrakUtilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Speech.Synthesis;
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
        public Tuple<double, double> OldCalibrationValues = null;
        
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
        private CalibrationStates _current_calibration_state = CalibrationStates.SetupNextCalibration;
        private string _current_countdown_text = string.Empty;
        private ConcurrentDictionary<double, double> _current_calibration_values = new ConcurrentDictionary<double, double>();

        public enum CalibrationStates
        {
            SetupNextCalibration,
            RunningCountdown,
            GrabbingValues,
            ThankUser
        }

        /// <summary>
        /// The current state of the calibration process
        /// </summary>
        public CalibrationStates CurrentCalibrationState
        {
            get
            {
                return _current_calibration_state;
            }
            private set
            {
                _current_calibration_state = value;
                _background_property_changed("CurrentCalibrationState");
            }
        }

        /// <summary>
        /// The current countdown text
        /// </summary>
        public string CurrentCountdownText
        {
            get
            {
                return _current_countdown_text.ToUpper();
            }
            private set
            {
                _current_countdown_text = value;
                _background_property_changed("CurrentCountdownText");
            }
        }

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

                //Subtract the event handlers to make sure that we do not add them as duplicates
                _background_thread.DoWork -= _background_thread_DoWork;
                _background_thread.ProgressChanged -= _background_thread_ProgressChanged;
                _background_thread.RunWorkerCompleted -= _background_thread_RunWorkerCompleted;

                //Add the event handlers
                _background_thread.DoWork += _background_thread_DoWork;
                _background_thread.ProgressChanged += _background_thread_ProgressChanged;
                _background_thread.RunWorkerCompleted += _background_thread_RunWorkerCompleted;

                //Run the background thread
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

        /// <summary>
        /// Saves the current calibration values to the motor baord
        /// </summary>
        public void SaveCalibration ()
        {
            var board = MotorBoard.GetInstance();
            int grams = 0;
            int ticks = 0;
            
            if (PullDevice.Slope > 1)
            {
                grams = Int16.MaxValue;
                ticks = Convert.ToInt32(Math.Round(Convert.ToDouble(grams) / PullDevice.Slope));
            }
            else
            {
                ticks = Int16.MaxValue;
                grams = Convert.ToInt32(Math.Round(PullDevice.Slope * Convert.ToDouble(ticks)));
            }

            //Set the baseline
            board.SetBaseline(Convert.ToInt32(PullDevice.Baseline));

            //Set the maximum number of grams to be the maximum of a 16-bit signed integer
            board.SetCalGrams(grams);

            //Set the maximum number of ticks to be the max grams / slope
            board.SetNPerCalGrams(ticks); 
        }

        /// <summary>
        /// Loads the previous calibration values to be the current calibration.
        /// Does NOT save them to the motor board.
        /// </summary>
        public void LoadPreviousCalibration ()
        {
            if (OldCalibrationValues != null)
            {
                //Update the current calibration to have the same values as the previous one
                PullDevice.Baseline = OldCalibrationValues.Item1;
                PullDevice.Slope = OldCalibrationValues.Item2;

                //Null out the previous calibration
                OldCalibrationValues = null;
            }
        }

        /// <summary>
        /// This function verifies that the calibration values currently on the board match
        /// the calibration values that we expect.
        /// </summary>
        /// <returns></returns>
        public bool VerifyCalibration ()
        {
            //Get the board instance
            var board = MotorBoard.GetInstance();

            //Stop streaming
            board.EnableStreaming(0);

            //Wait for a little bit
            Thread.Sleep(500);

            //Clear the stream
            board.ClearStream();

            //Query the current device
            var pull_device = board.GetMotorDevice();

            //Re-enable streaming
            board.EnableStreaming(1);

            //Make sure the current device's calibration values match what we have in memory
            bool result = true;
            for (int i = 0; i < pull_device.Coefficients.Count; i++)
            {
                var a = pull_device.Coefficients[i];
                var b = PullDevice.Coefficients[i];
                if (!MotorMath.EqualsApproximately(a, b, 0.0001))
                {
                    result = false;
                    break;
                }
            }

            return result;
        }

        #endregion

        #region Background thread methods

        private ConcurrentBag<string> _property_names = new ConcurrentBag<string>();

        private void _background_property_changed (string propertyName)
        {
            _property_names.Add(propertyName);
        }

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
            string result = string.Empty;
            bool success = false;
            while (_property_names.Count > 0)
            {
                success = _property_names.TryTake(out result);
                if (success)
                {
                    NotifyPropertyChanged(result);
                }
            }
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

            //Check to see if we will be rebaselining during this calibration process
            bool rebaseline = false;
            if (weights_to_calibrate.Contains(PullWeight.Grams_0))
            {
                rebaseline = true;
            }

            bool update_slope = false;
            int size = weights_to_calibrate.Where(x => x != PullWeight.Grams_0).Count();
            if (size > 0)
            {
                update_slope = true;
            }

            //Create a boolean variable to track whether the calibration process is finished
            bool done = false;

            //Set the initial calibration state
            CurrentCalibrationState = CalibrationStates.SetupNextCalibration;

            //Create a variable to hold the weight currently being calibrated
            PullWeight current_weight = PullWeight.Grams_0;

            //Strings to be said during the countdown
            List<string> countdown_strings = new List<string>();

            //Create a stopwatch for the countdown
            Stopwatch countdown_timer = new Stopwatch();

            //Instantiate a speech synthesizer object
            SpeechSynthesizer synthesizer = new SpeechSynthesizer();
            bool ready_for_next = true;

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
                            countdown_strings.Add("Please apply " + current_weight_string + " grams and hold");

                            if (IsCountdownOn)
                            {
                                countdown_strings.Add("3");
                                countdown_strings.Add("2");
                                countdown_strings.Add("1");
                                countdown_strings.Add("Measuring");
                            }

                            //Start the countdown timer
                            countdown_timer.Start();

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
                        
                        if (countdown_strings.Count > 0)
                        {
                            if (synthesizer.State == SynthesizerState.Ready)
                            {
                                if (ready_for_next)
                                {
                                    //Reset the ready flag
                                    ready_for_next = false;

                                    //Get the thing we need the computer to say
                                    string this_countdown_step = countdown_strings.FirstOrDefault();
                                    countdown_strings.RemoveAt(0);

                                    //Set the string for the GUI
                                    CurrentCountdownText = this_countdown_step;

                                    //Have the computer announce the next countdown step
                                    synthesizer.SpeakAsync(this_countdown_step);
                                }
                                else
                                {
                                    if (!countdown_timer.IsRunning)
                                    {
                                        //Restart the timer for the next countdown step
                                        countdown_timer.Restart();
                                    }
                                    else
                                    {
                                        double elapsed_time = countdown_timer.Elapsed.TotalMilliseconds;
                                        if (elapsed_time >= 250)
                                        {
                                            ready_for_next = true;
                                            countdown_timer.Stop();
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            //Set the current calibration state to do the actual grabbing of values for this weight value
                            CurrentCalibrationState = CalibrationStates.GrabbingValues;

                            //Reset the timer for the period of grabbing values
                            countdown_timer.Restart();
                        }

                        break;
                    case CalibrationStates.GrabbingValues:

                        if (countdown_timer.ElapsedMilliseconds >= 3000)
                        {
                            //Reset the countdown timer
                            countdown_timer.Reset();

                            //Take an average of all the data values currently in the buffer
                            try
                            {
                                var data_buffer = DeviceStreamModel.GetInstance().DataBuffer.ToList();
                                double avg = data_buffer.Average();

                                double w = PullWeightConverter.ConvertFromEnumeratedValueToNumerical(current_weight);
                                if (_current_calibration_values.ContainsKey(w))
                                {
                                    _current_calibration_values[w] = avg;
                                }
                                else
                                {
                                    _current_calibration_values.TryAdd(w, avg);
                                }
                            }
                            catch (Exception err)
                            {
                                ErrorLoggingService.GetInstance().LogExceptionError(err);
                            }

                            //Set the calibration state to the next state
                            CurrentCalibrationState = CalibrationStates.ThankUser;
                        }

                        break;
                    case CalibrationStates.ThankUser:

                        //Thank the user
                        synthesizer.Speak("Thank you");

                        //Sleep the thread for a little bit.  This simply gives the user time to
                        //prepare the next calibration weight
                        Thread.Sleep(500);

                        //Set the calibration state to set up the next calibration
                        CurrentCalibrationState = CalibrationStates.SetupNextCalibration;

                        break;
                }

                //Report on our progress
                _background_thread.ReportProgress(0);

                //Sleep for a little while so we don't consume the CPU
                Thread.Sleep(33);
            }

            //Save the old values to a Tuple object
            OldCalibrationValues = new Tuple<double, double>(PullDevice.Baseline, PullDevice.Slope);

            //At this point, the calibration process has finished.
            //Let's calculate the least squares regression line for all of the calibration weights
            if (update_slope)
            {
                try
                {
                    double[] weight_keys = _current_calibration_values.Keys.ToArray();
                    double[] ticks_values = _current_calibration_values.Values.ToArray();
                    if (weight_keys.Length > 1)
                    {
                        Tuple<double, double> reg_values = MathNet.Numerics.LinearRegression.SimpleRegression.Fit(ticks_values, weight_keys);
                        double slope = reg_values.Item2;
                        
                        //Save the new values to the pull device
                        PullDevice.Slope = slope;
                    }
                }
                catch (Exception err)
                {
                    ErrorLoggingService.GetInstance().LogExceptionError(err);
                }
            }
            
            //Update the baseline if need be
            if (rebaseline)
            {
                //Get the ticks at the 0 gram weight value
                if (_current_calibration_values.Keys.Contains(0))
                {
                    double new_baseline = _current_calibration_values[0];

                    //Save the new calibration values
                    PullDevice.Baseline = Convert.ToInt32(new_baseline);
                }
            }

            //Reset the calibration state for the next run
            CurrentCalibrationState = CalibrationStates.SetupNextCalibration;

            //Report progress one last time
            _background_thread.ReportProgress(0);
        }

        #endregion
    }
}
