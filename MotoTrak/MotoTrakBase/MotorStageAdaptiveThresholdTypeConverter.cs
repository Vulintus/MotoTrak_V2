using System;
using System.ComponentModel;
using System.Reflection;

namespace MotoTrakBase
{
    /// <summary>
    /// This static class is used for converting string descriptions of adaptive threshold types to enumerated values, and vice versa.
    /// </summary>
    public static class MotorStageAdaptiveThresholdTypeConverter
    {
        /// <summary>
        /// Converts a string to an adaptive threshold type.
        /// </summary>
        /// <param name="description">String description of an adaptive threshold type</param>
        /// <returns>Adaptive threshold type</returns>
        public static MotorStageAdaptiveThresholdType ConvertToMotorStageAdaptiveThresholdType(string description)
        {
            var type = typeof(MotorStageAdaptiveThresholdType);

            foreach (var field in type.GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (attribute != null)
                {
                    if (attribute.Description == description)
                        return (MotorStageAdaptiveThresholdType)field.GetValue(null);
                }
                else
                {
                    if (field.Name == description)
                        return (MotorStageAdaptiveThresholdType)field.GetValue(null);
                }
            }

            return MotorStageAdaptiveThresholdType.Undefined;
        }

        /// <summary>
        /// Converts an adaptive threshold type to a string description.
        /// </summary>
        /// <param name="thresholdType">Adaptive threshold type</param>
        /// <returns>String description of the adaptive threshold type.</returns>
        public static string ConvertToDescription(MotorStageAdaptiveThresholdType thresholdType)
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
