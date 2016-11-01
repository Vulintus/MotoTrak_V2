using MotoTrakUtilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MotoTrakBase
{
    /// <summary>
    /// This class holds all information for a MotoTrak session.  This includes previous sessions that are
    /// loaded into memory as well as the current session that is being run.
    /// </summary>
    public class MotoTrakSession : NotifyPropertyChangedObject
    {
        #region Private data members

        private MotorDevice _device = new MotorDevice();
        private MotorStage _selected_stage = new MotorStage();
        private List<MotorTrial> _trials = new List<MotorTrial>();
        private List<DateTime> _manual_feeds = new List<DateTime>();
        private List<Tuple<DateTime, DateTime>> _pauses = new List<Tuple<DateTime, DateTime>>();

        private string _booth_label = string.Empty;
        private string _rat_name = string.Empty;
        private DateTime _start_time = DateTime.MinValue;
        private DateTime _end_time = DateTime.MinValue;

        private string _session_notes = string.Empty;
        private List<Tuple<DateTime, string>> _timestamped_notes = new List<Tuple<DateTime, string>>();

        private List<Tuple<string, string>> _stream_descriptions = new List<Tuple<string, string>>();
        private List<Tuple<string, double>> _constant_value_parameters = new List<Tuple<string, double>>();
        private List<Tuple<string, string>> _constant_string_parameters = new List<Tuple<string, string>>();
        private List<string> _variable_value_parameters = new List<string>();
        
        #endregion
        
        #region Constructors

        /// <summary>
        /// Constructs a new MotoTrak session.
        /// </summary>
        public MotoTrakSession()
        {
            //empty constructor
        }

        #endregion
        
        #region Properties

        /// <summary>
        /// The booth in which this session is taking place.
        /// </summary>
        public string BoothLabel
        {
            get { return _booth_label; }
            set
            {
                _booth_label = value;
                NotifyPropertyChanged("BoothLabel");
            }
        }

        /// <summary>
        /// The device being used in this session.
        /// </summary>
        public MotorDevice Device
        {
            get { return _device; }
            set
            {
                _device = value;
                NotifyPropertyChanged("Device");
            }
        }

        /// <summary>
        /// The name of the rat being run in this session.
        /// </summary>
        public string RatName
        {
            get { return _rat_name; }
            set
            {
                _rat_name = value;
                NotifyPropertyChanged("RatName");
            }
        }

        /// <summary>
        /// The stage that has been selected for this session.
        /// </summary>
        public MotorStage SelectedStage
        {
            get
            {
                return _selected_stage;
            }
            set
            {
                _selected_stage = value;
                NotifyPropertyChanged("SelectedStage");
            }
        }

        /// <summary>
        /// This property contains the set of all trials for the session that is currently running.
        /// This property can ONLY be set by the background worker thread, in order to keep the application
        /// thread-safe.  This property can be read at any time by the main thread (the UI thread), but it should not
        /// be set by that thread.
        /// </summary>
        public List<MotorTrial> Trials
        {
            get
            {
                return _trials;
            }
            set
            {
                _trials = value;
                NotifyPropertyChanged("Trials");
            }
        }

        /// <summary>
        /// List containing timestamps of all manual feeds that occur during a session
        /// </summary>
        public List<DateTime> ManualFeeds
        {
            get
            {
                return _manual_feeds;
            }
            set
            {
                _manual_feeds = value;
                NotifyPropertyChanged("ManualFeeds");
            }
        }

        /// <summary>
        /// List containing timestamps of all pauses (beginning and end) that occur during a session.
        /// </summary>
        public List<Tuple<DateTime, DateTime>> Pauses
        {
            get
            {
                return _pauses;
            }
            set
            {
                _pauses = value;
                NotifyPropertyChanged("Pauses");
            }
        }

        /// <summary>
        /// The session start time
        /// </summary>
        public DateTime StartTime
        {
            get
            {
                return _start_time;
            }
            set
            {
                _start_time = value;
                NotifyPropertyChanged("StartTime");
            }
        }

        /// <summary>
        /// The session end time
        /// </summary>
        public DateTime EndTime
        {
            get
            {
                return _end_time;
            }
            set
            {
                _end_time = value;
                NotifyPropertyChanged("EndTime");
            }
        }

        /// <summary>
        /// A list of notes made throughout the session with their timestamps
        /// </summary>
        public List<Tuple<DateTime, string>> TimestampedNotes
        {
            get
            {
                return _timestamped_notes;
            }
            set
            {
                _timestamped_notes = value;
                NotifyPropertyChanged("TimestampedNotes");
            }
        }

        /// <summary>
        /// Notes about this session that were input by the user
        /// </summary>
        public string SessionNotes
        {
            get
            {
                return _session_notes;
            }
            set
            {
                _session_notes = value;
                NotifyPropertyChanged("SessionNotes");
            }
        }

        /// <summary>
        /// A list of tuples that contain strings describing each kind of data stream for this session.
        /// Each element of the list is a tuple that represents a description for an individual data stream.
        /// Each tuple has two strings: the first is the description of the associated stream, and the second
        /// is a description of the units for that stream.
        /// </summary>
        public List<Tuple<string, string>> StreamDescriptions
        {
            get
            {
                return _stream_descriptions;
            }

            set
            {
                _stream_descriptions = value;
                NotifyPropertyChanged("StreamDescriptions");
            }
        }

        /// <summary>
        /// This list is for user-defined numeric parameters that remain constant throughout the session.
        /// The first element of each tuple is a string that defines the name of the parameter.
        /// The second element of each tuple is a double that holds the value of the parameter.
        /// </summary>
        public List<Tuple<string, double>> ConstantValueParameters
        {
            get
            {
                return _constant_value_parameters;
            }

            set
            {
                _constant_value_parameters = value;
                NotifyPropertyChanged("ConstantValueParameters");
            }
        }

        /// <summary>
        /// This list is for user-defined string parameters that remain constant throughout the session.
        /// The first element of each tuple is a string that defines the name of the parameter.
        /// The second element of each tuple is a string that defines the value of the parameter.
        /// </summary>
        public List<Tuple<string, string>> ConstantStringParameters
        {
            get
            {
                return _constant_string_parameters;
            }

            set
            {
                _constant_string_parameters = value;
                NotifyPropertyChanged("ConstantStringParameters");
            }
        }

        /// <summary>
        /// This is a list that defines names of parameters that may change with each trial within a session.
        /// No values are stored at this level.  Rather, they are stored within individual trials.
        /// The user must reference this list to retrieve the names of those parameters.
        /// </summary>
        public List<string> VariableValueParameters
        {
            get
            {
                return _variable_value_parameters;
            }

            set
            {
                _variable_value_parameters = value;
                NotifyPropertyChanged("VariableValueParameters");
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Clears a sessions of its trials, messages, manual feeds, pauses, and times.
        /// Does NOT clear stage and device information from a session.
        /// </summary>
        public void ClearSession ()
        {
            //Clear the list of trials, manual feeds, and pauses
            Trials = Enumerable.Empty<MotorTrial>().ToList();
            ManualFeeds = Enumerable.Empty<DateTime>().ToList();
            Pauses = Enumerable.Empty<Tuple<DateTime, DateTime>>().ToList();

            //Clear the notes and the timestamped notes
            SessionNotes = string.Empty;
            TimestampedNotes = Enumerable.Empty<Tuple<DateTime, string>>().ToList();

            //Clear the start and end times of the session
            StartTime = DateTime.MinValue;
            EndTime = DateTime.MinValue;
        }

        /// <summary>
        /// Creates a new pause tuple within the list of pauses for this session
        /// </summary>
        /// <param name="t">The time at which the pause begins</param>
        public void CreatePause (DateTime t)
        {
            Tuple<DateTime, DateTime> new_pause = new Tuple<DateTime, DateTime>(t, DateTime.MinValue);
            Pauses.Add(new_pause);
        }

        /// <summary>
        /// Closes out the last pause tuple in the list for this session
        /// </summary>
        /// <param name="t">The timestamp at which the pause was finished</param>
        public bool ClosePause (DateTime t)
        {
            if (Pauses != null)
            {
                Tuple<DateTime, DateTime> last_pause = Pauses.LastOrDefault();
                if (last_pause != null)
                {
                    if (last_pause.Item2.Equals(DateTime.MinValue))
                    {
                        //Remove the last pause from the list
                        Pauses.RemoveAt(Pauses.Count - 1);

                        //Set the actual finish time of the pause
                        Tuple<DateTime, DateTime> new_last_pause = new Tuple<DateTime, DateTime>(last_pause.Item1, DateTime.Now);

                        //Insert the pause into the list
                        Pauses.Add(new_last_pause);

                        //Return a true value, indicating that a pause was closed
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion
    }

}
