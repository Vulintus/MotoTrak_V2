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
        [Description("0 g (Re-baseline)")]
        [NumericalWeight(0)]
        Grams_0,

        [Description("10 g")]
        [NumericalWeight(10)]
        Grams_10,

        [Description("20 g")]
        [NumericalWeight(20)]
        Grams_20,

        [Description("50 g")]
        [NumericalWeight(50)]
        Grams_50,

        [Description("90 g")]
        [NumericalWeight(90)]
        Grams_90,

        [Description("100 g")]
        [NumericalWeight(100)]
        Grams_100,

        [Description("130 g")]
        [NumericalWeight(130)]
        Grams_130,

        [Description("170 g")]
        [NumericalWeight(170)]
        Grams_170,

        [Description("200 g")]
        [NumericalWeight(200)]
        Grams_200,

        [Description("210 g")]
        [NumericalWeight(210)]
        Grams_210,

        [Description("250 g")]
        [NumericalWeight(250)]
        Grams_250
    }
}
