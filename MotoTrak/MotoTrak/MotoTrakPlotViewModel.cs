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
using MotoTrakUtilities;
using System.Diagnostics;

namespace MotoTrak
{
    /// <summary>
    /// A class that encapsulates functionality of a MotoTrak plot
    /// </summary>
    public class MotoTrakPlotViewModel : NotifyPropertyChangedObject
    {
        #region Private members
        
        private enum AnnotationFlag
        {
            DisplayAlways,
            DisplayOnActiveTrial
        }

        private PlotModel _plot_model = null;
        private MotoTrakModel _model = null;
        private bool _is_plotting_enabled = true;
        private int _stream_index = -1;

        private Stopwatch _plotting_timer = new Stopwatch();
        private Stopwatch _plot_time_gui_update = new Stopwatch();
        private List<long> _plotting_times = new List<long>();
        

        #endregion

        #region Constructors

        public MotoTrakPlotViewModel ( MotoTrakModel model, int stream_index )
        {
            //Create a new plot model for this plot.
            Plot = new PlotModel();
            Plot.Updating += OnPlotUpdating;
            Plot.Updated += OnPlotUpdated;
            _plot_time_gui_update.Start();

            //Set the MotoTrak model
            Model = model;

            //Subscribe to events from the model
            Model.PropertyChanged += ExecuteReactionsToModelPropertyChanged;
            if (Model.CurrentSession != null)
            {
                Model.CurrentSession.PropertyChanged += ExecuteReactionsToModelPropertyChanged;
            }
            
            //Initialize the plot
            InitializePlot();

            //Set which stream we will be reading from for this plot
            StreamIndex = stream_index;
        }

        private void OnPlotUpdated(object sender, EventArgs e)
        {
            //Stop the plotting timer
            _plotting_timer.Stop();

            //Grab the number of milliseconds that elapsed since plotting began
            long millis = _plotting_timer.ElapsedMilliseconds;

            //Store that value in a list
            _plotting_times.Add(millis);

            //Make sure we only keep the latest 1000 plotting times
            if (_plotting_times.Count > 1000)
            {
                //Remove the oldest plotting time if the number of stored times exceeds 1000 elements
                _plotting_times.RemoveAt(0);
            }

            //Check to see if we shuold update the debugging variable displayed in the GUI
            if (_plot_time_gui_update.ElapsedMilliseconds >= 1000)
            {
                //Restart the gui update timer
                _plot_time_gui_update.Restart();

                //Tell the GUI to refresh the average plotting time in the debugging information section
                NotifyPropertyChanged("AveragePlottingTime");
            }
        }

