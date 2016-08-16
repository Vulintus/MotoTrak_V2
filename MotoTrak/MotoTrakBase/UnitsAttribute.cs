using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakBase
{
    /// <summary>
    /// An attribute class to describe the kind of units that something uses, whether it be a device or a datastream
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public class UnitsAttribute : System.Attribute
    {
        public string UnitsDescription = string.Empty;

        public UnitsAttribute(string units)
        {
            UnitsDescription = units;
        }
    }
}
