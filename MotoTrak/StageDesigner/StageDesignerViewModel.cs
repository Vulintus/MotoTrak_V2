using MotoTrakBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StageDesigner
{
    /// <summary>
    /// A main view-model class for the StageDesigner app
    /// </summary>
    public class StageDesignerViewModel : NotifyPropertyChangedObject
    {
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        public StageDesignerViewModel()
        {
            //empty
        }

        #endregion

        #region Properties

        /// <summary>
        /// Returns a list of stages that are open for editing
        /// </summary>
        public List<StageViewModel> OpenStages
        {
            get
            {
                List<StageViewModel> new_stage_list = new List<StageViewModel>();
                new_stage_list.Add(new StageViewModel());
                return new_stage_list;
            }
        }

        #endregion
    }
}
