using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakBase
{
    /// <summary>
    /// This class describes a trial within the MotoTrak session, including the current trial that is running.
    /// </summary>
    public class MotorTrial
    {
        #region Constructors

        /// <summary>
        /// Construct a new, empty trial.
        /// </summary>
        public MotorTrial()
        {
            Result = MotorTrialResult.Unknown;
            HitTime = DateTime.MinValue;
            StartTime = DateTime.MinValue;
            VNSTime = new List<DateTime>();
        }

        #endregion

        #region Properties

        /// <summary>
        /// The data for this trial
        /// </summary>
        public List<List<int>> TrialData { get; set; }

        /// <summary>
        /// The result of this trial
        /// </summary>
        public MotorTrialResult Result { get; set; }

        /// <summary>
        /// The time at which a success occurred, if any
        /// </summary>
        public DateTime HitTime { get; set; }

        /// <summary>
        /// The time at which the trial began
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// The time(s) at which stimulation occurred in this trial
        /// </summary>
        public List<DateTime> VNSTime { get; set; }
        
        #endregion
    }
}
