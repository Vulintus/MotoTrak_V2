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
        MotorTaskParameter _model_task_parameter;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        public StageParameterControlViewModel(MotorStageParameter model_param, MotorTaskParameter model_task_param)
        {
            ModelParameter = model_param;
            ModelTaskParameter = model_task_param;
        }

        #endregion

        #region Private methods

        private List<MotorStageAdaptiveThresholdType> GetEnumValueForAdaptiveTypeListBox ()
        {
            var enum_values = Enum.GetValues(typeof(MotorStageAdaptiveThresholdType)).Cast<MotorStageAdaptiveThresholdType>().ToList();
            enum_values.Remove(MotorStageAdaptiveThresholdType.Static);
            enum_values.Remove(MotorStageAdaptiveThresholdType.Undefined);

            return enum_values;
        }

        private List<string> GetStringValuesForAdaptiveTypeListBox ()
        {
            List<string> result = new List<string>();
            var enum_values = GetEnumValueForAdaptiveTypeListBox();
            foreach (var v in enum_values)
            {
                result.Add(MotorStageAdaptiveThresholdTypeConverter.ConvertToDescription(v));
            }

            return result;
        }

        #endregion

        #region Properties
        
        /// <summary>
        /// All the adaptive modes that can be selected for a motor stage parameter
        /// </summary>
        public List<string> AdaptiveModes
        {
            get
            {
                return GetStringValuesForAdaptiveTypeListBox();
            }
        }

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
        /// The task parameter model
        /// </summary>
        public MotorTaskParameter ModelTaskParameter
        {
            get
            {
                return _model_task_parameter;
            }
            set
            {
                _model_task_parameter = value;
            }
        }

        /// <summary>
        /// Indicates whether this is a quantitative parameter
        /// </summary>
        public bool IsParameterQuantitative
        {
            get
            {
                return ModelTaskParameter.IsQuantitative;
            }
        }

        /// <summary>
        /// Indicates whether quantitiative parameter fields are visible
        /// </summary>
        public Visibility QuantitativeParameterVisibility
        {
            get
            {
                if (IsParameterQuantitative)
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
        /// Indicates whether nominal parameter fields are visible
        /// </summary>
        public Visibility NominalParameterVisibility
        {
            get
            {
                if (!IsParameterQuantitative)
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
        /// Returns a description about the parameter in question
        /// </summary>
        public string ParameterDescription
        {
            get
            {
                return ModelTaskParameter.ParameterDescription;
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
        /// Indicates whether this parameter is allowed to be adaptive or not.
        /// </summary>
        public bool CanThisParameterBeAdaptive
        {
            get
            {
                return ModelTaskParameter.IsAdaptive;
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
                if (ModelParameter.ParameterType == MotorStageParameter.StageParameterType.Variable &&
                    ModelTaskParameter.IsAdaptabilityCustomizeable && ModelTaskParameter.IsAdaptive)
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
                var enum_values = GetEnumValueForAdaptiveTypeListBox();
                int index = enum_values.IndexOf(ModelParameter.AdaptiveThresholdType);
                if (index < 0)
                    index = 0;
                return index;
            }
            set
            {
                var enum_values = GetEnumValueForAdaptiveTypeListBox();
                int index = value;
                if (enum_values.Count > 0 && index <= enum_values.Count)
                {
                    ModelParameter.AdaptiveThresholdType = enum_values[index];
                }
                
                NotifyPropertyChanged("AdaptiveModeSelectedIndex");
                NotifyPropertyChanged("IncrementText");
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
        public string ParameterIncrement
        {
            get
            {
                return ModelParameter.Increment.ToString();
            }
            set
            {
                string max_value_string = value;
                double result = 0;
                bool success = Double.TryParse(max_value_string, out result);
                if (success)
                {
                    ModelParameter.Increment = result;
                }

                NotifyPropertyChanged("ParameterIncrement");
            }
        }

        /// <summary>
        /// The text that labels the edit box for the motor stage parameter Increment
        /// </summary>
        public string IncrementText
        {
            get
            {
                if (ModelParameter.AdaptiveThresholdType == MotorStageAdaptiveThresholdType.Linear)
                {
                    return "Increment:";
                }
                else
                {
                    return "History size:";
                }
            }
        }

        /// <summary>
        /// The list of possible parameter values for a nominal task parameter
        /// </summary>
        public List<string> PossibleNominalValues
        {
            get
            {
                return ModelTaskParameter.PossibleValues;
            }
        }

        /// <summary>
        /// The index of the selected nominal parameter value
        /// </summary>
        public int SelectedNominalValue
        {
            get
            {
                int result = 0;

                if (!IsParameterQuantitative)
                {
                    result = ModelTaskParameter.PossibleValues.IndexOf(ModelParameter.NominalValue);
                    if (result == -1)
                    {
                        result = 0;
                    }
                }
                
                return result;
            }
            set
            {
                ModelParameter.NominalValue = ModelTaskParameter.PossibleValues[value];
                NotifyPropertyChanged("SelectedNominalValue");
            }
        }

        #endregion
    }
}
