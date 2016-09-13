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
        #region Constructors

        /// <summary>
        /// Basic constructor for pull stage implementation.
        /// </summary>
        public BasicStageImplementation() { /* empty */ }

        #endregion

        #region Methods implemented for the IMotorStageImplementation interface

        public virtual List<List<double>> TransformSignals(List<List<int>> new_data_from_controller, MotorStage stage, MotorDevice device)
        {
            //Create a 2d list for our result that we will return
            List<List<double>> result = new List<List<double>>();
            
            //Iterate over each stream
            for (int i = 0; i < new_data_from_controller.Count; i++)
            {
                var stream_data = new_data_from_controller[i];
                List<double> fp_stream_data = new List<double>();

                //Transform the data from each stream into the necessary format
                if (i == 1)
                {
                    //If this is the device stream
                    fp_stream_data = stream_data.Select(x => (double)(device.Slope * (x - device.Baseline))).ToList();
                }
                else
                {
                    //If this is not the device stream, just convert the data to a floating-point type.
                    fp_stream_data = stream_data.Select(x => (double)x).ToList();
                }

                //Add the transformed stream data to the result to be returned to the caller
                result.Add(fp_stream_data);
            }
            
            //Return the result
            return result;
        }

        public virtual int CheckSignalForTrialInitiation(List<List<double>> signal, int new_datapoint_count, MotorStage stage)
        {
            //Create a value that will be our return value
            int return_value = -1;

            //Only proceed if an initiation threshold has been defined for this stage
            if (stage.StageParameters.ContainsKey("Initiation Threshold"))
            {
                //Get the stage's initiation threshold
                double init_thresh = stage.StageParameters["Initiation Threshold"].CurrentValue;

                //Get the stream data from the device
                var stream_data = signal[1];

                //Check to make sure we actually have new data to work with before going on
                if (new_datapoint_count > 0 && new_datapoint_count <= stream_data.Count)
                {
                    //Look only at the most recent data from the signal
                    var stream_data_to_use = stream_data.Skip(stream_data.Count - new_datapoint_count).ToList();

                    //Calculate how many OLD elements there are
                    var difference_in_size = stream_data.Count - stream_data_to_use.Count;

                    //Retrieve the maximal value for the signal
                    double maximal_value = stream_data_to_use.Max();

                    //Check to see if the maximal value exceeds the initiation threshold
                    if (maximal_value >= init_thresh)
                    {
                        //Set the return value equal to the index at which we found the value that exceeds the initiation threshold
                        return_value = stream_data_to_use.IndexOf(maximal_value) + difference_in_size;
                    }
                }
            }
            
            return return_value;
        }

        public virtual List<Tuple<MotorTrialEventType, int>> CheckForTrialEvent(List<List<double>> trial_signal, MotorStage stage)
        {
            //Instantiate a list of tuples that will hold any events that capture as a result of this function.
            List<Tuple<MotorTrialEventType, int>> result = new List<Tuple<MotorTrialEventType, int>>();
            
            //Only proceed if a hit threshold has been defined for this stage
            if (stage.StageParameters.ContainsKey("Hit Threshold"))
            {
                //Get the stream data from the device
                var stream_data = trial_signal[1];

                //Check to see if the hit threshold has been exceeded
                var current_hit_thresh = stage.StageParameters["Hit Threshold"].CurrentValue;

                //Check to see if the stream data has exceeded the current hit threshold
                try
                {
                    var indices_of_hits = Enumerable.Range(0, stream_data.Count)
                    .Where(index => stream_data[index] >= current_hit_thresh &&
                    (index >= stage.TotalRecordedSamplesBeforeHitWindow) &&
                    (index < (stage.TotalRecordedSamplesBeforeHitWindow + stage.TotalRecordedSamplesDuringHitWindow))).ToList();

                    if (indices_of_hits != null && indices_of_hits.Count > 0)
                    {
                        result.Add(new Tuple<MotorTrialEventType, int>(MotorTrialEventType.SuccessfulTrial, indices_of_hits[0]));
                    }
                }
                catch
                {
                    //nothing here
                }
            }
            
            //Return the result
            return result;
        }

        public virtual List<MotorTrialAction> ReactToTrialEvents(List<Tuple<MotorTrialEventType, int>> new_events,
            List<Tuple<MotorTrialEventType, int>> all_events,
            List<List<double>> trial_signal, MotorStage stage)
        {
            //Create an empty list of actions that we will return to the caller
            List<MotorTrialAction> actions = new List<MotorTrialAction>();

            //Iterate through each event that occurred
            foreach (var event_tuple in new_events)
            {
                var event_type = event_tuple.Item1;

                switch(event_type)
                {
                    case MotorTrialEventType.SuccessfulTrial:

                        //If a successful trial happened, then feed the animal
                        actions.Add(new MotorTrialAction() { ActionType = MotorTrialActionType.TriggerFeeder });

                        //If stimulation is on for this stage, stimulate the animal
                        if (stage.OutputTriggerType == MotorStageStimulationType.All)
                        {
                            actions.Add(new MotorTrialAction() { ActionType = MotorTrialActionType.SendStimulationTrigger });
                        }

                        break;
                }
            }
            
            return actions;
        }

        public virtual List<MotorTrialAction> PerformActionDuringTrial(List<List<double>> trial_signal, MotorStage stage)
        {
            //No actions will be taken during the trial.
            List<MotorTrialAction> actions = new List<MotorTrialAction>();
            return actions;
        }

        public virtual string CreateEndOfTrialMessage(bool successful_trial, int trial_number, List<List<double>> trial_signal, MotorStage stage)
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

        public virtual double CalculateYValueForSessionOverviewPlot(List<List<double>> trial_signal, MotorStage stage)
        {
            //Adjust the hit threshold if necessary
            if (stage.StageParameters.ContainsKey("Hit Threshold"))
            {
                //Grab the device signal for this trial
                var stream_data = trial_signal[1];

                //Find the maximal force of the current trial
                double max_force = stream_data.Where((val, index) =>
                    (index >= stage.TotalRecordedSamplesBeforeHitWindow) &&
                    (index < (stage.TotalRecordedSamplesBeforeHitWindow + stage.TotalRecordedSamplesDuringHitWindow))).Max();

                return max_force;
            }

            return double.NaN;
        }

        public virtual void AdjustDynamicStageParameters(List<MotorTrial> all_trials, List<List<double>> trial_signal, MotorStage stage)
        {
            //Adjust the hit threshold if necessary
            if (stage.StageParameters.ContainsKey("Hit Threshold"))
            {
                //Grab the device signal for this trial
                var stream_data = trial_signal[1];

                //Find the maximal force of the current trial
                double max_force = stream_data.Where((val, index) =>
                    (index >= stage.TotalRecordedSamplesBeforeHitWindow) &&
                    (index < (stage.TotalRecordedSamplesBeforeHitWindow + stage.TotalRecordedSamplesDuringHitWindow))).Max();

                //Retain the maximal force of the most recent 10 trials
                stage.StageParameters["Hit Threshold"].History.Enqueue(max_force);
                stage.StageParameters["Hit Threshold"].CalculateAndSetBoundedCurrentValue();
            }            
        }

        #endregion
    }
}
