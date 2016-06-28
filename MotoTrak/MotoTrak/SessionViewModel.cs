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

        #region Properties

        /// <summary>
        /// The rat's name, as shown in the GUI.
        /// </summary>
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
                NotifyPropertyChanged("RatName");
                NotifyPropertyChanged("StartButtonEnabled");
                NotifyPropertyChanged("StartButtonColor");
            }
        }

        /// <summary>
        /// The booth number for this MotoTrak session.
        /// </summary>
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
                SessionModel.SelectedStage = SessionModel.AvailableStages[value];
                StageChangeRequired = false;
                NotifyPropertyChanged("StageSelectedIndex");
                NotifyPropertyChanged("StageChangeRequired");
                NotifyPropertyChanged("StartButtonEnabled");
                NotifyPropertyChanged("StartButtonColor");
            }
        }

        /// <summary>
        /// A boolean value indicating whether the user is currently able to change the selected stage.
        /// </summary>
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
        public string DeviceName
        {
            get
            {
                return SessionModel.Device.DeviceName;
            }
        }
        
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
        /// The string for the start/stop button
        /// </summary>
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
        public bool PauseButtonEnabled
        {
            get
            {
                return SessionModel.IsSessionRunning;
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

        /// <summary>
        /// Whether or not the manual feed button is enabled
        /// </summary>
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
        public bool IsSessionRunning
        {
            get
            {
                return SessionModel.IsSessionRunning;
            }
        }

        /// <summary>
        /// A collection of strings that acts as the messages to be displayed to the user.
        /// </summary>
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
        /// Whether or not to visualize certain debugging elements of the UI.
        /// </summary>
        public Visibility DebuggingVisibility
        {
            get
            {
                return Visibility.Visible;
            }
        }

        /// <summary>
        /// The frame rate of the program for debugging purposes.
        /// This is essentially how fast we are able to loop and process incoming data from the MotoTrak controller board.
        /// </summary>
        public string FrameRate
        {
            get
            {
                return SessionModel.FramesPerSecond.ToString();
            }
        }

        #endregion

        #region Plotting properties

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
            SessionModel.StopSession();
            StageChangeRequired = true;
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

        #endregion

        #region Method that listens for property change events on the model

        private void SessionModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //This function listens to notifications based on changes that are happening within the current session, and
            //then it does things in the user interface based on those changes (meaning, basically, that is sends up notifications
            //to the UI about those things changing.

            string prop_name = e.PropertyName;
            if (prop_name.Equals("IsSessionRunning"))
            {
                NotifyPropertyChanged("IsStageSelectionEnabled");
                NotifyPropertyChanged("StartOrStopButtonText");
                NotifyPropertyChanged("StartButtonEnabled");
                NotifyPropertyChanged("StartButtonColor");
                NotifyPropertyChanged("PauseButtonEnabled");
                NotifyPropertyChanged("ManualFeedButtonEnabled");
                NotifyPropertyChanged("IsSessionRunning");
                NotifyPropertyChanged("MessageItems");
            }
            else if (prop_name.Equals("IsSessionPaused"))
            {
                NotifyPropertyChanged("PauseOrUnpauseButtonText");
            }
            else if (prop_name.Equals("BoothNumber"))
            {
                NotifyPropertyChanged("BoothNumber");
            }
            else if (prop_name.Equals("Device"))
            {
                NotifyPropertyChanged("DeviceName");
            }
            else if (prop_name.Equals("RatName"))
            {
                NotifyPropertyChanged("RatName");
            }
            else if (prop_name.Equals("SelectedStage"))
            {
                NotifyPropertyChanged("StageSelectedIndex");
                NotifyPropertyChanged("IsStageSelectionEnabled");
                NotifyPropertyChanged("StartButtonEnabled");
                NotifyPropertyChanged("StartButtonColor");
                NotifyPropertyChanged("StartOrStopButtonText");

                var y_axis = MotoTrakPlot.Axes.Where(a => a.Position == AxisPosition.Left).FirstOrDefault();
                if (y_axis != null)
                {
                    y_axis.MinimumRange = SessionModel.SelectedStage.HitThresholdMaximum * 2;
                    MotoTrakPlot.InvalidatePlot(true);
                }
            }
            else if (prop_name.Equals("AvailableStages"))
            {
                NotifyPropertyChanged("StageList");
                NotifyPropertyChanged("StageSelectedIndex");
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

                NotifyPropertyChanged("MotoTrakPlot");
            }
            else if (prop_name.Equals("FramesPerSecond"))
            {
                NotifyPropertyChanged("FrameRate");
            }
            else if (prop_name.Equals("Trials"))
            {

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
        }

        private void CurrentTrial_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //This function listens to changes that occur from the current trial that is running, and then it 
            //does things based off of those changes.

            string prop_name = e.PropertyName;

            if (prop_name.Equals("HitIndex"))
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
    }
}
