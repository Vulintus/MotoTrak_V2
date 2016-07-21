using System;
using System.ComponentModel;
using System.Reflection;

namespace MotoTrakBase
{
    /// <summary>
    /// This static class exists for converting string descriptions of hit thresholds to values of an enumerated type and vice versa.
    /// </summary>
    public static class MotorTaskTypeV1Converter
    {
        /// <summary>
        /// Converts a string to a hit threshold type.
        /// </summary>
        /// <param name="description">String description of a hit threshold type</param>
        /// <returns>Hit threshold type</returns>
        public static MotorTaskTypeV1 ConvertToMotorStageHitThresholdType(string description)
        {
            var type = typeof(MotorTaskTypeV1);

            foreach (var field in type.GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (attribute != null)
                {
                    if (attribute.Description == description)
                        return (MotorTaskTypeV1)field.GetValue(null);
                }
                else
                {
                    if (field.Name == description)
                        return (MotorTaskTypeV1)field.GetValue(null);
                }
            }

            return MotorTaskTypeV1.Undefined;
        }

        /// <summary>
        /// Converts a hit threshold type to a string description.
        /// </summary>
        /// <param name="thresholdType">Hit threshold type</param>
        /// <returns>String description of the hit threshold type.</returns>
        public static string ConvertToDescription(MotorTaskTypeV1 thresholdType)
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
