using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakBase
{
    public class MotorStageParameterTone
    {
        #region Enumeration

        public enum ToneEventType
        {
            Hit,
            Miss,
            HitWindow,
            Rising,
            Falling,
            Cue,
            Unknown
        }

        #endregion

        #region Constructor

        public MotorStageParameterTone()
        {
            //empty
        }

        #endregion

        #region Properties

        public byte ToneIndex { get; set; } = 0;

        public UInt16 ToneFrequency { get; set; } = 0;

        public TimeSpan ToneDuration { get; set; } = TimeSpan.Zero;

        public ToneEventType ToneEvent { get; set; } = ToneEventType.Unknown;

        public Int32 ToneThreshold { get; set; } = Int32.MinValue;

        #endregion
    }
}
