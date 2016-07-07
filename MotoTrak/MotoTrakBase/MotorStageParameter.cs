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

        public MotorStageAdaptiveThresholdType AdaptiveThresholdType = MotorStageAdaptiveThresholdType.Undefined;
        public MotorStageHitThresholdType HitThresholdType = MotorStageHitThresholdType.Undefined;
        public MotorStageStimulationType OutputTriggerType = MotorStageStimulationType.Off;
        
        #endregion
    }
}
