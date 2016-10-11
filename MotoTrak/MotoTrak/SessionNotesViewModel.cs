using MotoTrakBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrak
{
    /// <summary>
    /// A view-model class for displaying all session notes
    /// </summary>
    public class SessionNotesViewModel : NotifyPropertyChangedObject
    {
        #region Private data members

        MotoTrakSession _session_model = null;
        int _selected_note_index = 0;

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="session">A MotoTrak session</param>
        public SessionNotesViewModel(MotoTrakSession session)
        {
            SessionModel = session;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The model session
        /// </summary>
        public MotoTrakSession SessionModel
        {
            get
            {
                return _session_model;
            }
            private set
            {
                _session_model = value;
                NotifyPropertyChanged("SessionModel");
            }
        }

        /// <summary>
        /// General session notes
        /// </summary>
        public string SessionNotes
        {
            get
            {
                return SessionModel.SessionNotes;
            }
            set
            {
                SessionModel.SessionNotes = value;
                NotifyPropertyChanged("SessionNotes");
            }
        }

        /// <summary>
        /// The list of notes by timestamp
        /// </summary>
        public List<string> SessionTimestampedNotes
        {
            get
            {
                if (SessionModel.TimestampedNotes.Count > 0)
                    return SessionModel.TimestampedNotes.Select(x => x.Item1.ToShortTimeString()).ToList();
                else return new List<string>();
            }
        }

        /// <summary>
        /// The index of the selected note among all the timestamped notes
        /// </summary>
        public int SelectedNoteIndex
        {
            get
            {
                return _selected_note_index;
            }
            set
            {
                _selected_note_index = value;
                NotifyPropertyChanged("SelectedNoteIndex");
                NotifyPropertyChanged("SelectedTimestampedNoteText");
            }
        }

        /// <summary>
        /// The text of the selected note among all of the timestamped notes
        /// </summary>
        public string SelectedTimestampedNoteText
        {
            get
            {
                if (SessionModel.TimestampedNotes.Count > 0 && SelectedNoteIndex < SessionModel.TimestampedNotes.Count)
                    return SessionModel.TimestampedNotes[SelectedNoteIndex].Item2;
                else return string.Empty;
            }
            set
            {
                if (SessionModel.TimestampedNotes.Count > 0 && SelectedNoteIndex < SessionModel.TimestampedNotes.Count)
                {
                    DateTime k = SessionModel.TimestampedNotes[SelectedNoteIndex].Item1;
                    SessionModel.TimestampedNotes[SelectedNoteIndex] = new Tuple<DateTime, string>(k, value);    
                }

                NotifyPropertyChanged("SelectedTimestampedNoteText");
            }
        }

        #endregion

        #region Methods

        public void UpdateView()
        {
            NotifyPropertyChanged("SelectedTimestampedNoteText");
            NotifyPropertyChanged("SelectedNoteIndex");
            NotifyPropertyChanged("SessionTimestampedNotes");
            NotifyPropertyChanged("SessionNotes");
        }

        #endregion
    }
}