        private void OnPlotUpdating(object sender, EventArgs e)
        {
            //Restart the timer to track how long it takes to plot.
            _plotting_timer.Restart();
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

        /// <summary>
        /// Returns the average time taken to plot on the GUI
        /// </summary>
        public string AveragePlottingTime
        {
            get
            {
                if (_plotting_times.Count > 0)
                {
                    return _plotting_times.Average().ToString("###0.0");
                }
                else
                {
                    return "NaN";
                }
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
            if (_is_plotting_enabled)
            {
                if (e.PropertyName.Equals("CurrentSession"))
                {
                    if (Model.CurrentSession != null)
                    {
                        Model.CurrentSession.PropertyChanged += ExecuteReactionsToModelPropertyChanged;
                    }
                }
                if (e.PropertyName.Equals("SelectedStage"))
                {
                    RunAnnotationLogic();
                }
                else if (e.PropertyName.Equals("SessionState"))
                {
                    RunAnnotationLogic();
                }
                else if (e.PropertyName.Equals("TrialState"))
                {
                    RunAnnotationLogic();
                }
                else if (e.PropertyName.Equals("CurrentTrial"))
                {
                    //If the "CurrentTrial" property has changed, then the property has either been set or unset.
                    //If it gets set to a new object, that means a new trial has begun.  If, on the other hand,
                    //the object is null, that means a trial is not currently taking place.
                    ScaleXAxis();
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
            }
            
            //Call the base function to handle anything else
            base.ExecuteReactionsToModelPropertyChanged(sender, e);
        }
        
        #endregion

        #region Basic setup and selection methods used

        /// <summary>
        /// InitializePlot instantiates axes for the plot.  This must be called
        /// at the beginning of the program and is necessary for any plotting to occur.
        /// </summary>
        private void InitializePlot()
        {
            //Create a y-axis for the plot
            LinearAxis y_axis = new LinearAxis();
            y_axis.Position = AxisPosition.Left;

            //Create an x-axis for the plot
            LinearAxis x_axis = new LinearAxis();
            x_axis.Position = AxisPosition.Bottom;
            
            //Add the axes to the plot model
            Plot.Axes.Clear();
            Plot.Axes.Add(y_axis);
            Plot.Axes.Add(x_axis);
        }
        
        /// <summary>
        /// SelectPlot is called any time the plot in MotoTrak is changed by the user.
        /// This method then calls the setup code for the respective plot and then
        /// draws the plot itself.
        /// </summary>
        private void SelectPlot ()
        {
            if (Model != null && Model.CurrentSession != null && Model.CurrentSession.SelectedStage != null)
            {
                if (StreamIndex < Model.CurrentSession.SelectedStage.DataStreamTypes.Count)
                {
                    //Set up the plot for streaming signal data
                    SetupStreamedDataPlot();

                    //Draw the streaming signal data
                    DrawStreamedData();

                    //Draw annotations
                    RunAnnotationLogic();

                    //Add annotations that are due to events occuring during a trial
                    AddTrialEventAnnotations();
                }
                else if (StreamIndex == Model.CurrentSession.SelectedStage.DataStreamTypes.Count)
                {
                    //Set up the session overview plot.
                    SetupSessionOverviewPlot();

                    //Plot the datapoints for the session overview
                    DrawSessionOverviewPlot();
                }
                else if (StreamIndex == Model.CurrentSession.SelectedStage.DataStreamTypes.Count + 1)
                {
                    //This is the recent performance history plot
                    DrawRecentPerformancePlot();
                }
                else
                {

                }
            }
        }

        #endregion

        #region Methods for drawing performance data from recent MotoTrak sessions

        private void DrawRecentPerformancePlot ()
        {
            Plot.Series.Clear();

            LineSeries total_trial_series = new LineSeries();
            LineSeries total_successful_trial_series = new LineSeries();

            List<MotoTrakSession> recent_sessions = MotoTrakModel.GetInstance().RecentBehaviorSessions;
            int x = 0;
            int max_y = 0;
            if (recent_sessions != null)
            {
                foreach (var s in recent_sessions)
                {
                    //Fetch the total trial count and the successful trial count from each previous session that is loaded into memory
                    int total_trials = s.Trials.Count;
                    int successful_trials = s.Trials.Where(t => t.Result == MotorTrialResult.Hit).ToList().Count;

                    //Add these points to the line series
                    total_trial_series.Points.Add(new DataPoint(x+1, total_trials));
                    total_successful_trial_series.Points.Add(new DataPoint(x+1, successful_trials));

                    //Update the maximal y value
                    max_y = Math.Max(max_y, total_trials);

                    //Increment the x-value for the series
                    x++;
                }
            }

            //Name each series
            total_trial_series.Title = "Total trials";
            total_trial_series.RenderInLegend = true;

            total_successful_trial_series.Title = "Total successful trials";
            total_successful_trial_series.RenderInLegend = true;

            //Set the color of each line
            total_trial_series.Color = OxyColor.FromRgb(0, 0, 255);
            total_successful_trial_series.Color = OxyColor.FromRgb(0, 180, 0);

            //Add the series to the plot
            Plot.Series.Add(total_trial_series);
            Plot.Series.Add(total_successful_trial_series);

            //Scale the x-axis correctly
            LinearAxis x_axis = Plot.Axes.FirstOrDefault(y => y.Position == AxisPosition.Bottom) as LinearAxis;
            if (x_axis != null)
            {
                x_axis.Minimum = 0;
                x_axis.Maximum = x;
                x_axis.MaximumRange = x+1;
                x_axis.MinimumRange = x+1;
            }

            //Scale the y-axis correctly
            LinearAxis y_axis = Plot.Axes.FirstOrDefault(y => y.Position == AxisPosition.Left) as LinearAxis;
            if (y_axis != null)
            {
                y_axis.Minimum = 0;
                y_axis.Maximum = max_y;
                y_axis.MinimumRange = max_y+1;
                y_axis.MaximumRange = max_y+1;
            }

            //Make sure to show the legend
            Plot.IsLegendVisible = true;
            Plot.LegendPlacement = LegendPlacement.Inside;
            Plot.LegendPosition = LegendPosition.TopRight;
            
            //Refresh the plot
            Plot.InvalidatePlot(true);
        }

        #endregion

        #region Session overview plot functions

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
                    //Set the x-axis limits to encompass how many trials exist
                    x_axis.Minimum = 0;
                    x_axis.MinimumRange = 10;
                    x_axis.Maximum = Math.Max(Model.CurrentSession.Trials.Count + 1, x_axis.MinimumRange);
                    x_axis.MaximumRange = Model.CurrentSession.Trials.Count + 1;
                }

                //Grab all of the scatter plot series
                List<ScatterSeries> s = Plot.Series.Select(x => x as ScatterSeries).ToList();

                //Add the new datapoints to each series
                s[0].Points.AddRange(successful_datapoints);
                s[1].Points.AddRange(failed_datapoints);

                //Invalidate the plot so it is updated on screen
                Plot.InvalidatePlot(true);
            }
        }
        
