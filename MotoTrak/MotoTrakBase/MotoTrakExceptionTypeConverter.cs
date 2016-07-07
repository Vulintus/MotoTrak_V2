using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakBase
{
    /// <summary>
    /// Converter class for MotoTrakExceptionType to convert the enum to a user-readable string, and vice versa.
    /// </summary>
    public static class MotoTrakExceptionTypeConverter
    {
        /// <summary>
        /// Converts a string to a MotoTrak exception type
        /// </summary>
        /// <param name="description">String description of a MotoTrak exception</param>
        /// <returns>Type of MotoTrak exception</returns>
        public static MotoTrakExceptionType ConvertToMotorStageAdaptiveThresholdType(string description)
        {
            var type = typeof(MotoTrakExceptionType);

            foreach (var field in type.GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (attribute != null)
                {
                    if (attribute.Description == description)
                        return (MotoTrakExceptionType)field.GetValue(null);
                }
                else
                {
                    if (field.Name == description)
                        return (MotoTrakExceptionType)field.GetValue(null);
                }
            }

            return MotoTrakExceptionType.Unknown;
        }

        /// <summary>
        /// Converts a MotoTrakExceptionType to a string description
        /// </summary>
        /// <param name="thresholdType">Exception type from a generated MotoTrak exception</param>
        /// <returns>String description of the exception</returns>
        public static string ConvertToDescription(MotoTrakExceptionType thresholdType)
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
