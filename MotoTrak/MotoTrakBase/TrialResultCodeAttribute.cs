using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakBase
{
    /// <summary>
    /// A simple class that can be used as an attribute for a trial result code
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public class TrialResultCodeAttribute : System.Attribute
    {
        public Byte ResultCode = 0;

        public TrialResultCodeAttribute(Byte r)
        {
            ResultCode = r;
        }
    }
}
