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

        public MotorTaskParameter (string name, string units, bool display, bool adaptive, bool custom)
        {
            ParameterName = name;
            ParameterUnits = units;
            DisplayOnPlot = display;
            IsAdaptive = adaptive;
            IsAdaptabilityCustomizeable = custom;
        }

        #endregion

        #region Fields

        public string ParameterName = string.Empty;
        public string ParameterUnits = string.Empty;
        public string ParameterDescription = string.Empty;
        public bool DisplayOnPlot = false;
        public bool IsAdaptive = false;
        public bool IsAdaptabilityCustomizeable = false;

        #endregion
    }
}
