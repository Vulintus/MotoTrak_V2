using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakBase
{
    /// <summary>
    /// Implements the IMotorStageImplementation interface and handles all logic for controlling the pull task.
    /// </summary>
    public class PullStageImplementation : IMotorStageImplementation
    {
        #region Constructors

        /// <summary>
        /// Basic constructor for pull stage implementation.
        /// </summary>
        PullStageImplementation() { /* empty */ }

        #endregion

        #region Methods implemented for the IMotorStageImplementation interface

        public int CheckSignalForTrialInitiation(List<double> signal, MotorStage stage)
        {
            //Create a value that will be our return value
            int return_value = -1;

            //Retrieve the maximal value from the pull signal
            double maximal_value = signal.Max();

            if (maximal_value >= stage.TrialInitiationThreshold)
            {
                //If the maximal value in the signal exceeded the trial initiation force threshold
                //Find the position at which the signal exceeded the threshold, and return it.
                return_value = signal.IndexOf(maximal_value);
            }

            return return_value;
        }

        public Tuple<MotorTrialResult, int> CheckForTrialSuccess(List<double> trial_signal, MotorStage stage)
        {
            //Instantiate a variable that will be used to return a result from this function.
            Tuple<MotorTrialResult, int> result = new Tuple<MotorTrialResult, int>(MotorTrialResult.Unknown, -1);

            if (stage.AdaptiveThresholdType == MotorStageAdaptiveThresholdType.Static)
            {
                //Find the point at which the animal exceeds the hit threshold.  This function returns -1 if nothing is found.
                int i = trial_signal.FindIndex(x => (x >= stage.HitThreshold));

                if (i >= stage.TotalRecordedSamplesBeforeHitWindow && i < (stage.TotalRecordedSamplesBeforeHitWindow + stage.TotalRecordedSamplesDuringHitWindow))
                {
                    //If the index "i" is within the range of the hit window, then we know the animal was successful.
                    result = new Tuple<MotorTrialResult, int>(MotorTrialResult.Hit, i);
                }
            }

            //Return the result
            return result;
        }

        public List<MotorTrialAction> ReactToTrialSuccess(List<double> trial_signal, MotorStage stage)
        {
            List<MotorTrialAction> actions = new List<MotorTrialAction>();
            actions.Add(MotorTrialAction.TriggerFeeder);

            if (stage.StimulationType == MotorStageStimulationType.On)
            {
                actions.Add(MotorTrialAction.SendStimulationTrigger);
            }

            return actions;
        }

        public List<MotorTrialAction> PerformActionDuringTrial(List<double> trial_signal, MotorStage stage)
        {
            List<MotorTrialAction> actions = new List<MotorTrialAction>();

            //no actions will be taken for this stage type (so far).

            return actions;
        }

        #endregion
    }
}
