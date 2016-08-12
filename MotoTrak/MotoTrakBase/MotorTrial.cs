using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakBase
{
    /// <summary>
    /// This class describes a trial within the MotoTrak session, including the current trial that is running.
    /// </summary>
    public class MotorTrial : NotifyPropertyChangedObject
    {
        #region Private data members

        private List<List<double>> _trial_data = new List<List<double>>();

        private DateTime _start_time = DateTime.MinValue;
        private DateTime _end_time = DateTime.MinValue;

        private MotorTrialResult _result = MotorTrialResult.Unknown;
        
        private double _hit_window_duration_in_seconds = 0;
        private double _pre_trial_sampling_period_in_seconds = 0;
        private double _post_trial_sampling_period_in_seconds = 0;
        private double _post_trial_time_out_in_seconds = 0;
        private double _manipulandum_position = 0;
        
        private List<int> _hit_indices = new List<int>();
        private List<DateTime> _hit_times = new List<DateTime>();
        private List<DateTime> _output_trigger_times = new List<DateTime>();

        private Dictionary<string, double> _variable_parameters = new Dictionary<string, double>();
        
        #endregion

        #region Constructors

        /// <summary>
        /// Construct a new, empty trial.
        /// </summary>
        public MotorTrial()
        {
            //empty
        }

        #endregion

        #region Properties
        
        /// <summary>
        /// The data for this trial.  The order of the list follows.
        /// Where we have N = 3 streams, called "a", "b", and "c", the List should be:
        /// [ [a1, a2, a3, ..., a_n], [b1 ... b_n], [c1 ... c_n] ]
        /// Therefore, each sub-list is a "stream" of data.
        /// </summary>
        public List<List<double>> TrialData
        {
            get
            {
                return _trial_data;
            }
            set
            {
                _trial_data = value;
                NotifyPropertyChanged("TrialData");
            }
        }

        /// <summary>
        /// The result of this trial
        /// </summary>
        public MotorTrialResult Result
        {
            get
            {
                return _result;
            }
            set
            {
                _result = value;
                NotifyPropertyChanged("Result");
            }
        }
        
        /// <summary>
        /// The time at which the trial began
        /// </summary>
        public DateTime StartTime
        {
            get
            {
                return _start_time;
            }
            set
            {
                _start_time = value;
                NotifyPropertyChanged("StartTime");
            }
        }

        /// <summary>
        /// The time(s) at which stimulation occurred in this trial
        /// </summary>
        public List<DateTime> OutputTriggers
        {
            get
            {
                return _output_trigger_times;
            }
            set
            {
                _output_trigger_times = value;
                NotifyPropertyChanged("OutputTriggers");
            }
        }
        
        /// <summary>
        /// The duration of the hit window for this trial
        /// </summary>
        public double HitWindowDurationInSeconds
        {
            get
            {
                return _hit_window_duration_in_seconds;
            }

            set
            {
                _hit_window_duration_in_seconds = value;
            }
        }

        /// <summary>
        /// The duration of the pre-trial sampling period for this trial
        /// </summary>
        public double PreTrialSamplingPeriodInSeconds
        {
            get
            {
                return _pre_trial_sampling_period_in_seconds;
            }

            set
            {
                _pre_trial_sampling_period_in_seconds = value;
            }
        }

        /// <summary>
        /// The duration of the post-trial sampling period for this trial
        /// </summary>
        public double PostTrialSamplingPeriodInSeconds
        {
            get
            {
                return _post_trial_sampling_period_in_seconds;
            }

            set
            {
                _post_trial_sampling_period_in_seconds = value;
            }
        }

        /// <summary>
        /// The duration of the post-trial time-out for this trial
        /// </summary>
        public double PostTrialTimeOutInSeconds
        {
            get
            {
                return _post_trial_time_out_in_seconds;
            }

            set
            {
                _post_trial_time_out_in_seconds = value;
            }
        }

        /// <summary>
        /// The position of the device for this trial
        /// </summary>
        public double DevicePosition
        {
            get
            {
                return _manipulandum_position;
            }

            set
            {
                _manipulandum_position = value;
            }
        }
        
        /// <summary>
        /// The indices into the trial data of where the hits occurred.
        /// </summary>
        public List<int> HitIndices
        {
            get
            {
                return _hit_indices;
            }

            set
            {
                _hit_indices = value;
            }
        }

        /// <summary>
        /// The timestamps at which the hits occurred during this trial
        /// </summary>
        public List<DateTime> HitTimes
        {
            get
            {
                return _hit_times;
            }

            set
            {
                _hit_times = value;
            }
        }

        /// <summary>
        /// Values of variable parameters that are defined by the user for this trial.
        /// </summary>
        public Dictionary<string, double> VariableParameters
        {
            get
            {
                return _variable_parameters;
            }

            set
            {
                _variable_parameters = value;
            }
        }

        #endregion
    }
}
