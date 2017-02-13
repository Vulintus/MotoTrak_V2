using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakBase
{
    /// <summary>
    /// Converts MotoTrak V1 stage parameters to string descriptions and vice-versa.
    /// </summary>
    public static class MotoTrak_V1_StageParameters_Converter
    {
        /// <summary>
        /// Converts a string to an MotoTrak V1 stage parameter type
        /// </summary>
        /// <param name="description">String description of an stage parameter</param>
        /// <returns>Stage parameter (MotoTrak V1)</returns>
        public static MotoTrak_V1_StageParameters ConvertToMotorStageParameterType(string description)
        {
            var type = typeof(MotoTrak_V1_StageParameters);

            foreach (var field in type.GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(field,
                    typeof(MotoTrak_V1_SpreadsheetColumnHeadingAttribute)) as MotoTrak_V1_SpreadsheetColumnHeadingAttribute;
                if (attribute != null)
                {
                    if (attribute.SpreadsheetColumnHeading != null)
                    {
                        foreach (var a in attribute.SpreadsheetColumnHeading)
                        {
                            if (description.Trim().Equals(a.Trim(), StringComparison.InvariantCultureIgnoreCase))
                            {
                                return (MotoTrak_V1_StageParameters)field.GetValue(null);
                            }
                        }
                    }
                }
            }

            return MotoTrak_V1_StageParameters.Description;
        }

        /// <summary>
        /// Converts an stage parameter description to a stage parameter type (MotoTrak V1)
        /// </summary>
        /// <param name="param_type">Description of stage parameter type</param>
        /// <returns>String description of the stage parameter type.</returns>
        public static string ConvertToDescription(MotoTrak_V1_StageParameters param_type)
        {
            FieldInfo fi = param_type.GetType().GetField(param_type.ToString());

            MotoTrak_V1_SpreadsheetColumnHeadingAttribute[] attributes =
                (MotoTrak_V1_SpreadsheetColumnHeadingAttribute[])fi.GetCustomAttributes(
                typeof(MotoTrak_V1_SpreadsheetColumnHeadingAttribute),
                false);

            if (attributes != null &&
                attributes.Length > 0 &&
                attributes[0].SpreadsheetColumnHeading != null)
                return attributes[0].SpreadsheetColumnHeading[0];
            else
                return param_type.ToString();
        }
    }
}
