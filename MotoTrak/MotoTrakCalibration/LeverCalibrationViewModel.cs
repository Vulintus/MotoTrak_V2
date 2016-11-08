using MotoTrakBase;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace MotoTrakCalibration
{
    public class LeverCalibrationViewModel : BaseCalibrationViewModel
    {
        #region Private data members

        private LeverCalibrationModel _model = null;
        private PlotModel _lever_plot_model = new PlotModel();

        private SimpleCommand _lever_calibration_command;

        #endregion

        #region Constructor

        public LeverCalibrationViewModel(string booth_name, string com_port, MotorDevice device, LeverCalibrationModel model)
            : base(booth_name, com_port, device)
        {
            //Set the model
            Model = model;

            //Subscribe to changes from the model
            Model.PropertyChanged += OnLeverCalibrationModelPropertyChanged;

            //Initialize the lever plot
            InitializeLoadCellPlot();

            //Subcribe to changes on the device stream
            DeviceStreamModel.GetInstance().PropertyChanged += OnDeviceStreamUpdated;
        }
        
        #endregion

        #region Private methods

        private void UpdateLoadCellPlot(List<DataPoint> buffer_data)
        {
            //Grab the first AreaSeries that is on the plot
            var s = LeverPlotModel.Series.FirstOrDefault() as AreaSeries;
            if (s != null)
            {
                //Clear the points in the dataset
                s.Points.Clear();

                //Add the new set of datapoints
                s.Points.AddRange(buffer_data);

                //Invalidate the plot so it updates
                LeverPlotModel.InvalidatePlot(true);
            }
        }

        private void InitializeLoadCellPlot()
        {
            //Create an area series to plot the latest loadcell data
            AreaSeries load_cell_data = new AreaSeries()
            {
                Color = OxyColors.CornflowerBlue
            };

            //Create axes for the plot model
            LinearAxis x_axis = new LinearAxis()
            {
                Position = AxisPosition.Bottom,
                Minimum = 0,
                Maximum = 500,
                MinimumRange = 500,
                MaximumRange = 500,
                TickStyle = TickStyle.None,
                IsAxisVisible = false
            };

            LinearAxis y_axis = new LinearAxis()
            {
                Position = AxisPosition.Left,
                Minimum = 0,
                Maximum = 1024,
                MinimumRange = 1024,
                MaximumRange = 1024,
                Title = "Loadcell",
                TitleFontWeight = OxyPlot.FontWeights.Bold,
                TitleFontSize = 14
            };

            //Add the axes and the area series to the plot model
            LeverPlotModel.Series.Add(load_cell_data);
            LeverPlotModel.Axes.Add(x_axis);
            LeverPlotModel.Axes.Add(y_axis);
        }

        private void OnDeviceStreamUpdated(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //This function is called any time the device stream reports new data in the buffer

            //Grab the data
            try
            {
                var buffer_data = DeviceStreamModel.GetInstance().DataBuffer.Select((y_val, x_val) =>
                new DataPoint(x_val + 1, y_val)).ToList();

                //Update the load cell plot
                UpdateLoadCellPlot(buffer_data);
            }
            catch (Exception err)
            {
                ErrorLoggingService.GetInstance().LogExceptionError(err);
            }
        }

        private void OnLeverCalibrationModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("IsRunningCalibration"))
            {
                NotifyPropertyChanged("RecordingStatus");
                NotifyPropertyChanged("RecordingStatusColor");
            }
            else if (e.PropertyName.Equals("SavedMaxValue"))
            {
                NotifyPropertyChanged("CalibrationValues");
            }
            else if (e.PropertyName.Equals("SavedMinValue"))
            {
                NotifyPropertyChanged("CalibrationValues");
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// The lever calibration model
        /// </summary>
        public LeverCalibrationModel Model
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
        /// The plot model for the lever calibration program
        /// </summary>
        public PlotModel LeverPlotModel
        {
            get
            {
                return _lever_plot_model;
            }
            set
            {
                _lever_plot_model = value;
                NotifyPropertyChanged("CalibrationPlotModel");
            }
        }

        /// <summary>
        /// The current calibration values for the lever
        /// </summary>
        public string CalibrationValues
        {
            get
            {
                if (Model != null)
                {
                    string result = "Baseline = " + Convert.ToInt32(Model.SavedMaxValue).ToString() + ", Range = " +
                        Convert.ToInt32(Model.SavedMaxValue - Model.SavedMinValue).ToString();
                    return result;
                }

                return string.Empty;
            }
        }

        /// <summary>
        /// The current recording status for the lever calibration program
        /// </summary>
        public string RecordingStatus
        {
            get
            {
                if (Model != null)
                {
                    if (Model.IsRunningCalibration)
                    {
                        return "Stop recording and save calibration values";
                    }
                    else
                    {
                        return "Start calibration process";
                    }
                }

                return string.Empty;
            }
        }

        /// <summary>
        /// The color of the text in the calibration button
        /// </summary>
        public SolidColorBrush RecordingStatusColor
        {
            get
            {
                if (Model != null)
                {
                    if (Model.IsRunningCalibration)
                    {
                        return new SolidColorBrush(Colors.Red);
                    }
                    else
                    {
                        return new SolidColorBrush(Colors.Green);
                    }
                }

                return new SolidColorBrush(Colors.Green);
            }
        }

        /// <summary>
        /// The command to run the lever calibration process
        /// </summary>
        public SimpleCommand CalibrateLeverCommand
        {
            get
            {
                return _lever_calibration_command ?? (_lever_calibration_command = new SimpleCommand(() => CalibrateLever(), true));
            }
        }

        #endregion

        #region Commands functions

        private void CalibrateLever ()
        {
            if (Model.IsRunningCalibration)
            {
                Model.StopCalibration();
            }
            else
            {
                Model.RunCalibration();
            }
        }

        #endregion
    }
}
