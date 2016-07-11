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

        #region Properties

        public string ParameterName = string.Empty;
        public StageParameterType ParameterType = StageParameterType.Fixed;
        public int ParameterStreamIndex = -1;

        public double InitialValue = double.NaN;
        public double MinimumValue = double.NaN;
        public double MaximumValue = double.NaN;
        public double CurrentValue = double.NaN;
        public double Increment = double.NaN;
        public FixedSizedQueue<double> History = new FixedSizedQueue<double>() { Limit = 10 };

        public MotorStageAdaptiveThresholdType AdaptiveThresholdType = MotorStageAdaptiveThresholdType.Undefined;

        #endregion

        #region Methods

        /// <summary>
        /// This function calculates a new CurrentValue for this parameter based on the History property and the AdaptiveThresholdType.
        /// </summary>
        public void CalculateAndSetBoundedCurrentValue ()
        {
            if (History.IsFull)
            {
                List<double> clone = History.ListClone;

                switch (AdaptiveThresholdType)
                {
                    case MotorStageAdaptiveThresholdType.Median:
                        CurrentValue = Math.Max(MinimumValue, Math.Min(MaximumValue, MotorMath.Median(clone)));
                        break;
                }
                
            }
        }

        #endregion
    }
}
