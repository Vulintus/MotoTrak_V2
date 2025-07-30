import clr
clr.AddReference('System.Core')
from System.Collections.Generic import List
from System import Tuple

import System
from System import DateTime, TimeSpan
from System.Diagnostics import Debug
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

clr.AddReference('MotoTrakUtilities')
from MotoTrakUtilities import MotorMath

class PythonKnobStageImplementation_Sustained (IMotorStageImplementation):

    #Variables needed to run this task
    Maximal_Turn_Angle_List = []
    Turn_Angle_Threshold_List = []

    Autopositioner_Trial_Interval = 50
    Autopositioner_Trial_Count_Handled = []
    Ending_Value_Of_Last_Trial = 0

    UpcomingRewardTimes = []

    Position_Of_Last_Trough = 0
    Position_Of_Hit = 0
    Longest_Sustained_Force = 0
    
    #Declare string parameters for this stage
    TaskDefinition = MotorTaskDefinition()

    def __init__(self):

        PythonKnobStageImplementation_Sustained.Maximal_Turn_Angle_List = []
        PythonKnobStageImplementation_Sustained.Turn_Angle_Threshold_List = []

        PythonKnobStageImplementation_Sustained.TaskDefinition.TaskName = "Knob Task"
        PythonKnobStageImplementation_Sustained.TaskDefinition.TaskDescription = "The knob task assesses an animal's ability to reach and the supinate with its forepaw."
        PythonKnobStageImplementation_Sustained.TaskDefinition.RequiredDeviceType = MotorDeviceType.Knob
        PythonKnobStageImplementation_Sustained.TaskDefinition.OutputTriggerOptions = List[System.String](["Off", "On", "Beginning of every trial"])

        PythonKnobStageImplementation_Sustained.TaskDefinition.DevicePosition.IsAdaptive = True
        PythonKnobStageImplementation_Sustained.TaskDefinition.DevicePosition.IsAdaptabilityCustomizeable = False

        hit_threshold_parameter = MotorTaskParameter(MotoTrak_V1_CommonParameters.HitThreshold, "degrees", True, True, True)
        initiation_threshold_parameter = MotorTaskParameter(MotoTrak_V1_CommonParameters.InitiationThreshold, "degrees", True, True, True)
        weight_parameter = MotorTaskParameter("Weight", "grams", False, False, False)
        weight_parameter.ParameterDescription = "The functionality of this task is different for stages using 0 grams of weight compared to higher amounts of weight."
        reward_delay_parameter = MotorTaskParameter("Reward Delay", "seconds", False, False, False)
        time_threshold = MotorTaskParameter("Sustained rotation duration threshold", "milliseconds", False, True, True)
        
        PythonKnobStageImplementation_Sustained.TaskDefinition.TaskParameters.Add(hit_threshold_parameter)
        PythonKnobStageImplementation_Sustained.TaskDefinition.TaskParameters.Add(initiation_threshold_parameter)
        PythonKnobStageImplementation_Sustained.TaskDefinition.TaskParameters.Add(weight_parameter)
        PythonKnobStageImplementation_Sustained.TaskDefinition.TaskParameters.Add(reward_delay_parameter)
        PythonKnobStageImplementation_Sustained.TaskDefinition.TaskParameters.Add(time_threshold)

        return

    def AdjustBeginningStageParameters(self, recent_behavior_sessions, current_session_stage):

        PythonKnobStageImplementation_Sustained.Maximal_Turn_Angle_List = []
        PythonKnobStageImplementation_Sustained.Turn_Angle_Threshold_List = []
        PythonKnobStageImplementation_Sustained.Autopositioner_Trial_Count_Handled = []
        PythonKnobStageImplementation_Sustained.Ending_Value_Of_Last_Trial = 0
        PythonKnobStageImplementation_Sustained.UpcomingRewardTimes = []

        #Take only recent behavior sessions that have at least 50 successful trials
        total_hits = 0
        for i in recent_behavior_sessions:
            this_session_hits = i.Trials.Where(lambda x: x.Result == MotorTrialResult.Hit).Count()
            if this_session_hits >= 1:
                total_hits += this_session_hits
                
        #Now, based off the total number of hits that have occurred in previous sessions, set the position of the autopositioner
        position = 0.5
        if total_hits >= 50 and total_hits < 100:
            position = 0.5
        elif total_hits >= 100 and total_hits < 150:
            position = 1.0
        elif total_hits >= 150 and total_hits < 200:
            position = 1.5
        elif total_hits >= 200:
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
                transformed_stream_data = List[System.Double](stream_data.Select(lambda x: -System.Double(device.Slope * (x - device.Baseline)) - PythonKnobStageImplementation_Sustained.Ending_Value_Of_Last_Trial).ToList())
            else:
                transformed_stream_data = List[System.Double](stream_data.Select(lambda x: System.Double(x)).ToList())
            result.Add(transformed_stream_data)
        return result

    def CheckSignalForTrialInitiation(self, signal, new_datapoint_count, stage):
        #Create the value that will be our return value
        return_value = -1

        #Get the name of the initiation threshold parameter
        initiation_threshold_parameter_name = PythonKnobStageImplementation_Sustained.TaskDefinition.TaskParameters[1].ParameterName 

        #Get the weight parameter name
        weight_parameter_name = PythonKnobStageImplementation_Sustained.TaskDefinition.TaskParameters[2].ParameterName

        #Look to see if the Initiation Threshold key exists
        if stage.StageParameters.ContainsKey(initiation_threshold_parameter_name):
            #Get the stage's initiation threshold
            init_thresh = stage.StageParameters[initiation_threshold_parameter_name].CurrentValue

            #Get the data stream itself
            stream_data = signal[1]

            if stage.StageParameters.ContainsKey(weight_parameter_name):
                #Get the weight value for this stage
                weight_grams = stage.StageParameters[weight_parameter_name].CurrentValue
                if weight_grams < 1:
                    #stream_data = MotorMath.SubtractScalarFromList(stream_data, PythonKnobStageImplementation_Sustained.Ending_Value_Of_Last_Trial)
                    stream_data = MotorMath.AbsList(stream_data)

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

                    PythonKnobStageImplementation_Sustained.Position_Of_Last_Trough = return_value
                    PythonKnobStageImplementation_Sustained.Position_Of_Hit = -1
                    PythonKnobStageImplementation_Sustained.Longest_Sustained_Force = 0
                    PythonKnobStageImplementation_Sustained.UpcomingRewardTimes = []
                
        return return_value

    def CheckForTrialEvent(self, trial, new_datapoint_count, stage):
        #Instantiate a list of tuples that will hold any events that capture as a result of this function.
        result = List[Tuple[MotorTrialEventType, System.Int32]]()

        #Get the hit threshold parameter name
        hit_threshold_parameter_name = PythonKnobStageImplementation_Sustained.TaskDefinition.TaskParameters[0].ParameterName

        #Get the duration threshold parameter name
        time_threshold_name = PythonKnobStageImplementation_Sustained.TaskDefinition.TaskParameters[4].ParameterName

        #Get the weight parameter name
        weight_parameter_name = PythonKnobStageImplementation_Sustained.TaskDefinition.TaskParameters[2].ParameterName

        #Only proceed if a hit threshold has been defined for this stage
        if stage.StageParameters.ContainsKey(hit_threshold_parameter_name) and stage.StageParameters.ContainsKey(time_threshold_name):
            #Get the stream data from the device
            stream_data = trial.TrialData[1]

            if stage.StageParameters.ContainsKey(weight_parameter_name):
                #Get the weight value for this stage
                weight_grams = stage.StageParameters[weight_parameter_name].CurrentValue
                if weight_grams < 1:
                    #stream_data = MotorMath.SubtractScalarFromList(stream_data, PythonKnobStageImplementation_Sustained.Ending_Value_Of_Last_Trial)
                    stream_data = MotorMath.AbsList(stream_data)
            
            #Check to see if the hit threshold has been exceeded
            current_hit_thresh = stage.StageParameters[hit_threshold_parameter_name].CurrentValue
            current_time_threshold = stage.StageParameters[time_threshold_name].CurrentValue

            #Now iterate over the signal
            for i in range(0, stream_data.Count):
                #If a pull above the force threshold is currently occurring... (doesn't matter if we are in the hit window)
                if (pull_state == 1):
                    if (stream_data[i] >= current_hit_thresh):
                        #Assuming the current sample we are inspecting remains above the force threshold, calculate how long it has been...
                        samples_above_force_threshold = i - PythonKnobStageImplementation_Sustained.Position_Of_Last_Trough + 1
                        time_above_force_threshold = stage.SamplePeriodInMilliseconds * samples_above_force_threshold

                        if (time_above_force_threshold >= PythonKnobStageImplementation_Sustained.Longest_Sustained_Force):
                            PythonKnobStageImplementation_Sustained.Longest_Sustained_Force = time_above_force_threshold

                        #Check to see if a hit has occurred
                        if (time_above_force_threshold >= current_time_threshold and not hit_found):
                            result.Add(Tuple[MotorTrialEventType, int](MotorTrialEventType.SuccessfulTrial, i))
                            PythonKnobStageImplementation_Sustained.Position_Of_Hit = i
                            hit_found = True
                    else:
                        #Otherwise, set the state indicating the force has fallen below the force threshold...
                        pull_state = 0

                #Only look at samples within the hit window for initiating a new pull...
                if (i >= stage.TotalRecordedSamplesBeforeHitWindow) and (i < (stage.TotalRecordedSamplesBeforeHitWindow + stage.TotalRecordedSamplesDuringHitWindow)):
                    if (pull_state == 0):
                        #If the pull state is "below" the force threshold, check to see if the force has now gone above it.
                        if (stream_data[i] >= current_hit_thresh):
                            pull_state = 1
                            PythonKnobStageImplementation_Sustained.Position_Of_Last_Trough = i

        #Return the result
        return result

    def ReactToTrialEvents(self, trial, stage):
        result = List[MotorTrialAction]()

        #Get the name of the reward delay parameter
        reward_delay_millis = 0
        reward_delay_parameter_name = PythonKnobStageImplementation_Sustained.TaskDefinition.TaskParameters[3].ParameterName
        if (stage.StageParameters.ContainsKey(reward_delay_parameter_name)):
            #Get the reward delay value
            reward_delay_millis = stage.StageParameters[reward_delay_parameter_name].CurrentValue * 1000

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

                #Check to see if the feed action should be delayed
                if (reward_delay_millis > 0):
                    #Calculate the current time in milliseconds
                    current_time = DateTime.Now
                    
                    #Calculate the expected feed time
                    expected_feed_time = current_time + TimeSpan.FromMilliseconds(reward_delay_millis)
                    
                    #Determine the time at which the feed should occur, and append it to the list of upcoming feed times
                    PythonKnobStageImplementation_Sustained.UpcomingRewardTimes.append(expected_feed_time)
                    #Debug.WriteLine("Current = " + str(current_time) + ", expected = " + str(expected_feed_time))
                else:
                    #Debug.WriteLine("Fed immediately")
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

        if (len(PythonKnobStageImplementation_Sustained.UpcomingRewardTimes) > 0):
            current_time = DateTime.Now
            first_feed_time = PythonKnobStageImplementation_Sustained.UpcomingRewardTimes[0]
            
            if (current_time >= first_feed_time):
                #Debug.WriteLine("Time to feed!")
                #If it's time to feed, remove the timestamp from the upcoming feed times list
                PythonKnobStageImplementation_Sustained.UpcomingRewardTimes.pop(0)
                
                #If it's time to feed, then add the feed action to the result
                new_action = MotorTrialAction()
                new_action.ActionType = MotorTrialActionType.TriggerFeeder
                result.Add(new_action)

        return result

    def CreateEndOfTrialMessage(self, trial_number, trial, stage):
        msg = ""
        msg += System.DateTime.Now.ToShortTimeString() + ", "

        #Get the weight parameter name
        weight_parameter_name = PythonKnobStageImplementation_Sustained.TaskDefinition.TaskParameters[2].ParameterName

        #Get the device stream data
        device_stream = trial.TrialData[1]

        if stage.StageParameters.ContainsKey(weight_parameter_name):
            #Get the weight value for this stage
            weight_grams = stage.StageParameters[weight_parameter_name].CurrentValue
            if weight_grams < 1:
                device_stream = MotorMath.AbsList(device_stream)

        try:
            peak_turn_angle = device_stream.GetRange(stage.TotalRecordedSamplesBeforeHitWindow, stage.TotalRecordedSamplesDuringHitWindow).Max()
            PythonKnobStageImplementation_Sustained.Maximal_Turn_Angle_List.append(peak_turn_angle)

            msg += "Trial " + str(trial_number) + " "
            if trial.Result == MotorTrialResult.Hit:
                msg += "HIT, "
            else:
                msg += "MISS, "

            msg += "peak turn angle = " + System.Convert.ToInt32(System.Math.Floor(peak_turn_angle)).ToString() + " degrees."

            #Get the name of the time threshold
            time_threshold_name = PythonKnobStageImplementation_Sustained.TaskDefinition.TaskParameters[4].ParameterName

            if stage.StageParameters.ContainsKey(time_threshold_name):
                #Grab the hit threshold for the current trial
                current_hit_threshold = stage.StageParameters[time_threshold_name].CurrentValue

                #Add the hit threshold to the list of all hit thresholds that we are maintaining for this session
                PythonKnobStageImplementation_Sustained.Force_Threshold_List.append(current_hit_threshold)

                #If this is an adaptive stage, then display the hit threshold of the current trial in the "end-of-trial" message to the user
                if stage.StageParameters[time_threshold_name].ParameterType == MotorStageParameter.StageParameterType.Variable:
                    msg += " (Rotation duration threshold = " + System.Math.Floor(current_hit_threshold).ToString() + " ms)"
            
            return msg
        except ValueError:
            return System.String.Empty;

    def CalculateYValueForSessionOverviewPlot(self, trial, stage):
        if PythonKnobStageImplementation_Sustained.Position_Of_Hit > -1:
            return PythonKnobStageImplementation_Sustained.Longest_Sustained_Force

        return System.Double.NaN

    def AdjustDynamicStageParameters(self, all_trials, current_trial, stage):

        #Get the hit threshold parameter name
        hit_threshold_parameter_name = PythonKnobStageImplementation_Sustained.TaskDefinition.TaskParameters[0].ParameterName

        #Get the initiation threshold parameter name
        initiation_threshold_parameter_name = PythonKnobStageImplementation_Sustained.TaskDefinition.TaskParameters[1].ParameterName

        #Get the weight parameter name
        weight_parameter_name = PythonKnobStageImplementation_Sustained.TaskDefinition.TaskParameters[2].ParameterName

        #Get the name of the lower bound force threshold
        time_threshold_name = PythonKnobStageImplementation_Sustained.TaskDefinition.TaskParameters[4].ParameterName

        #Grab the device signal for this trial
        stream_data = current_trial.TrialData[1]

        #Adjust the duration threshold
        if stage.StageParameters.ContainsKey(time_threshold_name):
            #Retain the maximal force of the most recent 10 trials
            stage.StageParameters[time_threshold_name].History.Enqueue(PythonKnobStageImplementation_Sustained.Longest_Sustained_Force)
            stage.StageParameters[time_threshold_name].CalculateAndSetBoundedCurrentValue()

        #Adjust the initiation threshold and hit threshold for the case in which we have 0 grams of weight
        if stage.StageParameters.ContainsKey(weight_parameter_name):
            #Get the weight value for this stage
            weight_grams = stage.StageParameters[weight_parameter_name].CurrentValue
            if weight_grams < 1:
                #Adjust the hit threshold and initiation threshold based on the knob's position
                PythonKnobStageImplementation_Sustained.Ending_Value_Of_Last_Trial += stream_data.Last()
                #stage.StageParameters[hit_threshold_parameter_name].CurrentValue = PythonKnobStageImplementation_Sustained.Ending_Value_Of_Last_Trial + stage.StageParameters[hit_threshold_parameter_name].InitialValue
                #stage.StageParameters[initiation_threshold_parameter_name].CurrentValue = PythonKnobStageImplementation_Sustained.Ending_Value_Of_Last_Trial + stage.StageParameters[initiation_threshold_parameter_name].InitialValue

        #Adjust the rotation degrees threshold
        if stage.StageParameters.ContainsKey(hit_threshold_parameter_name):
            #Find the maximal force from the current trial
            max_force = stream_data.Where(lambda val, index: \
                (index >= stage.TotalRecordedSamplesBeforeHitWindow) and \
                (index < (stage.TotalRecordedSamplesBeforeHitWindow + stage.TotalRecordedSamplesDuringHitWindow))).Max()

            #Retain the maximal force of the most recent 10 trials
            stage.StageParameters[hit_threshold_parameter_name].History.Enqueue(max_force)
            stage.StageParameters[hit_threshold_parameter_name].CalculateAndSetBoundedCurrentValue()

        #Adjust the position of the auto-positioner, according to the stage settings
        if stage.Position.ParameterType == MotorStageParameter.StageParameterType.Variable:
            hit_count = all_trials.Select(lambda t: t.Result == MotorTrialResult.Hit).Count()
            hit_count_modulus = hit_count % PythonKnobStageImplementation_Sustained.Autopositioner_Trial_Interval
            if hit_count > 0 and hit_count_modulus is 0:
                if not PythonKnobStageImplementation_Sustained.Autopositioner_Trial_Count_Handled.Contains(hit_count):
                    if stage.Position.CurrentValue < 2.0:
                        PythonKnobStageImplementation_Sustained.Autopositioner_Trial_Count_Handled.append(hit_count)
                        stage.Position.CurrentValue = stage.Position.CurrentValue + 0.5
                        MotoTrakAutopositioner.GetInstance().SetPosition(stage.Position.CurrentValue)
            
        return

    def CreateEndOfSessionMessage(self, current_session):
        #Get the name of the hit threshold parameter
        hit_threshold_parameter_name = PythonKnobStageImplementation_Sustained.TaskDefinition.TaskParameters[0].ParameterName

        # Find the percentage of trials that exceeded the maximum possible hit threshold in this session
        maximal_hit_threshold = current_session.SelectedStage.StageParameters[hit_threshold_parameter_name].MaximumValue
        number_of_trials_greater_than_max = sum(i >= maximal_hit_threshold for i in PythonKnobStageImplementation_Sustained.Maximal_Turn_Angle_List)
        total_trials = len(PythonKnobStageImplementation_Sustained.Maximal_Turn_Angle_List)
        percent_trials_greater_than_max = 0        
        if len(PythonKnobStageImplementation_Sustained.Maximal_Turn_Angle_List) > 0:
            percent_trials_greater_than_max = (System.Double(number_of_trials_greater_than_max) / System.Double(total_trials)) * 100
        
        # Find the number of feedings that occurred in this session
        number_of_feedings = current_session.Trials.Where(lambda x: x.Result == MotorTrialResult.Hit).Count();

        # Find the median maximal force from the sesion
        net_max_turn_angle_list = List[System.Double]()
        for i in PythonKnobStageImplementation_Sustained.Maximal_Turn_Angle_List:
            net_max_turn_angle_list.Add(i)
        median_peak_turn_angle = MotorMath.Median(net_max_turn_angle_list)
        if (System.Double.IsNaN(median_peak_turn_angle)):
            median_peak_turn_angle = 0

        # Find the median force threshold from this session
        net_threshold_list = List[System.Double]()
        for i in PythonKnobStageImplementation_Sustained.Turn_Angle_Threshold_List:
            net_threshold_list.Add(i)
        median_turn_angle_threshold = MotorMath.Median(net_threshold_list)
        if (System.Double.IsNaN(median_turn_angle_threshold)):
            median_turn_angle_threshold = 0

        end_of_session_messages = List[System.String]()
        end_of_session_messages.Add(System.DateTime.Now.ToShortTimeString() + " - Session ended.")
        end_of_session_messages.Add("Pellets fed: " + System.Convert.ToInt32(number_of_feedings).ToString())
        end_of_session_messages.Add("Median Peak Turn Angle: " + System.Convert.ToInt32(median_peak_turn_angle).ToString())
        end_of_session_messages.Add("% Trials > Maximum turn angle threshold: " + System.Convert.ToInt32(percent_trials_greater_than_max).ToString())
        end_of_session_messages.Add("Median Force Threshold: " + System.Convert.ToInt32(median_turn_angle_threshold).ToString())

        return end_of_session_messages