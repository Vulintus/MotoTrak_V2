using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakBase
{
    /// <summary>
    /// This class represents a stage within the MotoTrak program.
    /// </summary>
    public class MotorStage
    {
        #region Constructors

        /// <summary>
        /// Constructs an empty MotoTrak stage.
        /// </summary>
        public MotorStage()
        {
            //empty
        }

        #endregion

        #region Properties - V1

        public string StageNumber { get; set; }
        public string Description { get; set; }
        public MotorDeviceType DeviceType { get; set; }
        public double Position { get; set; }
        public MotorStageAdaptiveThresholdType AdaptiveThresholdType { get; set; }

        public double HitThresholdMinimum { get; set; }
        public double HitThresholdMaximum { get; set; }
        public double HitThresholdIncrement { get; set; }
        public double HitThreshold { get; set; }

        public double TrialInitiationThreshold { get; set; }
        public MotorStageHitThresholdType HitThresholdType { get; set; }
        public double HitWindowInSeconds { get; set; }
        public int SamplePeriodInMilliseconds { get; set; }
        public MotorStageStimulationType StimulationType { get; set; }

        public List<MotorStageHitThresholdType> PossibleHitThresholdTypes
        {
            get
            {
                List<MotorStageHitThresholdType> possibleTypes = new List<MotorStageHitThresholdType>();
                switch (DeviceType)
                {
                    case MotorDeviceType.Pull:
                        possibleTypes.Add(MotorStageHitThresholdType.PeakForce);
                        possibleTypes.Add(MotorStageHitThresholdType.SustainedForce);
                        break;
                    case MotorDeviceType.Knob:
                        possibleTypes.Add(MotorStageHitThresholdType.TotalDegrees);
                        break;
                    default:
                        possibleTypes.Add(MotorStageHitThresholdType.Undefined);
                        break;
                }

                return possibleTypes;
            }
        }

        #endregion
    }
}
