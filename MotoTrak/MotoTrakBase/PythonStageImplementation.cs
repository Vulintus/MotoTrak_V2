using System;
using System.Collections.Generic;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using Dynamitey;
using System.Runtime.Remoting;
using System.Linq;
using System.Collections.Concurrent;

namespace MotoTrakBase
{
    /// <summary>
    /// A shell class that implements IMotorStageImplementation and calls into IronPython code to execute the methods.
    /// </summary>
    public class PythonStageImplementation : IMotorStageImplementation
    {
        #region Private data members

        private ConcurrentDictionary<string, Tuple<string, string, bool>> _required_stage_parameters = new ConcurrentDictionary<string, Tuple<string, string, bool>>();

        private ScriptScope _pythonScriptScope = null;
        private dynamic _pythonStageImplementationInstance;

        #endregion

        #region Public properties

        /// <summary>
        /// This is the list of parameters defined WITHIN the Python file that are required to be defined by the stage definition file
        /// in order for this stage to function.
        /// </summary>
        public ConcurrentDictionary<string, Tuple<string, string, bool>> RequiredStageParameters
        {
            get
            {
                return _required_stage_parameters;
            }
            set
            {
                _required_stage_parameters = value;
            }
        }

        #endregion

        #region Constructors

        public PythonStageImplementation(string python_script_file_path)
        {
            PythonEngine engine = PythonEngine.GetInstance();
            _pythonScriptScope = engine.PythonScriptingEngine.CreateScope();

            engine.PythonScriptingEngine.ExecuteFile(python_script_file_path, _pythonScriptScope);

            bool class_found = false;

            var all_python_script_items = _pythonScriptScope.GetItems();
            var python_script_items = all_python_script_items.Where(x => x.Value is IronPython.Runtime.Types.PythonType);

            foreach (var item in python_script_items)
            {
                //Get the type of the python object
                System.Type item_type = (Type)item.Value;

                //Get the interfaces that are implemented by the type
                var implemented_interfaces = item_type.GetInterfaces();
                if (implemented_interfaces != null && implemented_interfaces.Length > 0)
                {
                    //Check to see if this type implements IMotorStageImplementation
                    if (implemented_interfaces.ToList().Contains(typeof(IMotorStageImplementation)))
                    {
                        //If so, it is the class that we want
                        var python_stage_impl_class = item.Value;
                        _pythonStageImplementationInstance = python_stage_impl_class();

                        //var class_to_instantiate = _pythonScriptScope.GetVariable(item.Key);
                        //_pythonStageImplementationInstance = class_to_instantiate();
                        
                        var list_of_members = Dynamic.GetMemberNames(_pythonStageImplementationInstance);
                        foreach (var member_name in list_of_members)
                        {
                            object member_object = null;
                            bool success = _pythonScriptScope.Engine.Operations.TryGetMember(_pythonStageImplementationInstance, member_name, out member_object);
                            if (success)
                            {
                                Tuple<string, string, bool> type_converted_object = member_object as Tuple<string, string, bool>;
                                if (type_converted_object != null)
                                {
                                    RequiredStageParameters[type_converted_object.Item1] = type_converted_object;
                                }
                            }
                        }
                        
                        //Set the class_found flag to be true
                        class_found = true;

                        //Break out of the loop.  Our work is finished.
                        break;
                    }
                }
            }

            //If we couldn't find the python class, log the error and inform the user
            if (!class_found)
            {
                MotoTrakMessaging.GetInstance().AddMessage("Unable to find python stage class!");
                ErrorLoggingService.GetInstance().LogStringError("Unable to find python stage class!");
            }
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
