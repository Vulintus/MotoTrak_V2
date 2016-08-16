using System;
using System.ComponentModel;
using System.Reflection;

namespace MotoTrakBase
{
    /// <summary>
    /// This class is meant to convert a string description of a motor device into an enumerated type, and vice versa.
    /// </summary>
    public static class MotorDeviceTypeConverter
    {
        /// <summary>
        /// Converts a string to a motor device type.
        /// </summary>
        /// <param name="description">String description of a motor device</param>
        /// <returns>Motor device type</returns>
        public static MotorDeviceType ConvertToMotorDeviceType(string description)
        {
            var type = typeof(MotorDeviceType);

            foreach (var field in type.GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (attribute != null)
                {
                    if (attribute.Description.Equals(description, StringComparison.InvariantCultureIgnoreCase))
                        return (MotorDeviceType)field.GetValue(null);
                }
                else
                {
                    if (field.Name.Equals(description, StringComparison.InvariantCultureIgnoreCase))
                        return (MotorDeviceType)field.GetValue(null);
                }
            }

            return MotorDeviceType.Unknown;
        }

        /// <summary>
        /// Converts a motor device type to a string description.
        /// </summary>
        /// <param name="thresholdType">Motor device type</param>
        /// <returns>String description of the motor device type.</returns>
        public static string ConvertToDescription(MotorDeviceType thresholdType)
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

        /// <summary>
        /// Retrieves the units string associated with a device type.
        /// </summary>
        /// <param name="thresholdType">The type of device</param>
        /// <returns>The units that the device uses</returns>
        public static string ConvertToUnitsDescription(MotorDeviceType thresholdType)
        {
            FieldInfo fi = thresholdType.GetType().GetField(thresholdType.ToString());

            UnitsAttribute[] attributes = (UnitsAttribute[])fi.GetCustomAttributes(typeof(UnitsAttribute), false);
            if (attributes != null && attributes.Length > 0)
            {
                return attributes[0].UnitsDescription;
            }
            else
            {
                return thresholdType.ToString();
            }
        }
    }
}
