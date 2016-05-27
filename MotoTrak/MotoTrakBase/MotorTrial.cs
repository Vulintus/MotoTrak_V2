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

        private List<List<int>> _trialData = new List<List<int>>();
        private MotorTrialResult _result = MotorTrialResult.Unknown;
        private DateTime _hitTime = DateTime.MinValue;
        private DateTime _startTime = DateTime.MinValue;
        private ObservableCollection<DateTime> _vnsTime = new ObservableCollection<DateTime>();
        private int _hitIndex = -1;

        #endregion

        #region Constructors

        /// <summary>
        /// Construct a new, empty trial.
        /// </summary>
        public MotorTrial()
        {
            VNSTime.CollectionChanged += VNSTime_CollectionChanged;
        }

        #endregion

        #region Method that listens for changes to the "vns times" collection, and sends notifications up to whoever is listening

        private void VNSTime_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged("VNSTime");
        }

        #endregion

        #region Properties

        /// <summary>
        /// The data for this trial
        /// </summary>
        public List<List<int>> TrialData
        {
            get
            {
                return _trialData;
            }
            set
            {
                _trialData = value;
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
        /// The time at which a success occurred, if any
        /// </summary>
        public DateTime HitTime
        {
            get
            {
                return _hitTime;
            }
            set
            {
                _hitTime = value;
                NotifyPropertyChanged("HitTime");
            }
        }

        /// <summary>
        /// The time at which the trial began
        /// </summary>
        public DateTime StartTime
        {
            get
            {
                return _startTime;
            }
            set
            {
                _startTime = value;
                NotifyPropertyChanged("StartTime");
            }
        }

        /// <summary>
        /// The time(s) at which stimulation occurred in this trial
        /// </summary>
        public ObservableCollection<DateTime> VNSTime
        {
            get
            {
                return _vnsTime;
            }
            set
            {
                NotifyPropertyChanged("VNSTime");
            }
        }

        /// <summary>
        /// The index into the TrialData array at which the hit occured
        /// </summary>
        public int HitIndex
        {
            get
            {
                return _hitIndex;
            }
            set
            {
                _hitIndex = value;
                NotifyPropertyChanged("HitIndex");
            }
        }
        
        #endregion
    }
}
