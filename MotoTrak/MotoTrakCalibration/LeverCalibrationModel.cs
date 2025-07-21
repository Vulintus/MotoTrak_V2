using MotoTrakBase;
using MotoTrakUtilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace MotoTrakCalibration
{
    /// <summary>
    /// Model class for running lever calibration
    /// </summary>
    public class LeverCalibrationModel : NotifyPropertyChangedObject
    {
        #region Singleton Constructor

        private static LeverCalibrationModel _instance = null;

        /// <summary>
        /// Constructor
        /// </summary>
        private LeverCalibrationModel ()
        {
            //empty
        }

        /// <summary>
        /// Returns singleton instance of the class
        /// </summary>
        public static LeverCalibrationModel GetInstance ()
        {
            if (_instance == null)
            {
                _instance = new LeverCalibrationModel();
            }

            return _instance;
        }

        #endregion

        #region Private data members

        private BackgroundWorker _background_thread = new BackgroundWorker();
        private bool _is_running_calibration = false;
        private int _max_value = 0;
        private int _min_value = 1024;
        private int _saved_max = 0;
        private int _saved_min = 0;

        #endregion

        #region Public data members

        public MotorDevice LeverDevice = null;

        #endregion

        #region Public properties

        /// <summary>
        /// Whether or not the calibration process is currently running
        /// </summary>
        public bool IsRunningCalibration
        {
            get
            {
                return _is_running_calibration;
            }
            private set
            {
                _is_running_calibration = value;
                NotifyPropertyChanged("IsRunningCalibration");
            }
        }

        /// <summary>
        /// The max value saved to the board
        /// </summary>
        public int SavedMaxValue
        {
            get
            {
                return _saved_max;
            }
            set
            {
                _saved_max = value;
                NotifyPropertyChanged("SavedMaxValue");
            }
        }

        /// <summary>
        /// The min value saved to the board
        /// </summary>
        public int SavedMinValue
        {
            get
            {
                return _saved_min;
            }
            set
            {
                _saved_min = value;
                NotifyPropertyChanged("SavedMinValue");
            }
        }

        /// <summary>
        /// The maximum value from the calibration process
        /// </summary>
        public int MaxValue
        {
            get
            {
                return _max_value;
            }
            private set
            {
                _max_value = value;
                _background_property_changed("MaxValue");
            }
        }

        /// <summary>
        /// The minimum value from the calibration process
        /// </summary>
        public int MinValue
        {
            get
            {
                return _min_value;
            }
            private set
            {
                _min_value = value;
                _background_property_changed("MinValue");
            }
        }

        #endregion
        
        #region Public methods

        /// <summary>
        /// Begins the calibration process
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
        /// Stops the calibration process
        /// </summary>
        public void StopCalibration ()
        {
            if (_background_thread.IsBusy)
            {
                _background_thread.CancelAsync();
            }
        }

        #endregion

        #region Background thread methods

        /// <summary>
        /// This function verifies that the calibration values currently on the board match
        /// the calibration values that we expect.
        /// </summary>
        /// <returns></returns>
        private bool _verify_calibration (MotorDevice device_expected)
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
            var device_actual = board.GetMotorDevice();

            //Re-enable streaming
            board.EnableStreaming(1);

            //Make sure the current device's calibration values match what we have in memory
            bool result = true;
            for (int i = 0; i < device_actual.Coefficients.Count; i++)
            {
                var a = device_actual.Coefficients[i];
                var b = device_expected.Coefficients[i];
                if (!MotorMath.EqualsApproximately(a, b, 0.0001))
                {
                    result = false;
                    break;
                }
            }

            return result;
        }

        private ConcurrentBag<string> _property_names = new ConcurrentBag<string>();

        private void _background_property_changed(string propertyName)
        {
            _property_names.Add(propertyName);
        }

        private void _background_thread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //Set the flag indicating the calibration is no longer running
            IsRunningCalibration = false;

            //Save the data to the board
            var board = MotorBoard.GetInstance();

            //Sat the saved values for the GUI
            SavedMaxValue = MaxValue;
            SavedMinValue = MinValue;

            //Set the lever baseline
            board.SetBaseline(MaxValue);

            //Set the range of the lever
            board.SetNPerCalGrams(MaxValue - MinValue);

            //Set the range of the lever in degrees
            board.SetCalGrams(MotorDevice.LeverRangeInDegrees);

            //Set the calibration values on the device object
            if (LeverDevice != null)
            {
                LeverDevice.Baseline = MaxValue;
                LeverDevice.Slope = -Convert.ToDouble(MotorDevice.LeverRangeInDegrees) / Convert.ToDouble(MaxValue - MinValue);
            }

            //Verify the calibration values were saved as expected
            bool calibration_success = _verify_calibration(LeverDevice);
            if (calibration_success)
            {
                MessageBox.Show("Calibration successful!", "MotoTrak Calibration", MessageBoxButton.OK);
            }
            else
            {
                MessageBox.Show("Calibration FAILED!", "MotoTrak Calibration", MessageBoxButton.OK);
            }
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
            //Reset the max and min values for the upcoming calibration run
            MaxValue = 0;
            MinValue = 1024;

            while (!_background_thread.CancellationPending)
            {
                var latest_data = DeviceStreamModel.GetInstance().DataBuffer.ToList();
                var max = latest_data.Max();
                var min = latest_data.Min();

                if (max >= MaxValue)
                {
                    MaxValue = Convert.ToInt32(max);
                }

                if (min <= MinValue)
                {
                    MinValue = Convert.ToInt32(min);
                }

                //Report our progress
                _background_thread.ReportProgress(0);

                //Sleep the thread for a bit to not consume too much processor time
                Thread.Sleep(33);
            }
        }

        #endregion
    }
}
