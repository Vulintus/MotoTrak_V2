using Microsoft.Win32;
using MotoTrakBase;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace SessionViewer
{
    /// <summary>
    /// A view-model class for the MotoTrak session viewer app.
    /// </summary>
    public class SessionViewerViewModel : NotifyPropertyChangedObject
    {
        #region Private data members

        private MotoTrakSession _model_session = null;
        private int _current_trial_value = 0;
        private PlotModel _trial_plot_model = new PlotModel();
        private PlotModel _session_plot_model = new PlotModel();
        private bool _is_session_plot_visible = true;

        private SimpleCommand _open_session_command;
        private SimpleCommand _save_plot_command;
        private SimpleCommand _toggle_session_plot_command;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public SessionViewerViewModel ()
        {
            //empty
        }

        #endregion

        #region Properties

        /// <summary>
        /// The session model
        /// </summary>
        public MotoTrakSession SessionModel
        {
            get
            {
                return _model_session;
            }
            set
            {
                _model_session = value;
            }
        }

        /// <summary>
        /// The rat from this session
        /// </summary>
        public string RatName
        {
            get
            {
                if (SessionModel != null)
                {
                    return SessionModel.RatName;
                }

                return string.Empty;
            }
        }

        /// <summary>
        /// The start time of the session
        /// </summary>
        public string SessionStartTime
        {
            get
            {
                if (SessionModel != null)
                {
                    return SessionModel.StartTime.ToString();
                }

                return string.Empty;
            }
        }

        /// <summary>
        /// Displays the trial number of the trial currently being viewed by the user
        /// </summary>
        public string TrialNumber
        {
            get
            {
                if (SessionModel != null)
                {
                    if (SessionModel.Trials.Count > 0)
                    {
                        string result = _current_trial_value.ToString() + "/" + SessionModel.Trials.Count.ToString();
                        return result;
                    }
                    else
                    {
                        return "No trials found";
                    }
                }

                return string.Empty;
            }
        }

        /// <summary>
        /// The plot model for the trial plot
        /// </summary>
        public PlotModel TrialPlotModel
        {
            get
            {
                return _trial_plot_model;
            }
            set
            {
                _trial_plot_model = value;
            }
        }

        /// <summary>
        /// The plot model for the session plot
        /// </summary>
        public PlotModel SessionPlotModel
        {
            get
            {
                return _session_plot_model;
            }
            set
            {
                _session_plot_model = value;
            }
        }

        /// <summary>
        /// Indicates whether or not the session overview plot is visible in the session viewer
        /// </summary>
        public Visibility SessionViewerPlotVisibility
        {
            get
            {
                if (_is_session_plot_visible)
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
        /// Returns the total number of trials from this session.
        /// </summary>
        public double TotalTrials
        {
            get
            {
                if (SessionModel != null)
                {
                    if (SessionModel.Trials.Count > 0)
                    {
                        return SessionModel.Trials.Count;
                    }
                }

                return 1;
            }
        }

        /// <summary>
        /// The viewport size for the scrollbar
        /// </summary>
        public double ScrollBarViewportSize
        {
            get
            {
                if (SessionModel != null)
                {
                    if (SessionModel.Trials.Count >= 1)
                    {
                        double p = 1.0 / Convert.ToDouble(SessionModel.Trials.Count);
                        double vp_size = (SessionModel.Trials.Count - 1) * p / (1.0 - p);
                        return vp_size;
                    }
                }

                return 0;
            }
        }

        /// <summary>
        /// Whether or not the scroll bar is enabled
        /// </summary>
        public bool IsScrollBarEnabled
        {
            get
            {
                if (SessionModel != null)
                {
                    if (SessionModel.Trials.Count > 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// The command to open a new session in the session viewer
        /// </summary>
        public SimpleCommand OpenSessionCommand
        {
            get
            {
                return _open_session_command ?? (_open_session_command = new SimpleCommand(() => OpenSession(), true));
            }
        }

        /// <summary>
        /// The command to save the trial plot as a PNG file
        /// </summary>
        public SimpleCommand SavePlotCommand
        {
            get
            {
                return _save_plot_command ?? (_save_plot_command = new SimpleCommand(() => SaveTrialPlot(), true));
            }
        }

        public SimpleCommand ToggleSessionViewCommand
        {
            get
            {
                return _toggle_session_plot_command ?? (_toggle_session_plot_command = new SimpleCommand(() => ToggleSessionPlot(), true));
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Opens a new session for viewing
        /// </summary>
        public void OpenSession ()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "MotoTrak Session Files (*.MotoTrak)|*.MotoTrak";
            dlg.InitialDirectory = MotoTrakConfiguration.GetInstance().DataPath;
            if (dlg.ShowDialog() == true)
            {
                //Read the session
                var session = MotoTrakFileRead.ReadFile(dlg.FileName);

                //Set the session as the model
                SessionModel = session;

                //Initialize each of the plots
                InitializeTrialPlot();
                InitializeSessionPlot();

                //Set the value of the current trial
                if (SessionModel.Trials.Count > 0)
                {
                    SetCurrentTrial(1);
                }
                else
                {
                    SetCurrentTrial(0);
                }

                //Notify property changed on lots of things
                NotifyPropertyChanged("RatName");
                NotifyPropertyChanged("SessionStartTime");
                NotifyPropertyChanged("TrialNumber");
                NotifyPropertyChanged("ScrollBarViewportSize");
                NotifyPropertyChanged("TotalTrials");
                NotifyPropertyChanged("IsScrollBarEnabled");
            }
        }

        /// <summary>
        /// Saves the current trial being viewed as a PNG file
        /// </summary>
        public void SaveTrialPlot ()
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "PNG Images (*.png)|*.png";
            dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            if (dlg.ShowDialog() == true)
            {
                var pngExporter = new OxyPlot.Wpf.PngExporter();
                var bitmap_image = pngExporter.ExportToBitmap(TrialPlotModel);

                var fileStream = new FileStream(dlg.FileName, FileMode.Create);
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap_image));
                encoder.Save(fileStream);
                fileStream.Close();
            }
        }

        /// <summary>
        /// Toggles whether the session overview plot is visible
        /// </summary>
        public void ToggleSessionPlot ()
        {
            _is_session_plot_visible = !_is_session_plot_visible;
            NotifyPropertyChanged("SessionViewerPlotVisibility");
        }

        /// <summary>
        /// Sets the current trial being viewed
        /// </summary>
        /// <param name="val">The number of the trial</param>
        public void SetCurrentTrial (int val)
        {
            //Set the current trial value
            _current_trial_value = val;

            if (_current_trial_value > 0)
            {
                //Update the trial plot on the GUI
                UpdateTrialPlot();

                //Update the session plot on the GUI
                UpdateSessionPlot();

                //Update the trial number on the GUI
                NotifyPropertyChanged("TrialNumber");
            }
        }

        /// <summary>
        /// Updates the trial plot based on the current trial value
        /// </summary>
        private void UpdateTrialPlot ()
        {
            //Get the signal series
            var signal_series = TrialPlotModel.Series.FirstOrDefault() as AreaSeries;
            if (signal_series != null)
            {
                if (SessionModel.SelectedStage != null)
                {
                    if (SessionModel.SelectedStage.DataStreamTypes != null && SessionModel.SelectedStage.DataStreamTypes.Count > 0)
                    {
                        var signal_index = SessionModel.SelectedStage.DataStreamTypes.IndexOf(MotorBoardDataStreamType.DeviceValue);
                        if (signal_index >= 0)
                        {
                            int trial_index = _current_trial_value - 1;
                            var signal_data = SessionModel.Trials[trial_index].TrialData[signal_index];
                            var datapoints_collection = signal_data.Select((y, x) => new DataPoint(x, y)).ToList();

                            signal_series.Points.Clear();
                            signal_series.Points.AddRange(datapoints_collection);
                        }
                    }
                }
            }

            //Update the plot on the GUI
            TrialPlotModel.InvalidatePlot(true);
        }

        private void UpdateSessionPlot ()
        {
            //Clear all existing annotations
            SessionPlotModel.Annotations.Clear();

            //Get the y-axis
            LinearAxis y_axis = SessionPlotModel.Axes.Where(x => x.Position == AxisPosition.Left).FirstOrDefault() as LinearAxis;

            if (y_axis != null)
            {
                //Create a line annotation at the x-index of the current trial we are viewing
                LineAnnotation k = new LineAnnotation()
                {
                    Type = LineAnnotationType.Vertical,
                    X = _current_trial_value,
                    MinimumY = y_axis.ActualMinimum,
                    MaximumY = y_axis.ActualMaximum,
                    Color = OxyColors.Black,
                    StrokeThickness = 2
                };

                //Add the line annotation to our plot model
                SessionPlotModel.Annotations.Add(k);

                //Invalidate the session plot model so that it updates on the GUI
                SessionPlotModel.InvalidatePlot(true);
            }
        }

        /// <summary>
        /// Populates the session plot with scatterplot points
        /// </summary>
        private void PopulateSessionPlot ()
        {
            //Get the scatter plot series
            var successful_trials = SessionPlotModel.Series.Where(x => x.Tag.Equals("Successful")).FirstOrDefault() as ScatterSeries;
            var failed_trials = SessionPlotModel.Series.Where(x => x.Tag.Equals("Failed")).FirstOrDefault() as ScatterSeries;

            if (successful_trials != null && failed_trials != null)
            {
                successful_trials.Points.Clear();
                failed_trials.Points.Clear();

                for (int i = 0; i < SessionModel.Trials.Count; i++)
                {
                    double x_value = i + 1;
                    double y_value = GetTrialYValue(SessionModel.SelectedStage, SessionModel.Trials[i]);

                    //Populate the successful and failed trials
                    if (SessionModel.Trials[i].Result == MotorTrialResult.Hit)
                    {
                        successful_trials.Points.Add(new ScatterPoint(x_value, y_value));
                    }
                    else
                    {
                        failed_trials.Points.Add(new ScatterPoint(x_value, y_value));
                    }
                }
            }

            //Invalidate the session overview plot to update the GUI
            SessionPlotModel.InvalidatePlot(true);
        }

        /// <summary>
        /// Calculates a Y-value to be displayed for a trial in the session overview plot
        /// </summary>
        /// <param name="s">The stage</param>
        /// <param name="t">The trial</param>
        /// <returns>A y-value representing the trial</returns>
        private double GetTrialYValue (MotorStage s, MotorTrial t)
        {
            //Get the indices of the hit window
            int starting_index = Convert.ToInt32(s.SamplesPerSecond * t.PreTrialSamplingPeriodInSeconds);
            int ending_index = Convert.ToInt32(s.SamplesPerSecond * (t.PreTrialSamplingPeriodInSeconds + t.HitWindowDurationInSeconds));

            //Get the signal data from the trial
            var signal_index = SessionModel.SelectedStage.DataStreamTypes.IndexOf(MotorBoardDataStreamType.DeviceValue);
            if (signal_index >= 0)
            {
                var signal_data = t.TrialData[signal_index];

                if (starting_index >= 0 && ending_index >= 0 && starting_index < signal_data.Count && ending_index < signal_data.Count)
                {
                    var max_value = signal_data.Skip(starting_index).Take(ending_index - starting_index + 1).Max();
                    return max_value;
                }
            }

            return 0;
        }

        /// <summary>
        /// Initializes the trial plot
        /// </summary>
        private void InitializeTrialPlot ()
        {
            //Clear the plot
            TrialPlotModel.Axes.Clear();
            TrialPlotModel.Series.Clear();

            //Create linear axes for the plot
            LinearAxis x_axis = new LinearAxis()
            {
                Position = AxisPosition.Bottom
            };

            LinearAxis y_axis = new LinearAxis()
            {
                Position = AxisPosition.Left
            };

            TrialPlotModel.Axes.Add(x_axis);
            TrialPlotModel.Axes.Add(y_axis);

            //Create an area series to show the trial signal
            AreaSeries signal_data = new AreaSeries()
            {
                Color = OxyColors.CornflowerBlue
            };

            TrialPlotModel.Series.Add(signal_data);

            //Invalidate the plot to update it
            TrialPlotModel.InvalidatePlot(true);
        }

        /// <summary>
        /// Initializes the session plot
        /// </summary>
        private void InitializeSessionPlot ()
        {
            //Clear the plot
            SessionPlotModel.Axes.Clear();
            SessionPlotModel.Series.Clear();

            //Create linear axes for the plot
            LinearAxis x_axis = new LinearAxis()
            {
                Position = AxisPosition.Bottom,
                IsAxisVisible = false
            };

            LinearAxis y_axis = new LinearAxis()
            {
                Position = AxisPosition.Left,
                IsAxisVisible = false
            };

            SessionPlotModel.Axes.Add(x_axis);
            SessionPlotModel.Axes.Add(y_axis);

            //Create a scatter series for the session data
            ScatterSeries successful_trials = new ScatterSeries()
            {
                MarkerFill = OxyColors.Green,
                MarkerStroke = OxyColors.Green,
                MarkerType = MarkerType.Star,
                MarkerSize = 3,
                Tag = "Successful"
            };

            ScatterSeries failed_trials = new ScatterSeries()
            {
                MarkerFill = OxyColors.Red,
                MarkerStroke = OxyColors.Red,
                MarkerType = MarkerType.Star,
                MarkerSize = 3,
                Tag = "Failed"
            };
            
            SessionPlotModel.Series.Add(successful_trials);
            SessionPlotModel.Series.Add(failed_trials);

            PopulateSessionPlot();
        }

        #endregion
    }
}
