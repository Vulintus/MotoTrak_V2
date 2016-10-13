using System;
using System.ComponentModel;
using System.Reflection;

namespace MotoTrakBase
{
    /// <summary>
    /// This static class is used for converting string descriptions of data stream types to enumerated values, and vice versa.
    /// </summary>
    public static class MotorBoardDataStreamTypeConverter
    {
        /// <summary>
        /// Converts a string to a data stream type.
        /// </summary>
        /// <param name="description">String description of a data stream type</param>
        /// <returns>data stream type</returns>
        public static MotorBoardDataStreamType ConvertToMotorBoardDataStreamType(string description)
        {
            var type = typeof(MotorBoardDataStreamType);

            foreach (var field in type.GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (attribute != null)
                {
                    if (attribute.Description == description)
                        return (MotorBoardDataStreamType)field.GetValue(null);
                }
                else
                {
                    if (field.Name == description)
                        return (MotorBoardDataStreamType)field.GetValue(null);
                }
            }

            return MotorBoardDataStreamType.Unknown;
        }

        /// <summary>
        /// Converts a data stream type to a string description.
        /// </summary>
        /// <param name="thresholdType">data stream type</param>
        /// <returns>String description of the data stream type.</returns>
        public static string ConvertToDescription(MotorBoardDataStreamType thresholdType)
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
        /// Retrieves the units string associated with a stream type.
        /// </summary>
        /// <param name="thresholdType">The type of data stream</param>
        /// <returns>The units that the data stream uses</returns>
        public static string ConvertToUnitsDescription(MotorBoardDataStreamType thresholdType)
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
