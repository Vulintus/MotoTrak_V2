using MotoTrakBase;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakCalibration
{
    public class KnobCalibrationViewModel : BaseCalibrationViewModel
    {
        #region Private data members

        private PlotModel _knob_plot_model = new PlotModel();
        private SimpleCommand _knob_rebaseline_command;

        #endregion

        #region Constructor

        /// <summary>
        /// The constructor
        /// </summary>
        public KnobCalibrationViewModel(string booth_name, string com_port, MotorDevice device)
            : base(booth_name, com_port, device)
        {
            //Listen to changes on the device stream
            DeviceStreamModel.GetInstance().PropertyChanged += OnDeviceStreamPropertyChanged;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The plot model for the knob calibration plot
        /// </summary>
        public PlotModel KnobPlotModel
        {
            get
            {
                return _knob_plot_model;
            }
            set
            {
                _knob_plot_model = value;
                NotifyPropertyChanged("KnobPlotModel");
            }
        }

        /// <summary>
        /// The command to reset the baseline for the knob device
        /// </summary>
        public SimpleCommand KnobResetBaselineCommand
        {
            get
            {
                return _knob_rebaseline_command ?? (_knob_rebaseline_command = new SimpleCommand(() => KnobResetBaseline(), true));
            }
        }

        #endregion

        #region Method to react to device stream updates

        private void OnDeviceStreamPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //This function is called any time the device stream reports new data in the buffer

            //Grab the data
            try
            {
                var buffer_data = DeviceStreamModel.GetInstance().DataBuffer.Select((y_val, x_val) =>
                new DataPoint(x_val + 1, y_val)).ToList();

                //Update the load cell plot
                UpdateKnobPlot(buffer_data);
            }
            catch (Exception err)
            {
                ErrorLoggingService.GetInstance().LogExceptionError(err);
            }
        }

        #endregion

        #region Private methods

        private void UpdateKnobPlot (List<DataPoint> buffer_data)
        {
            //Grab the first AreaSeries that is on the plot
            var s = KnobPlotModel.Series.FirstOrDefault() as AreaSeries;
            if (s != null)
            {
                //Clear the points in the dataset
                s.Points.Clear();

                //Add the new set of datapoints
                s.Points.AddRange(buffer_data);

                //Invalidate the plot so it updates
                KnobPlotModel.InvalidatePlot(true);
            }
        }

        private void InitializeKnobPlot ()
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
                Title = "Ticks",
                TitleFontWeight = OxyPlot.FontWeights.Bold,
                TitleFontSize = 14
            };

            //Add the axes and the area series to the plot model
            KnobPlotModel.Series.Add(load_cell_data);
            KnobPlotModel.Axes.Add(x_axis);
            KnobPlotModel.Axes.Add(y_axis);
        }

        #endregion

        #region Command Methods

        /// <summary>
        /// Resets the baseline for the device
        /// </summary>
        public void KnobResetBaseline ()
        {
            //Grab the data
            try
            {
                var buffer_data = DeviceStreamModel.GetInstance().DataBuffer.LastOrDefault();

                if (!Double.IsNaN(buffer_data))
                {
                    int new_baseline = Convert.ToInt32(Math.Round(buffer_data));
                    MotorBoard.GetInstance().SetBaseline(new_baseline);
                    DeviceModel.Baseline = new_baseline;
                    NotifyPropertyChanged("BaselineValue");
                }
            }
            catch (Exception err)
            {
                ErrorLoggingService.GetInstance().LogExceptionError(err);
            }
        }

        #endregion
    }
}
