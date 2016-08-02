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
        
        public override string CreateEndOfTrialMessage(bool successful_trial, int trial_number, List<List<double>> trial_signal, MotorStage stage)
        {
            string msg = string.Empty;

            //Get the device stream
            var device_stream = trial_signal[1];

            //Get the peak force within the hit window
            try
            {
                double peak_force = device_stream.GetRange(stage.TotalRecordedSamplesBeforeHitWindow,
                stage.TotalRecordedSamplesDuringHitWindow).Max();

                msg += "Trial " + trial_number.ToString() + " ";

                if (successful_trial)
                {
                    msg += "HIT, ";
                }
                else
                {
                    msg += "MISS, ";
                }

                msg += "maximal force = " + Convert.ToInt32(Math.Floor(peak_force)).ToString() + " grams.";

                if (stage.StageParameters.ContainsKey("Hit Threshold"))
                {
                    if (stage.StageParameters["Hit Threshold"].AdaptiveThresholdType == MotorStageAdaptiveThresholdType.Median)
                    {
                        double current_hit_threshold = stage.StageParameters["Hit Threshold"].CurrentValue;
                        msg += "(Hit threshold = " + Math.Floor(current_hit_threshold).ToString() + " grams)";
                    }
                }
                
                return msg;
            }
            catch
            {
                return base.CreateEndOfTrialMessage(successful_trial, trial_number, trial_signal, stage);
            } 
        }

        #endregion
    }
}
