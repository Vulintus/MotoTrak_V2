using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakBase
{
    /// <summary>
    /// This class encapsulates parameters that describe how to act on a stream of data
    /// </summary>
    public class MotorStreamParameters
    {
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        public MotorStreamParameters()
        {
            //empty
        }

        #endregion

        #region Properties

        public MotorStageParameter InitiationThreshold = null;
        public MotorStageParameter HitThreshold = null;

        public MotorStageHitThresholdType HitThresholdType = MotorStageHitThresholdType.Undefined;
        public MotorBoardDataStreamType StreamType = MotorBoardDataStreamType.Unknown;

        public int StreamIndex = -1;
        
        #endregion
    }
}
