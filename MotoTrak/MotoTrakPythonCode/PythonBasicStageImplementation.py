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
from MotoTrakBase import MotorStageStimulationType
from MotoTrakBase import MotorStageAdaptiveThresholdType

clr.AddReference('MotoTrakUtilities')
from MotoTrakUtilities import MotorMath

class PythonBasicStageImplementation (IMotorStageImplementation):

    #This is a private variable that is referred to by one of the implemented methods
    _peak_force = List[System.Double]()

    def CheckSignalForTrialInitiation(self, signal, new_datapoint_count, stage):
        #Create the value that will be our return value
        return_value = -1

        #We must have more than 0 new datapoints.  If the code inside the if-statement was run with 0 new datapoints, it would
        #generate an exception.
        if new_datapoint_count > 0:
            #Look only at the most recent data from the signal
            signal_to_use = signal.Skip(signal.Count - new_datapoint_count).ToList()
            
            #Calculate the difference in size between the two
            difference_in_size = signal.Count - signal_to_use.Count

            #Retrieve the maximal value from the device signal
            maximal_value = signal_to_use.Max()
            if maximal_value >= stage.TrialInitiationThreshold:
                #If the maximal value in the signal exceeded the trial initiation force threshold
                #Find the position at which the signal exceeded the threshold, and return it.
                return_value = signal_to_use.IndexOf(maximal_value) + difference_in_size
        return return_value

    def CheckForTrialSuccess(self, trial_signal, stage):
        #Create an empty Tuple that will hold our result
        result = Tuple[MotorTrialResult, System.Int32](MotorTrialResult.Unknown, -1)

        #Find the point at which the animal exceeds the hit threshold.  This function returns -1 if nothing is found.
        l = Enumerable.Range(0, trial_signal.Count).Where(lambda index: trial_signal[index] >= stage.HitThreshold and \
            (index >= stage.TotalRecordedSamplesBeforeHitWindow) and \
            (index < stage.TotalRecordedSamplesBeforeHitWindow + stage.TotalRecordedSamplesDuringHitWindow)).ToList()
        if l is not None and l.Count > 0:
            result = Tuple[MotorTrialResult, System.Int32](MotorTrialResult.Hit, l[0])

        return result

    def ReactToTrialSuccess(self, trial_signal, stage):
        result = List[MotorTrialAction]()
        result.Add(MotorTrialAction.TriggerFeeder)
        if stage.StimulationType == MotorStageStimulationType.On:
            result.Add(MotorTrialAction.SendStimulationTrigger)
        return result

    def PerformActionDuringTrial(self, trial_signal, stage):
        result = List[MotorTrialAction]()
        return result

    def CreateEndOfTrialMessage(self, successful_trial, trial_number, trial_signal, stage):
        msg = ""
        msg += "Trial " + str(trial_number) + " "
        if successful_trial:
            msg += "HIT"
        else:
            msg += "MISS"
        return msg

    def AdjustDynamicHitThreshold(self, all_trials, trial_signal, stage):
        if stage.AdaptiveThresholdType == MotorStageAdaptiveThresholdType.Median:
            #Find the maximal force from the current trial
            max_force = trial_signal.Where(lambda val, index: \
                (index >= stage.TotalRecordedSamplesBeforeHitWindow) and \
                (index < (stage.TotalRecordedSamplesBeforeHitWindow + stage.TotalRecordedSamplesDuringHitWindow))).Max()
            
            #Retain the maximal force of the most recent 10 trials
            _peak_force.Add(max_force)
            if _peak_force.Count > stage.TrialsToRetainForAdaptiveAdjustments:
                _peak_force.RemoveAt(0)
            
            #Adjust the hit threshold
            if _peak_force.Count == stage.TrialsToRetainForAdaptiveAdjustments:
                median = MotorMath.Median(_peak_force)
                stage.HitThreshold = System.Math.Max(stage.HitThresholdMinimum, Math.Min(stage.HitThresholdMaximum, median))

        return


