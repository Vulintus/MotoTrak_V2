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
from MotoTrakBase import MotorTaskDefinition
from MotoTrakBase import MotorTaskParameter
from MotoTrakBase import MotoTrakSession

clr.AddReference('MotoTrakUtilities')
from MotoTrakUtilities import MotorMath

class PythonPullStageImplementation_TXBDC_PostShaping (IMotorStageImplementation):

    #Variables used by this task
    Autopositioner_Trial_Interval = 5
    Autopositioner_Trial_Count_Handled = []
    Maximal_Force_List = []
    Force_Threshold_List = []
    LastTrialInitiatedTimestamp = System.DateTime.MinValue
    HasTrialBeenInitiated = False    

    #Declare string parameters for this stage
    TaskDefinition = MotorTaskDefinition()

    def __init__(self):

        PythonPullStageImplementation_TXBDC_PostShaping.TaskDefinition.TaskName = "Pull Task TXBDC"
        PythonPullStageImplementation_TXBDC_PostShaping.TaskDefinition.TaskDescription = "The pull task is a straightforward task in which subjects must pull a handle to receive a reward."
        PythonPullStageImplementation_TXBDC_PostShaping.TaskDefinition.RequiredDeviceType = MotorDeviceType.Pull
        PythonPullStageImplementation_TXBDC_PostShaping.TaskDefinition.OutputTriggerOptions = List[System.String](["Off", "On", "Beginning of every trial"])

        PythonPullStageImplementation_TXBDC_PostShaping.TaskDefinition.DevicePosition.IsAdaptive = True
        PythonPullStageImplementation_TXBDC_PostShaping.TaskDefinition.DevicePosition.IsAdaptabilityCustomizeable = False

        hit_threshold_parameter = MotorTaskParameter(MotoTrak_V1_CommonParameters.HitThreshold, "grams", True, True, True)
        initiation_threshold_parameter = MotorTaskParameter(MotoTrak_V1_CommonParameters.InitiationThreshold, "grams", True, True, True)
        
        PythonPullStageImplementation_TXBDC_PostShaping.TaskDefinition.TaskParameters.Add(hit_threshold_parameter)
        PythonPullStageImplementation_TXBDC_PostShaping.TaskDefinition.TaskParameters.Add(initiation_threshold_parameter)

        return

    def AdjustBeginningStageParameters(self, recent_behavior_sessions, current_session_stage):

        PythonPullStageImplementation_TXBDC_PostShaping.Maximal_Force_List = []
        PythonPullStageImplementation_TXBDC_PostShaping.Force_Threshold_List = []
        PythonPullStageImplementation_TXBDC_PostShaping.Autopositioner_Trial_Count_Handled = []
        PythonPullStageImplementation_TXBDC_PostShaping.LastTrialInitiatedTimestamp = System.DateTime.MinValue
        PythonPullStageImplementation_TXBDC_PostShaping.HasTrialBeenInitiated = False

        #Take only recent behavior sessions that have at least 50 successful trials
        final_position = 0.0
        for i in recent_behavior_sessions:
            final_trial = i.Trials.LastOrDefault()
            if final_trial is not None:
                final_session_position = final_trial.DevicePosition
                if final_session_position > final_position:
                    final_position = final_session_position

        final_position = final_position - 0.5
        if final_position < 0.0:
            final_position = 0.0
                
        #Now, based off the total number of hits that have occurred in previous sessions, set the position of the autopositioner
        position = final_position
        
        #Set the position of the autopositioner if it is supposed to be adaptively set
        if current_session_stage.Position.ParameterType == MotorStageParameter.StageParameterType.Variable:
            current_session_stage.Position.CurrentValue = position
            MotoTrakAutopositioner.GetInstance().SetPosition(position)

        return

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
        #For stages that move the handle back in after a period of time, let's check to see how much time it has been
        #since the last trial initiation
        if PythonPullStageImplementation_TXBDC_PostShaping.HasTrialBeenInitiated is True:
            current_time = System.DateTime.Now
            time_since_last_trial = current_time - PythonPullStageImplementation_TXBDC_PostShaping.LastTrialInitiatedTimestamp
            if time_since_last_trial.TotalMinutes >= 10:
                if stage.Position.ParameterType == MotorStageParameter.StageParameterType.Variable:
                    PythonPullStageImplementation_TXBDC_PostShaping.LastTrialInitiatedTimestamp = System.DateTime.Now
                    if stage.Position.CurrentValue > 0.0:
                        stage.Position.CurrentValue = stage.Position.CurrentValue - 0.5
                        MotoTrakAutopositioner.GetInstance().SetPosition(stage.Position.CurrentValue)

        #Create the value that will be our return value
        return_value = -1

        #Get the name of the initiation threshold parameter
        initiation_threshold_parameter_name = PythonPullStageImplementation_TXBDC_PostShaping.TaskDefinition.TaskParameters[1].ParameterName

        #Look to see if the Initiation Threshold key exists
        if stage.StageParameters.ContainsKey(initiation_threshold_parameter_name):
            #Get the stage's initiation threshold
            init_thresh = stage.StageParameters[initiation_threshold_parameter_name].CurrentValue

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
                    PythonPullStageImplementation_TXBDC_PostShaping.LastTrialInitiatedTimestamp = System.DateTime.Now
                    PythonPullStageImplementation_TXBDC_PostShaping.HasTrialBeenInitiated = True
                    return_value = stream_data_to_use.IndexOf(maximal_value) + difference_in_size
                
        return return_value

    def CheckForTrialEvent(self, trial, new_datapoint_count, stage):
        #Instantiate a list of tuples that will hold any events that capture as a result of this function.
        result = List[Tuple[MotorTrialEventType, System.Int32]]()

        #Get the name of the hit threshold parameter
        hit_threshold_parameter_name = PythonPullStageImplementation_TXBDC_PostShaping.TaskDefinition.TaskParameters[0].ParameterName

        #Only proceed if a hit threshold has been defined for this stage
        if stage.StageParameters.ContainsKey(hit_threshold_parameter_name):
            #Get the stream data from the device
            stream_data = trial.TrialData[1]
            
            #Check to see if the hit threshold has been exceeded
            current_hit_thresh = stage.StageParameters[hit_threshold_parameter_name].CurrentValue

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
            evt.Handled = True

            #If a trial has been initiated, and the output trigger type is set to trigger on every trial initiation,
            #then send an output trigger
            if event_type.value__ is MotorTrialEventType.TrialInitiation.value__:
                output_trigger_type = str(stage.OutputTriggerType)
                if output_trigger_type.lower() == "Beginning of every trial".lower():
                    new_stim_action = MotorTrialAction()
                    new_stim_action.ActionType = MotorTrialActionType.SendStimulationTrigger
                    result.Add(new_stim_action)

            if event_type.value__ is MotorTrialEventType.SuccessfulTrial.value__:
                #If a successful trial happened, then feed the animal
                new_action = MotorTrialAction()
                new_action.ActionType = MotorTrialActionType.TriggerFeeder
                result.Add(new_action)

                #If stimulation is on for this stage, stimulate the animal
                output_trigger_type = str(stage.OutputTriggerType)
                if output_trigger_type.lower() == "On".lower():
                    new_stim_action = MotorTrialAction()
                    new_stim_action.ActionType = MotorTrialActionType.SendStimulationTrigger
                    result.Add(new_stim_action)

        return result

    def PerformActionDuringTrial(self, trial, stage):
        result = List[MotorTrialAction]()
        return result

    def CreateEndOfTrialMessage(self, trial_number, trial, stage):
        msg = ""
        msg += System.DateTime.Now.ToShortTimeString() + ", "

        #Get the device stream data
        device_stream = trial.TrialData[1]
        try:
            peak_force = device_stream.GetRange(stage.TotalRecordedSamplesBeforeHitWindow, stage.TotalRecordedSamplesDuringHitWindow).Max()
            PythonPullStageImplementation_TXBDC_PostShaping.Maximal_Force_List.append(peak_force)
            
            msg += "Trial " + str(trial_number) + " "
            if trial.Result == MotorTrialResult.Hit:
                msg += "HIT, "
            else:
                msg += "MISS, "

            msg += "maximal force = " + System.Convert.ToInt32(System.Math.Floor(peak_force)).ToString() + " grams."

            #Get the name of the hit threshold parameter
            hit_threshold_parameter_name = PythonPullStageImplementation_TXBDC_PostShaping.TaskDefinition.TaskParameters[0].ParameterName

            if stage.StageParameters.ContainsKey(hit_threshold_parameter_name):
                #Grab the hit threshold for the current trial
                current_hit_threshold = stage.StageParameters[hit_threshold_parameter_name].CurrentValue

                #Add the hit threshold to the list of all hit thresholds that we are maintaining for this session
                PythonPullStageImplementation_TXBDC_PostShaping.Force_Threshold_List.append(current_hit_threshold)

                #If this is an adaptive stage, then display the hit threshold of the current trial in the "end-of-trial" message to the user
                if stage.StageParameters[hit_threshold_parameter_name].ParameterType == MotorStageParameter.StageParameterType.Variable:
                    msg += " (Hit threshold = " + System.Math.Floor(current_hit_threshold).ToString() + " grams)"
            
            return msg
        except ValueError:
            return System.String.Empty;

    def CalculateYValueForSessionOverviewPlot(self, trial, stage):
        #Get the name of the hit threshold parameter
        hit_threshold_parameter_name = PythonPullStageImplementation_TXBDC_PostShaping.TaskDefinition.TaskParameters[0].ParameterName

        #Adjust the hit threshold if necessary
        if stage.StageParameters.ContainsKey(hit_threshold_parameter_name):
            #Grab the device signal for this trial
            stream_data = trial.TrialData[1]

            #Find the maximal force of the current trial
            max_force = stream_data.Where(lambda val, index: \
                    (index >= stage.TotalRecordedSamplesBeforeHitWindow) and \
                    (index < (stage.TotalRecordedSamplesBeforeHitWindow + stage.TotalRecordedSamplesDuringHitWindow))).Max()

            return max_force

        return System.Double.NaN

    def AdjustDynamicStageParameters(self, all_trials, current_trial, stage):
        #Get the name of the hit threshold parameter
        hit_threshold_parameter_name = PythonPullStageImplementation_TXBDC_PostShaping.TaskDefinition.TaskParameters[0].ParameterName

        #Adjust the hit threshold
        if stage.StageParameters.ContainsKey(hit_threshold_parameter_name):
            #Grab the device signal for this trial
            stream_data = current_trial.TrialData[1]
        
            #Find the maximal force from the current trial
            max_force = stream_data.Where(lambda val, index: \
                (index >= stage.TotalRecordedSamplesBeforeHitWindow) and \
                (index < (stage.TotalRecordedSamplesBeforeHitWindow + stage.TotalRecordedSamplesDuringHitWindow))).Max()

            #Retain the maximal force of the most recent 10 trials
            stage.StageParameters[hit_threshold_parameter_name].History.Enqueue(max_force)
            stage.StageParameters[hit_threshold_parameter_name].CalculateAndSetBoundedCurrentValue()

        #Adjust the position of the auto-positioner, according to the stage settings
        if stage.Position.ParameterType == MotorStageParameter.StageParameterType.Variable:
            trial_count = all_trials.Count
            trial_count_modulus = trial_count % PythonPullStageImplementation_TXBDC_PostShaping.Autopositioner_Trial_Interval
            if trial_count > 0 and trial_count_modulus is 0:
                if not PythonPullStageImplementation_TXBDC_PostShaping.Autopositioner_Trial_Count_Handled.Contains(trial_count):
                    if stage.Position.CurrentValue < 2.0:
                        PythonPullStageImplementation_TXBDC_PostShaping.Autopositioner_Trial_Count_Handled.append(trial_count)
                        stage.Position.CurrentValue = stage.Position.CurrentValue + 0.25
                        MotoTrakAutopositioner.GetInstance().SetPosition(stage.Position.CurrentValue)
                
        return

    def CreateEndOfSessionMessage(self, current_session):
        #Get the name of the hit threshold parameter
        hit_threshold_parameter_name = PythonPullStageImplementation_TXBDC_PostShaping.TaskDefinition.TaskParameters[0].ParameterName

        # Find the percentage of trials that exceeded the maximum possible hit threshold in this session
        maximal_hit_threshold = current_session.SelectedStage.StageParameters[hit_threshold_parameter_name].MaximumValue
        number_of_trials_greater_than_max = sum(i >= maximal_hit_threshold for i in PythonPullStageImplementation_TXBDC_PostShaping.Maximal_Force_List)
        total_trials = len(PythonPullStageImplementation_TXBDC_PostShaping.Maximal_Force_List)        
        percent_trials_greater_than_max = 0        
        if len(PythonPullStageImplementation_TXBDC_PostShaping.Maximal_Force_List) > 0:
            percent_trials_greater_than_max = (System.Double(number_of_trials_greater_than_max) / System.Double(total_trials)) * 100
        
        # Find the number of feedings that occurred in this session
        number_of_feedings = current_session.Trials.Where(lambda x: x.Result == MotorTrialResult.Hit).Count()

        # Find the median maximal force from the sesion
        net_max_force_list = List[System.Double]()
        for i in PythonPullStageImplementation_TXBDC_PostShaping.Maximal_Force_List:
            net_max_force_list.Add(i)
        median_maximal_force = MotorMath.Median(net_max_force_list)
        if (System.Double.IsNaN(median_maximal_force)):
            median_maximal_force = 0

        # Find the median force threshold from this session
        net_threshold_list = List[System.Double]()
        for i in PythonPullStageImplementation_TXBDC_PostShaping.Force_Threshold_List:
            net_threshold_list.Add(i)
        median_force_threshold = MotorMath.Median(net_threshold_list)
        if (System.Double.IsNaN(median_force_threshold)):
            median_force_threshold = 0

        # Find the number of trials performed at the final position
        all_trials = current_session.Trials
        final_trial = all_trials.LastOrDefault()
        final_position = 0.0
        if final_trial is not None:
            final_position = final_trial.DevicePosition
        trial_count_last_position = all_trials.Where(lambda x: x.DevicePosition == final_position).Count()
        
        end_of_session_messages = List[System.String]()
        end_of_session_messages.Add(System.DateTime.Now.ToShortTimeString() + " - Session ended.")
        end_of_session_messages.Add("Pellets fed: " + System.Convert.ToInt32(number_of_feedings).ToString())
        end_of_session_messages.Add("Median Peak Force: " + System.Convert.ToInt32(median_maximal_force).ToString())
        end_of_session_messages.Add("% Trials > Maximum force threshold: " + System.Convert.ToInt32(percent_trials_greater_than_max).ToString())
        end_of_session_messages.Add("Median Force Threshold: " + System.Convert.ToInt32(median_force_threshold).ToString())
        end_of_session_messages.Add("Final position: " + System.Convert.ToDouble(final_position).ToString())
        end_of_session_messages.Add("Trials performed at final position: " + System.Convert.ToInt32(trial_count_last_position).ToString())

        return end_of_session_messages