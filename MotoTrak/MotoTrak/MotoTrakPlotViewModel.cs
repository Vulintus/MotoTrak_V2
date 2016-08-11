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
using OxyPlot.Annotations;

namespace MotoTrak
{
    /// <summary>
    /// A class that encapsulates functionality of a MotoTrak plot
    /// </summary>
    public class MotoTrakPlotViewModel : NotifyPropertyChangedObject
    {
        #region Private members

        private enum SeriesType
        {
            ScatterSeries,
            AreaSeries
        }

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

            //Initialize the plot
            InitializePlot();

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

        /// <summary>
        /// The plot model
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
            if (StreamIndex < Model.MonitoredSignal.Count)
            {
                if (inside)
                {
                    foreach (var a in Plot.Annotations)
                    {
                        TextAnnotation text_a = a as TextAnnotation;
                        if (text_a != null)
                        {
                            text_a.TextColor = OxyColor.FromArgb(255, 0, 0, 0);
                        }
                    }
                }
                else
                {
                    foreach (var a in Plot.Annotations)
                    {
                        TextAnnotation text_a = a as TextAnnotation;
                        if (text_a != null)
                        {
                            text_a.TextColor = OxyColor.FromArgb(0, 0, 0, 0);
                        }
                    }
                }
            }
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

        private void InitializePlot()
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
            x_axis.Position = AxisPosition.Bottom;
            x_axis.Minimum = 0;
            x_axis.MinimumRange = 500;

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
        
        private void SelectPlot ()
        {
            if (StreamIndex < Model.MonitoredSignal.Count)
            {
                var k = GetPlotSeries(SeriesType.AreaSeries);
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
                    //First, let's create the vertical line annotation
                    LineAnnotation trial_event_line = new LineAnnotation();
                    trial_event_line.Type = LineAnnotationType.Vertical;
                    trial_event_line.LineStyle = LineStyle.Solid;
                    trial_event_line.StrokeThickness = 2;
                    trial_event_line.Color = OxyColor.FromRgb(255, 0, 0);
                    trial_event_line.X = trial_event.Item2;

                    var y_axis = Plot.Axes.Where(a => a.Position == AxisPosition.Left).FirstOrDefault();
                    if (y_axis != null)
                    {
                        trial_event_line.MinimumY = y_axis.AbsoluteMinimum;
                        trial_event_line.MaximumY = y_axis.AbsoluteMaximum;
                    }

                    //Add the line annotation to the set of annotations
                    Plot.Annotations.Add(trial_event_line);

                    //Now create the text annotation for this event
                    TextAnnotation trial_event_text_annotation = new TextAnnotation();
                    trial_event_text_annotation.Text = MotorTrialEventTypeConverter.ConvertToDescription(trial_event.Item1);
                    trial_event_text_annotation.TextPosition = new DataPoint(trial_event.Item2 - 2, 0);
                    trial_event_text_annotation.TextColor = OxyColor.FromArgb(0, 0, 0, 0);
                    trial_event_text_annotation.FontSize = 10;
                    trial_event_text_annotation.StrokeThickness = 0;
                    trial_event_text_annotation.TextHorizontalAlignment = HorizontalAlignment.Left;
                    trial_event_text_annotation.TextRotation = -90;

                    //Add the text annotation to the plot
                    Plot.Annotations.Add(trial_event_text_annotation);

                    //Invalidate the current plot so it gets updated in the GUI
                    Plot.InvalidatePlot(true);
                }
            }
        }

