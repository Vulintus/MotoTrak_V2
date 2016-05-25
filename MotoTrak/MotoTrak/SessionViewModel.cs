using MotoTrakBase;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System.Collections.Generic;
using System.Linq;
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
        private List<string> _viewList = new List<string>() { "Realtime device signal", "Session overview" };
        private int _viewSelectedIndex = 0;
        private bool _stageChangeRequired = false;

        #endregion

        #region Constructors

        public SessionViewModel()
        {
            SessionModel.PropertyChanged += SessionModel_PropertyChanged;
            MotoTrakPlot = new PlotModel() { Title = string.Empty };
            
            LinearAxis y_axis = new LinearAxis();
            y_axis.Minimum = -10;
            y_axis.Maximum = 200;
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
        /// The list of messages to be displayed to the user
        /// </summary>
        public List<string> MessageItems
        {
            get
            {
                return new List<string>();
            }
            set
            {
                //empty
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
            else if (prop_name.Equals("Trials"))
            {

            }
        }

        #endregion
    }
}
