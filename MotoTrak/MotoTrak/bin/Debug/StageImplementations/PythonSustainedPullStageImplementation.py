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

clr.AddReference('MotoTrakUtilities')
from MotoTrakUtilities import MotorMath

class PythonSustainedPullStageImplementation (IMotorStageImplementation):

    #Store some variables that the task needs to store for proper operation of the task
    peak_hold_time = 0

    #Declare string parameters for this stage
    RecommendedDevice = MotorDeviceType.Pull
    TaskName = "Sustained Pull Task"
    TaskDescription = "The sustained pull task requires animals to pull with a sustained amount of force over a long period of time."
    Initiation_Threshold_Parameter = System.Tuple[System.String, System.String, System.Boolean](MotoTrak_V1_CommonParameters.InitiationThreshold, "grams", True)
    Minimum_Force_Threshold_Parameter = System.Tuple[System.String, System.String, System.Boolean]("Minimum Force", "grams", True)
    Hold_Duration_Parameter = System.Tuple[System.String, System.String, System.Boolean]("Hold Duration", "milliseconds", False)
    
    def AdjustBeginningStageParameters(self, recent_behavior_sessions, current_session_stage):
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

        #Look to see if the Initiation Threshold key exists
        if stage.StageParameters.ContainsKey(PythonSustainedPullStageImplementation.Initiation_Threshold_Parameter.Item1):
            #Get the stage's initiation threshold
            init_thresh = stage.StageParameters[PythonSustainedPullStageImplementation.Initiation_Threshold_Parameter.Item1].CurrentValue

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
                    #Reset peak hold time for the next trial
                    PythonSustainedPullStageImplementation.peak_hold_time = 0

                    #Calculate the return value
                    return_value = stream_data_to_use.IndexOf(maximal_value) + difference_in_size
                
        return return_value

    def CheckForTrialEvent(self, trial, new_datapoint_count, stage):
        #Instantiate a list of tuples that will hold any events that capture as a result of this function.
        result = List[Tuple[MotorTrialEventType, System.Int32]]()

        #Only proceed if a hold duration threshold has been defined for this stage
        if stage.StageParameters.ContainsKey(PythonSustainedPullStageImplementation.Hold_Duration_Parameter.Item1):
            #Get the stream data from the device
            stream_data = trial.TrialData[1]
            
            success_found = False
            currently_in_hold = False
            index_of_hold_beginning = 0
            samples_of_current_hold = 0
            for i in range(0, stream_data.Count):
                #Check to see if a hold has started
                if currently_in_hold is False:
                    #Holds can only start within the hit window (but they may end outside the hit window)
                    if (i >= stage.TotalRecordedSamplesBeforeHitWindow) and (i < (stage.TotalRecordedSamplesBeforeHitWindow + stage.TotalRecordedSamplesDuringHitWindow)):
                        if (stream_data[i] >= stage.StageParameters[PythonSustainedPullStageImplementation.Minimum_Force_Threshold_Parameter.Item1].CurrentValue):
                            currently_in_hold = True
                            index_of_hold_beginning = i
                            samples_of_current_hold = 1
                else:
                    #If a hold is currently happening...
                    if (stream_data[i] >= stage.StageParameters[PythonSustainedPullStageImplementation.Minimum_Force_Threshold_Parameter.Item1].CurrentValue):
                        #Increment the number of samples included in this hold
                        samples_of_current_hold += 1

                        #Check to see if the hold duration has exceeded the necessary hold duration to achieve a hit
                        ms_of_current_hold = samples_of_current_hold * stage.SamplePeriodInMilliseconds

                        #Check to see if this is a new peak hold time for this trial
                        if ms_of_current_hold > PythonSustainedPullStageImplementation.peak_hold_time:
                            PythonSustainedPullStageImplementation.peak_hold_time = ms_of_current_hold

                        if ms_of_current_hold >= stage.StageParameters[PythonSustainedPullStageImplementation.Hold_Duration_Parameter.Item1].CurrentValue and \
                            success_found is not True:
                            #Create a result that we will then pass back to the calling function
                            result.Add(Tuple[MotorTrialEventType, System.Int32](MotorTrialEventType.SuccessfulTrial, i))
                            success_found = True
                            
                    else:
                        #If the force drops below the minimum force threshold, indicate that a hold is no longer happening
                        currently_in_hold = False
                    
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

        #Get the device stream data
        device_stream = trial.TrialData[1]
        try:
            msg += "Trial " + str(trial_number) + " "
            if trial.Result == MotorTrialResult.Hit:
                msg += "HIT, "
            else:
                msg += "MISS, "

            msg += "longest hold = " + str(PythonSustainedPullStageImplementation.peak_hold_time) + " ms."

            return msg
        except ValueError:
            return System.String.Empty;

    def CalculateYValueForSessionOverviewPlot(self, trial, stage):
        return PythonSustainedPullStageImplementation.peak_hold_time

    def AdjustDynamicStageParameters(self, all_trials, current_trial, stage):
        #Adjust the hold duration as necessary
        if stage.StageParameters.ContainsKey(PythonSustainedPullStageImplementation.Hold_Duration_Parameter.Item1):
            stage.StageParameters[PythonSustainedPullStageImplementation.Hold_Duration_Parameter.Item1].History.Enqueue(PythonSustainedPullStageImplementation.peak_hold_time)
            stage.StageParameters[PythonSustainedPullStageImplementation.Hold_Duration_Parameter.Item1].CalculateAndSetBoundedCurrentValue()
            
        return

    def CreateEndOfSessionMessage(self, current_session):
        return List[System.String]()