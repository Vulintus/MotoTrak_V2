using MotoTrakBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StageDesigner
{
    /// <summary>
    /// This view-model represents a stage in the StageDesigner app
    /// </summary>
    public class StageViewModel : NotifyPropertyChangedObject
    {
        #region Constructors

        /// <summary>
        /// Constructor for the stage view-model class
        /// </summary>
        public StageViewModel()
        {
            //empty
        }

        #endregion

        #region Properties

        /// <summary>
        /// This is a list of all tasks that are available to choose from
        /// </summary>
        public List<string> TaskNames
        {
            get
            {
                List<string> result = new List<string>();
                var stage_implementations = MotoTrakConfiguration.GetInstance().PythonStageImplementations;
                string unknown_task_basic_stage = string.Empty;
                foreach (var impl in stage_implementations)
                {
                    PythonStageImplementation this_stage_impl = impl.Value as PythonStageImplementation;
                    if (this_stage_impl != null)
                    {
                        string this_name = this_stage_impl.TaskName + " (" + impl.Key + ")";
                        if (this_stage_impl.TaskName.Equals("Unknown"))
                        {
                            unknown_task_basic_stage = this_name;
                        }
                        else
                        {
                            result.Add(this_name);
                        }
                        
                    }
                }

                if (!string.IsNullOrEmpty(unknown_task_basic_stage))
                {
                    result.Add(unknown_task_basic_stage);
                }

                return result;
            }
        }

        public string SelectedStageName
        {
            get
            {
                return "Hello, World!";
            }
        }

        #endregion
    }
}
