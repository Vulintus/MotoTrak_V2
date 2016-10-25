using MotoTrakBase;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakCalibration
{
    /// <summary>
    /// A view-model class for calibrating the pull handle
    /// </summary>
    public class PullCalibrationViewModel : BaseCalibrationViewModel
    {
        #region Private data members

        PullCalibrationModel _model = null;
        PlotModel _calibration_plot_model = new PlotModel();
        PlotModel _loadcell_plot_model = new PlotModel();

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public PullCalibrationViewModel (string booth_name, string com_port, MotorDevice device, PullCalibrationModel model)
            : base(booth_name, com_port, device)
        {
            //Set the model object
            Model = model;

            //Initialize plots
            InitializeLoadCellPlot();
            InitializeCalibrationPlot();

            //Listen to events from the device stream
            DeviceStreamModel.GetInstance().PropertyChanged += OnDeviceStreamUpdated;
        }

        #endregion
        
        #region Private Methods

        private void InitializeLoadCellPlot ()
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
                TitleFontWeight = FontWeights.Bold,
                TitleFontSize = 14
            };

            //Add the axes and the area series to the plot model
            LoadCellPlotModel.Series.Add(load_cell_data);
            LoadCellPlotModel.Axes.Add(x_axis);
            LoadCellPlotModel.Axes.Add(y_axis);
        }

        private void InitializeCalibrationPlot ()
        {
            //Create two axes
            LinearAxis x_axis = new LinearAxis()
            {
                Position = AxisPosition.Bottom,
                Minimum = 0,
                Maximum = 800,
                MinimumRange = 800,
                MaximumRange = 800,
                Title = "Loadcell Reading (ticks)",
                TitleFontSize = 14,
                TitleFontWeight = FontWeights.Bold
            };

            //Calculate the y-axis limits
            double y_min = DeviceModel.Slope * (0 - DeviceModel.Baseline);
            double y_max = DeviceModel.Slope * (800 - DeviceModel.Baseline);

            LinearAxis y_axis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Minimum = y_min,
                Maximum = y_max,
                Title = "Force (gm)",
                TitleFontWeight = FontWeights.Bold,
                TitleFontSize = 14
            };

            //Assign the axes to the calibration plot
            CalibrationPlotModel.Axes.Add(x_axis);
            CalibrationPlotModel.Axes.Add(y_axis);
        }

        private void UpdateCalibrationPlot (double calibrated_value, double raw_value)
        {
            //Clear all annotations on the plot
            CalibrationPlotModel.Annotations.Clear();
            
            string calibrated_value_string = Convert.ToInt32(Math.Round(calibrated_value)).ToString() + " g";
            string raw_value_string = Convert.ToInt32(Math.Round(raw_value)).ToString();

            //Calculate the y-axis limits
            double y_min = DeviceModel.Slope * (0 - DeviceModel.Baseline);
            double y_max = DeviceModel.Slope * (800 - DeviceModel.Baseline);

            //Draw lines indicating the current calibrated value and current load cell value
            LineAnnotation annotation1 = new LineAnnotation()
            {
                Type = LineAnnotationType.Horizontal,
                Y = calibrated_value,
                MinimumX = 0,
                MaximumX = raw_value,
                Color = OxyColors.Red,
                LineStyle = LineStyle.Solid,
                LineJoin = LineJoin.Round,
                StrokeThickness = 3,
            };

            LineAnnotation annotation2 = new LineAnnotation()
            {
                Type = LineAnnotationType.Vertical,
                X = raw_value,
                MinimumY = y_min,
                MaximumY = calibrated_value,
                Color = OxyColors.Red,
                LineStyle = LineStyle.Solid,
                LineJoin = LineJoin.Round,
                StrokeThickness = 3,
            };
            
            LineAnnotation annotation_fixed_line = new LineAnnotation()
            {
                Type = LineAnnotationType.LinearEquation,
                Intercept = y_min,
                Slope = DeviceModel.Slope,
                LineStyle = LineStyle.Dash,
                Color = OxyColors.Black
            };

            LineAnnotation zero_force_line = new LineAnnotation()
            {
                Type = LineAnnotationType.Horizontal,
                Y = 0,
                MinimumX = 0,
                MaximumX = 800,
                LineStyle = LineStyle.Solid,
                StrokeThickness = 3,
                Color = OxyColors.DarkBlue
            };

            LineAnnotation raw_baseline_line = new LineAnnotation()
            {
                Type = LineAnnotationType.Vertical,
                X = DeviceModel.Baseline,
                MinimumY = y_min,
                MaximumY = y_max,
                LineStyle = LineStyle.Solid,
                StrokeThickness = 3,
                Color = OxyColors.DarkBlue
            };

            //Now for some text annotations
            TextAnnotation text1 = new TextAnnotation()
            {
                Background = OxyColors.White,
                StrokeThickness = 2,
                Stroke = OxyColors.Red,
                Text = calibrated_value_string,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                TextPosition = new DataPoint(50, calibrated_value)
            };

            TextAnnotation text2 = new TextAnnotation()
            {
                Background = OxyColors.White,
                StrokeThickness = 2,
                Stroke = OxyColors.Red,
                Text = raw_value_string,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                TextPosition = new DataPoint(raw_value, y_min),
            };
            
            //Add the new annotations to the plot model
            CalibrationPlotModel.Annotations.Add(annotation1);
            CalibrationPlotModel.Annotations.Add(annotation2);
            CalibrationPlotModel.Annotations.Add(annotation_fixed_line);
            CalibrationPlotModel.Annotations.Add(zero_force_line);
            CalibrationPlotModel.Annotations.Add(raw_baseline_line);
            CalibrationPlotModel.Annotations.Add(text1);
            CalibrationPlotModel.Annotations.Add(text2);

            //Invalid the plot model to plot everything
            CalibrationPlotModel.InvalidatePlot(true);
        }

        private void UpdateLoadCellPlot ( List<DataPoint> buffer_data )
        {
            //Grab the first AreaSeries that is on the plot
            var s = LoadCellPlotModel.Series.FirstOrDefault() as AreaSeries;
            if (s != null)
            {
                //Clear the points in the dataset
                s.Points.Clear();

                //Add the new set of datapoints
                s.Points.AddRange(buffer_data);

                //Invalidate the plot so it updates
                LoadCellPlotModel.InvalidatePlot(true);
            }
        }
        
        private void OnDeviceStreamUpdated(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //This function is called any time the device stream reports new data in the buffer

            //Grab the data
            var buffer_data = DeviceStreamModel.GetInstance().DataBuffer.Select((y_val, x_val) =>
                new DataPoint(x_val + 1, y_val)).ToList();

            //Update the load cell plot
            UpdateLoadCellPlot(buffer_data);

            //Get the latest raw value
            var latest = buffer_data.Skip(DeviceStreamModel.GetInstance().BufferSize - 50).Select(x => x.Y).ToList();
            double latest_raw = latest.Average();
            double latest_calibrated = DeviceModel.Slope * (latest_raw - DeviceModel.Baseline);

            //Update the calibration plot
            UpdateCalibrationPlot(latest_calibrated, latest_raw);
        }

        #endregion

        #region Public data members

        /// <summary>
        /// The plot model for the load cell plot
        /// </summary>
        public PlotModel LoadCellPlotModel
        {
            get
            {
                return _loadcell_plot_model;
            }
            set
            {
                _loadcell_plot_model = value;
                NotifyPropertyChanged("LoadCellPlotModel");
            }
        }
            
        /// <summary>
        /// The plot model for the calibration values
        /// </summary>
        public PlotModel CalibrationPlotModel
        {
            get
            {
                return _calibration_plot_model;
            }
            set
            {
                _calibration_plot_model = value;
                NotifyPropertyChanged("CalibrationPlotModel");
            }
        }

        /// <summary>
        /// The pull calibration model
        /// </summary>
        public PullCalibrationModel Model
        {
            get
            {
                return _model;
            }
            set
            {
                _model = value;
            }
        }

        /// <summary>
        /// Returns a set of view-model classes for all the test-weights
        /// </summary>
        public List<PullWeightViewModel> WeightOptions
        {
            get
            {
                return Model.TestWeights.Select(x => new PullWeightViewModel(x)).ToList();
            }
        }

        #endregion
    }
}
