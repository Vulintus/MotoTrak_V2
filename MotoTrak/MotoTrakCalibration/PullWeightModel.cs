using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakCalibration
{
    /// <summary>
    /// A model class for an individual weight to test
    /// </summary>
    public class PullWeightModel
    {
        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public PullWeightModel()
        {
            //empty
        }

        #endregion

        #region Fields

        public PullWeight Weight = PullWeight.Grams_0;
        public bool IsVoice = true;

        #endregion
    }
}
