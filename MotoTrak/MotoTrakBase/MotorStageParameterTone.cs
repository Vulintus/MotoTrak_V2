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

        /// <summary>
        /// The tone index that will be specified when setting the tone parameters on the MotoTrak board
        /// </summary>
        public byte ToneIndex { get; set; } = 0;

        /// <summary>
        /// The frequency of the tone to be played
        /// </summary>
        public UInt16 ToneFrequency { get; set; } = 0;

        /// <summary>
        /// The duration of the tone to be played
        /// </summary>
        public TimeSpan ToneDuration { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// This specifies the primary criteria for when the tone gets triggered. For example, a "Hit" tone gets played when
        /// a hit occurs during a trial.
        /// </summary>
        public ToneEventType ToneEvent { get; set; } = ToneEventType.Unknown;

        /// <summary>
        /// 
        /// </summary>
        public Int32 ToneThreshold { get; set; } = Int32.MinValue;

        /// <summary>
        /// After the tone criteria are met, this is the number of milliseconds to wait to play the tone
        /// </summary>
        public Int32 ToneDelayMilliseconds { get; set; } = 0;

        #endregion
    }
}
