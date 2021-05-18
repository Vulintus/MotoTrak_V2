using MotoTrakBase;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
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
        private Visibility _add_note_overlay_visibility = Visibility.Collapsed;
        private Visibility _session_overview_notes_visibility = Visibility.Collapsed;
        private string _current_note_text = string.Empty;

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
        public bool StageChangeRequired
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
                    if (Model.AvailableStages.Count > 0 && value < Model.AvailableStages.Count)
                    {
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

                        //Set a flag indicating a new stage was selected, so that the model can take any necessary actions
                        Model.NewStageSelectedFlag = true;

                        //Move the autopositioner to the correct position for the new stage
                        MotoTrakAutopositioner.GetInstance().SetPosition(Model.CurrentSession.SelectedStage.Position.CurrentValue);
                        
                        //Load the rat's recent history based on the newly selected stage
                        Model.LoadRecentHistory();

                        //Restart streaming based on the new stage's streaming properties
                        Model.RestartStreaming();

                        //Change the scale of the X-axis and Y-axis for the plot based on the newly selected stage.
                        if (PlotViewModel != null)
                        {
                            PlotViewModel.ScaleYAxis();
                            PlotViewModel.ScaleXAxis();
                        }

                        //Run session prep steps
                        //Model.RunSessionPreparationSteps();
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
        /// Whether or not the "add note" button is enabled
        /// </summary>
        [ReactToModelPropertyChanged(new string[] { "IsSessionRunning" })]
        public bool AddNoteButtonEnabled
        {
            get
            {
                return Model.IsSessionRunning;
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
        /// Visibility of the add note button
        /// </summary>
        [ReactToModelPropertyChanged(new string[] { "IsSessionRunning" })]
        public Visibility AddNoteButtonVisibility
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
                if (MotoTrakConfiguration.GetInstance().DebuggingMode)
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
        /// The visibility of the overlay that allows the user to add a note
        /// </summary>
        public Visibility AddNoteOverlayVisibility
        {
            get
            {
                return _add_note_overlay_visibility;
            }
            private set
            {
                _add_note_overlay_visibility = value;
                NotifyPropertyChanged("AddNoteOverlayVisibility");
            }
        }

        /// <summary>
        /// Visibility of the view that shows all session notes
        /// </summary>
        public Visibility SessionNotesViewVisibility
        {
            get
            {
                return _session_overview_notes_visibility;
            }
            set
            {
                _session_overview_notes_visibility = value;
                NotifyPropertyChanged("SessionNotesViewVisibility");
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

                    //Load the rat's recent history based on its name
                    Model.LoadRecentHistory();
                }
            }
        }

        /// <summary>
        /// Returns the current device position for display in the GUI
        /// </summary>
        [ReactToModelPropertyChanged(new string[] { "Position", "SelectedStage" })]
        public string Position
        {
            get
            {
                if (Model != null && Model.CurrentSession != null && Model.CurrentSession.SelectedStage != null)
                {
                    return Model.CurrentSession.SelectedStage.Position.CurrentValue.ToString("0.00");
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// The visibility of the session timer
        /// </summary>
        [ReactToModelPropertyChanged(new string[] { "IsSessionRunning" })]
        public Visibility SessionTimerVisibility
        {
            get
            {
                if (Model != null)
                {
                    if (Model.IsSessionRunning)
                    {
                        return Visibility.Visible;
                    }
                }

                return Visibility.Collapsed;
            }
        }

        /// <summary>
        /// The text of the session timer, shown while a session is running.
        /// </summary>
        [ReactToModelPropertyChanged(new string[] { "IsSessionRunning", "ElapsedSessionTime" })]
        public string SessionTimerText
        {
            get
            {
                string result = string.Empty;

                if (Model != null && Model.IsSessionRunning)
                {
                    TimeSpan k = Model.ElapsedSessionTime;
                    result = k.ToString(@"hh\:mm\:ss");
                }

                return result;
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

        /// <summary>
        /// The text of the current note that the user is entering
        /// </summary>
        public string CurrentNoteText
        {
            get
            {
                return _current_note_text;
            }
            set
            {
                _current_note_text = value;
                NotifyPropertyChanged("CurrentNoteText");
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

            //Display the session notes to the user
            SessionNotesViewVisibility = Visibility.Visible;
        }

        private void ReactToAutoStop ()
        {
            StageChangeRequired = true;
            SessionNotesViewVisibility = Visibility.Visible;
        }

        /// <summary>
        /// Finalizes a MotoTrak session.  This session is called after the user has both
        /// ended the session AND entered notes for the session.
        /// </summary>
        public void FinalizeSession ()
        {
            Model.FinalizeSession();

            //Hide the session notes view from the user
            SessionNotesViewVisibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Start a new session.
        /// </summary>
        public void StartSession ()
        {
            Model.StartSession();
        }

        /// <summary>
        /// Shows the panel that allows the user to add a note
        /// </summary>
        public void ShowAddNotePanel ()
        {
            AddNoteOverlayVisibility = Visibility.Visible;
        }

        /// <summary>
        /// Closes the panel that allows the user to add a note
        /// </summary>
        /// <param name="cancel">A boolean value indicating whether the note was canceled</param>
        public void CloseAddNotePanel ( bool cancel )
        {
            //Close the overlay
            AddNoteOverlayVisibility = Visibility.Collapsed;

            //If the result was that the user wanted to save the note
            if (!cancel)
            {
                if (Model != null && Model.CurrentSession != null)
                {
                    //Add the note to the list of notes in the session model
                    Model.CurrentSession.TimestampedNotes.Add(new System.Tuple<System.DateTime, string>(DateTime.Now, CurrentNoteText));
                    MotoTrakMessaging.GetInstance().AddMessage("User note saved at " + DateTime.Now.ToShortTimeString());
                }
            }

            //Clear the note's text in the GUI
            CurrentNoteText = string.Empty;
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
        public bool InitializeMotoTrak (string comPort)
        {
            bool result = Model.InitializeMotoTrak(comPort);

            if (result)
            {
                //This line of code is necessary to initiate the default stage selection.
                //When this happens, it selects a default "view" to be plotted from the view-list.
                StageSelectedIndex = StageSelectedIndex;
            }

            //Return whether or not MotoTrak successfully intialized
            return result;
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

        /// <summary>
        /// This function is called when the window is being moved or resized.
        /// When a move/resize event is in operation, this function will set
        /// a flag on the model indicating that plotting should cease.  Plotting
        /// while a move/resize event is in operation slows the program down
        /// a lot.
        /// </summary>
        /// <param name="resize">True if a move/resize event is taking place, false if completed</param>
        public void SetWindowResizeFlag (bool resize)
        {
            PlotViewModel.SetPlotting(!resize);
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
            else if (prop_name.Equals("AutoStop"))
            {
                ReactToAutoStop();
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
