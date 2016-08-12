using System;
using System.Collections.Generic;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using Dynamitey;

namespace MotoTrakBase
{
    public class PythonStageImplementation : IMotorStageImplementation
    {
        #region Private data members
        
        private ScriptScope _pythonScriptScope = null;
        private dynamic _pythonStageImplementationInstance;
        private string _class_name = "PythonBasicStageImplementation";

        #endregion

        #region Constructors

        public PythonStageImplementation(string python_script_file_path)
        {
            PythonEngine engine = PythonEngine.GetInstance();
            _pythonScriptScope = engine.PythonScriptingEngine.CreateScope();

            engine.PythonScriptingEngine.ExecuteFile(python_script_file_path, _pythonScriptScope);

            //Get the class type and instantiate an object of that class type
            var c = _pythonScriptScope.GetVariable(_class_name);
            _pythonStageImplementationInstance = c();
        }

        #endregion

        #region Implementation of IMotorStageImplementation

        public List<List<double>> TransformSignals(List<List<int>> new_data_from_controller, MotorStage stage, MotorDevice device)
        {
            return Dynamic.InvokeMember(_pythonStageImplementationInstance, "TransformSignals", new_data_from_controller, stage, device);
        }

        public int CheckSignalForTrialInitiation(List<List<double>> signal, int new_datapoint_count, MotorStage stage)
        {
            //Option 1 (slower):
            //return PythonEngine.GetInstance().PythonScriptingEngine.Operations.InvokeMember(_pythonStageImplementationInstance, "CheckSignalForTrialInitiation",
            //    signal, new_datapoint_count, stage);   
            //Option 2 (faster):
            return Dynamic.InvokeMember(_pythonStageImplementationInstance, "CheckSignalForTrialInitiation", signal, new_datapoint_count, stage);
        }

        public List<Tuple<MotorTrialEventType, int>> CheckForTrialEvent(List<List<double>> trial_signal, MotorStage stage)
        {
            return Dynamic.InvokeMember(_pythonStageImplementationInstance, "CheckForTrialEvent", trial_signal, stage);
        }

        public List<MotorTrialAction> ReactToTrialEvents(List<Tuple<MotorTrialEventType, int>> trial_events_list,
                List<List<double>> trial_signal, MotorStage stage)
        {
            return Dynamic.InvokeMember(_pythonStageImplementationInstance, "ReactToTrialEvents", trial_events_list, trial_signal, stage);
        }

        public List<MotorTrialAction> PerformActionDuringTrial(List<List<double>> trial_signal, MotorStage stage)
        {
            return Dynamic.InvokeMember(_pythonStageImplementationInstance, "PerformActionDuringTrial", trial_signal, stage);
        }

        public string CreateEndOfTrialMessage(bool successful_trial, int trial_number, List<List<double>> trial_signal, MotorStage stage)
        {
            return Dynamic.InvokeMember(_pythonStageImplementationInstance, "CreateEndOfTrialMessage", successful_trial, trial_number, trial_signal, stage);
        }

        public double CalculateYValueForSessionOverviewPlot(List<List<double>> trial_signal, MotorStage stage)
        {
            return Dynamic.InvokeMember(_pythonStageImplementationInstance, "CalculateYValueForSessionOverviewPlot", trial_signal, stage);
        }

        public void AdjustDynamicStageParameters(List<MotorTrial> all_trials, List<List<double>> trial_signal, MotorStage stage)
        {
            Dynamic.InvokeMemberAction(_pythonStageImplementationInstance, "AdjustDynamicStageParameters", all_trials, trial_signal, stage);
        }
        
        #endregion
    }
}
