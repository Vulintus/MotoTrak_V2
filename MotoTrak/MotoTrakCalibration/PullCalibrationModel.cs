using MotoTrakBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakCalibration
{
    /// <summary>
    /// Model class for doing pull calibrations
    /// </summary>
    public class PullCalibrationModel
    {
        #region Public data members

        public List<PullWeightModel> TestWeights = new List<PullWeightModel>();
        public MotorDevice PullDevice = null;
        
        #endregion

        #region Singleton Constructor

        private static PullCalibrationModel _instance = null;

        /// <summary>
        /// Standard constructor
        /// </summary>
        private PullCalibrationModel()
        {
            InitializeTestWeights();
        }

        /// <summary>
        /// Gets the singleton instance of the pull calibration model class
        /// </summary>
        public static PullCalibrationModel GetInstance ()
        {
            if (_instance == null)
            {
                _instance = new PullCalibrationModel();
            }

            return _instance;
        }

        #endregion

        #region Private methods

        private void InitializeTestWeights()
        {
            //Get all possible weights
            var all_weights = Enum.GetValues(typeof(PullWeight)).Cast<PullWeight>().ToList();

            //Add each weight to the array
            foreach (var w in all_weights)
            {
                PullWeightModel new_weight = new PullWeightModel
                {
                    Weight = w,
                    IsVoice = true
                };

                TestWeights.Add(new_weight);
            }

            //Sort the list, just to be sure
            TestWeights = TestWeights.OrderBy(x => PullWeightConverter.ConvertFromEnumeratedValueToNumerical(x.Weight)).ToList();
        }

        #endregion
    }
}
