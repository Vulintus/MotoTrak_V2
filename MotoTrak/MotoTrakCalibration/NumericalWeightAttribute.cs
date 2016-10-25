using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakCalibration
{
    /// <summary>
    /// An attribute class for the PullWeights enumeration to allow tagging each enumerated value with a numerical weight.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public class NumericalWeightAttribute : System.Attribute
    {
        public int Weight = 0;

        public NumericalWeightAttribute(int w)
        {
            Weight = w;
        }
    }
}
