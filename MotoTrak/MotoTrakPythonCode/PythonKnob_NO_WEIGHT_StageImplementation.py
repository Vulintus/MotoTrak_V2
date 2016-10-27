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
from MotoTrakBase import MotorDeviceType
from MotoTrakBase import MotoTrakAutopositioner
from MotoTrakBase import MotorStageParameter

clr.AddReference('MotoTrakUtilities')
from MotoTrakUtilities import MotorMath

class PythonKnob_NO_WEIGHT_StageImplementation (IMotorStageImplementation):

    #Variables needed to run this task
    Autopositioner_Trial_Interval = 10
    Ending_Value_Of_Last_Trial = 0

    #Declare string parameters for this stage
    RecommendedDevice = MotorDeviceType.Knob
    TaskName = "Knob Task (NO WEIGHT)"
    TaskDescription = "The knob task allows assessment of a rat's ability to reach and then supinate with its wrist."
    Hit_Threshold_Parameter = System.Tuple[System.String, System.String, System.Boolean](MotoTrak_V1_CommonParameters.HitThreshold, "degrees", True)
    Initiation_Threshold_Parameter = System.Tuple[System.String, System.String, System.Boolean](MotoTrak_V1_CommonParameters.InitiationThreshold, "degrees", True)
    
    def TransformSignals(self, new_data_from_controller, stage, device):
        result = List[List[System.Double]]()
        for i in range(0, new_data_from_controller.Count):
            stream_data = new_data_from_controller[i]
            transformed_stream_data = List[System.Double]()
            if (i is 1):
                transformed_stream_data = List[System.Double](stream_data.Select(lambda x: -System.Double(device.Slope * (x - device.Baseline))).ToList())
            else:
                transformed_stream_data = List[System.Double](stream_data.Select(lambda x: System.Double(x)).ToList())
            result.Add(transformed_stream_data)
        return result

    def CheckSignalForTrialInitiation(self, signal, new_datapoint_count, stage):
        #Create the value that will be our return value
        return_value = -1

        #Look to see if the Initiation Threshold key exists
        if stage.StageParameters.ContainsKey(PythonKnob_NO_WEIGHT_StageImplementation.Initiation_Threshold_Parameter.Item1):
            #Get the stage's initiation threshold
            init_thresh = stage.StageParameters[PythonKnob_NO_WEIGHT_StageImplementation.Initiation_Threshold_Parameter.Item1].CurrentValue

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

    def CheckForTrialEvent(self, trial, new_datapoint_count, stage):
        #Instantiate a list of tuples that will hold any events that capture as a result of this function.
        result = List[Tuple[MotorTrialEventType, System.Int32]]()

        #Only proceed if a hit threshold has been defined for this stage
        if stage.StageParameters.ContainsKey(PythonKnob_NO_WEIGHT_StageImplementation.Hit_Threshold_Parameter.Item1):
            #Get the stream data from the device
            stream_data = trial.TrialData[1]
            
            #Check to see if the hit threshold has been exceeded
            current_hit_thresh = stage.StageParameters[PythonKnob_NO_WEIGHT_StageImplementation.Hit_Threshold_Parameter.Item1].CurrentValue

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

    def ReactToTrialEvents(self, trial, stage):
        result = List[MotorTrialAction]()
        trial_events = trial.TrialEvents.Where(lambda x: x.Handled is False)
        for evt in trial_events:
            event_type = evt.EventType
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

    def PerformActionDuringTrial(self, trial, stage):
        result = List[MotorTrialAction]()
        return result

    def CreateEndOfTrialMessage(self, trial_number, trial, stage):
        msg = ""

        #Get the device stream data
        device_stream = trial.TrialData[1]
        try:
            peak_turn_angle = device_stream.GetRange(stage.TotalRecordedSamplesBeforeHitWindow, stage.TotalRecordedSamplesDuringHitWindow).Max()

            msg += "Trial " + str(trial_number) + " "
            if trial.Result == MotorTrialResult.Hit:
                msg += "HIT, "
            else:
                msg += "MISS, "

            msg += "peak turn angle = " + System.Convert.ToInt32(System.Math.Floor(peak_turn_angle)).ToString() + " degrees."

            if stage.StageParameters.ContainsKey(PythonKnob_NO_WEIGHT_StageImplementation.Hit_Threshold_Parameter.Item1):
                if stage.StageParameters[PythonKnob_NO_WEIGHT_StageImplementation.Hit_Threshold_Parameter.Item1].AdaptiveThresholdType is MotorStageAdaptiveThresholdType.Median:
                    current_hit_threshold = stage.StageParameters[PythonKnob_NO_WEIGHT_StageImplementation.Hit_Threshold_Parameter.Item1].CurrentValue
                    msg += "(Hit threshold = " + Math.Floor(current_hit_threshold).ToString() + " degrees)"
            
            return msg
        except ValueError:
            return System.String.Empty;

    def CalculateYValueForSessionOverviewPlot(self, trial, stage):
        #Adjust the hit threshold if necessary
        if stage.StageParameters.ContainsKey(PythonKnob_NO_WEIGHT_StageImplementation.Hit_Threshold_Parameter.Item1):
            #Grab the device signal for this trial
            stream_data = trial.TrialData[1]

            #Find the maximal force of the current trial
            max_force = stream_data.Where(lambda val, index: \
                    (index >= stage.TotalRecordedSamplesBeforeHitWindow) and \
                    (index < (stage.TotalRecordedSamplesBeforeHitWindow + stage.TotalRecordedSamplesDuringHitWindow))).Max()

            return max_force

        return System.Double.NaN

    def AdjustDynamicStageParameters(self, all_trials, current_trial, stage):

        #Set the ending value of the trial (this is used for initiation of the next trial)
        PythonKnob_NO_WEIGHT_StageImplementation.Ending_Value_Of_Last_Trial = current_trial.TrialData[1].LastOrDefault()

        #Adjust the hit threshold
        if stage.StageParameters.ContainsKey(PythonKnob_NO_WEIGHT_StageImplementation.Hit_Threshold_Parameter.Item1):
            #Grab the device signal for this trial
            stream_data = current_trial.TrialData[1]
        
            #Find the maximal force from the current trial
            max_force = stream_data.Where(lambda val, index: \
                (index >= stage.TotalRecordedSamplesBeforeHitWindow) and \
                (index < (stage.TotalRecordedSamplesBeforeHitWindow + stage.TotalRecordedSamplesDuringHitWindow))).Max()

            #Retain the maximal force of the most recent 10 trials
            stage.StageParameters[PythonKnob_NO_WEIGHT_StageImplementation.Hit_Threshold_Parameter.Item1].History.Enqueue(max_force)
            stage.StageParameters[PythonKnob_NO_WEIGHT_StageImplementation.Hit_Threshold_Parameter.Item1].CalculateAndSetBoundedCurrentValue()

        #Adjust the position of the auto-positioner, according to the stage settings
        if stage.Position.ParameterType == MotorStageParameter.StageParameterType.Variable:
            hit_count = all_trials.Select(lambda t: t.Result == MotorTrialResult.Hit).Count()
            hit_count_modulus = hit_count % PythonKnob_NO_WEIGHT_StageImplementation.Autopositioner_Trial_Interval
            if hit_count > 0 and hit_count_modulus is 0:
                stage.Position.CurrentValue = stage.Position.CurrentValue + 0.5
                MotoTrakAutopositioner.GetInstance().SetPosition(stage.Position.CurrentValue)
            
        return

