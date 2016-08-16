using MotoTrakBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Forms.DataVisualization.Charting;

namespace MotoTrak
{
    /// <summary>
    /// A class that encapsulates functionality of a MotoTrak plot
    /// </summary>
    public class MotoTrakPlotViewModel_WinForms : NotifyPropertyChangedObject
    {
        #region Winforms charting tools

        private Chart _winforms_plot = null;

        public Chart WinFormsPlot
        {
            get
            {
                return _winforms_plot;
            }
            set
            {
                _winforms_plot = value;
            }
        }
        
        #endregion

        #region Private members

        private enum SeriesType
        {
            ScatterSeries,
            AreaSeries
        }
        
        private MotoTrakModel _model = null;
        private int _stream_index = -1;

        #endregion

        #region Constructors

        public MotoTrakPlotViewModel_WinForms (MotoTrakModel model, int stream_index)
        {
            //Set the MotoTrak model
            Model = model;

            //Subscribe to events from the model
            Model.PropertyChanged += ExecuteReactionsToModelPropertyChanged;

            //Initialize the plot
            //InitializePlot();

            //Set which stream we will be reading from for this plot
            StreamIndex = stream_index;
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

        #endregion

        #region Properties

        #endregion

        #region Properties and methods accessed by the GUI that modify the plot

        /// <summary>
        /// The index into the streams
        /// </summary>
        public int StreamIndex
        {
            get
            {
                return _stream_index;
            }
            set
            {
                _stream_index = value;
                SelectPlot();
            }
        }

        /// <summary>
        /// This function is called whenever the user hovers the mouse over the plot.
        /// Currently, if there is an active trial, we will display some annotations
        /// pertinent to the currently active trial while the mouse is being hovered.
        /// </summary>
        /// <param name="inside">Whether the mouse is inside or outside the plot</param>
        public void HandleMouseHover(bool inside)
        {
        }

        #endregion

        #region Function to listen to changes in the model that could modify the plot

        protected override void ExecuteReactionsToModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("CurrentTrial"))
            {
                //If the "CurrentTrial" property has changed, then the property has either been set or unset.
                //If it gets set to a new object, that means a new trial has begun.  If, on the other hand,
                //the object is null, that means a trial is not currently taking place.
                DrawTrialAnnotations();
            }
            else if (e.PropertyName.Equals("TrialEventsQueue"))
            {
                //If we enter this portion of the if-statement, it indicates that some kind of event has occurred within
                //the trial that is currently executing.

                //We need to process any new events that have occurred and create line annotations of them on the graph.
                //This is very similar to the process of creating line annotations for stage parameters, except that
                //these will be vertical line annotations instead of horizontal line annotations.
                //We will also create text annotations for these, just like we did for stage parameters.
                AddTrialEventAnnotations();
            }
            else if (e.PropertyName.Equals("MonitoredSignal"))
            {
                //Update the plot signal
                DrawStreamedData();
            }
            else if (e.PropertyName.Equals("SessionOverviewValues"))
            {
                //Update the session overview plot
                DrawSessionOverviewPlot();
            }

            //Call the base function to handle anything else
            base.ExecuteReactionsToModelPropertyChanged(sender, e);
        }


        #endregion

        #region Private methods

        public void InitializePlot()
        {
            var chart_area = WinFormsPlot.ChartAreas.FirstOrDefault();
            if (chart_area != null)
            {
                chart_area.AxisY.LabelStyle.Enabled = false;
                chart_area.AxisY.MajorGrid.Enabled = false;
                chart_area.AxisX.MajorTickMark.Enabled = false;
                chart_area.AxisX.MajorGrid.Enabled = false;
                //WinFormsPlot.Update();
            }
        }

        private void SelectPlot()
        {
            if (StreamIndex < Model.MonitoredSignal.Count)
            {
                //var k = GetPlotSeries(SeriesType.AreaSeries);
                DrawStreamedData();
                DrawTrialAnnotations();
                AddTrialEventAnnotations();
            }
            else if (StreamIndex == Model.MonitoredSignal.Count)
            {
                DrawSessionOverviewPlot();
            }
            else
            {

            }
        }
        
        private void AddTrialEventAnnotations ()
        {
            while (!Model.TrialEventsQueue.IsEmpty)
            {
                //Process each event found in the queue
                Tuple<MotorTrialEventType, int> trial_event = null;
                bool success = Model.TrialEventsQueue.TryDequeue(out trial_event);
                if (success)
                {
                    
                }
            }
        }
        
        private void DrawTrialAnnotations ()
        {
            
        }
        
        private void DrawStreamedData ()
        {
            if (StreamIndex >= 0 && StreamIndex < Model.MonitoredSignal.Count)
            {
                var area_series = WinFormsPlot.Series.FirstOrDefault();
                if (area_series == null)
                {
                    //If a series doesn't already exist, let's create one
                    area_series = new System.Windows.Forms.DataVisualization.Charting.Series()
                    {
                        Name = "Signal",
                        ChartType = SeriesChartType.Area,
                        Color = System.Drawing.Color.FromArgb(186, 215, 157),
                        BorderColor = System.Drawing.Color.FromArgb(100, 166, 37),
                        BorderDashStyle = ChartDashStyle.Solid,
                        BorderWidth = 2
                    };

                    //Add the series to the plot
                    WinFormsPlot.Series.Add(area_series);
                }

                //Add the points to the series and databind
                area_series.Points.DataBindY(Model.MonitoredSignal[StreamIndex]);
            }
        }

        private void DrawSessionOverviewPlot()
        {

        }
        
        private Series GetPlotSeries ( SeriesType series_type )
        {
            return null;    
        }
        
        #endregion
    }
}
