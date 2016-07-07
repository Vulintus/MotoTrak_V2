using MotoTrakBase;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace MotoTrak
{
    /// <summary>
    /// View-model class that handles interactions between the MotoTrak session model and the GUI.
    /// </summary>
    public class SessionViewModel : NotifyPropertyChangedObject
    {
        #region Private data members

        MotoTrakSession _session = new MotoTrakSession();
        private List<string> _viewList = new List<string>() { "Realtime device signal", "Session overview", "Recent performance" };
        private int _viewSelectedIndex = 0;
        private bool _stageChangeRequired = false;

        #endregion

        #region Constructors

        public SessionViewModel()
        {
            SessionModel.PropertyChanged += SessionModel_PropertyChanged;
            SessionModel.Messages.CollectionChanged += Messages_CollectionChanged;
            MotoTrakPlot = new PlotModel() { Title = string.Empty };
            
            LinearAxis y_axis = new LinearAxis();
            y_axis.Minimum = -10;
            //y_axis.Maximum = 300;
            y_axis.Position = AxisPosition.Left;

            LinearAxis x_axis = new LinearAxis();
            x_axis.Minimum = 0;
            x_axis.Maximum = 500;
            x_axis.Position = AxisPosition.Bottom;

            MotoTrakPlot.Axes.Add(y_axis);
            MotoTrakPlot.Axes.Add(x_axis);

            AreaSeries k = new AreaSeries();
            MotoTrakPlot.Series.Add(k);
        }
        
        #endregion

        #region Private properties

        /// <summary>
        /// The session model object
        /// </summary>
        MotoTrakSession SessionModel
        {
            get
            {
                return _session;
            }
            set
            {
                _session = value;
            }
        }

        /// <summary>
        /// Whether or not it is required for the user to select a new stage before being allowed
        /// to initiate a new session.
        /// </summary>
        private bool StageChangeRequired
        {
            get
            {
                return _stageChangeRequired;
            }
            set
            {
                _stageChangeRequired = value;
                NotifyPropertyChanged("StageChangeRequired");
            }
        }

        #endregion

        #region Public properties that don't depend on the model

        /// <summary>
        /// The list of possible views that can be plotted
        /// </summary>
        public List<string> ViewList
        {
            get
            {
                return _viewList;
            }
        }

        /// <summary>
        /// The index into possible plots that can be displayed, indicating which is currently selected.
        /// </summary>
        public int ViewSelectedIndex
        {
            get
            {
                return _viewSelectedIndex;
            }
            set
            {
                _viewSelectedIndex = value;
                NotifyPropertyChanged("ViewSelectedIndex");
            }
        }

        /// <summary>
        /// Text of the manual feed button
        /// </summary>
        public string ManualFeedButtonText
        {
            get
            {
                return "Feed";
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// The rat's name, as shown in the GUI.
        /// </summary>
        [ReactToModelPropertyChanged(new string[] { "RatName" })]
        public string RatName
        {
            get
            {
                return SessionModel.RatName;
            }
            set
            {
                string rat_name = value;
                SessionModel.RatName = ViewHelperMethods.CleanInput(rat_name.Trim()).ToUpper();
            }
        }

        /// <summary>
        /// The booth number for this MotoTrak session.
        /// </summary>
        [ReactToModelPropertyChanged(new string[] { "BoothNumber" })]
        public string BoothNumber
        {
            get
            {
                return SessionModel.BoothNumber.ToString();
            }
        }

        /// <summary>
        /// The list of all available stages that the user can choose from, given the current device that is connected
        /// to the MotoTrak controller.
        /// </summary>
        [ReactToModelPropertyChanged(new string[] { "AvailableStages" })]
        public List<string> StageList
        {
            get
            {
                List<string> stages = new List<string>();
                foreach (MotorStage s in SessionModel.AvailableStages)
                {
                    stages.Add(s.StageNumber + " - " + s.Description);
                }

                return stages;
            }
        }

        /// <summary>
        /// An index into the list of available stages that indicates the currently selected stage.
        /// </summary>
        [ReactToModelPropertyChanged(new string[] { "SelectedStage", "AvailableStages" })]
        public int StageSelectedIndex
        {
            get
            {
                if (SessionModel.SelectedStage != null)
                {
                    return SessionModel.AvailableStages.IndexOf(SessionModel.SelectedStage);
                }

                return 0;
            }
            set
            {
                //This line of code needs to come before the next one.
                //The reason is that there are properties that listen for "SelectedStage"
                //to change on the model, but there verification of that change is by checking
                //the "StageChangeRequired" boolean value.  So the boolean value needs to be
                //set before the notification occurs.
                StageChangeRequired = false;
                SessionModel.SelectedStage = SessionModel.AvailableStages[value];
            }
        }

        /// <summary>
        /// A boolean value indicating whether the user is currently able to change the selected stage.
        /// </summary>
        [ReactToModelPropertyChanged(new string[] { "IsSessionRunning" })]
        public bool IsStageSelectionEnabled
        {
            get
            {
                return !SessionModel.IsSessionRunning;
            }
        }

        /// <summary>
        /// The name of the device that is currently connected to the MotoTrak board, as displayed to the user.
        /// </summary>
        [ReactToModelPropertyChanged(new string[] { "Device" })]
        public string DeviceName
        {
            get
            {
                return SessionModel.Device.DeviceName;
            }
        }

        /// <summary>
        /// The string for the start/stop button
        /// </summary>
        [ReactToModelPropertyChanged(new string[] { "IsSessionRunning" })]
        public string StartOrStopButtonText
        {
            get
            {
                if (SessionModel.IsSessionRunning)
                {
                    return "Stop";
                }

                return "Start";
            }
        }

        /// <summary>
        /// Whether or not the start button should be enabled
        /// </summary>
        [ReactToModelPropertyChanged(new string[] { "IsSessionRunning", "RatName", "SelectedStage", "Device" })]
        public bool StartButtonEnabled
        {
            get
            {
                if (SessionModel.Device != null && SessionModel.SelectedStage != null)
                {
                    return (SessionModel.RatName != string.Empty && !StageChangeRequired);
                }

                return false;
            }
        }

        /// <summary>
        /// The color of the text for the start/stop button
        /// </summary>
        [ReactToModelPropertyChanged(new string[] { "IsSessionRunning", "RatName", "SelectedStage" })]
        public SolidColorBrush StartButtonColor
        {
            get
            {
                if (StartButtonEnabled)
                {
                    if (SessionModel.IsSessionRunning)
                    {
                        return new SolidColorBrush(Colors.Red);
                    }
                    else
                    {
                        return new SolidColorBrush(Colors.Green);
                    }
                }
                else
                {
                    return new SolidColorBrush(Colors.DarkGray);
                }
            }
        }

        /// <summary>
        /// The text of the pause button
        /// </summary>
        [ReactToModelPropertyChanged(new string[] { "IsSessionRunning", "IsSessionPaused" })]
        public string PauseOrUnpauseButtonText
        {
            get
            {
                if (SessionModel.IsSessionRunning && SessionModel.IsSessionPaused)
                {
                    return "Unpause";
                }

                return "Pause";
            }
        }

        /// <summary>
        /// Boolean indicating whether the pause button should be enabled
        /// </summary>
        [ReactToModelPropertyChanged(new string[] { "IsSessionRunning" })]
        public bool PauseButtonEnabled
        {
            get
            {
                return SessionModel.IsSessionRunning;
            }
        }

        /// <summary>
        /// Whether or not the manual feed button is enabled
        /// </summary>
        [ReactToModelPropertyChanged(new string[] { "IsSessionRunning", "IsSessionPaused" })]
        public bool ManualFeedButtonEnabled
        {
            get
            {
                return SessionModel.IsSessionRunning && !SessionModel.IsSessionPaused;
            }
        }

        /// <summary>
        /// Indicates whether the session is currently running or idle
        /// </summary>
        [ReactToModelPropertyChanged(new string[] { "IsSessionRunning" })]
        public bool IsSessionRunning
        {
            get
            {
                return SessionModel.IsSessionRunning;
            }
        }

        /// <summary>
        /// Boolean indicating whether the session is NOT running
        /// </summary>
        [ReactToModelPropertyChanged(new string[] { "IsSessionRunning" })]
        public bool IsSessionNotRunning
        {
            get
            {
                return !SessionModel.IsSessionRunning;
            }
        }

        /// <summary>
        /// A collection of strings that acts as the messages to be displayed to the user.
        /// </summary>
        [ReactToModelPropertyChanged(new string[] { "IsSessionRunning" })]
        public List<string> MessageItems
        {
            get
            {
                return SessionModel.Messages.Select(t => t.Item2).ToList();
            }
        }

        /// <summary>
        /// Returns the selected index of the messages items to insure that the last message is always on screen.
        /// </summary>
        public int MessagesSelectedIndex
        {
            get
            {
                return MessageItems.Count - 1;
            }
        }

        /// <summary>
        /// Visibility of the "reset baseline" button
        /// </summary>
        [ReactToModelPropertyChanged(new string[] { "IsSessionRunning" })]
        public Visibility ResetBaselineButtonVisibility
        {
            get
            {
                if (IsSessionRunning)
                {
                    return Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// Visibility of the pause button
        /// </summary>
        [ReactToModelPropertyChanged(new string[] { "IsSessionRunning" })]
        public Visibility PauseButtonVisibility
        {
            get
            {
                if (IsSessionRunning)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// Visibility of the manual feed button
        /// </summary>
        [ReactToModelPropertyChanged(new string[] { "IsSessionRunning" })]
        public Visibility ManualFeedButtonVisibility
        {
            get
            {
                if (IsSessionRunning)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// Whether or not to visualize certain debugging elements of the UI.
        /// </summary>
        public Visibility DebuggingVisibility
        {
            get
            {
                return Visibility.Visible;
            }
        }

        #endregion

        #region Debugging properties

        /// <summary>
        /// The frame rate of the program for debugging purposes.
        /// This is essentially how fast we are able to loop and process incoming data from the MotoTrak controller board.
        /// </summary>
        [ReactToModelPropertyChanged(new string[] { "FramesPerSecond" })]
        public string FrameRate
        {
            get
            {
                return (SessionModel.FramesPerSecond.ToString());
            }
        }

        /// <summary>
        /// The baseline value of the device
        /// </summary>
        [ReactToModelPropertyChanged(new string[] { "Device" })]
        public string DeviceBaseline
        {
            get
            {
                return (SessionModel.Device.Baseline.ToString());
            }
        }

        /// <summary>
        /// The most recent analog value read from the device
        /// </summary>
        [ReactToModelPropertyChanged(new string[] { "DeviceAnalogValue" })]
        public string DeviceAnalogValue
        {
            get
            {
                return (SessionModel.DeviceAnalogValue.ToString());
            }
        }

        /// <summary>
        /// The most recent calibrated reading from the device
        /// </summary>
        [ReactToModelPropertyChanged(new string[] { "DeviceCalibratedValue" })]
        public string DeviceCalibratedValue
        {
            get
            {
                return (SessionModel.DeviceCalibratedValue.ToString());
            }
        }

        #endregion

        #region Plotting properties

        [ReactToModelPropertyChanged(new string[] { "MonitoredSignal" })]
        public PlotModel MotoTrakPlot { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        /// Pause or unpause the current session that is running
        /// </summary>
        public void TogglePause ()
        {
            SessionModel.PauseSession(!SessionModel.IsSessionPaused);
        }

        /// <summary>
        /// Stop the session that is currently running
        /// </summary>
        public void StopSession ()
        {
            StageChangeRequired = true;
            SessionModel.StopSession();
        }

        /// <summary>
        /// Start a new session.
        /// </summary>
        public void StartSession ()
        {
            SessionModel.StartSession();
        }

        /// <summary>
        /// Triggers a manual feed
        /// </summary>
        public void TriggerManualFeed ()
        {
            SessionModel.TriggerManualFeed();
        }

        /// <summary>
        /// Connects to the motor board and initializes streaming
        /// </summary>
        public void InitializeSession (string comPort)
        {
            SessionModel.InitializeSession(comPort);
        }

        /// <summary>
        /// This function is called whenever the user closes MotoTrak.
        /// </summary>
        public void CancelStreaming ()
        {
            SessionModel.CancelBackgroundLoop();
        }

        /// <summary>
        /// Call into the model to reset the baseline.
        /// </summary>
        public void ResetBaseline ()
        {
            SessionModel.ResetBaseline();
        }

        #endregion

        #region Method that listens for property change events on the model

        private void SessionModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //This function listens to notifications based on changes that are happening within the current session, and
            //then it does things in the user interface based on those changes (meaning, basically, that is sends up notifications
            //to the UI about those things changing.

            //Grab the name of the property that changed on the model
            string prop_name = e.PropertyName;

            //Update the plot if necessary
            UpdatePlotBasedOnModelPropertyChanged(prop_name);

            //Update other user interface components
            ExecuteReactionsToModelPropertyChanged(prop_name);
        }

        private void ExecuteReactionsToModelPropertyChanged(string model_property_changed)
        {
            //Get a System.Type object representing the current view-model object
            System.Type t = typeof(SessionViewModel);

            //Retrieve all property info for the view-model
            var property_info = t.GetProperties();

            //Iterate through each property
            foreach (var property in property_info)
            {
                //Get the custom attributes defined for this property
                var attributes = property.GetCustomAttributes(false);
                foreach (var attribute in attributes)
                {
                    //If the property is listening for changes on the model
                    var a = attribute as ReactToModelPropertyChanged;
                    if (a != null)
                    {
                        //If the property that was changed on the model matches the name
                        //that this view-model property is listening for...
                        if (a.ModelPropertyNames.Contains(model_property_changed))
                        {
                            //Notify the UI that the view-model property has been changed
                            NotifyPropertyChanged(property.Name);
                        }
                    }
                }
            }
        }

        private void CurrentTrial_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //This function listens to changes that occur from the current trial that is running, and then it 
            //does things based off of those changes.

            //Get the property from the current trial that changed
            string prop_name = e.PropertyName;

            //Udate the plot
            UpdatePlotBasedOnModelPropertyChanged(prop_name);
        }

        /// <summary>
        /// Listens to the model's "Messages" collection and reacts to changes in the collection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Messages_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged("MessageItems");
            NotifyPropertyChanged("MessagesSelectedIndex");
        }

        #endregion

        #region Methods that update the plot

        public void UpdatePlotBasedOnModelPropertyChanged ( string prop_name )
        {
            if (prop_name.Equals("SelectedStage"))
            {
                var y_axis = MotoTrakPlot.Axes.Where(a => a.Position == AxisPosition.Left).FirstOrDefault();
                if (y_axis != null)
                {
                    y_axis.MinimumRange = SessionModel.SelectedStage.HitThresholdMaximum * 2;
                    MotoTrakPlot.InvalidatePlot(true);
                }
            }
            else if (prop_name.Equals("MonitoredSignal"))
            {
                var datapoints = SessionModel.MonitoredSignal.Select((y_val, x_val) => new DataPoint(x_val, y_val)).ToList();

                var s = MotoTrakPlot.Series[0] as AreaSeries;
                if (s != null)
                {
                    s.Points.Clear();
                    s.Points.AddRange(datapoints);
                }

                MotoTrakPlot.InvalidatePlot(true);
            }
            else if (prop_name.Equals("CurrentTrial"))
            {
                //If the "current trial" has been set, make sure we are listening to events from the new object.
                if (SessionModel.CurrentTrial != null)
                {
                    //Listen to events from the current trial
                    SessionModel.CurrentTrial.PropertyChanged += CurrentTrial_PropertyChanged;

                    //Set up lines annotations around the hit window
                    LineAnnotation start_line = new LineAnnotation();
                    start_line.Type = LineAnnotationType.Vertical;
                    start_line.LineStyle = LineStyle.Solid;
                    start_line.StrokeThickness = 2;
                    start_line.Color = OxyColor.FromRgb(0, 0, 0);
                    start_line.X = SessionModel.SelectedStage.TotalRecordedSamplesBeforeHitWindow;

                    LineAnnotation end_line = new LineAnnotation();
                    end_line.Type = LineAnnotationType.Vertical;
                    end_line.LineStyle = LineStyle.Solid;
                    end_line.StrokeThickness = 2;
                    end_line.Color = OxyColor.FromRgb(0, 0, 0);
                    end_line.X = SessionModel.SelectedStage.TotalRecordedSamplesBeforeHitWindow + SessionModel.SelectedStage.TotalRecordedSamplesDuringHitWindow;

                    LineAnnotation hit_threshold_line = new LineAnnotation();
                    hit_threshold_line.Type = LineAnnotationType.Horizontal;
                    hit_threshold_line.LineStyle = LineStyle.Dash;
                    hit_threshold_line.StrokeThickness = 2;
                    hit_threshold_line.Color = OxyColor.FromRgb(0, 0, 0);
                    hit_threshold_line.Y = SessionModel.SelectedStage.HitThreshold;
                    hit_threshold_line.MinimumX = SessionModel.SelectedStage.TotalRecordedSamplesBeforeHitWindow;
                    hit_threshold_line.MaximumX = SessionModel.SelectedStage.TotalRecordedSamplesBeforeHitWindow + SessionModel.SelectedStage.TotalRecordedSamplesDuringHitWindow;

                    var y_axis = MotoTrakPlot.Axes.Where(a => a.Position == AxisPosition.Left).FirstOrDefault();
                    if (y_axis != null)
                    {
                        start_line.MinimumY = y_axis.AbsoluteMinimum;
                        start_line.MaximumY = y_axis.AbsoluteMaximum;

                        end_line.MinimumX = y_axis.AbsoluteMinimum;
                        end_line.MaximumY = y_axis.AbsoluteMaximum;
                    }

                    MotoTrakPlot.Annotations.Add(start_line);
                    MotoTrakPlot.Annotations.Add(end_line);
                    MotoTrakPlot.Annotations.Add(hit_threshold_line);
                    MotoTrakPlot.InvalidatePlot(true);
                }
                else
                {
                    //If the current trial has been set to null, it means there is not a trial that is currently happening.
                    //This means we need to make sure that any lines that are being plotted that pertain to the current
                    //trial are removed.

                    MotoTrakPlot.Annotations.Clear();
                    MotoTrakPlot.InvalidatePlot(true);
                }
            }
            else if (prop_name.Equals("HitIndex"))
            {
                LineAnnotation hit_line = new LineAnnotation();
                hit_line.Type = LineAnnotationType.Vertical;
                hit_line.LineStyle = LineStyle.Solid;
                hit_line.StrokeThickness = 2;
                hit_line.Color = OxyColor.FromRgb(255, 0, 0);

                hit_line.X = SessionModel.CurrentTrial.HitIndex;

                var y_axis = MotoTrakPlot.Axes.Where(a => a.Position == AxisPosition.Left).FirstOrDefault();
                if (y_axis != null)
                {
                    hit_line.MinimumY = y_axis.AbsoluteMinimum;
                    hit_line.MaximumY = y_axis.AbsoluteMaximum;
                }

                MotoTrakPlot.Annotations.Add(hit_line);
                MotoTrakPlot.InvalidatePlot(true);
            }
        }

        #endregion
    }
}
