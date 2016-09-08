using MotoTrakBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace StageDesigner
{
    /// <summary>
    /// A view-model class for the stage parameter control
    /// </summary>
    public class StageParameterControlViewModel : NotifyPropertyChangedObject
    {
        #region Private data members

        MotorStageParameter _model_parameter;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        public StageParameterControlViewModel(MotorStageParameter model_param)
        {
            ModelParameter = model_param;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The model MotorStageParameter object
        /// </summary>
        public MotorStageParameter ModelParameter
        {
            get
            {
                return _model_parameter;
            }
            private set
            {
                _model_parameter = value;
            }
        }

        /// <summary>
        /// The name of the stage parameter
        /// </summary>
        public string ParameterName
        {
            get
            {
                return ModelParameter.ParameterName;
            }
            set
            {
                ModelParameter.ParameterName = value;
                NotifyPropertyChanged("ParameterName");
            }
        }

        /// <summary>
        /// The units that this parameter is measured in
        /// </summary>
        public string ParameterUnits
        {
            get
            {
                return ModelParameter.ParameterUnits;
            }
            set
            {
                ModelParameter.ParameterUnits = value;
                NotifyPropertyChanged("ParameterUnits");
            }
        }

        /// <summary>
        /// The initial value of the stage parameter
        /// </summary>
        public string ParameterInitialValue
        {
            get
            {
                return ModelParameter.InitialValue.ToString();
            }
            set
            {
                string init_value_string = value;
                double result = double.NaN;
                bool success = Double.TryParse(init_value_string, out result);
                if (success)
                {
                    ModelParameter.InitialValue = result;
                    ModelParameter.CurrentValue = result;
                }

                NotifyPropertyChanged("ParameterInitialValue");
            }
        }

        /// <summary>
        /// An index into the combo box that allows the user to choose whether or not this stage parameter
        /// is adaptive or fixed
        /// </summary>
        public int AdaptiveSelectedIndex
        {
            get
            {
                if (ModelParameter.ParameterType == MotorStageParameter.StageParameterType.Variable)
                    return 0;
                else return 1;
            }
            set
            {
                int index = value;
                if (index == 0)
                {
                    ModelParameter.ParameterType = MotorStageParameter.StageParameterType.Variable;
                }
                else
                {
                    ModelParameter.ParameterType = MotorStageParameter.StageParameterType.Fixed;
                }

                NotifyPropertyChanged("AdaptiveSelectedIndex");
                NotifyPropertyChanged("AdaptiveParametersVisibility");
                NotifyPropertyChanged("AdaptiveParametersAreEnabled");
            }
        }

        /// <summary>
        /// Whether or not to display controls that are meant for adaptive parameters
        /// </summary>
        public Visibility AdaptiveParametersVisibility
        {
            get
            {
                if (ModelParameter.ParameterType == MotorStageParameter.StageParameterType.Variable)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// Whether or not to enable controls that are meant for adaptive parameters
        /// </summary>
        public bool AdaptiveParametersAreEnabled
        {
            get
            {
                return (ModelParameter.ParameterType == MotorStageParameter.StageParameterType.Variable);
            }
        }

        /// <summary>
        /// Determines how the stage parameter is adaptively determined
        /// </summary>
        public int AdaptiveModeSelectedIndex
        {
            get
            {
                switch (ModelParameter.AdaptiveThresholdType)
                {
                    case MotorStageAdaptiveThresholdType.Median:
                        return 0;
                    case MotorStageAdaptiveThresholdType.Linear:
                        return 1;
                    case MotorStageAdaptiveThresholdType.Dynamic:
                        return 2;
                    default:
                        return 0;
                }
            }
            set
            {
                int index = value;
                switch (index)
                {
                    case 0:
                        ModelParameter.AdaptiveThresholdType = MotorStageAdaptiveThresholdType.Median;
                        break;
                    case 1:
                        ModelParameter.AdaptiveThresholdType = MotorStageAdaptiveThresholdType.Linear;
                        break;
                    case 2:
                        ModelParameter.AdaptiveThresholdType = MotorStageAdaptiveThresholdType.Dynamic;
                        break;
                    default:
                        ModelParameter.AdaptiveThresholdType = MotorStageAdaptiveThresholdType.Static;
                        break;
                }

                NotifyPropertyChanged("AdaptiveModeSelectedIndex");
            }
        }

        /// <summary>
        /// The minimum possible value of the stage parameter
        /// </summary>
        public string ParameterMinimumValue
        {
            get
            {
                return ModelParameter.MinimumValue.ToString();
            }
            set
            {
                string min_value_string = value;
                double result = double.NaN;
                bool success = Double.TryParse(min_value_string, out result);
                if (success)
                {
                    ModelParameter.MinimumValue = result;
                }

                NotifyPropertyChanged("ParameterMinimumValue");
            }
        }

        /// <summary>
        /// The maximum possible value of the stage parameter
        /// </summary>
        public string ParameterMaximumValue
        {
            get
            {
                return ModelParameter.MaximumValue.ToString();
            }
            set
            {
                string max_value_string = value;
                double result = double.NaN;
                bool success = Double.TryParse(max_value_string, out result);
                if (success)
                {
                    ModelParameter.MaximumValue = result;
                }

                NotifyPropertyChanged("ParameterMaximumValue");
            }
        }

        /// <summary>
        /// The size of the history for this stage parameter
        /// </summary>
        public string ParameterHistorySize
        {
            get
            {
                return Convert.ToInt32(ModelParameter.Increment).ToString();
            }
            set
            {
                string max_value_string = value;
                int result = 0;
                bool success = Int32.TryParse(max_value_string, out result);
                if (success)
                {
                    ModelParameter.MaximumValue = result;
                }

                NotifyPropertyChanged("ParameterHistorySize");
            }
        }

        #endregion
    }
}
