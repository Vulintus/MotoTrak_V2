import clr
clr.AddReference('System.Core')
from System.Collections.Generic import List
from System import Tuple
from System import Math

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

class PythonPullStageImplementation_Sustained (IMotorStageImplementation):

    #Variables used by this task
    Autopositioner_Trial_Interval = 50
    Autopositioner_Trial_Count_Handled = []
    Maximal_Force_List = []
    Force_Threshold_List = []

    Position_Of_Last_Trough = 0
    Position_Of_Hit = 0
    Longest_Sustained_Force = 0

    #Declare string parameters for this stage
    TaskDefinition = MotorTaskDefinition()

    def __init__(self):

        PythonPullStageImplementation_Sustained.TaskDefinition.TaskName = "Pull Task with sustained force"
        PythonPullStageImplementation_Sustained.TaskDefinition.TaskDescription = "This version of the pull task tests whether subjects can sustain force for a long duration."
        PythonPullStageImplementation_Sustained.TaskDefinition.RequiredDeviceType = MotorDeviceType.Pull
        PythonPullStageImplementation_Sustained.TaskDefinition.OutputTriggerOptions = List[System.String](["Off", "On", "Beginning of every trial"])

        PythonPullStageImplementation_Sustained.TaskDefinition.DevicePosition.IsAdaptive = True
        PythonPullStageImplementation_Sustained.TaskDefinition.DevicePosition.IsAdaptabilityCustomizeable = False

        force_threshold = MotorTaskParameter("Force threshold", "grams", True, True, True)
        time_threshold = MotorTaskParameter("Sustained force duration threshold", "milliseconds", True, True, True)
        initiation_threshold_parameter = MotorTaskParameter(MotoTrak_V1_CommonParameters.InitiationThreshold, "grams", True, True, True)
        
        PythonPullStageImplementation_Sustained.TaskDefinition.TaskParameters.Add(force_threshold)
        PythonPullStageImplementation_Sustained.TaskDefinition.TaskParameters.Add(time_threshold)
        PythonPullStageImplementation_Sustained.TaskDefinition.TaskParameters.Add(initiation_threshold_parameter)

        return

    def AdjustBeginningStageParameters(self, recent_behavior_sessions, current_session_stage):

        PythonPullStageImplementation_Sustained.Maximal_Force_List = []
        PythonPullStageImplementation_Sustained.Force_Threshold_List = []
        PythonPullStageImplementation_Sustained.Autopositioner_Trial_Count_Handled = []

        #Take only recent behavior sessions that have at least 50 successful trials
        total_hits = 0
        for i in recent_behavior_sessions:
            this_session_hits = i.Trials.Where(lambda x: x.Result == MotorTrialResult.Hit).Count()
            if this_session_hits >= 1:
                total_hits += this_session_hits
                
        #Now, based off the total number of hits that have occurred in previous sessions, set the position of the autopositioner
        position = -1.0
        if total_hits >= 50 and total_hits < 100:
            position = -0.5
        elif total_hits >= 100 and total_hits < 150:
            position = 0.0
        elif total_hits >= 150 and total_hits < 200:
            position = 0.5
        elif total_hits >= 200 and total_hits < 250:
            position = 1.0
        elif total_hits >= 250 and total_hits < 300:
            position = 1.5
        elif total_hits >= 300:
            position = 2.0
        
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
        #Create the value that will be our return value
        return_value = -1

        #Get the name of the initiation threshold parameter
        initiation_threshold_parameter_name = PythonPullStageImplementation_Sustained.TaskDefinition.TaskParameters[2].ParameterName

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
                    return_value = stream_data_to_use.IndexOf(maximal_value) + difference_in_size
                    PythonPullStageImplementation_Sustained.Position_Of_Last_Trough = return_value
                    PythonPullStageImplementation_Sustained.Position_Of_Hit = -1
                    PythonPullStageImplementation_Sustained.Longest_Sustained_Force = 0
                
        return return_value

    def CheckForTrialEvent(self, trial, new_datapoint_count, stage):
        #Instantiate a list of tuples that will hold any events that capture as a result of this function.
        result = List[Tuple[MotorTrialEventType, System.Int32]]()

        #Get the name of the lower bound force threshold
        force_threshold_name = PythonPullStageImplementation_Sustained.TaskDefinition.TaskParameters[0].ParameterName

        #Get the name of the upper bound force threshold
        time_threshold_name = PythonPullStageImplementation_Sustained.TaskDefinition.TaskParameters[1].ParameterName

        #Get the name of the initiation threshold parameter
        initiation_threshold_parameter_name = PythonPullStageImplementation_Sustained.TaskDefinition.TaskParameters[2].ParameterName

        #Only proceed if a hit threshold has been defined for this stage
        if stage.StageParameters.ContainsKey(force_threshold_name) and \
            stage.StageParameters.ContainsKey(time_threshold_name):

            #Get the stream data from the device
            stream_data = trial.TrialData[1]
            
            #Check to see if the hit threshold has been exceeded
            current_force_threshold = stage.StageParameters[force_threshold_name].CurrentValue
            current_time_threshold = stage.StageParameters[time_threshold_name].CurrentValue
            current_initiation_threshold = stage.StageParameters[initiation_threshold_parameter_name].CurrentValue
            
            # 0 = below, 1 = above
            pull_state = 0
            hit_found = False

            #Now iterate over the signal
            for i in range(0, stream_data.Count):
                #If a pull above the force threshold is currently occurring... (doesn't matter if we are in the hit window)
                if (pull_state == 1):
                    if (stream_data[i] >= current_force_threshold):
                        #Assuming the current sample we are inspecting remains above the force threshold, calculate how long it has been...
                        samples_above_force_threshold = i - PythonPullStageImplementation_Sustained.Position_Of_Last_Trough + 1
                        time_above_force_threshold = stage.SamplePeriodInMilliseconds * samples_above_force_threshold

                        if (time_above_force_threshold >= PythonPullStageImplementation_Sustained.Longest_Sustained_Force):
                            PythonPullStageImplementation_Sustained.Longest_Sustained_Force = time_above_force_threshold

                        #Check to see if a hit has occurred
                        if (time_above_force_threshold >= current_time_threshold and not hit_found):
                            result.Add(Tuple[MotorTrialEventType, int](MotorTrialEventType.SuccessfulTrial, i))
                            PythonPullStageImplementation_Sustained.Position_Of_Hit = i
                            hit_found = True
                    else:
                        #Otherwise, set the state indicating the force has fallen below the force threshold...
                        pull_state = 0

                #Only look at samples within the hit window for initiating a new pull...
                if (i >= stage.TotalRecordedSamplesBeforeHitWindow) and (i < (stage.TotalRecordedSamplesBeforeHitWindow + stage.TotalRecordedSamplesDuringHitWindow)):
                    if (pull_state == 0):
                        #If the pull state is "below" the force threshold, check to see if the force has now gone above it.
                        if (stream_data[i] >= current_force_threshold):
                            pull_state = 1
                            PythonPullStageImplementation_Sustained.Position_Of_Last_Trough = i

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
                if stage.OutputTriggerType == "On":
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
            PythonPullStageImplementation_Sustained.Maximal_Force_List.append(peak_force)
            
            msg += "Trial " + str(trial_number) + " "
            if trial.Result == MotorTrialResult.Hit:
                msg += "HIT, "
            else:
                msg += "MISS, "
            
            msg += "duration = " + System.Convert.ToInt32(PythonPullStageImplementation_Sustained.Longest_Sustained_Force).ToString() + " ms"

            #Get the name of the time threshold
            time_threshold_name = PythonPullStageImplementation_Sustained.TaskDefinition.TaskParameters[1].ParameterName

            if stage.StageParameters.ContainsKey(time_threshold_name):
                #Grab the hit threshold for the current trial
                current_hit_threshold = stage.StageParameters[time_threshold_name].CurrentValue

                #Add the hit threshold to the list of all hit thresholds that we are maintaining for this session
                PythonPullStageImplementation_Sustained.Force_Threshold_List.append(current_hit_threshold)

                #If this is an adaptive stage, then display the hit threshold of the current trial in the "end-of-trial" message to the user
                if stage.StageParameters[time_threshold_name].ParameterType == MotorStageParameter.StageParameterType.Variable:
                    msg += " (Force duration threshold = " + System.Math.Floor(current_hit_threshold).ToString() + " ms)"
            
            return msg
        except ValueError:
            return System.String.Empty;

    def CalculateYValueForSessionOverviewPlot(self, trial, stage):
        if PythonPullStageImplementation_Sustained.Position_Of_Hit > -1:
            return PythonPullStageImplementation_Sustained.Longest_Sustained_Force

        return System.Double.NaN

    def AdjustDynamicStageParameters(self, all_trials, current_trial, stage):
        #Get the name of the lower bound force threshold
        time_threshold_name = PythonPullStageImplementation_Sustained.TaskDefinition.TaskParameters[1].ParameterName

        #Adjust the hit threshold
        if stage.StageParameters.ContainsKey(time_threshold_name):
            #Retain the maximal force of the most recent 10 trials
            stage.StageParameters[time_threshold_name].History.Enqueue(PythonPullStageImplementation_Sustained.Longest_Sustained_Force)
            stage.StageParameters[time_threshold_name].CalculateAndSetBoundedCurrentValue()

        #Adjust the position of the auto-positioner, according to the stage settings
        if stage.Position.ParameterType == MotorStageParameter.StageParameterType.Variable:
            hit_count = all_trials.Where(lambda t: t.Result == MotorTrialResult.Hit).Count()
            hit_count_modulus = hit_count % PythonPullStageImplementation_Sustained.Autopositioner_Trial_Interval
            if hit_count > 0 and hit_count_modulus is 0:
                if not PythonPullStageImplementation_Sustained.Autopositioner_Trial_Count_Handled.Contains(hit_count):
                    if stage.Position.CurrentValue < 2.0:
                        PythonPullStageImplementation_Sustained.Autopositioner_Trial_Count_Handled.append(hit_count)
                        stage.Position.CurrentValue = stage.Position.CurrentValue + 0.5
                        MotoTrakAutopositioner.GetInstance().SetPosition(stage.Position.CurrentValue)
                
        return

    def CreateEndOfSessionMessage(self, current_session):
        # Find the number of feedings that occurred in this session
        number_of_feedings = current_session.Trials.Where(lambda x: x.Result == MotorTrialResult.Hit).Count();

        end_of_session_messages = List[System.String]()
        end_of_session_messages.Add(System.DateTime.Now.ToShortTimeString() + " - Session ended.")
        end_of_session_messages.Add("Pellets fed: " + System.Convert.ToInt32(number_of_feedings).ToString())

        return end_of_session_messages