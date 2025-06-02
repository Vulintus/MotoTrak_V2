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
    public class PythonTaskImplementation : IMotorTaskImplementation
    {
        #region Private data members

        //Each task implementation runs within its own scope. This variable holds that scope.
        private ScriptScope _python_script_scope = null;

        //This variable holds the actual instance of the Python class
        private dynamic _python_task_implementation_instance;

        //Each task implementation Python file defines a "task definition" object. We will retrieve that object
        //from the Python class and store it here.
        private MotorTaskDefinition _motor_task_definition;

        #endregion

        #region Constructors

        public PythonTaskImplementation(string python_script_file_path)
        {
            PythonEngine engine = PythonEngine.GetInstance();
            _python_script_scope = engine.PythonScriptingEngine.CreateScope();

            engine.PythonScriptingEngine.ExecuteFile(python_script_file_path, _python_script_scope);

            bool class_found = false;

            var all_python_script_items = _python_script_scope.GetItems();
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
                    if (implemented_interfaces.ToList().Contains(typeof(IMotorTaskImplementation)))
                    {
                        //If so, it is the class that we want
                        var python_stage_impl_class = item.Value;
                        _python_task_implementation_instance = python_stage_impl_class();

                        //Iterate over all members of the class
                        var list_of_members = Dynamic.GetMemberNames(_python_task_implementation_instance);
                        foreach (string member_name in list_of_members)
                        {
                            object member_object = null;
                            bool success = _python_script_scope.Engine.Operations.TryGetMember(_python_task_implementation_instance, member_name, out member_object);
                            if (success)
                            {
                                //If this member's name is "TaskDefinition"...
                                if (member_name.Equals("TaskDefinition"))
                                {
                                    //Then let's store a reference to the task definition object in the _motor_task_definition variable. This makes it easier
                                    //to access from C# code.
                                    _motor_task_definition = member_object as MotorTaskDefinition;
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

        #region Public properties

        /// <summary>
        /// The task definition as defined in the Python file
        /// </summary>
        public MotorTaskDefinition TaskDefinition
        {
            get
            {
                return _motor_task_definition;
            }
        }

        #endregion

        #region Implementation of IMotorTaskImplementation

        /// <summary>
        /// This method adjusts various stage parameters at the beginning of a session. As inputs to the function, we provide a list
        /// of previous sessions that the subject has completed, and we also provide an object that defines the currently selected stage
        /// for the task. Information from these objects can inform how to adjust the various parameters of the stage.
        /// </summary>
        /// <param name="recent_behavior_sessions">A list of completed sessions by the subject</param>
        /// <param name="current_session_stage">An object representing the currently selected stage</param>
        public void AdjustBeginningStageParameters(List<MotoTrakSession> recent_behavior_sessions, MotorStage current_session_stage)
        {
            Dynamic.InvokeMemberAction(_python_task_implementation_instance, "AdjustBeginningStageParameters",
                recent_behavior_sessions, current_session_stage);
        }

        /// <summary>
        /// This method receives new data from the microcontroller board and transorms the signal data based on what is necessary for the
        /// selected task.
        /// </summary>
        /// <param name="new_data_from_controller">New data being received from the microcontroller</param>
        /// <param name="stage">The currently selected stage for this task</param>
        /// <param name="device">The device being used for this task (such as pull, knob, lever, etc)</param>
        /// <returns>The transformed data</returns>
        public List<List<double>> TransformSignals(List<List<Int64>> new_data_from_controller, MotorStage stage, MotorDevice device)
        {
            return Dynamic.InvokeMember(_python_task_implementation_instance, "TransformSignals", new_data_from_controller, stage, device);
        }

        /// <summary>
        /// This method checks the most recently received data from the microcontroller and determines if a trial should be initiated.
        /// </summary>
        /// <param name="signal">The most recently received data from the microcontroller (after it has already been transformed by the TransormSignals
        /// method)</param>
        /// <param name="new_datapoint_count">The number of new datapoints that have been received</param>
        /// <param name="stage">The currently selected stage</param>
        /// <returns>The index into the signal data indicating at which position the trial-initiation criteria was met, or -1 if no trial initiation occurred.</returns>
        public int CheckSignalForTrialInitiation(List<List<double>> signal, int new_datapoint_count, MotorStage stage)
        {
            //Option 1 (slower):
            //return PythonEngine.GetInstance().PythonScriptingEngine.Operations.InvokeMember(_pythonStageImplementationInstance, "CheckSignalForTrialInitiation",
            //    signal, new_datapoint_count, stage);   
            //Option 2 (faster):
            return Dynamic.InvokeMember(_python_task_implementation_instance, "CheckSignalForTrialInitiation", signal, new_datapoint_count, stage);
        }

        /// <summary>
        /// Trials can signal "events". One very common example of an event is a "successful trial". This method checks the current trial object to see if any
        /// "events" should be signaled to the caller. If so, it returns a list of any events that it finds and the indices into the signal data where those
        /// events occurred.
        /// </summary>
        /// <param name="trial">The current ongoing trial</param>
        /// <param name="new_datapoint_count">The number of new datapoints received during this frame</param>
        /// <param name="stage">The currently selected stage</param>
        /// <returns>A list containing any events that occurred and the indices at which they occurred in the signal data.</returns>
        public List<Tuple<MotorTrialEventType, int>> CheckForTrialEvent(MotorTrial trial, int new_datapoint_count, MotorStage stage)
        {
            return Dynamic.InvokeMember(_python_task_implementation_instance, "CheckForTrialEvent", trial, new_datapoint_count, stage);
        }

        /// <summary>
        /// After an event has occurred, this function is called to properly react to that event. One example of a reaction may be triggering the
        /// feeder or issuing a stimulation using some neuromodulatory equipment.
        /// </summary>
        /// <param name="trial">The current ongoing trial</param>
        /// <param name="stage">The currently selected stage</param>
        /// <returns>The action that should be completed</returns>
        public List<MotorTrialAction> ReactToTrialEvents(MotorTrial trial, MotorStage stage)
        {
            return Dynamic.InvokeMember(_python_task_implementation_instance, "ReactToTrialEvents", trial, stage);
        }

        /// <summary>
        /// Sometimes actions may want to be taken, but not in reaction to a trial event. This method can be used by
        /// a task to perform some actions during a trial.
        /// </summary>
        /// <param name="trial">The current ongoing trial.</param>
        /// <param name="stage">The currently selected stage</param>
        /// <returns>A list of tactions that should be completed.</returns>
        public List<MotorTrialAction> PerformActionDuringTrial(MotorTrial trial, MotorStage stage)
        {
            return Dynamic.InvokeMember(_python_task_implementation_instance, "PerformActionDuringTrial", trial, stage);
        }

        /// <summary>
        /// When a trial is completed, we output a brief message to the user with some information about the trial. This
        /// method formulates that message to the user.
        /// </summary>
        /// <param name="trial_number">The trial number</param>
        /// <param name="trial">The current ongoing trial</param>
        /// <param name="stage">The selected stage</param>
        /// <returns>The message to output to the user</returns>
        public string CreateEndOfTrialMessage(int trial_number, MotorTrial trial, MotorStage stage)
        {
            return Dynamic.InvokeMember(_python_task_implementation_instance, "CreateEndOfTrialMessage", trial_number, trial, stage);
        }

        /// <summary>
        /// When a trial is completed, we plot a summary value of the trial on the "session overview plot" in the MotoTrak window.
        /// This method calculates that summary value for the trial.
        /// </summary>
        /// <param name="trial">The current ongoing trial.</param>
        /// <param name="stage">The currently selected stage.</param>
        /// <returns></returns>
        public double CalculateYValueForSessionOverviewPlot(MotorTrial trial, MotorStage stage)
        {
            return Dynamic.InvokeMember(_python_task_implementation_instance, "CalculateYValueForSessionOverviewPlot", trial, stage);
        }

        /// <summary>
        /// When a trial is completed, the task may want to adjust some parameters of the stage before it proceeds to the next trial.
        /// This method takes care of all of that.
        /// </summary>
        /// <param name="all_trials">A list of all completed trials during the current session</param>
        /// <param name="current_trial">The current ongoing trial</param>
        /// <param name="stage">The currently selected stage.</param>
        public void AdjustDynamicStageParameters(List<MotorTrial> all_trials, MotorTrial current_trial, MotorStage stage)
        {
            Dynamic.InvokeMemberAction(_python_task_implementation_instance, "AdjustDynamicStageParameters", all_trials, current_trial, stage);
        }

        /// <summary>
        /// At the end of a session, this method is called if the task wishes to display a final end-of-session message to the user.
        /// </summary>
        /// <param name="current_session">The current session</param>
        /// <returns>A list of all messages to be displayed to the user</returns>
        public List<string> CreateEndOfSessionMessage(MotoTrakSession current_session)
        {
            return Dynamic.InvokeMember(_python_task_implementation_instance, "CreateEndOfSessionMessage", current_session);
        }

        #endregion
    }
}