        private void SetupSessionOverviewPlot ()
        {
            //Clear the current set of series on the plot
            Plot.Series.Clear();
            
            //Create a scatter plot series that will be used for successful trials
            //This series is green in color
            var scatter1 = new ScatterSeries()
            {
                MarkerType = MarkerType.Triangle,
                MarkerStroke = OxyColor.FromRgb(0, 255, 0),
                MarkerFill = OxyColor.FromRgb(128, 255, 128)
            };

            //Create another scatter plot series that will be used for failed trials
            //This series is red in color
            var scatter2 = new ScatterSeries()
            {
                MarkerType = MarkerType.Triangle,
                MarkerStroke = OxyColor.FromRgb(255, 0, 0),
                MarkerFill = OxyColor.FromRgb(255, 128, 128)
            };

            //Add both series to the plot
            Plot.Series.Add(scatter1);
            Plot.Series.Add(scatter2);

            //Set the x-axis and y-axis limits for the session overview plot
            LinearAxis x_axis = Plot.Axes.FirstOrDefault(x => x.Position == AxisPosition.Bottom) as LinearAxis;
            LinearAxis y_axis = Plot.Axes.FirstOrDefault(x => x.Position == AxisPosition.Left) as LinearAxis;
            if (x_axis != null && y_axis != null)
            {
                var model = MotoTrakModel.GetInstance();
                if (model.CurrentSession != null)
                {
                    //Set the x-axis limits to encompass how many trials exist
                    x_axis.Minimum = 0;
                    x_axis.MinimumRange = 10;
                    x_axis.Maximum = Math.Max(model.CurrentSession.Trials.Count + 1, x_axis.MinimumRange);
                    x_axis.MaximumRange = model.CurrentSession.Trials.Count + 1;

                    //Remove y-axis limits
                    y_axis.Minimum = double.NaN;
                    y_axis.MinimumRange = double.NaN;
                    y_axis.Maximum = double.NaN;
                    y_axis.MaximumRange = double.NaN;
                }
            }
        }

        #endregion

        #region Plot functions for streaming signal data

        /// <summary>
        /// This method is called when the user selects any of the "streaming signals" from the dropdown box to view in the plot.
        /// It sets up the plot in such a manner that they can easily view the signals both during a trial and not during a trial.
        /// </summary>
        private void SetupStreamedDataPlot ()
        {
            //Clear whatever series are currently on the plot
            Plot.Series.Clear();

            //Turn off the legend
            Plot.IsLegendVisible = false;

            //Add a new Area series for the signal
            Plot.Series.Add(new AreaSeries());

            //Get the current MotoTrak model
            var model = MotoTrakModel.GetInstance();

            //Set the x-axis and y-axis properties based on the selected stage for the current session
            if (model.CurrentSession != null && model.CurrentSession.SelectedStage != null)
            {
                //Set up the y-axis properties
                var y_axis = Plot.Axes.FirstOrDefault(x => x.Position == AxisPosition.Left) as LinearAxis;

                //Set up the x-axis properties
                var x_axis = Plot.Axes.FirstOrDefault(x => x.Position == AxisPosition.Bottom) as LinearAxis;
                x_axis.Minimum = 0;
                x_axis.Maximum = Model.CurrentSession.SelectedStage.TotalRecordedSamplesPerTrial;
                x_axis.MinimumRange = x_axis.Maximum;
                x_axis.MaximumRange = x_axis.Maximum;
            }

            //Invalidate the plot so that it is refreshed
            Plot.InvalidatePlot(true);
        }

