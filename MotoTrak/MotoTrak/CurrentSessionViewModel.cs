using MotoTrakBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrak
{
    public class CurrentSessionViewModel : NotifyPropertyChangedObject
    {
        #region Private data members

        MotoTrakSession _model = null;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor.  MUST take a MotoTrakSession model object.
        /// </summary>
        /// <param name="s_model">MotoTrakSession model object</param>
        public CurrentSessionViewModel ( MotoTrakSession s_model )
        {
            Model = s_model;
            Model.PropertyChanged += ExecuteReactionsToModelPropertyChanged;
        }

        #endregion

        #region Private Properties

        private MotoTrakSession Model
        {
            get
            {
                return _model;
            }
            set
            {
                _model = value;
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// The booth label
        /// </summary>
        [ReactToModelPropertyChanged(new string [] { "BoothLabel" })]
        public string BoothLabel
        {
            get
            {
                return Model.BoothLabel;
            }
        }

        /// <summary>
        /// The name of the device for this session
        /// </summary>
        [ReactToModelPropertyChanged(new string[] { "Device" })]
        public string DeviceName
        {
            get
            {
                return Model.Device.DeviceName;
            }
        }

        /// <summary>
        /// The name of the rat being used for this session
        /// </summary>
        [ReactToModelPropertyChanged(new string[] { "RatName" })]
        public string RatName
        {
            get
            {
                return Model.RatName;
            }
            set
            {
                string rat_name = value;
                Model.RatName = ViewHelperMethods.CleanInput(rat_name.Trim()).ToUpper();
            }
        }
        
        #endregion
    }
}
