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

class PythonLeverStageImplementation (IMotorStageImplementation):

    #Variables needed for this task to operate
    inter_press_interval = 0
    Autopositioner_Trial_Interval = 10

    #Declare string parameters for this stage
    RecommendedDevice = MotorDeviceType.Lever
    TaskName = "Lever Task"
    TaskDescription = "This stage implementation is for the Lever Task, a classic task in which animals must press on a lever either once or multiple times to receive a reward."

    Initiation_Threshold_Parameter = System.Tuple[System.String, System.String, System.Boolean](MotoTrak_V1_CommonParameters.InitiationThreshold, "degrees", True)    
    Lever_Full_Press_Parameter = System.Tuple[System.String, System.String, System.Boolean]("Full Press", "degrees", True)    
    Lever_Release_Point_Parameter = System.Tuple[System.String, System.String, System.Boolean]("Release Point", "degrees", True)    
    Hit_Threshold_Parameter = System.Tuple[System.String, System.String, System.Boolean](MotoTrak_V1_CommonParameters.HitThreshold, "presses", False)
    
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
        if stage.StageParameters.ContainsKey(PythonLeverStageImplementation.Initiation_Threshold_Parameter.Item1):
            #Get the stage's initiation threshold
            init_thresh = stage.StageParameters[PythonLeverStageImplementation.Initiation_Threshold_Parameter.Item1].CurrentValue

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

        #Only proceed if a hit threshold has been defined for this stage
        if stage.StageParameters.ContainsKey(PythonLeverStageImplementation.Hit_Threshold_Parameter.Item1):
            #Get the stream data from the device
            stream_data = trial.TrialData[1]
            
            #Check to see if the hit threshold has been exceeded
            current_hit_thresh = stage.StageParameters[PythonLeverStageImplementation.Hit_Threshold_Parameter.Item1].CurrentValue

            #Check to see if the stream data has exceeded the current hit threshold
            try:
                #For the lever task, the hit threshold is in units of "presses", while the signal is in units of "degrees"
                #We must analyze the signal to determine how many "presses" have occurred

                #Let's keep a press count, as well as indices of each press, and a current state.
                press_count = 0
                indices_of_presses = List[System.Int32]()
                press_state = 0;   #0 = released, 1 = pressed

                #Now iterate over the signal
                for i in range(0, stream_data.Count):
                    #Only look at samples within the hit window
                    if (i >= stage.TotalRecordedSamplesBeforeHitWindow) and (i < (stage.TotalRecordedSamplesBeforeHitWindow + stage.TotalRecordedSamplesDuringHitWindow)):
                        #If the lever is currently released, check to see if it has been pressed
                        if (press_state is 0):
                            if (stream_data[i] > stage.StageParameters[PythonLeverStageImplementation.Lever_Full_Press_Parameter.Item1].CurrentValue):
                                press_count = press_count + 1
                                indices_of_presses.Add(i)
                                press_state = 1
                        elif (press_state is 1):
                            #Otherwise, if the lever is pressed, check to see if it has been fully released
                            if (stream_data[i] <= stage.StageParameters[PythonLeverStageImplementation.Lever_Release_Point_Parameter.Item1].CurrentValue):
                                press_state = 0

                #If 2 hits have been detected, add a result to return to the caller
                if (press_count >= stage.StageParameters[PythonLeverStageImplementation.Hit_Threshold_Parameter.Item1].CurrentValue):
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
        msg += ", " + str(PythonLeverStageImplementation.inter_press_interval) + " ms"
        return msg

    def CalculateYValueForSessionOverviewPlot(self, trial, stage):
        return PythonLeverStageImplementation.inter_press_interval

    def AdjustDynamicStageParameters(self, all_trials, current_trial, stage):        

        #Adjust the hit window duration if necessary.  This is adjusted according to the isi of recent trials
        if stage.HitWindowInSeconds.ParameterType == MotorStageParameter.StageParameterType.Variable:
            isi_to_add = inter_press_interval
            if inter_press_interval is 0:
                isi_to_add = stage.HitWindowInSeconds.CurrentValue * 1000
            stage.History.Enqueue(isi_to_add)
            stage.History.CalculateAndSetBoundedCurrentValue()
            
        #Adjust the position of the auto-positioner, according to the stage settings
        if stage.Position.ParameterType == MotorStageParameter.StageParameterType.Variable:
            hit_count = all_trials.Select(lambda t: t.Result == MotorTrialResult.Hit).Count()
            hit_count_modulus = hit_count % PythonLeverStageImplementation.Autopositioner_Trial_Interval
            if hit_count > 0 and hit_count_modulus is 0:
                stage.Position.CurrentValue = stage.Position.CurrentValue + 0.5
                if stage.Position.CurrentValue is -0.5 or stage.Position.CurrentValue is 0:
                    stage.Position.CurrentValue = 0.5
                MotoTrakAutopositioner.GetInstance().SetPosition(stage.Position.CurrentValue)

        return

