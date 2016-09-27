using System;
using System.ComponentModel;
using System.Reflection;

namespace MotoTrakBase
{
    /// <summary>
    /// This static class exists for converting string descriptions of trial event types to values of an enumerated type and vice versa.
    /// </summary>
    public static class MotorTrialEventTypeConverter
    {
        /// <summary>
        /// Converts a string to an event type.
        /// </summary>
        /// <param name="description">String description of an event type</param>
        /// <returns>Event type</returns>
        public static MotorTrialEventType ConvertToMotorStageStimulationType(string description)
        {
            var type = typeof(MotorTrialEventType);

            foreach (var field in type.GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (attribute != null)
                {
                    if (attribute.Description == description)
                        return (MotorTrialEventType)field.GetValue(null);
                }
                else
                {
                    if (field.Name == description)
                        return (MotorTrialEventType)field.GetValue(null);
                }
            }

            return MotorTrialEventType.UndefinedEvent;
        }

        /// <summary>
        /// Converts an event type to a string description.
        /// </summary>
        /// <param name="thresholdType">Event type</param>
        /// <returns>String description of the event type.</returns>
        public static string ConvertToDescription(MotorTrialEventType thresholdType)
        {
            FieldInfo fi = thresholdType.GetType().GetField(thresholdType.ToString());

            DescriptionAttribute[] attributes =
                (DescriptionAttribute[])fi.GetCustomAttributes(
                typeof(DescriptionAttribute),
                false);

            if (attributes != null &&
                attributes.Length > 0)
                return attributes[0].Description;
            else
                return thresholdType.ToString();
        }

        public static bool AreMultipleEventsAllowed ( MotorTrialEventType eventType )
        {
            FieldInfo fi = eventType.GetType().GetField(eventType.ToString());
            MultipleEventsAllowedAttribute[] attributes = (MultipleEventsAllowedAttribute[])fi.GetCustomAttributes(
                typeof(MultipleEventsAllowedAttribute), false);
            if (attributes != null && attributes.Length > 0)
                return attributes[0].AllowMultipleEvents;
            else
                return false;
        }
    }
}
