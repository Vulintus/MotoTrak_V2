using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakBase
{
    public class MotorTrialEvent
    {
        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public MotorTrialEvent()
        {
            //empty
        }

        #endregion

        #region Properties

        /// <summary>
        /// The type of event that this is
        /// </summary>
        public MotorTrialEventType EventType = MotorTrialEventType.UndefinedEvent;

        /// <summary>
        /// The index into the array of trial samples that this event occurs at
        /// </summary>
        public int EventIndex = 0;

        /// <summary>
        /// Indicates whether the event has been handled by MotoTrak yet
        /// </summary>
        public bool Handled = false;

        #endregion
    }
}