        private void DrawTrialAnnotations ()
        {
            if (Model.CurrentTrial != null)
            {
                //In this case, a trial has been initiated

                //If there are previous annotations still being displayed, let's clear them
                if (Plot.Annotations.Count > 0)
                {
                    Plot.Annotations.Clear();
                }

                //Next, we need to create vertical line annotations for the point of the trial
                //initiation and the end of the hit window
                int x_pos_of_trial_initiation = Model.CurrentSession.SelectedStage.TotalRecordedSamplesBeforeHitWindow;
                int x_pos_of_hit_window_end = Model.CurrentSession.SelectedStage.TotalRecordedSamplesBeforeHitWindow +
                    Model.CurrentSession.SelectedStage.TotalRecordedSamplesDuringHitWindow;

                //Set up lines annotations around the hit window
                LineAnnotation start_line = new LineAnnotation();
                start_line.Type = LineAnnotationType.Vertical;
                start_line.LineStyle = LineStyle.Solid;
                start_line.StrokeThickness = 2;
                start_line.Color = OxyColor.FromRgb(0, 0, 0);
                start_line.X = x_pos_of_trial_initiation;

                LineAnnotation end_line = new LineAnnotation();
                end_line.Type = LineAnnotationType.Vertical;
                end_line.LineStyle = LineStyle.Solid;
                end_line.StrokeThickness = 2;
                end_line.Color = OxyColor.FromRgb(0, 0, 0);
                end_line.X = x_pos_of_hit_window_end;

                Plot.Annotations.Add(start_line);
                Plot.Annotations.Add(end_line);

                //Now, let's iterate over each stage parameter and create horizontal line annotations
                foreach (var sp_key in Model.CurrentSession.SelectedStage.StageParameters.Keys)
                {
                    //Get the stage parameter itself
                    var sp = Model.CurrentSession.SelectedStage.StageParameters[sp_key];

                    //Create a horizontal line annotation for the value of this parameter.
                    LineAnnotation parameter_annotation = new LineAnnotation();
                    parameter_annotation.Type = LineAnnotationType.Horizontal;
                    parameter_annotation.LineStyle = LineStyle.Dash;
                    parameter_annotation.StrokeThickness = 2;
                    parameter_annotation.Color = OxyColor.FromRgb(0, 0, 0);
                    parameter_annotation.Y = sp.CurrentValue;
                    parameter_annotation.MinimumX = x_pos_of_trial_initiation;
                    parameter_annotation.MaximumX = x_pos_of_hit_window_end;

                    Plot.Annotations.Add(parameter_annotation);

                    //We create a text annotation that is positioned very close to the line annotation
                    //This text annotation is invisible by default, but because visible when the user hovers the mouse over
                    //the plot.  The text of the annotation is the name of the parameter, and allows the user to see how
                    //each parameter is plotted as a line annotation.
                    TextAnnotation parameter_name_annotation = new TextAnnotation();
                    parameter_name_annotation.Text = sp_key;
                    parameter_name_annotation.TextPosition = new DataPoint(x_pos_of_trial_initiation + 5, sp.CurrentValue);
                    parameter_name_annotation.TextColor = OxyColor.FromArgb(0, 0, 0, 0);
                    parameter_name_annotation.FontSize = 10;
                    parameter_name_annotation.StrokeThickness = 0;
                    parameter_name_annotation.TextHorizontalAlignment = HorizontalAlignment.Left;

                    Plot.Annotations.Add(parameter_name_annotation);
                }
            }
            else
            {
                //In this case, no trial is currently happening.
                //If no trial is happening, we need to delete all annotations from the plot.
                Plot.Annotations.Clear();

                //Invalidate the plot so that it gets updated in the GUI
                Plot.InvalidatePlot(true);
            }
        }

        private void DrawStreamedData ()
        {
            if (StreamIndex >= 0 && StreamIndex < Model.MonitoredSignal.Count)
            {
                //Copy over the data from the stream that is currently being displayed
                var datapoints = Model.MonitoredSignal[StreamIndex].Select((y_val, x_val) =>
                    new DataPoint(x_val + 1, y_val)).ToList();

                //Set the x-axis limit
                LinearAxis x_axis = Plot.Axes.FirstOrDefault(x => x.Position == AxisPosition.Bottom) as LinearAxis;
                if (x_axis != null)
                {
                    x_axis.MinimumRange = 500;
                }

                //Grab the first AreaSeries that is on the plot
                var s = GetPlotSeries(SeriesType.AreaSeries) as AreaSeries;
                if (s != null)
                {
                    //Clear the points in the dataset
                    s.Points.Clear();

                    //Add the new set of datapoints
                    s.Points.AddRange(datapoints);
                }
            }

            //Invalidate the plot so it is updated on screen
            Plot.InvalidatePlot(true);
        }

