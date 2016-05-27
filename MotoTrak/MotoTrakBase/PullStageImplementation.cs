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
    public class PullStageImplementation : IMotorStageImplementation
    {
        #region Singleton implementation

        private static PullStageImplementation _instance = null;

        /// <summary>
        /// Basic constructor for pull stage implementation.
        /// </summary>
        private PullStageImplementation() { /* empty */ }

        /// <summary>
        /// Gets the one and only instance of this class that is allowed to exist.
        /// </summary>
        /// <returns>Instance of PullStageImplementation class</returns>
        public static PullStageImplementation GetInstance()
        {
            if (_instance == null)
            {
                _instance = new PullStageImplementation();
            }

            return _instance;
        }

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
                //int i = trial_signal.FindIndex(x => (x >= stage.HitThreshold));

                var l = Enumerable.Range(0, trial_signal.Count)
                    .Where(index => trial_signal[index] >= stage.HitThreshold &&
                    (index >= stage.TotalRecordedSamplesBeforeHitWindow) &&
                    (index < (stage.TotalRecordedSamplesBeforeHitWindow + stage.TotalRecordedSamplesDuringHitWindow))).ToList();
                if (l != null && l.Count > 0)
                {
                    result = new Tuple<MotorTrialResult, int>(MotorTrialResult.Hit, l[0]);
                }

                /*if (i >= stage.TotalRecordedSamplesBeforeHitWindow && i < (stage.TotalRecordedSamplesBeforeHitWindow + stage.TotalRecordedSamplesDuringHitWindow))
                {
                    //If the index "i" is within the range of the hit window, then we know the animal was successful.
                    result = new Tuple<MotorTrialResult, int>(MotorTrialResult.Hit, i);
                }*/
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

        public string CreateEndOfTrialMessage(bool successful_trial, int trial_number, List<double> trial_signal, MotorStage stage)
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
