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

        private List<List<int>> _trial_data = new List<List<int>>();

        private DateTime _start_time = DateTime.MinValue;
        private DateTime _end_time = DateTime.MinValue;

        private MotorTrialResult _result = MotorTrialResult.Unknown;
        private ObservableCollection<DateTime> _output_trigger_times = new ObservableCollection<DateTime>();
        
        private double _hit_window_duration_in_seconds = 0;
        private double _pre_trial_sampling_period_in_seconds = 0;
        private double _post_trial_sampling_period_in_seconds = 0;
        private double _post_trial_time_out_in_seconds = 0;
        private double _manipulandum_position = 0;
        private double _trial_initiation_threshold = 0;
        private double _minimum_hit_threshold = 0;
        private double _maximum_hit_threshold = 0;

        private List<int> _hit_indices = new List<int>();
        private List<DateTime> _hit_times = new List<DateTime>();

        private List<double> _variable_parameters = new List<double>();
        
        #endregion

        #region Constructors

        /// <summary>
        /// Construct a new, empty trial.
        /// </summary>
        public MotorTrial()
        {
            OutputTriggers.CollectionChanged += OutputTriggers_CollectionChanged;
        }

        #endregion

        #region Method that listens for changes to the "vns times" collection, and sends notifications up to whoever is listening

        private void OutputTriggers_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged("OutputTriggers");
        }

        #endregion

        #region Properties
        
        /// <summary>
        /// The data for this trial.
        /// Each nested list contains data from a set of datastreams for a single time-point.
        /// The collection of nested lists spans all time-points.
        /// </summary>
        public List<List<int>> TrialData
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
        public ObservableCollection<DateTime> OutputTriggers
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
        /// The initiation threshold for this trial
        /// </summary>
        public double TrialInitiationThreshold
        {
            get
            {
                return _trial_initiation_threshold;
            }

            set
            {
                _trial_initiation_threshold = value;
            }
        }

        /// <summary>
        /// The minimum hit threshold for this trial
        /// </summary>
        public double MinimumHitThreshold
        {
            get
            {
                return _minimum_hit_threshold;
            }

            set
            {
                _minimum_hit_threshold = value;
            }
        }

        /// <summary>
        /// The maximum hit threshold for this trial
        /// </summary>
        public double MaximumHitThreshold
        {
            get
            {
                return _maximum_hit_threshold;
            }

            set
            {
                _maximum_hit_threshold = value;
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
        public List<double> VariableParameters
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
