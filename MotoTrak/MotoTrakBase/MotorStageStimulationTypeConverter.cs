using System;
using System.ComponentModel;
using System.Reflection;

namespace MotoTrakBase
{
    /// <summary>
    /// This static class exists for converting string descriptions of stimulation types to values of an enumerated type and vice versa.
    /// </summary>
    public static class MotorStageStimulationTypeConverter
    {
        /// <summary>
        /// Converts a string to a stimulation type.
        /// </summary>
        /// <param name="description">String description of a stimulation type</param>
        /// <returns>Stimulation type</returns>
        public static MotorStageStimulationType ConvertToMotorStageStimulationType(string description)
        {
            var type = typeof(MotorStageStimulationType);

            foreach (var field in type.GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (attribute != null)
                {
                    if (String.Equals(attribute.Description, description, StringComparison.OrdinalIgnoreCase))
                        return (MotorStageStimulationType)field.GetValue(null);
                }
                else
                {
                    if (String.Equals(field.Name, description, StringComparison.OrdinalIgnoreCase))
                        return (MotorStageStimulationType)field.GetValue(null);
                }
            }

            return MotorStageStimulationType.Off;
        }

        /// <summary>
        /// Converts a stimulation type to a string description.
        /// </summary>
        /// <param name="thresholdType">Stimulation type</param>
        /// <returns>String description of the stimulation type.</returns>
        public static string ConvertToDescription(MotorStageStimulationType thresholdType)
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
    }
}
