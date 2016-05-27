using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakBase
{
    /// <summary>
    /// This class is meant to define the basic sustained pull task
    /// </summary>
    public class SustainedPullStageImplementation : BasicStageImplementation
    {
        #region Singleton implementation

        private new static SustainedPullStageImplementation _instance = null;

        /// <summary>
        /// Basic constructor for pull stage implementation.
        /// </summary>
        private SustainedPullStageImplementation() { /* empty */ }

        /// <summary>
        /// Gets the one and only instance of this class that is allowed to exist.
        /// </summary>
        /// <returns>Instance of PullStageImplementation class</returns>
        public new static SustainedPullStageImplementation GetInstance()
        {
            if (_instance == null)
            {
                _instance = new SustainedPullStageImplementation();
            }

            return _instance;
        }

        #endregion

        #region Methods implemented for the IMotorStageImplementation interface

        public override Tuple<MotorTrialResult, int> CheckForTrialSuccess(List<double> trial_signal, MotorStage stage)
        {
            //Instantiate a variable that will be used to return a result from this function.
            Tuple<MotorTrialResult, int> result = new Tuple<MotorTrialResult, int>(MotorTrialResult.Unknown, -1);

            if (stage.AdaptiveThresholdType == MotorStageAdaptiveThresholdType.Static)
            {
                //Find the point at which the animal exceeds the hit threshold.  This function returns -1 if nothing is found.
                var l = Enumerable.Range(0, trial_signal.Count)
                    .Where(index => trial_signal[index] >= stage.HitThreshold &&
                    (index >= stage.TotalRecordedSamplesBeforeHitWindow) &&
                    (index < (stage.TotalRecordedSamplesBeforeHitWindow + stage.TotalRecordedSamplesDuringHitWindow))).ToList();
                if (l != null && l.Count > 0)
                {
                    result = new Tuple<MotorTrialResult, int>(MotorTrialResult.Hit, l[0]);
                }
            }

            //Return the result
            return result;
        }

        public override string CreateEndOfTrialMessage(bool successful_trial, int trial_number, List<double> trial_signal, MotorStage stage)
        {
            string msg = string.Empty;

            int hit_win_pk_force = Convert.ToInt32(trial_signal.GetRange(stage.TotalRecordedSamplesBeforeHitWindow, stage.TotalRecordedSamplesDuringHitWindow).Max());

            msg += "Trial " + trial_number.ToString() + " ";

            if (successful_trial)
            {
                msg += "HIT, ";
            }
            else
            {
                msg += "MISS, ";
            }

            msg += "maximal force = " + hit_win_pk_force.ToString() + " grams.";

            return msg;
        }

        #endregion
    }
}
