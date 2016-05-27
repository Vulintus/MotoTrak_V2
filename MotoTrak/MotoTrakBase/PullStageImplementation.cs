using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakBase
{
    /// <summary>
    /// Implements the IMotorStageImplementation interface and handles all logic for controlling the pull task.
    /// This class is a singleton.  All stages that use it should simply ask for the singleton instance.
    /// </summary>
    public class PullStageImplementation : BasicStageImplementation
    {
        #region Constructors
        
        /// <summary>
        /// Basic constructor for pull stage implementation.
        /// </summary>
        public PullStageImplementation() { /* empty */ }
        
        #endregion

        #region Methods implemented for the IMotorStageImplementation interface
        
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

            if (stage.AdaptiveThresholdType == MotorStageAdaptiveThresholdType.Median)
            {
                msg += "(Hit threshold = " + Math.Floor(stage.HitThreshold).ToString() + " grams)";
            }

            return msg;
        }

        #endregion
    }
}
