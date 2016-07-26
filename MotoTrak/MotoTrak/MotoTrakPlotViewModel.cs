using MotoTrakBase;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace MotoTrak
{
    /// <summary>
    /// A class that encapsulates functionality of a MotoTrak plot
    /// </summary>
    public class MotoTrakPlotViewModel : NotifyPropertyChangedObject
    {
        #region Private members

        private PlotModel _plot_model = null;
        private MotoTrakModel _model = null;
        private int _stream_index = -1;

        #endregion

        #region Constructors

        public MotoTrakPlotViewModel ( MotoTrakModel model, int stream_index )
        {
            //Create a new plot model for this plot.
            Plot = new PlotModel();

            //Set the MotoTrak model
            Model = model;

            //Subscribe to events from the model
            Model.PropertyChanged += ExecuteReactionsToModelPropertyChanged;

            //Set which stream we will be reading from for this plot
            StreamIndex = stream_index;
        }

        private void Model_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Private properties

        /// <summary>
        /// The MotoTrak model object
        /// </summary>
        private MotoTrakModel Model
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
        /// The index into the streams
        /// </summary>
        private int StreamIndex
        {
            get
            {
                return _stream_index;
            }
            set
            {
                _stream_index = value;
                UpdatePlotProperties();
            }
        }

        #endregion

        #region Private methods

        protected override void ExecuteReactionsToModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("MonitoredSignal"))
            {
                //Update the plot signal
                UpdatePlotSignal();
            }
            
            //Call the base function to handle anything else
            base.ExecuteReactionsToModelPropertyChanged(sender, e);
        }

        private void UpdatePlotSignal ()
        {
            //Copy over the data from the stream that is currently being displayed
            var datapoints = Model.MonitoredSignal[StreamIndex].Select((y_val, x_val) =>
                new DataPoint(x_val, y_val)).ToList();

            //Grab the first AreaSeries that is on the plot
            var s = Plot.Series[0] as AreaSeries;
            if (s != null)
            {
                //Clear the points in the dataset
                s.Points.Clear();
                
                //Add the new set of datapoints
                s.Points.AddRange(datapoints);
            }

            //Invalidate the plot so it is updated on screen
            Plot.InvalidatePlot(true);
        }

        private void UpdatePlotProperties ()
        {
            //Grab the stream description from the model
            //This will be a tuple.
            //Item1 = description of stream
            //Item2 = units of stream
            //var stream_description = Model.CurrentSession.StreamDescriptions[this.StreamIndex];

            //Set the title on the plot model
            //Plot.Title = stream_description.Item1;

            //Create a y-axis for this plot
            LinearAxis y_axis = new LinearAxis();
            y_axis.Position = AxisPosition.Left;

            //Set the y-axis title to be the units for the stream
            //y_axis.Title = stream_description.Item2;

            //Create an x-axis for this plot
            LinearAxis x_axis = new LinearAxis();
            x_axis.Position = AxisPosition.Right;

            //Add the axes to the plot model
            Plot.Axes.Clear();
            Plot.Axes.Add(y_axis);
            Plot.Axes.Add(x_axis);

            //Add an area series to the plot model
            AreaSeries plot_data = new AreaSeries();
            Plot.Series.Clear();
            Plot.Series.Add(plot_data);

            //Invalidate the plot
            Plot.InvalidatePlot(true);
        }

        #endregion

        #region Properties

        /// <summary>
        /// The model for the plot of this stream
        /// </summary>
        public PlotModel Plot
        {
            get
            {
                return _plot_model;
            }
            set
            {
                _plot_model = value;
            }
        }

        #endregion
    }
}
