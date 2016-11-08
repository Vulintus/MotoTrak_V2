using MotoTrakBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace MotoTrakCalibration
{
    /// <summary>
    /// View-model class for an individual pull weight
    /// </summary>
    public class PullWeightViewModel : NotifyPropertyChangedObject
    {
        #region Private data members

        private PullCalibrationModel _calibration_model = PullCalibrationModel.GetInstance();
        private PullWeightModel _model = null;
        private SimpleCommand _weightSkipButtonCommand;
        private SimpleCommand _weightAmountCommand;

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public PullWeightViewModel (PullWeightModel model)
        {
            Model = model;

            //Listen for changes on the calibration model
            _calibration_model.PropertyChanged += _calibration_model_PropertyChanged;
        }
        
        #endregion

        #region Public members

        /// <summary>
        /// The model
        /// </summary>
        public PullWeightModel Model
        {
            get
            {
                return _model;
            }
            set
            {
                _model = value;
                NotifyPropertyChanged("Model");
            }
        }

        /// <summary>
        /// The weight amount that is shown on the button
        /// </summary>
        public string WeightAmount
        {
            get
            {
                return PullWeightConverter.ConvertFromEnumeratedValueToString(Model.Weight);
            }
        }

        /// <summary>
        /// Indicates whether this weight uses the voice or is skipped
        /// </summary>
        public string Skipping
        {
            get
            {
                bool is_voice = Model.IsVoice;
                if (!is_voice)
                {
                    return "SKIP";
                }
                else
                {
                    return "VOICE";
                }
            }
        }

        /// <summary>
        /// The color of the text on the voice/skip button
        /// </summary>
        public SolidColorBrush SkipButtonColor
        {
            get
            {
                if (_calibration_model.IsRunningCalibration)
                {
                    return new SolidColorBrush(Color.FromArgb(0xFF, 0xAD, 0xAD, 0xAD));
                }
                else
                {
                    bool is_voice = Model.IsVoice;
                    if (!is_voice)
                    {
                        return new SolidColorBrush(Colors.Red);
                    }
                    else
                    {
                        return new SolidColorBrush(Colors.Green);
                    }
                }
            }
        }
        
        /// <summary>
        /// A command to toggle skipping of the weight or not
        /// </summary>
        public SimpleCommand WeightSkipButtonCommand
        {
            get
            {
                return _weightSkipButtonCommand ?? (_weightSkipButtonCommand = new SimpleCommand(() => ToggleWeightSkip(), true));
            }
        }

        /// <summary>
        /// A command to run the weight calibration
        /// </summary>
        public SimpleCommand WeightAmountCommand
        {
            get
            {
                return _weightAmountCommand ?? (_weightAmountCommand = new SimpleCommand(() => RunWeightCalibration(), true));
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Toggles skipping of the weight
        /// </summary>
        public void ToggleWeightSkip ()
        {
            //Toggle the "is voice" field
            Model.IsVoice = !Model.IsVoice;

            //Notify property changes
            NotifyPropertyChanged("Skipping");
            NotifyPropertyChanged("SkipButtonColor");
        }

        /// <summary>
        /// Runs the weight calibration for this weight
        /// </summary>
        public void RunWeightCalibration ()
        {
            _calibration_model.RunCalibration(_model.Weight);
        }

        #endregion

        #region Method that listens for changes on the calibration model

        private void _calibration_model_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("IsRunningCalibration"))
            {
                NotifyPropertyChanged("SkipButtonColor");
            }
        }

        #endregion
    }
}