        private void DrawSessionOverviewPlot()
        {
            if (StreamIndex == Model.MonitoredSignal.Count)
            {
                //Get the points that need to be plotted
                var datapoints = Model.SessionOverviewValues.Select(x => new ScatterPoint(x.Item1, x.Item2)).ToList();

                var successful_trials = Model.SessionOverviewValues.Where(t => t.Item3 == true).ToList();
                var successful_datapoints = successful_trials.Select(x => new ScatterPoint(x.Item1, x.Item2)).ToList();

                var failed_trials = Model.SessionOverviewValues.Where(t => t.Item3 == false).ToList();
                var failed_datapoints = failed_trials.Select(x => new ScatterPoint(x.Item1, x.Item2)).ToList();

                //Set the x-axis limit
                LinearAxis x_axis = Plot.Axes.FirstOrDefault(x => x.Position == AxisPosition.Bottom) as LinearAxis;
                if (x_axis != null)
                {
                    x_axis.MinimumRange = 10;
                }

                List<ScatterSeries> s = SetupSessionOverviewPlot();
                s[0].Points.AddRange(successful_datapoints);
                s[1].Points.AddRange(failed_datapoints);

                //Invalidate the plot so it is updated on screen
                Plot.InvalidatePlot(true);
            }
        }

        private List<ScatterSeries> SetupSessionOverviewPlot ()
        {
            bool create_series = false;

            if (Plot.Series.Count >= 2)
            {
                if (!(Plot.Series[0] is ScatterSeries) || !(Plot.Series[1] is ScatterSeries))
                {
                    create_series = true;
                }
            }
            else
            {
                create_series = true;
            }

            if (create_series)
            {
                Plot.Series.Clear();

                var scatter1 = new ScatterSeries()
                {
                    MarkerType = MarkerType.Triangle,
                    MarkerStroke = OxyColor.FromRgb(0, 255, 0),
                    MarkerFill = OxyColor.FromRgb(128, 255, 128)
                };

                var scatter2 = new ScatterSeries()
                {
                    MarkerType = MarkerType.Triangle,
                    MarkerStroke = OxyColor.FromRgb(255, 0, 0),
                    MarkerFill = OxyColor.FromRgb(255, 128, 128)
                };

                Plot.Series.Add(scatter1);
                Plot.Series.Add(scatter2);
            }

            return new List<ScatterSeries>() { Plot.Series[0] as ScatterSeries, Plot.Series[1] as ScatterSeries };
        }

        private Series GetPlotSeries ( SeriesType series_type )
        {
            var first_series = Plot.Series.FirstOrDefault();

            switch (series_type)
            {
                case SeriesType.AreaSeries:

                    if (first_series is AreaSeries)
                    {
                        return first_series;
                    }
                    else
                    {
                        Plot.Series.Clear();
                        Plot.Series.Add(new AreaSeries());
                        return Plot.Series.FirstOrDefault();
                    }

                    break;
                case SeriesType.ScatterSeries:
                    
                    if (first_series is ScatterSeries)
                    {
                        return first_series;
                    }
                    else
                    {
                        var scatter = new ScatterSeries()
                        {
                            MarkerType = MarkerType.Triangle,
                            MarkerStroke = OxyColor.FromRgb(255, 0, 0),
                            MarkerFill = OxyColor.FromRgb(255, 128, 128)
                        };
                        
                        Plot.Series.Clear();
                        Plot.Series.Add(scatter);
                        return Plot.Series.FirstOrDefault();
                    }

                    break;
            }

            return first_series;
        }

        #endregion
    }
}
