using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakBase
{
    /// <summary>
    /// Class that defines an action that needs to take place by MotoTrak
    /// </summary>
    public class MotorTrialAction
    {
        #region Private data members

        private MotorTrialActionType _actionType = MotorTrialActionType.Unknown;
        private Dictionary<object, object> _parameters = new Dictionary<object, object>();
        private DateTime _actionTime = DateTime.Now;
        private bool _completed = false;

        #endregion

        #region

        /// <summary>
        /// Parameters that need to be defined when defining an action that plays a sound.
        /// </summary>
        public enum SoundActionParameterType
        {
            SoundDuration,
            SoundFrequency
        }

        /// <summary>
        /// Parameters that need to be defined when specifying an action for the autopositioner.
        /// </summary>
        public enum AutopositionerParameterType
        {
            Position
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        public MotorTrialAction()
        {
            //empty
        }

        #endregion

        #region Public properties

        /// <summary>
        /// The type of action that is to be taken
        /// </summary>
        public MotorTrialActionType ActionType
        {
            get
            {
                return _actionType;
            }
            set
            {
                _actionType = value;
            }
        }

        /// <summary>
        /// The parameters of the action that needs to take place
        /// </summary>
        public Dictionary<object, object> ActionParameters
        {
            get
            {
                return _parameters;
            }
            set
            {
                _parameters = value;
            }
        }

        /// <summary>
        /// The time at which this action is supposed to take place
        /// </summary>
        public DateTime ActionTime
        {
            get
            {
                return _actionTime;
            }
            set
            {
                _actionTime = value;
            }
        }

        /// <summary>
        /// Whether or not this action has been completed
        /// </summary>
        public bool Completed
        {
            get
            {
                return _completed;
            }
            private set
            {
                _completed = value;
            }
        }

        #endregion

        #region Public methods

        public void ExecuteAction()
        {
            if (DateTime.Now >= ActionTime)
            {
                switch (this.ActionType)
                {
                    case MotorTrialActionType.AdjustAutopositionerPosition:

                        //Unbox the double from the object
                        double position = (double)this.ActionParameters[AutopositionerParameterType.Position];

                        //Perform the autopositioner action
                        MotoTrakAutopositioner.GetInstance().SetPosition(position);

                        break;
                    case MotorTrialActionType.PlaySound:

                        //Unbox the data from the dictionary
                        double frequency = (double)this.ActionParameters[SoundActionParameterType.SoundFrequency];
                        double duration = (double)this.ActionParameters[SoundActionParameterType.SoundDuration];

                        //Perform the sound action
                        //TO DO: complete this

                        break;
                    case MotorTrialActionType.SendStimulationTrigger:

                        //Perform the stimulation trigger
                        MotorBoard.GetInstance().TriggerStim();

                        break;
                    case MotorTrialActionType.TriggerFeeder:

                        //Perform the feed
                        MotorBoard.GetInstance().TriggerFeeder();

                        break;
                }

                Completed = true;
            }
        }

        #endregion
    }
}
