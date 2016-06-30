using MotoTrakUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakBase
{
    /// <summary>
    /// A basic stage implementation.  This stage implementation is sufficient to run
    /// many basic tasks, including the static pull task.
    /// </summary>
    public class BasicStageImplementation : IMotorStageImplementation
    {
        #region Private data members

        List<double> _peak_force = new List<double>();

        #endregion

        #region Constructors

        /// <summary>
        /// Basic constructor for pull stage implementation.
        /// </summary>
        public BasicStageImplementation() { /* empty */ }

        #endregion

        #region Methods implemented for the IMotorStageImplementation interface

        public virtual int CheckSignalForTrialInitiation(List<double> signal, int new_datapoint_count, MotorStage stage)
        {
            //Create a value that will be our return value
            int return_value = -1;

            //We must have more than 0 new datapoints.  If the code inside the if-statement was run with 0 new datapoints, it would
            //generate an exception.
            if (new_datapoint_count > 0)
            {
                //Look only at the most recent data from the signal
                var signal_to_use = signal.Skip(signal.Count - new_datapoint_count).ToList();

                //Calculate the difference in size between the two
                var difference_in_size = signal.Count - signal_to_use.Count;

                //Retrieve the maximal value from the device signal
                double maximal_value = signal_to_use.Max();

                if (maximal_value >= stage.TrialInitiationThreshold)
                {
                    //If the maximal value in the signal exceeded the trial initiation force threshold
                    //Find the position at which the signal exceeded the threshold, and return it.
                    return_value = signal_to_use.IndexOf(maximal_value) + difference_in_size;
                }
            }

            return return_value;
        }

        public virtual Tuple<MotorTrialResult, int> CheckForTrialSuccess(List<double> trial_signal, MotorStage stage)
        {
            //Instantiate a variable that will be used to return a result from this function.
            Tuple<MotorTrialResult, int> result = new Tuple<MotorTrialResult, int>(MotorTrialResult.Unknown, -1);
            
            //Find the point at which the animal exceeds the hit threshold.  This function returns -1 if nothing is found.
            var l = Enumerable.Range(0, trial_signal.Count)
                .Where(index => trial_signal[index] >= stage.HitThreshold &&
                (index >= stage.TotalRecordedSamplesBeforeHitWindow) &&
                (index < (stage.TotalRecordedSamplesBeforeHitWindow + stage.TotalRecordedSamplesDuringHitWindow))).ToList();
            if (l != null && l.Count > 0)
            {
                result = new Tuple<MotorTrialResult, int>(MotorTrialResult.Hit, l[0]);
            }

            //Return the result
            return result;
        }

        public virtual List<MotorTrialAction> ReactToTrialSuccess(List<double> trial_signal, MotorStage stage)
        {
            List<MotorTrialAction> actions = new List<MotorTrialAction>();
            actions.Add(new MotorTrialAction() { ActionType = MotorTrialActionType.TriggerFeeder });

            if (stage.StimulationType == MotorStageStimulationType.On)
            {
                actions.Add(new MotorTrialAction() { ActionType = MotorTrialActionType.SendStimulationTrigger });
            }

            return actions;
        }

        public virtual List<MotorTrialAction> PerformActionDuringTrial(List<double> trial_signal, MotorStage stage)
        {
            //No actions will be taken during the trial.
            List<MotorTrialAction> actions = new List<MotorTrialAction>();
            return actions;
        }

        public virtual string CreateEndOfTrialMessage(bool successful_trial, int trial_number, List<double> trial_signal, MotorStage stage)
        {
            string msg = string.Empty;
            
            msg += "Trial " + trial_number.ToString() + " ";

            if (successful_trial)
            {
                msg += "HIT, ";
            }
            else
            {
                msg += "MISS, ";
            }
            
            return msg;
        }

        public virtual void AdjustDynamicHitThreshold(List<MotorTrial> all_trials, List<double> trial_signal, MotorStage stage)
        {
            if (stage.AdaptiveThresholdType == MotorStageAdaptiveThresholdType.Median)
            {
                //Find the maximal force of the current trial
                double max_force = trial_signal.Where((val, index) =>
                    (index >= stage.TotalRecordedSamplesBeforeHitWindow) &&
                    (index < (stage.TotalRecordedSamplesBeforeHitWindow + stage.TotalRecordedSamplesDuringHitWindow))).Max();

                //Retain the maximal force of the most recent 10 trials
                _peak_force.Add(max_force);
                if (_peak_force.Count > stage.TrialsToRetainForAdaptiveAdjustments)
                {
                    _peak_force.RemoveAt(0);
                }

                //Adjust the hit threshold
                if (_peak_force.Count == stage.TrialsToRetainForAdaptiveAdjustments)
                {
                    double median = MotorMath.Median(_peak_force);
                    stage.HitThreshold = Math.Max(stage.HitThresholdMinimum, Math.Min(stage.HitThresholdMaximum, median));
                }
            }
        }

        #endregion
    }
}