        /// <summary>
        /// This method plots the streaming signal data.
        /// </summary>
        private void DrawStreamedData()
        {
            if (StreamIndex >= 0 && StreamIndex < Model.MonitoredSignal.Count)
            {
                //Copy over the data from the stream that is currently being displayed
                var datapoints = Model.MonitoredSignal[StreamIndex].Select((y_val, x_val) =>
                    new DataPoint(x_val + 1, y_val)).ToList();
                
                //Grab the first AreaSeries that is on the plot
                var s = Plot.Series.FirstOrDefault() as AreaSeries;
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

        private void RunAnnotationLogic ()
        {
            ClearAllAnnotations();
            DrawAnnotations_AlwaysOn();
            DrawAnnotations_DisplayDuringTrial();
        }

        /// <summary>
        /// Clears all annotations on the plot
        /// </summary>
        private void ClearAllAnnotations ()
        {
            Plot.Annotations.Clear();
            Plot.InvalidatePlot(true);
        }

        private void DrawAnnotations_AlwaysOn ()
        {
            //Calculate x-axis positions for the annotations
            int x_pos_of_trial_initiation = 0;
            int x_pos_of_hit_window_end = 0;
            if (Model != null && Model.CurrentSession != null && Model.CurrentSession.SelectedStage != null)
            {
                if (Model.TrialState == MotoTrakModel.TrialRunState.TrialRun)
                {
                    x_pos_of_trial_initiation = Model.CurrentSession.SelectedStage.TotalRecordedSamplesBeforeHitWindow;
                    x_pos_of_hit_window_end = Model.CurrentSession.SelectedStage.TotalRecordedSamplesBeforeHitWindow +
                        Model.CurrentSession.SelectedStage.TotalRecordedSamplesDuringHitWindow;
                }
                else
                {
                    x_pos_of_hit_window_end = Model.CurrentSession.SelectedStage.TotalRecordedSamplesPerTrial;
                }
            }
            
            //Now, let's iterate over each stage parameter and create horizontal line annotations
            foreach (var sp_key in Model.CurrentSession.SelectedStage.StageParameters.Keys)
            {
                //Get the stage parameter itself
                var sp = Model.CurrentSession.SelectedStage.StageParameters[sp_key];

                //Now check to see if this stage parameter should be added as an annotation
                var stage_impl = Model.CurrentSession.SelectedStage.StageImplementation as PythonStageImplementation;
                if (stage_impl != null)
                {
                    var parameter_to_plot = stage_impl.TaskDefinition.TaskParameters.Where(x => x.ParameterName.Equals(sp_key)).FirstOrDefault();
                    if (parameter_to_plot == null || !parameter_to_plot.DisplayOnPlot)
                    {
                        //If the stage implementation says to not create an annotation for this specific parameter,
                        //then we will tell the loop to continue with its next iteration.
                        //Otherwise, the code below this if-statement will execute and an annotation will be created.
                        continue;
                    }
                }

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

            //Update the plot
            Plot.InvalidatePlot(true);
        }

        private void DrawAnnotations_DisplayDuringTrial ()
        {
            if (Model != null && Model.CurrentTrial != null && Model.TrialState == MotoTrakModel.TrialRunState.TrialRun)
            {
                //In this case, a trial has been initiated
                
                //We need to create vertical line annotations for the point of the trial
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
            }

            //Update the plot
            Plot.InvalidatePlot(true);
        }

        /// <summary>
        /// This method is called during a trial.
        /// It adds annotations to the plot that represent events that have occured during the trial itself,
        /// such as a "hit" or any other event the user would like to see represented in the trial plot.
        /// </summary>
        private void AddTrialEventAnnotations()
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
        
        /// <summary>
        /// This method scales the y-axis of the streaming signal plot so that it is within the appropriate
        /// range to show the data being presented.
        /// </summary>
        public void ScaleYAxis()
        {
            //Figure out the proper scale
            if (Model != null && Model.CurrentSession != null && Model.CurrentSession.SelectedStage != null)
            {
                List<double> values = new List<double>();
                values.Add(0); //Make sure the baseline of 0 is in the values array
                var keys = Model.CurrentSession.SelectedStage.StageParameters.Keys;

                foreach (var k in keys)
                {
                    var sp = Model.CurrentSession.SelectedStage.StageParameters[k];

                    //Now check to see if this stage parameter is a "plotted" parameter
                    var stage_impl = Model.CurrentSession.SelectedStage.StageImplementation as PythonStageImplementation;
                    if (stage_impl != null)
                    {
                        var task_parameter = stage_impl.TaskDefinition.TaskParameters.Where(x => x.ParameterName.Equals(sp.ParameterName)).FirstOrDefault();
                        if (task_parameter == null || !task_parameter.DisplayOnPlot)
                        {
                            //If the stage implementation says to not create an annotation for this specific parameter,
                            //then we will tell the loop to continue with its next iteration.
                            //Otherwise, the code below this if-statement will execute and an annotation will be created.
                            continue;
                        }
                    }

                    values.Add(sp.MinimumValue);
                    values.Add(sp.MaximumValue);
                    values.Add(sp.InitialValue);
                    values.Add(sp.CurrentValue);
                }

                var ymin = MotorMath.NanMin(values);
                var ymax = MotorMath.NanMax(values);
                var yrange = Math.Abs(ymax - ymin) * 1.2;  //Multiply the range by 120% to extend it a bit

                //Get the y-axis object
                var y_axis = Plot.Axes.Where(x => x.Position == AxisPosition.Left).FirstOrDefault();
                if (y_axis != null)
                {
                    //Set the minimum range of the y-axis object
                    //We do NOT set the maximum range in this instance (we want the maximum to be able to scale upwards
                    //in case the data has very large values).
                    y_axis.MinimumRange = yrange;
                    y_axis.AbsoluteMinimum = -10;
                }

                Plot.InvalidatePlot(true);
            }
        }

        /// <summary>
        /// This method scales the x-axis of the streaming signal data plot so it shows the appropriate
        /// number of data points for a trial.
        /// </summary>
        public void ScaleXAxis()
        {
            //Figure out the proper scale
            if (Model != null && Model.CurrentSession != null && Model.CurrentSession.SelectedStage != null)
            {
                //Get the x-axis object
                var x_axis = Plot.Axes.Where(x => x.Position == AxisPosition.Bottom).FirstOrDefault();
                if (x_axis != null)
                {
                    //Set the range of the x-axis
                    x_axis.Minimum = 1;
                    x_axis.Maximum = Model.CurrentSession.SelectedStage.TotalRecordedSamplesPerTrial;
                    x_axis.MinimumRange = Model.CurrentSession.SelectedStage.TotalRecordedSamplesPerTrial;
                    x_axis.MaximumRange = x_axis.MinimumRange;
                    
                    //Invalidate the plot to update the range
                    Plot.InvalidatePlot(true);
                }
            }
        }

        #endregion

        #region Methods to manually turn on/off plotting and subscribing to model events

        /// <summary>
        /// This function will cause plotting in MotoTrak to be turned on or off.
        /// This function is typically called when a window resize/move event is either
        /// begun or completed.  While a window resize/move event is taking place, we
        /// don't plot because it slows down the computer during the event.
        /// </summary>
        /// <param name="is_plotting_enabled">Whether or not to enable plotting</param>
        public void SetPlotting (bool is_plotting_enabled)
        {
            _is_plotting_enabled = is_plotting_enabled;

            if (is_plotting_enabled)
            {
                //Remove the handlers if they already are set
                //This insures that we don't add a handler multiple times
                Model.PropertyChanged -= ExecuteReactionsToModelPropertyChanged;
                if (Model.CurrentSession != null)
                {
                    Model.CurrentSession.PropertyChanged -= ExecuteReactionsToModelPropertyChanged;
                }

                //Now add the handlers
                Model.PropertyChanged += ExecuteReactionsToModelPropertyChanged;
                if (Model.CurrentSession != null)
                {
                    Model.CurrentSession.PropertyChanged += ExecuteReactionsToModelPropertyChanged;
                }
            }
            else
            {
                //Remove the handlers
                Model.PropertyChanged -= ExecuteReactionsToModelPropertyChanged;
                if (Model.CurrentSession != null)
                {
                    Model.CurrentSession.PropertyChanged -= ExecuteReactionsToModelPropertyChanged;
                }
            }
        }

        #endregion
    }
}
