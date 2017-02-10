using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakBase
{
    [AttributeUsage(AttributeTargets.All)]
    public class MotoTrak_V1_SpreadsheetColumnHeadingAttribute : System.Attribute
    {
        public string[] SpreadsheetColumnHeading = null;

        public MotoTrak_V1_SpreadsheetColumnHeadingAttribute(string [] headings)
        {
            SpreadsheetColumnHeading = headings;
        }
    }
}
