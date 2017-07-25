using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakBase
{
    [AttributeUsage(AttributeTargets.All)]
    public class MotoTrak_V1_SpreadsheetColumnValueAttribute : System.Attribute
    {
        public string[] SpreadsheetColumnValue = null;

        public MotoTrak_V1_SpreadsheetColumnValueAttribute(string[] values)
        {
            SpreadsheetColumnValue = values;
        }
    }
}
