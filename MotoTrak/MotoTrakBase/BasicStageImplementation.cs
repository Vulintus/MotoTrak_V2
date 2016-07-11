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

        public virtual List<List<double>> TransformSignals(List<List<int>> new_data_from_controller, MotorStage stage, MotorDevice device)
        {
            //Create a 2d list for our result that we will return
            List<List<double>> result = new List<List<double>>();
            
            //Iterate over each set of streaming parameters that have been defined
            foreach (var sp in stage.StreamParameters)
            {
                if (sp.StreamIndex >= 0)
                {
                    //Grab the associated stream
                    var stream_data = new_data_from_controller[sp.StreamIndex];
                    List<double> fp_stream_data = new List<double>();

                    switch (sp.StreamType)
                    {
                        case MotorBoardDataStreamType.DeviceValue:
                            //Transform the device data if this is the device stream
                            fp_stream_data = stream_data.Select(x => (double)(device.Slope * (x - device.Baseline))).ToList();
                            break;
                        default:
                            //Convert all values to floating point values
                            fp_stream_data = stream_data.Select(x => (double)x).ToList();
                            break;
                    }
                    
                    //Add this stream to our result
                    result.Add(fp_stream_data);
                }
            }
            
            //Return the result
            return result;
        }

        public virtual int CheckSignalForTrialInitiation(List<List<double>> signal, int new_datapoint_count, MotorStage stage)
        {
            //Create a value that will be our return value
            int return_value = -1;

            //Iterate over each set of streaming parameters that have been defined
            foreach (var sp in stage.StreamParameters)
            {
                //If initiation threshold parameters have been set for this stream of data
                if (sp.InitiationThreshold != null && sp.StreamIndex >= 0)
                {
                    //Get the stream we will be working with
                    var stream_data = signal[sp.StreamIndex];
                    
                    //Check to make sure we actually have new data to consider before going on
                    if (new_datapoint_count > 0 && new_datapoint_count <= stream_data.Count)
                    {
                        //Look only at the most recent data from the signal
                        var stream_data_to_use = stream_data.Skip(signal.Count - new_datapoint_count).ToList();

                        //Calculate how many OLD elements there are
                        var difference_in_size = stream_data.Count - stream_data_to_use.Count;

                        //Retrieve the maximal value for the signal
                        double maximal_value = stream_data_to_use.Max();
                        
                        //Check to see if the maximal value exceeds the initiation threshold
                        if (maximal_value >= sp.InitiationThreshold.CurrentValue)
                        {
                            //Set the return value equal to the index at which we found the value that exceeds the initiation threshold
                            return_value = stream_data_to_use.IndexOf(maximal_value) + difference_in_size;
                        }
                    }
                }
            }

            return return_value;
        }

        public virtual List<Tuple<MotorTrialEventType, int>> CheckForTrialEvent(List<List<double>> trial_signal, MotorStage stage)
        {
            //Instantiate a list of tuples that will hold any events that capture as a result of this function.
            List<Tuple<MotorTrialEventType, int>> result = new List<Tuple<MotorTrialEventType, int>>();
            
            //Iterate over all streaming parameters for this stage
            foreach (var sp in stage.StreamParameters)
            {
                //If there is a hit threshold defined for this stream
                if (sp.HitThreshold != null && sp.HitThresholdType != MotorStageHitThresholdType.Undefined && sp.StreamIndex >= 0)
                {
                    //Get the associated stream
                    var stream_data = trial_signal[sp.StreamIndex];

                    //Get the current value of the hit threshold for this stream
                    var current_hit_threshold = sp.HitThreshold.CurrentValue;

                    //Check to see if the stream data has exceeded the current hit threshold
                    var l = Enumerable.Range(0, trial_signal.Count)
                        .Where(index => stream_data[index] >= current_hit_threshold &&
                        (index >= stage.TotalRecordedSamplesBeforeHitWindow) &&
                        (index < (stage.TotalRecordedSamplesBeforeHitWindow + stage.TotalRecordedSamplesDuringHitWindow))).ToList();
                    if (l != null && l.Count > 0)
                    {
                        result.Add(new Tuple<MotorTrialEventType, int>(MotorTrialEventType.SuccessfulTrial, l[0]));
                    }
                }
            }
            
            //Return the result
            return result;
        }

        public virtual List<MotorTrialAction> ReactToTrialEvents(List<Tuple<MotorTrialEventType, int>> trial_events_list, 
            List<List<double>> trial_signal, MotorStage stage)
        {
            //Create an empty list of actions that we will return to the caller
            List<MotorTrialAction> actions = new List<MotorTrialAction>();

            //Iterate through each event that occurred
            foreach (var event_tuple in trial_events_list)
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

        public virtual void AdjustDynamicStageParameters(List<MotorTrial> all_trials, List<List<double>> trial_signal, MotorStage stage)
        {
            //We must now adjust adaptive thresholds for each stream we are monitoring
            foreach (var sp in stage.StreamParameters)
            {
                if (sp.StreamIndex >= 0)
                {
                    //Grab the signal data for this stream
                    var stream_data = trial_signal[sp.StreamIndex];

                    //If this stream has a hit threshold
                    if (sp.HitThreshold != null)
                    {
                        //Find the maximal force of the current trial
                        double max_force = stream_data.Where((val, index) =>
                            (index >= stage.TotalRecordedSamplesBeforeHitWindow) &&
                            (index < (stage.TotalRecordedSamplesBeforeHitWindow + stage.TotalRecordedSamplesDuringHitWindow))).Max();

                        //Retain the maximal force of the most recent 10 trials
                        sp.HitThreshold.History.Enqueue(max_force);
                        sp.HitThreshold.CalculateAndSetBoundedCurrentValue();
                    }
                }
            }
        }

        #endregion
    }
}
