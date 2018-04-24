using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakBase
{
    /// <summary>
    /// A class used by task definition files to define required task parameters
    /// </summary>
    public class MotorTaskParameter
    {
        #region Constructor

        public MotorTaskParameter ()
        {
            //empty
        }

        public MotorTaskParameter (string name, string units, bool display, bool adaptive, bool custom, 
            bool is_quantitative = true, List<string> possible_values = null, double default_quant_value = 0, string default_nominal_value = "")
        {
            ParameterName = name;
            ParameterUnits = units;
            DisplayOnPlot = display;
            IsAdaptive = adaptive;
            IsAdaptabilityCustomizeable = custom;
            IsQuantitative = is_quantitative;
            PossibleValues = possible_values ?? new List<string>();
            DefaultQuantitativeValue = default_quant_value;
            DefaultNominalValue = default_nominal_value;
        }
        
        #endregion

        #region Fields

        public string ParameterName = string.Empty;
        public string ParameterUnits = string.Empty;
        public string ParameterDescription = string.Empty;
        public bool DisplayOnPlot = false;
        public bool IsAdaptive = false;
        public bool IsAdaptabilityCustomizeable = false;
        public bool IsQuantitative = true;
        public List<string> PossibleValues = new List<string>();

        public double DefaultQuantitativeValue = 0;
        public string DefaultNominalValue = string.Empty;

        #endregion
    }
}
