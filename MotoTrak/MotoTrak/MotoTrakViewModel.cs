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
    public class MotoTrakViewModel : NotifyPropertyChangedObject
    {
        #region Private data members

        MotoTrakModel _model = MotoTrakModel.GetInstance();
        MotoTrakPlotViewModel _plot_view_model = null;
        
        private int _viewSelectedIndex = 0;
        private bool _stageChangeRequired = false;

        #endregion

        #region Constructors

        public MotoTrakViewModel()
        {
            //Subscribe to notifications from the MotoTrak model
            Model.PropertyChanged += ExecuteReactionsToModelPropertyChanged;

            //Initialize the plot view-model
            PlotViewModel = new MotoTrakPlotViewModel(Model, 1);
        }
        
        #endregion

        #region Private properties

        /// <summary>
        /// The session model object
        /// </summary>
        MotoTrakModel Model
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
        [ReactToModelPropertyChanged(new string[] { "SelectedStage" })]
        public List<string> ViewList
        {
            get
            {
                List<string> result = new List<string>();

                //Only populate the view-list if there is a current session object with a selected stage
                if (Model.CurrentSession != null && Model.CurrentSession.SelectedStage != null)
                {
                    foreach (var sp in Model.CurrentSession.SelectedStage.DataStreamTypes)
                    {
                        string stream_type_description = MotorBoardDataStreamTypeConverter.ConvertToDescription(sp);
                        result.Add(stream_type_description);
                    }

                    result.Add("Session overview");
                    result.Add("Recent performance");
                }
                
                return result;
            }
        }
        
        /// <summary>
        /// List with enumerated type of what is in each view selection
        /// </summary>
        public List<MotoTrakPlotViewType> ViewTypeList
        {
            get
            {
                List<MotoTrakPlotViewType> result = new List<MotoTrakPlotViewType>();

                //Only populate the view-list if there is a current session object with a selected stage
                if (Model.CurrentSession != null && Model.CurrentSession.SelectedStage != null)
                {
                    foreach (var sp in Model.CurrentSession.SelectedStage.DataStreamTypes)
                    {
                        result.Add(MotoTrakPlotViewType.DataStream);
                    }

                    result.Add(MotoTrakPlotViewType.SessionOverview);
                    result.Add(MotoTrakPlotViewType.RecentPerformance);
                }

                return result;
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
                PlotViewModel.StreamIndex = value;
                
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
        /// This is a separate view-model object for the MotoTrak plot
        /// </summary>
        public MotoTrakPlotViewModel PlotViewModel
        {
            get
            {
                return _plot_view_model;
            }
            set
            {
                _plot_view_model = value;
                NotifyPropertyChanged("PlotViewModel");
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
                foreach (MotorStage s in Model.AvailableStages)
                {
                    stages.Add(s.StageName + " - " + s.Description);
                }

                return stages;
            }
        }

        /// <summary>
        /// An index into the list of available stages that indicates the currently selected stage.
        /// </summary>
        [ReactToModelPropertyChanged(new string[] { "SelectedStage", "AvailableStages", "CurrentSession" })]
        public int StageSelectedIndex
        {
            get
            {
                if (Model.CurrentSession != null && Model.CurrentSession.SelectedStage != null)
                {
                    return Model.AvailableStages.IndexOf(Model.CurrentSession.SelectedStage);
                }

                return 0;
            }
            set
            {
                if (Model.CurrentSession != null)
                {
                    //This line of code needs to come before the next one.
                    //The reason is that there are properties that listen for "SelectedStage"
                    //to change on the model, but there verification of that change is by checking
                    //the "StageChangeRequired" boolean value.  So the boolean value needs to be
                    //set before the notification occurs.
                    StageChangeRequired = false;

                    //Now let's change the default view being displayed to the user
                    var new_stage = Model.AvailableStages[value];
                    try
                    {
                        //Set the selected plot view to be the index of the device stream by default
                        ViewSelectedIndex = new_stage.DataStreamTypes.IndexOf(MotorBoardDataStreamType.DeviceValue);
                    }
                    catch
                    {
                        //If there was an error for any reason, set the default plot view to be the 0th stream index
                        ViewSelectedIndex = 0;
                    }

                    //Finally, let's change the selected stage itself
                    Model.CurrentSession.SelectedStage = new_stage;

                    //Change the scale of the X-axis and Y-axis for the plot based on the newly selected stage.
                    if (PlotViewModel != null)
                    {
                        PlotViewModel.ScaleYAxis();
                        PlotViewModel.ScaleXAxis();
                    }
                }
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
                return !Model.IsSessionRunning;
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
                if (Model.IsSessionRunning)
                {
                    return "Stop";
                }

                return "Start";
            }
        }

        /// <summary>
        /// Whether or not the start button should be enabled
        /// </summary>
        [ReactToModelPropertyChanged(new string[] { "IsSessionRunning", "RatName", "SelectedStage", "CurrentDevice", "CurrentSession" })]
        public bool StartButtonEnabled
        {
            get
            {
                if (Model.CurrentSession != null && Model.CurrentDevice != null && Model.CurrentSession.SelectedStage != null)
                {
                    return (Model.CurrentSession.RatName != string.Empty && !StageChangeRequired);
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
                    if (Model.IsSessionRunning)
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
                if (Model.IsSessionRunning && Model.IsSessionPaused)
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
                return Model.IsSessionRunning;
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
                return Model.IsSessionRunning && !Model.IsSessionPaused;
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
                return Model.IsSessionRunning;
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
                return !Model.IsSessionRunning;
            }
        }

        /// <summary>
        /// A collection of strings that acts as the messages to be displayed to the user.
        /// </summary>
        [ReactToModelPropertyChanged(new string[] { "IsSessionRunning", "Messages" })]
        public List<string> MessageItems
        {
            get
            {
                return MotoTrakMessaging.GetInstance().RetrieveAllMessages();
            }
        }

        /// <summary>
        /// Returns the selected index of the messages items to insure that the last message is always on screen.
        /// </summary>
        [ReactToModelPropertyChanged(new string[] { "IsSessionRunning", "Messages" })]
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
                //return Visibility.Collapsed;
            }
        }

        /// <summary>
        /// The booth label
        /// </summary>
        [ReactToModelPropertyChanged(new string[] { "BoothLabel" })]
        public string BoothLabel
        {
            get
            {
                return Model.BoothLabel;
            }
        }

        /// <summary>
        /// The name of the device for this session
        /// </summary>
        [ReactToModelPropertyChanged(new string[] { "CurrentDevice" })]
        public string DeviceName
        {
            get
            {
                string device_name = string.Empty;
                if (Model.CurrentDevice != null)
                {
                    device_name = Model.CurrentDevice.DeviceName;
                }

                return device_name;
            }
        }

        /// <summary>
        /// The name of the rat being used for this session
        /// </summary>
        [ReactToModelPropertyChanged(new string[] { "RatName", "CurrentSession" })]
        public string RatName
        {
            get
            {
                if (Model.CurrentSession != null)
                {
                    return Model.CurrentSession.RatName;
                }
                else
                {
                    return string.Empty;
                }
            }
            set
            {
                if (Model.CurrentSession != null)
                {
                    string rat_name = value;
                    Model.CurrentSession.RatName = ViewHelperMethods.CleanInput(rat_name.Trim()).ToUpper();
                }
            }
        }

        #endregion

        #region Debugging properties

        /// <summary>
        /// The frame rate of the program for debugging purposes.
        /// This is essentially how fast we are able to loop and process incoming data from the MotoTrak controller board.
        /// This value is throttled, however, by how long we choose to wait inbetween loop iterations using the Thread.Sleep() function.
        /// </summary>
        [ReactToModelPropertyChanged(new string[] { "FramesPerSecond" })]
        public string FrameRate
        {
            get
            {
                return (Model.FramesPerSecond.ToString());
            }
        }

        /// <summary>
        /// The number of milliseconds it takes to run a single MotoTrak frame.
        /// This is the average of the last 1000 frames.
        /// This value is NOT throttled and can be used to calculate a true frame rate not dependent on Thread.Sleep() if necessary.
        /// </summary>
        [ReactToModelPropertyChanged(new string[] { "MillisecondsPerFrame" } )]
        public string MillisecondsPerFrame
        {
            get
            {
                return (Model.MillisecondsPerFrame.ToString("#.####"));
            }
        }

        /// <summary>
        /// The baseline value of the device
        /// </summary>
        [ReactToModelPropertyChanged(new string[] { "CurrentDevice" })]
        public string DeviceBaseline
        {
            get
            {
                string baseline = string.Empty;
                if (Model.CurrentDevice != null)
                {
                    baseline = Model.CurrentDevice.Baseline.ToString();
                }
                
                return baseline;
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
                return (Model.DeviceAnalogValue.ToString());
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
                return (Model.DeviceCalibratedValue.ToString());
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
            Model.PauseSession(!Model.IsSessionPaused);
        }

        /// <summary>
        /// Stop the session that is currently running
        /// </summary>
        public void StopSession ()
        {
            StageChangeRequired = true;
            Model.StopSession();
        }

        /// <summary>
        /// Start a new session.
        /// </summary>
        public void StartSession ()
        {
            Model.StartSession();
        }

        /// <summary>
        /// Triggers a manual feed
        /// </summary>
        public void TriggerManualFeed ()
        {
            Model.TriggerManualFeed();
        }

        /// <summary>
        /// Connects to the motor board and initializes streaming
        /// </summary>
        public void InitializeMotoTrak (string comPort)
        {
            Model.InitializeMotoTrak(comPort);

            //This line of code is necessary to initiate the default stage selection.
            //When this happens, it selects a default "view" to be plotted from the view-list.
            StageSelectedIndex = StageSelectedIndex;
        }

        /// <summary>
        /// This function is called whenever the user closes MotoTrak.
        /// </summary>
        public void ShutdownMotoTrak ()
        {
            Model.ShutdownMotoTrak();
        }

        /// <summary>
        /// Call into the model to reset the baseline.
        /// </summary>
        public void ResetBaseline ()
        {
            Model.ResetBaseline();
        }

        #endregion

        #region Method that listens for property change events on the model

        protected override void ExecuteReactionsToModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //This function listens to notifications based on changes that are happening within the current session, and
            //then it does things in the user interface based on those changes (meaning, basically, that is sends up notifications
            //to the UI about those things changing.

            //Grab the name of the property that changed on the model
            string prop_name = e.PropertyName;
            
            //Subscribe to events from the new current session if the current session has been changed
            if (prop_name.Equals("CurrentSession"))
            {
                if (Model.CurrentSession != null)
                {
                    Model.CurrentSession.PropertyChanged += ExecuteReactionsToModelPropertyChanged;
                }
            }

            //Update other user interface components
            base.ExecuteReactionsToModelPropertyChanged(sender, e);
        }
        
        private void CurrentTrial_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //This function listens to changes that occur from the current trial that is running, and then it 
            //does things based off of those changes.

            //Get the property from the current trial that changed
            string prop_name = e.PropertyName;

            //Code here if needed
        }

        #endregion
    }
}
