using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakBase
{
    /// <summary>
    /// This interface defines a set of methods that MotoTrak stages can use to handle incoming streaming data.
    /// </summary>
    public interface IMotorStageImplementation
    {
        /// <summary>
        /// This function takes in new data from the MotoTrak controller board and performs transforms on the data
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        List<List<double>> TransformSignals(List<List<int>> new_data_from_controller, MotorStage stage, MotorDevice device);

        /// <summary>
        /// This function takes the currently buffered signal as a parameter, and checks the signal to see if a trial initiation
        /// has occurred.  
        /// </summary>
        /// <param name="signal">The entire signal that is currently in the buffer.</param>
        /// <param name="stage">The stage that is currently being used.</param>
        /// <returns>An integer representing an index into the signal at which a trial initiation occurred.  Return -1 if no
        /// trial initiation was found.</returns>
        int CheckSignalForTrialInitiation(List<List<double>> signal, int new_datapoint_count, MotorStage stage);

        /// <summary>
        /// This function takes the current signal within a trial as a parameter, and checks to see if the trial has been
        /// successful.  It then returns a tuple with two parameters: a MotorTrialResult object, indicating the current
        /// status of success/failure of the trial, and an integer index, indicating the index into the trial signal at which
        /// a success occurred, if any.  If no success has occurred, the integer will be -1.
        /// </summary>
        /// <param name="trial_signal">The device signal</param>
        /// <param name="stage">The currently selected stage</param>
        /// <returns></returns>
        List<Tuple<MotorTrialEventType, int>> CheckForTrialEvent(List<List<double>> trial_signal, MotorStage stage);

        /// <summary>
        /// Creates a list of actions that MotoTrak should take given the success of a trial.
        /// </summary>
        /// <param name="trial_signal">The device signal</param>
        /// <param name="stage">The currently selected stage</param>
        /// <returns></returns>
        List<MotorTrialAction> ReactToTrialEvents(List<Tuple<MotorTrialEventType, int>> trial_events_list, 
            List<List<double>> trial_signal, MotorStage stage);

        /// <summary>
        /// Creates a list of actions that MotoTrak should take given some event in a trial that is 
        /// unrelated the actual success of the trial.  For example, if you wanted to stimulate at a 
        /// certain point during a trial, unrelated to whether the trial has been successful or not,
        /// that logic would go in this function.
        /// </summary>
        /// <param name="trial_signal">The device signal</param>
        /// <param name="stage">The currently selected stage</param>
        /// <returns></returns>
        List<MotorTrialAction> PerformActionDuringTrial(List<List<double>> trial_signal, MotorStage stage);

        /// <summary>
        /// Allows the creator of this stage to make custom messages that get shown to the user at the 
        /// end of each trial
        /// </summary>
        /// <param name="successful_trial">Whether or not this trial was successful</param>
        /// <param name="trial_signal">The trial's signal</param>
        /// <param name="stage">The motor stage</param>
        /// <returns>A message as a string</returns>
        string CreateEndOfTrialMessage(bool successful_trial, int trial_number, List<List<double>> trial_signal, MotorStage stage);

        /// <summary>
        /// Allows the creator of the stage to adjust the hit threshold at the end of each trial.
        /// </summary>
        /// <param name="all_trials">All trials from the session up until the current point in time</param>
        /// <param name="stage">The stage that is currently being run</param>
        void AdjustDynamicStageParameters(List<MotorTrial> all_trials, List<List<double>> trial_signal, MotorStage stage);
    }
}
