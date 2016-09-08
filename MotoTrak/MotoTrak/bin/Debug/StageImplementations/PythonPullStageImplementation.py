import clr
clr.AddReference('System.Core')
from System.Collections.Generic import List
from System import Tuple

import System
clr.ImportExtensions(System.Linq)
from System.Linq import Enumerable

clr.AddReference('MotoTrakBase')
from MotoTrakBase import IMotorStageImplementation
from MotoTrakBase import MotorStage
from MotoTrakBase import MotorTrialResult
from MotoTrakBase import MotorTrialAction
from MotoTrakBase import MotorTrialActionType
from MotoTrakBase import MotorStageStimulationType
from MotoTrakBase import MotorStageAdaptiveThresholdType
from MotoTrakBase import MotoTrak_V1_CommonParameters
from MotoTrakBase import MotorTrialEventType

clr.AddReference('MotoTrakUtilities')
from MotoTrakUtilities import MotorMath

class PythonPullStageImplementation (IMotorStageImplementation):

    #Declare string parameters for this stage
    RecommendedDevice = 0
    TaskName = "Pull Task"
    TaskDescription = "The pull task is a straightforward task in which subjects must pull a handle with a certain amount of force to receive a reward."
    Hit_Threshold_Parameter = System.Tuple[System.String, System.String, System.Boolean](MotoTrak_V1_CommonParameters.HitThreshold, "grams", True)
    Initiation_Threshold_Parameter = System.Tuple[System.String, System.String, System.Boolean](MotoTrak_V1_CommonParameters.InitiationThreshold, "grams", True)
    
    def TransformSignals(self, new_data_from_controller, stage, device):
        result = List[List[System.Double]]()
        for i in range(0, new_data_from_controller.Count):
            stream_data = new_data_from_controller[i]
            transformed_stream_data = List[System.Double]()
            if (i is 1):
                transformed_stream_data = List[System.Double](stream_data.Select(lambda x: System.Double(device.Slope * (x - device.Baseline))).ToList())
            else:
                transformed_stream_data = List[System.Double](stream_data.Select(lambda x: System.Double(x)).ToList())
            result.Add(transformed_stream_data)
        return result

    def CheckSignalForTrialInitiation(self, signal, new_datapoint_count, stage):
        #Create the value that will be our return value
        return_value = -1

        #Look to see if the Initiation Threshold key exists
        if stage.StageParameters.ContainsKey(PythonPullStageImplementation.Initiation_Threshold_Parameter.Item1):
            #Get the stage's initiation threshold
            init_thresh = stage.StageParameters[PythonPullStageImplementation.Initiation_Threshold_Parameter.Item1].CurrentValue

            #Get the data stream itself
            stream_data = signal[1]

            #Check to make sure we actually have new data to work with before going on
            if new_datapoint_count > 0 and new_datapoint_count <= stream_data.Count:
                #Look only at the most recent data from the signal
                stream_data_to_use = stream_data.Skip(stream_data.Count - new_datapoint_count).ToList()

                #Calculate how many OLD elements there are
                difference_in_size = stream_data.Count - stream_data_to_use.Count

                #Retrieve the maximal value for the signal
                maximal_value = stream_data_to_use.Max()

                if maximal_value >= init_thresh:
                    return_value = stream_data_to_use.IndexOf(maximal_value) + difference_in_size
                
        return return_value

    def CheckForTrialEvent(self, trial_signal, stage):
        #Instantiate a list of tuples that will hold any events that capture as a result of this function.
        result = List[Tuple[MotorTrialEventType, System.Int32]]()

        #Only proceed if a hit threshold has been defined for this stage
        if stage.StageParameters.ContainsKey(PythonPullStageImplementation.Hit_Threshold_Parameter.Item1):
            #Get the stream data from the device
            stream_data = trial_signal[1]
            
            #Check to see if the hit threshold has been exceeded
            current_hit_thresh = stage.StageParameters[PythonPullStageImplementation.Hit_Threshold_Parameter.Item1].CurrentValue

            #Check to see if the stream data has exceeded the current hit threshold
            try:
                indices_of_hits = Enumerable.Range(0, stream_data.Count) \
                    .Where(lambda index: stream_data[index] >= current_hit_thresh and \
                    (index >= stage.TotalRecordedSamplesBeforeHitWindow) and \
                    (index < (stage.TotalRecordedSamplesBeforeHitWindow + stage.TotalRecordedSamplesDuringHitWindow))).ToList()

                if indices_of_hits is not None and indices_of_hits.Count > 0:
                    result.Add(Tuple[MotorTrialEventType, int](MotorTrialEventType.SuccessfulTrial, indices_of_hits[0]))
            except ValueError:
                pass

        #Return the result
        return result

    def ReactToTrialEvents(self, trial_events_list, trial_signal, stage):
        result = List[MotorTrialAction]()
        for event_tuple in trial_events_list:
            event_type = event_tuple.Item1
            if event_type is MotorTrialEventType.SuccessfulTrial:
                #If a successful trial happened, then feed the animal
                new_action = MotorTrialAction()
                new_action.ActionType = MotorTrialActionType.TriggerFeeder
                result.Add(new_action)

                #If stimulation is on for this stage, stimulate the animal
                if stage.OutputTriggerType is MotorStageStimulationType.All:
                    new_stim_action = MotorTrialAction()
                    new_stim_action.ActionType = MotorTrialActionType.SendStimulationTrigger
                    result.Add(new_stim_action)

        return result

    def PerformActionDuringTrial(self, trial_signal, stage):
        result = List[MotorTrialAction]()
        return result

    def CreateEndOfTrialMessage(self, successful_trial, trial_number, trial_signal, stage):
        msg = ""

        #Get the device stream data
        device_stream = trial_signal[1]
        try:
            peak_force = device_stream.GetRange(stage.TotalRecordedSamplesBeforeHitWindow, stage.TotalRecordedSamplesDuringHitWindow).Max()

            msg += "Trial " + str(trial_number) + " "
            if successful_trial:
                msg += "HIT, "
            else:
                msg += "MISS, "

            msg += "maximal force = " + System.Convert.ToInt32(System.Math.Floor(peak_force)).ToString() + " grams."

            if stage.StageParameters.ContainsKey(PythonPullStageImplementation.Hit_Threshold_Parameter.Item1):
                if stage.StageParameters[PythonPullStageImplementation.Hit_Threshold_Parameter.Item1].AdaptiveThresholdType is MotorStageAdaptiveThresholdType.Median:
                    current_hit_threshold = stage.StageParameters[PythonPullStageImplementation.Hit_Threshold_Parameter.Item1].CurrentValue
                    msg += "(Hit threshold = " + Math.Floor(current_hit_threshold).ToString() + " grams)"
            
            return msg
        except ValueError:
            return System.String.Empty;

    def CalculateYValueForSessionOverviewPlot(self, trial_signal, stage):
        #Adjust the hit threshold if necessary
        if stage.StageParameters.ContainsKey(PythonPullStageImplementation.Hit_Threshold_Parameter.Item1):
            #Grab the device signal for this trial
            stream_data = trial_signal[1]

            #Find the maximal force of the current trial
            max_force = stream_data.Where(lambda val, index: \
                    (index >= stage.TotalRecordedSamplesBeforeHitWindow) and \
                    (index < (stage.TotalRecordedSamplesBeforeHitWindow + stage.TotalRecordedSamplesDuringHitWindow))).Max()

            return max_force

        return System.Double.NaN

    def AdjustDynamicStageParameters(self, all_trials, trial_signal, stage):
        #Adjust the hit threshold if necessary
        if stage.StageParameters.ContainsKey(PythonPullStageImplementation.Hit_Threshold_Parameter.Item1):
            #Grab the device signal for this trial
            stream_data = trial_signal[1]
        
            #Find the maximal force from the current trial
            max_force = stream_data.Where(lambda val, index: \
                (index >= stage.TotalRecordedSamplesBeforeHitWindow) and \
                (index < (stage.TotalRecordedSamplesBeforeHitWindow + stage.TotalRecordedSamplesDuringHitWindow))).Max()

            #Retain the maximal force of the most recent 10 trials
            stage.StageParameters[PythonPullStageImplementation.Hit_Threshold_Parameter.Item1].History.Enqueue(max_force);
            stage.StageParameters[PythonPullStageImplementation.Hit_Threshold_Parameter.Item1].CalculateAndSetBoundedCurrentValue();
            
        return

