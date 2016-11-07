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

clr.AddReference('MotoTrakUtilities')
from MotoTrakUtilities import MotorMath

class PythonLeverStageImplementation (IMotorStageImplementation):

    #Variables needed for this task to operate
    Inter_Press_Interval_List = []
    Inter_Press_Interval_Threshold_List = []
    Press_Count_List = []

    inter_press_interval = 0
    press_count = 0

    Autopositioner_Trial_Interval = 10

    #Declare string parameters for this stage
    TaskDefinition = MotorTaskDefinition()
    
    def __init__(self):

        PythonLeverStageImplementation.TaskDefinition.TaskName = "Lever Task"
        PythonLeverStageImplementation.TaskDefinition.TaskDescription = "The lever task requires an animal to press a lever (once or multiple times) to receive a reward."
        PythonLeverStageImplementation.TaskDefinition.RequiredDeviceType = MotorDeviceType.Lever
        PythonLeverStageImplementation.TaskDefinition.OutputTriggerOptions = List[System.String](["Off", "On"])

        PythonLeverStageImplementation.TaskDefinition.HitWindowDuration.IsAdaptive = True
        PythonLeverStageImplementation.TaskDefinition.HitWindowDuration.IsAdaptabilityCustomizeable = True
        PythonLeverStageImplementation.TaskDefinition.HitWindowDuration.ParameterDescription = "In the lever task, the hit window can adaptively shrink or grow based on an animal's inter-press interval performance."

        PythonLeverStageImplementation.TaskDefinition.DevicePosition.IsAdaptive = True
        PythonLeverStageImplementation.TaskDefinition.DevicePosition.IsAdaptabilityCustomizeable = False

        initiation_threshold_parameter = MotorTaskParameter(MotoTrak_V1_CommonParameters.InitiationThreshold, "degrees", True, True, True)
        lever_full_press_parameter = MotorTaskParameter("Full Press", "degrees", True, True, True)
        lever_release_point_parameter = MotorTaskParameter("Release Point", "degrees", True, True, True)
        hit_threshold_parameter = MotorTaskParameter(MotoTrak_V1_CommonParameters.HitThreshold, "presses", False, True, True)
        
        PythonLeverStageImplementation.TaskDefinition.TaskParameters.Add(hit_threshold_parameter)
        PythonLeverStageImplementation.TaskDefinition.TaskParameters.Add(initiation_threshold_parameter)
        PythonLeverStageImplementation.TaskDefinition.TaskParameters.Add(lever_full_press_parameter)
        PythonLeverStageImplementation.TaskDefinition.TaskParameters.Add(lever_release_point_parameter)
        
        return

    def AdjustBeginningStageParameters(self, recent_behavior_sessions, current_session_stage):

        PythonLeverStageImplementation.Inter_Press_Interval_List = []
        PythonLeverStageImplementation.Inter_Press_Interval_Threshold_List = []
        PythonLeverStageImplementation.Press_Count_List = []

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
        initiation_threshold_name = PythonLeverStageImplementation.TaskDefinition.TaskParameters[1].ParameterName

        #Look to see if the Initiation Threshold key exists
        if stage.StageParameters.ContainsKey(initiation_threshold_name):
            #Get the stage's initiation threshold
            init_thresh = stage.StageParameters[initiation_threshold_name].CurrentValue

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
                    #Reset the inter-press-interval for the upcoming trial
                    PythonLeverStageImplementation.inter_press_interval = 0

                    #Set the return value
                    return_value = stream_data_to_use.IndexOf(maximal_value) + difference_in_size
                
        return return_value

    def CheckForTrialEvent(self, trial, new_datapoint_count, stage):
        #Instantiate a list of tuples that will hold any events that capture as a result of this function.
        result = List[Tuple[MotorTrialEventType, System.Int32]]()

        #Get the name of the hit threshold parameter
        hit_threshold_parameter_name = PythonLeverStageImplementation.TaskDefinition.TaskParameters[0].ParameterName

        #Get the name of the lever full press parameter
        full_press_parameter_name = PythonLeverStageImplementation.TaskDefinition.TaskParameters[2].ParameterName

        #Get the name of the lever release point parameter
        release_point_parameter_name = PythonLeverStageImplementation.TaskDefinition.TaskParameters[3].ParameterName

        #Only proceed if a hit threshold has been defined for this stage
        if stage.StageParameters.ContainsKey(hit_threshold_parameter_name):
            #Get the stream data from the device
            stream_data = trial.TrialData[1]
            
            #Grab the current hit threshold (in units of presses)
            current_hit_thresh = stage.StageParameters[hit_threshold_parameter_name].CurrentValue

            #Check to see if the stream data has exceeded the current hit threshold
            try:
                #For the lever task, the hit threshold is in units of "presses", while the signal is in units of "degrees"
                #We must analyze the signal to determine how many "presses" have occurred

                #Let's keep a press count, as well as indices of each press, and a current state.
                PythonLeverStageImplementation.press_count = 0
                indices_of_presses = List[System.Int32]()
                press_state = 0;   #0 = released, 1 = pressed

                #Now iterate over the signal
                for i in range(0, stream_data.Count):
                    #Only look at samples within the hit window
                    if (i >= stage.TotalRecordedSamplesBeforeHitWindow) and (i < (stage.TotalRecordedSamplesBeforeHitWindow + stage.TotalRecordedSamplesDuringHitWindow)):
                        #If the lever is currently released, check to see if it has been pressed
                        if (press_state is 0):
                            if (stream_data[i] > stage.StageParameters[full_press_parameter_name].CurrentValue):
                                PythonLeverStageImplementation.press_count = PythonLeverStageImplementation.press_count + 1
                                indices_of_presses.Add(i)
                                press_state = 1
                        elif (press_state is 1):
                            #Otherwise, if the lever is pressed, check to see if it has been fully released
                            if (stream_data[i] <= stage.StageParameters[release_point_parameter_name].CurrentValue):
                                press_state = 0

                #If 2 hits have been detected, add a result to return to the caller
                if (PythonLeverStageImplementation.press_count >= stage.StageParameters[hit_threshold_parameter_name].CurrentValue):
                    #Create a successful trial result
                    result.Add(Tuple[MotorTrialEventType, int](MotorTrialEventType.SuccessfulTrial, indices_of_presses[indices_of_presses.Count-1]))

                    #Calculate the inter-press interval for this trial
                    indices_between_presses = MotorMath.DiffInt(indices_of_presses)
                    avg_indices_bw_presses = indices_between_presses.Average();
                    PythonLeverStageImplementation.inter_press_interval = avg_indices_bw_presses * stage.SamplePeriodInMilliseconds

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
            if event_type.value__ is MotorTrialEventType.SuccessfulTrial.value__:
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
        msg += "Trial " + str(trial_number) + " "
        if trial.Result == MotorTrialResult.Hit:
            msg += "HIT"
        else:
            msg += "MISS"

        #Add the number of presses from this trial to the press count list
        PythonLeverStageImplementation.Press_Count_List.Add(PythonLeverStageImplementation.press_count)

        #Add the press count to the message to the user
        if (PythonLeverStageImplementation.press_count is 1):
            msg += ", " + str(PythonLeverStageImplementation.press_count) + " press"
        else:
            msg += ", " + str(PythonLeverStageImplementation.press_count) + " presses"

        #If the number of presses is greater than 1, add the isi to the isi list
        if PythonLeverStageImplementation.press_count > 1:
            PythonLeverStageImplementation.Inter_Press_Interval_List.Add(PythonLeverStageImplementation.inter_press_interval)

            #Finish the message to display to the user
            msg += ", inter-press interval: " + str(PythonLeverStageImplementation.inter_press_interval) + " ms"

        #Add the inter-press interval threshold for the trial to the threshold list
        PythonLeverStageImplementation.Inter_Press_Interval_Threshold_List.Add(stage.HitWindowInSeconds.CurrentValue * 1000)

        return msg

    def CalculateYValueForSessionOverviewPlot(self, trial, stage):
        return PythonLeverStageImplementation.inter_press_interval

    def AdjustDynamicStageParameters(self, all_trials, current_trial, stage):        

        #Adjust the hit window duration if necessary.  This is adjusted according to the isi of recent trials
        if stage.HitWindowInSeconds.ParameterType == MotorStageParameter.StageParameterType.Variable:
            isi_to_add = PythonLeverStageImplementation.inter_press_interval
            if PythonLeverStageImplementation.press_count is 1 or PythonLeverStageImplementation.inter_press_interval is 0:
                isi_to_add = stage.HitWindowInSeconds.MaximumValue * 1000
            stage.HitWindowInSeconds.History.Enqueue(System.Double(isi_to_add) / System.Double(1000))
            stage.HitWindowInSeconds.CalculateAndSetBoundedCurrentValue()
            
        #Adjust the position of the auto-positioner, according to the stage settings
        if stage.Position.ParameterType == MotorStageParameter.StageParameterType.Variable:
            hit_count = all_trials.Where(lambda t: t.Result == MotorTrialResult.Hit).Count()
            hit_count_modulus = hit_count % PythonLeverStageImplementation.Autopositioner_Trial_Interval
            if hit_count > 0 and hit_count_modulus is 0:
                stage.Position.CurrentValue = stage.Position.CurrentValue + 0.5
                if stage.Position.CurrentValue is -0.5 or stage.Position.CurrentValue is 0:
                    stage.Position.CurrentValue = 0.5
                MotoTrakAutopositioner.GetInstance().SetPosition(stage.Position.CurrentValue)

        return

    def CreateEndOfSessionMessage(self, current_session):

        # Find the number of feedings that occurred in this session
        number_of_feedings = current_session.Trials.Where(lambda x: x.Result == MotorTrialResult.Hit).Count();

        #Find the median number of presses that occurred per trial
        net_press_count_list = List[System.Double]()
        for i in PythonLeverStageImplementation.Press_Count_List:
            net_press_count_list.Add(i)
        median_press_count = MotorMath.Median(net_press_count_list)
        if (System.Double.IsNaN(median_press_count)):
            median_press_count = 0

        #Find the median inter-press interval:
        net_isi_list = List[System.Double]()
        for i in PythonLeverStageImplementation.Inter_Press_Interval_List:
            net_isi_list.Add(i)
        median_isi = MotorMath.Median(net_isi_list)
        if (System.Double.IsNaN(median_isi)):
            median_isi = 0

        #Find the percentage of trials that had an ISI lower than the minimum possible ISI
        minimum_possible_isi = current_session.SelectedStage.HitWindowInSeconds.MinimumValue * 1000
        hit_rate = 0
        trial_count = current_session.Trials.Count
        if trial_count > 0:
            hit_count = net_isi_list.Where(lambda x: x <= minimum_possible_isi).Count() 
            hit_rate = (System.Double(hit_count) / System.Double(trial_count)) * 100

        #Find the median isi threshold
        net_isi_thresh_list = List[System.Double]()
        for i in PythonLeverStageImplementation.Inter_Press_Interval_Threshold_List:
            net_isi_thresh_list.Add(i)
        median_isi_thresh = MotorMath.Median(net_isi_thresh_list)
        if (System.Double.IsNaN(median_isi_thresh)):
            median_isi_thresh = 0

        #Create the end-of-session messages to display to the user
        end_of_session_messages = List[System.String]()
        end_of_session_messages.Add(System.DateTime.Now.ToShortTimeString() + " - Session ended.")
        end_of_session_messages.Add("Pellets fed: " + System.Convert.ToInt32(number_of_feedings).ToString())
        end_of_session_messages.Add("Median presses per trial: " + System.Convert.ToInt32(median_press_count).ToString())
        end_of_session_messages.Add("Median inter-press interval: " + System.Convert.ToInt32(median_isi).ToString())
        end_of_session_messages.Add("% Trials < minimum inter-press interval: " + System.Convert.ToInt32(hit_rate).ToString())
        end_of_session_messages.Add("Median inter-press interval threshold: " + System.Convert.ToInt32(median_isi_thresh).ToString())

        return end_of_session_messages