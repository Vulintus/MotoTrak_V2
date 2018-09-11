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

class PythonLeverIndividualPressStageImplementation (IMotorStageImplementation):

    #Variables needed for this task to operate
    Inter_Press_Interval_List = []
    Inter_Press_Interval_Threshold_List = []
    Press_Count_List = []
    
    inter_press_interval = 0
    press_count = 0
    press_state = 0
    feed_count = 0
    feed_flag = False
    last_feed = System.DateTime.MinValue
    nth_feed_parameter_value = 1

    Minimum_Trial_Count_To_Consider_Previous_Session = 10
    Autopositioner_Between_Session_Trial_Interval = 40
    Autopositioner_Trial_Interval = 30
    Autopositioner_Trial_Count_Handled = []

    #Declare string parameters for this stage
    TaskDefinition = MotorTaskDefinition()
    
    def __init__(self):

        PythonLeverIndividualPressStageImplementation.TaskDefinition.TaskName = "Lever Task Individual Press"
        PythonLeverIndividualPressStageImplementation.TaskDefinition.TaskDescription = "The lever task requires an animal to press a lever to receive a reward."
        PythonLeverIndividualPressStageImplementation.TaskDefinition.RequiredDeviceType = MotorDeviceType.Lever
        PythonLeverIndividualPressStageImplementation.TaskDefinition.OutputTriggerOptions = List[System.String](["Off", "On"])

        PythonLeverIndividualPressStageImplementation.TaskDefinition.HitWindowDuration.IsAdaptive = False
        PythonLeverIndividualPressStageImplementation.TaskDefinition.HitWindowDuration.IsAdaptabilityCustomizeable = False
        
        PythonLeverIndividualPressStageImplementation.TaskDefinition.DevicePosition.IsAdaptive = False
        PythonLeverIndividualPressStageImplementation.TaskDefinition.DevicePosition.IsAdaptabilityCustomizeable = False

        initiation_threshold_parameter = MotorTaskParameter(MotoTrak_V1_CommonParameters.InitiationThreshold, "degrees", True, False, False)
        lever_full_press_parameter = MotorTaskParameter("Full Press", "degrees", True, True, True)
        lever_release_point_parameter = MotorTaskParameter("Release Point", "degrees", True, True, True)
        press_counting_parameter = MotorTaskParameter("Method for counting presses", "0 = count on downward motion; 1 = count on release motion", False, False, False)
        nth_feed_parameter = MotorTaskParameter("Feed on Nth press", "Number of presses between feeds", False, False, False)
        
        PythonLeverIndividualPressStageImplementation.TaskDefinition.TaskParameters.Add(initiation_threshold_parameter)
        PythonLeverIndividualPressStageImplementation.TaskDefinition.TaskParameters.Add(lever_full_press_parameter)
        PythonLeverIndividualPressStageImplementation.TaskDefinition.TaskParameters.Add(lever_release_point_parameter)
        PythonLeverIndividualPressStageImplementation.TaskDefinition.TaskParameters.Add(press_counting_parameter)
        PythonLeverIndividualPressStageImplementation.TaskDefinition.TaskParameters.Add(nth_feed_parameter)
        
        return

    def AdjustBeginningStageParameters(self, recent_behavior_sessions, current_session_stage):

        PythonLeverIndividualPressStageImplementation.Inter_Press_Interval_List = []
        PythonLeverIndividualPressStageImplementation.Inter_Press_Interval_Threshold_List = []
        PythonLeverIndividualPressStageImplementation.Press_Count_List = []
        PythonLeverIndividualPressStageImplementation.Autopositioner_Trial_Count_Handled = []
        PythonLeverIndividualPressStageImplementation.feed_flag = False
        PythonLeverIndividualPressStageImplementation.press_count = 0
        PythonLeverIndividualPressStageImplementation.last_feed = System.DateTime.MinValue
        PythonLeverIndividualPressStageImplementation.press_state = 0
        PythonLeverIndividualPressStageImplementation.feed_count = 0

        #Get the nth feed parameter
        nth_feed_parameter = PythonLeverIndividualPressStageImplementation.TaskDefinition.TaskParameters[4].ParameterName
        if current_session_stage.StageParameters.ContainsKey(nth_feed_parameter):
            PythonLeverIndividualPressStageImplementation.nth_feed_parameter_value = current_session_stage.StageParameters[nth_feed_parameter].CurrentValue
            
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
        #Automatically initiate a trial
        return 0

    def CheckForTrialEvent(self, trial, new_datapoint_count, stage):
        #Instantiate a list of tuples that will hold any events that capture as a result of this function.
        result = List[Tuple[MotorTrialEventType, System.Int32]]()

        #Get the name of the lever full press parameter
        full_press_parameter_name = PythonLeverIndividualPressStageImplementation.TaskDefinition.TaskParameters[1].ParameterName

        #Get the name of the lever release point parameter
        release_point_parameter_name = PythonLeverIndividualPressStageImplementation.TaskDefinition.TaskParameters[2].ParameterName

        #Get the name of the feed-on-press/feed-on-release parameter
        press_counting_parameter_name = PythonLeverIndividualPressStageImplementation.TaskDefinition.TaskParameters[3].ParameterName

        #Get the value of the press counting parameter
        press_counting_parameter_value = 0

        if stage.StageParameters.ContainsKey(press_counting_parameter_name):
            press_counting_parameter_value = stage.StageParameters[press_counting_parameter_name].CurrentValue

        #Get the stream data from the device
        stream_data = trial.TrialData[1]
            
        try:
            #For the lever task the signal is in units of "degrees"
            #Let's keep a press count, as well as indices of each press, and a current state.
            #PythonLeverIndividualPressStageImplementation.press_count = 0
            indices_of_presses = List[System.Int32]()
            #PythonLeverIndividualPressStageImplementation.press_state = 0;   #0 = released, 1 = pressed

            new_data = stream_data.Skip(stream_data.Count - new_datapoint_count).ToList();

            #Now iterate over the signal
            for i in range(0, new_data.Count):
                #If the lever is currently released, check to see if it has been pressed
                if (PythonLeverIndividualPressStageImplementation.press_state == 0):
                    if (new_data[i] > stage.StageParameters[full_press_parameter_name].CurrentValue):
                        PythonLeverIndividualPressStageImplementation.press_state = 1

                        #If we are counting presses based on downward motion
                        if (press_counting_parameter_value != 1):
                            PythonLeverIndividualPressStageImplementation.press_count = PythonLeverIndividualPressStageImplementation.press_count + 1
                            indices_of_presses.Add(i)
                            is_nth_feed = (PythonLeverIndividualPressStageImplementation.press_count - 1) % PythonLeverIndividualPressStageImplementation.nth_feed_parameter_value
                            if (is_nth_feed == 0):
                                PythonLeverIndividualPressStageImplementation.feed_flag = True

                elif (PythonLeverIndividualPressStageImplementation.press_state == 1):
                    #Otherwise, if the lever is pressed, check to see if it has been fully released
                    if (new_data[i] <= stage.StageParameters[release_point_parameter_name].CurrentValue):
                        PythonLeverIndividualPressStageImplementation.press_state = 0

                        #If we are counting presses based on releasing motion
                        if (press_counting_parameter_value == 1):
                            PythonLeverIndividualPressStageImplementation.press_count = PythonLeverIndividualPressStageImplementation.press_count + 1
                            indices_of_presses.Add(i)
                            is_nth_feed = (PythonLeverIndividualPressStageImplementation.press_count - 1) % PythonLeverIndividualPressStageImplementation.nth_feed_parameter_value
                            if (is_nth_feed == 0):
                                PythonLeverIndividualPressStageImplementation.feed_flag = True

        except ValueError:
            pass

        #Return the result
        return result

    def ReactToTrialEvents(self, trial, stage):
        result = List[MotorTrialAction]()
        if PythonLeverIndividualPressStageImplementation.feed_flag is True:
            current_time = System.DateTime.Now
            reference_time = current_time - System.TimeSpan.FromSeconds(1.0);
            if (reference_time >= PythonLeverIndividualPressStageImplementation.last_feed):
                PythonLeverIndividualPressStageImplementation.last_feed = current_time
                PythonLeverIndividualPressStageImplementation.feed_flag = False

                #If a successful trial happened, then feed the animal
                new_action = MotorTrialAction()
                new_action.ActionType = MotorTrialActionType.TriggerFeeder
                result.Add(new_action)

                PythonLeverIndividualPressStageImplementation.feed_count = PythonLeverIndividualPressStageImplementation.feed_count + 1

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
        return msg

    def CalculateYValueForSessionOverviewPlot(self, trial, stage):
        return 0

    def AdjustDynamicStageParameters(self, all_trials, current_trial, stage):
        return

    def CreateEndOfSessionMessage(self, current_session):
        # Find the number of feedings that occurred in this session
        number_of_feedings = PythonLeverIndividualPressStageImplementation.feed_count

        #Create the end-of-session messages to display to the user
        end_of_session_messages = List[System.String]()
        end_of_session_messages.Add(System.DateTime.Now.ToShortTimeString() + " - Session ended.")
        end_of_session_messages.Add("Pellets fed: " + System.Convert.ToInt32(number_of_feedings).ToString())

        return end_of_session_messages