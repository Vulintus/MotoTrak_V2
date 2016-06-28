import clr
clr.AddReference('System.Core')
from System.Collections.Generic import List
clr.AddReference('MotoTrakBase')
from MotoTrakBase import IMotorStageImplementation
from MotoTrakBase import MotorStage
from MotoTrakBase import MotorTrialResult

import System
clr.ImportExtensions(System.Linq)

class PythonBasicStageImplementation (IMotorStageImplementation):
    def CheckSignalForTrialInitiation(self, signal, new_datapoint_count, stage):
        return_value = -1
        if new_datapoint_count > 0:
            quantity_to_skip = signal.Count - new_datapoint_count
            signal_to_use = signal[::quantity_to_skip]                 #  C# version: signal_to_use = signal.Skip(signal.Count - new_datapoint_count).ToList()
            difference_in_size = signal.Count - signal_to_use.Count
            maximal_value = signal_to_use.Max()
            if maximal_value >= stage.TrialInitiationThreshold:
                return_value = signal_to_use.IndexOf(maximal_value) + difference_in_size
        return return_value

    def AdjustDynamicHitThreshold(self, all_trials, trial_signal, stage):    
        return

    def CheckForTrialSuccess(self, trial_signal, stage):
        return (MotorTrialResult.Unknown, -1)

    def CreateEndOfTrialMessage(self, successful_trial, trial_number, trial_signal, stage):
        return "Hello from Python!"

    def PerformActionDuringTrial(self, trial_signal, stage):
        return []

    def ReactToTrialSuccess(self, trial_signal, stage):
        return []
