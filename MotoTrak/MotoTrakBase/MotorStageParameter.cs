using MotoTrakUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakBase
{
    /// <summary>
    /// This class is meant for defining certain parameters of motor stages that vary across
    /// trials within a session.
    /// </summary>
    public class MotorStageParameter
    {
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public MotorStageParameter() { }

        #endregion

        #region Public enumeration used by parameters

        /// <summary>
        /// This enumerated type is used by certain properties of stages to indicate whether they remain fixed
        /// for an entire session, or whether they vary trial-by-trial.
        /// </summary>
        public enum StageParameterType
        {
            Fixed,
            Variable
        }

        #endregion

        #region Private properties

        private double _increment = double.NaN;

        #endregion

        #region Properties

        /// <summary>
        /// A string representing the name of this parameter
        /// </summary>
        public string ParameterName = string.Empty;

        /// <summary>
        /// A string representing the units that this parameter is measured in
        /// </summary>
        public string ParameterUnits = string.Empty;

        /// <summary>
        /// Whether or not this parameter is fixed (static) or adaptive
        /// </summary>
        public StageParameterType ParameterType = StageParameterType.Fixed;
        
        /// <summary>
        /// The initial value of the motor parameter
        /// </summary>
        public double InitialValue = double.NaN;

        /// <summary>
        /// The minimum value of the motor parameter
        /// </summary>
        public double MinimumValue = double.NaN;

        /// <summary>
        /// The maximum value of the motor parameter
        /// </summary>
        public double MaximumValue = double.NaN;

        /// <summary>
        /// The current value of the motor parameter
        /// </summary>
        public double CurrentValue = double.NaN;

        /// <summary>
        /// Increment is the amount by which the CurrentValue changes during Linear adaptive changes.
        /// It is also the size of the History queue for Percentile25, Percentile75, and Median adaptive changes.
        /// </summary>
        public double Increment
        {
            get
            {
                return _increment;
            }
            set
            {
                _increment = value;

                //Set the size of the history
                if (_increment > 0)
                {
                    History.Limit = Convert.ToInt32(_increment);
                }
            }
        }

        /// <summary>
        /// The history of this parameter.  The size of this history is the same as the value of Increment.
        /// Default size is 10.
        /// </summary>
        public FixedSizedQueue<double> History = new FixedSizedQueue<double>() { Limit = 10 };

        /// <summary>
        /// The type of adaptive change that will be used for this motor parameter
        /// </summary>
        public MotorStageAdaptiveThresholdType AdaptiveThresholdType = MotorStageAdaptiveThresholdType.Undefined;

        #endregion

        #region Methods

        /// <summary>
        /// Resets the current value of a motor stage parameter to its initial value
        /// </summary>
        public void ResetParameterToInitialValue ()
        {
            CurrentValue = InitialValue;
        }

        /// <summary>
        /// This function calculates a new CurrentValue for this parameter based on the History property and the AdaptiveThresholdType.
        /// </summary>
        public void CalculateAndSetBoundedCurrentValue ()
        {
            //Check to see if this parameter is supposed to change
            if (ParameterType == StageParameterType.Variable)
            {
                if (AdaptiveThresholdType == MotorStageAdaptiveThresholdType.Linear)
                {
                    //Change the current value based on the increment
                    CurrentValue += Increment;
                }
                else
                {
                    if (History.IsFull)
                    {
                        List<double> clone = History.ListClone;

                        switch (AdaptiveThresholdType)
                        {
                            case MotorStageAdaptiveThresholdType.Median:
                                CurrentValue = Math.Max(MinimumValue, Math.Min(MaximumValue, MotorMath.Median(clone)));
                                break;
                            case MotorStageAdaptiveThresholdType.Percentile25:
                                CurrentValue = Math.Max(MinimumValue, Math.Min(MaximumValue, MotorMath.Percentile(clone.ToArray(), 0.25)));
                                break;
                            case MotorStageAdaptiveThresholdType.Percentile75:
                                CurrentValue = Math.Max(MinimumValue, Math.Min(MaximumValue, MotorMath.Percentile(clone.ToArray(), 0.75)));
                                break;
                        }

                    }
                }
            }
        }

        #endregion
    }
}
