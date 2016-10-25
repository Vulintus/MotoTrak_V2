using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakCalibration
{
    /// <summary>
    /// Converter class for pull weight enumerated type
    /// </summary>
    public static class PullWeightConverter
    {
        /// <summary>
        /// Returns the enumerated type value corresponding to to a weight as a number
        /// </summary>
        public static PullWeight ConvertFromNumericalToEnumeratedValue (int w)
        {
            var type = typeof(PullWeight);

            foreach (var field in type.GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(field,
                    typeof(NumericalWeightAttribute)) as NumericalWeightAttribute;
                if (attribute != null)
                {
                    if (attribute.Weight == w)
                        return (PullWeight)field.GetValue(null);
                }
            }

            return PullWeight.Grams_0;
        }

        /// <summary>
        /// Returns the enumerated type value corresponding with a weight in string format
        /// </summary>
        public static PullWeight ConvertFromStringToEnumeratedValue (string description)
        {
            var type = typeof(PullWeight);

            foreach (var field in type.GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (attribute != null)
                {
                    if (attribute.Description == description)
                        return (PullWeight)field.GetValue(null);
                }
                else
                {
                    if (field.Name == description)
                        return (PullWeight)field.GetValue(null);
                }
            }

            return PullWeight.Grams_0;
        }

        /// <summary>
        /// Returns the numerical weight associated with an enumerated type's value
        /// </summary>
        public static int ConvertFromEnumeratedValueToNumerical (PullWeight w)
        {
            int result = 0;

            FieldInfo fi = w.GetType().GetField(w.ToString());

            NumericalWeightAttribute[] attributes =
                (NumericalWeightAttribute[])fi.GetCustomAttributes(
                typeof(NumericalWeightAttribute),
                false);

            if (attributes != null && attributes.Length > 0)
                result = attributes[0].Weight;
            
            return result;
        }

        /// <summary>
        /// Returns the string description of an enumerated type's value
        /// </summary>
        public static string ConvertFromEnumeratedValueToString (PullWeight w)
        {
            FieldInfo fi = w.GetType().GetField(w.ToString());

            DescriptionAttribute[] attributes =
                (DescriptionAttribute[])fi.GetCustomAttributes(
                typeof(DescriptionAttribute),
                false);

            if (attributes != null &&
                attributes.Length > 0)
                return attributes[0].Description;
            else
                return w.ToString();
        }
    }
}
