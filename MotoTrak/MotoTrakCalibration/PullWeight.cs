using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakCalibration
{
    /// <summary>
    /// An enumeration describing possible weights the user can choose from to calibrate the pull handle
    /// </summary>
    public enum PullWeight
    {
        [Description("0 gm (Re-baseline)")]
        [NumericalWeight(0)]
        Grams_0,

        [Description("10 gm")]
        [NumericalWeight(10)]
        Grams_10,

        [Description("20 gm")]
        [NumericalWeight(20)]
        Grams_20,

        [Description("50 gm")]
        [NumericalWeight(50)]
        Grams_50,

        [Description("90 gm")]
        [NumericalWeight(90)]
        Grams_90,

        [Description("100 gm")]
        [NumericalWeight(100)]
        Grams_100,

        [Description("130 gm")]
        [NumericalWeight(130)]
        Grams_130,

        [Description("170 gm")]
        [NumericalWeight(170)]
        Grams_170,

        [Description("200 gm")]
        [NumericalWeight(200)]
        Grams_200,

        [Description("210 gm")]
        [NumericalWeight(210)]
        Grams_210,

        [Description("250 gm")]
        [NumericalWeight(250)]
        Grams_250
    }
}
