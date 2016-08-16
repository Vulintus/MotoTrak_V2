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
    /// Converts a MotoTrialResult to a string description or a result code
    /// </summary>
    public static class MotorTrialResultConverter 
    {
        /// <summary>
        /// Converts a string to a motor trial result
        /// </summary>
        /// <param name="description">String description of a trial result</param>
        /// <returns>Motor trial result</returns>
        public static MotorTrialResult ConvertToMotorStageAdaptiveThresholdType(string description)
        {
            var type = typeof(MotorTrialResult);

            foreach (var field in type.GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (attribute != null)
                {
                    if (attribute.Description == description)
                        return (MotorTrialResult)field.GetValue(null);
                }
                else
                {
                    if (field.Name == description)
                        return (MotorTrialResult)field.GetValue(null);
                }
            }

            return MotorTrialResult.Unknown;
        }

        /// <summary>
        /// Converts a trial result to a string description
        /// </summary>
        /// <param name="trial_result">Trial result</param>
        /// <returns>String description of the trial result.</returns>
        public static string ConvertToDescription(MotorTrialResult trial_result)
        {
            FieldInfo fi = trial_result.GetType().GetField(trial_result.ToString());

            DescriptionAttribute[] attributes =
                (DescriptionAttribute[])fi.GetCustomAttributes(
                typeof(DescriptionAttribute),
                false);

            if (attributes != null &&
                attributes.Length > 0)
                return attributes[0].Description;
            else
                return trial_result.ToString();
        }

        /// <summary>
        /// Retrieves the result code associated with a MotorTrialResult
        /// </summary>
        /// <param name="result_type">The type of result of a motor trial</param>
        /// <returns>The corresponding result code</returns>
        public static Byte ConvertToTrialResultCode(MotorTrialResult result_type)
        {
            FieldInfo fi = result_type.GetType().GetField(result_type.ToString());

            TrialResultCodeAttribute[] attributes = (TrialResultCodeAttribute[])fi.GetCustomAttributes(typeof(TrialResultCodeAttribute), false);
            if (attributes != null && attributes.Length > 0)
            {
                return attributes[0].ResultCode;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Converts a result code into a MotorTrialResult enumerated type
        /// </summary>
        /// <param name="result_code">The result code</param>
        /// <returns>a MotorTrialResult</returns>
        public static MotorTrialResult ConvertResultCodeToEnumeratedType (Byte result_code)
        {
            var type = typeof(MotorTrialResult);

            foreach (var field in type.GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(field,
                    typeof(TrialResultCodeAttribute)) as TrialResultCodeAttribute;
                if (attribute != null)
                {
                    if (attribute.ResultCode == result_code)
                        return (MotorTrialResult)field.GetValue(null);
                }
            }

            return MotorTrialResult.Unknown;
        }
    }
}
