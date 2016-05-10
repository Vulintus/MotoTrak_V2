using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakBase
{
    public class MotoTrakSession : NotifyPropertyChangedObject
    {
        #region Private enumerated types

        /// <summary>
        /// This enumerates the possible states a session could be in.
        /// </summary>
        private enum SessionRunState
        {
            Scan,                   //This is the idle state
            TrialWait,
            TrialRun,
            TrialEnd,
            TrialManualFeed,
            Pause
        }

        #endregion

        #region Private data members

        private SessionRunState _sessionState = SessionRunState.Scan;
        private MotorBoard _ardy = MotorBoard.GetInstance();

        #endregion

        #region Private properties

        private MotorBoard ArdyBoard
        {
            get { return _ardy; }
            set { _ardy = value; }
        }

        private SessionRunState SessionState
        {
            get { return _sessionState; }
            set
            {
                _sessionState = value;
                NotifyPropertyChanged("SessionState");
            }
        }

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

        #region Methods

        public void InitializeSession(string comPort)
        {
            //Connect to the motortrak board
            ArdyBoard.ConnectToArduino(comPort);
            if (!ArdyBoard.IsSerialConnectionValid)
            {
                throw new MotoTrakException("Unable to connect to the MotoTrak controller!");
            }
        }

        #endregion
    }
}
