using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakBase
{
    public class MotorStageParameterTone
    {
        #region Enumeration

        public enum ToneEventType
        {
            [Description("hit")]
            Hit,

            [Description("miss")]
            Miss,

            [Description("hitwin")]
            HitWindow,

            [Description("rising")]
            Rising,

            [Description("falling")]
            Falling,

            [Description("cue")]
            Cue,

            [Description("unknown")]
            Unknown
        }

        #endregion

        #region Tone event type converter

        public class ToneEventTypeConverter
        {
            /// <summary>
            /// Converts a string to a tone event type
            /// </summary>
            /// <param name="description">String description of a tone event type</param>
            /// <returns>The tone event type</returns>
            public static ToneEventType ConvertToToneEventType(string description)
            {
                var type = typeof(ToneEventType);

                foreach (var field in type.GetFields())
                {
                    var attribute = Attribute.GetCustomAttribute(field,
                        typeof(DescriptionAttribute)) as DescriptionAttribute;
                    if (attribute != null)
                    {
                        if (String.Equals(attribute.Description, description, StringComparison.OrdinalIgnoreCase))
                            return (ToneEventType)field.GetValue(null);
                    }
                    else
                    {
                        if (String.Equals(field.Name, description, StringComparison.OrdinalIgnoreCase))
                            return (ToneEventType)field.GetValue(null);
                    }
                }

                return ToneEventType.Unknown;
            }

            /// <summary>
            /// Converts a tone event type to a string description.
            /// </summary>
            /// <param name="tone_event_type">The tone event type</param>
            /// <returns>String description of the tone event type.</returns>
            public static string ConvertToDescription(ToneEventType tone_event_type)
            {
                FieldInfo fi = tone_event_type.GetType().GetField(tone_event_type.ToString());

                DescriptionAttribute[] attributes =
                    (DescriptionAttribute[])fi.GetCustomAttributes(
                    typeof(DescriptionAttribute),
                    false);

                if (attributes != null &&
                    attributes.Length > 0)
                    return attributes[0].Description;
                else
                    return tone_event_type.ToString();
            }
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
