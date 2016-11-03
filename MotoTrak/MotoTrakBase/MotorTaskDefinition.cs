using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakBase
{
    /// <summary>
    /// A class used by task implementation files to define task parameters
    /// </summary>
    public class MotorTaskDefinition
    {
        #region Constructor

        public MotorTaskDefinition ()
        {
            //empty
        }

        #endregion

        #region Fields

        public string TaskName = string.Empty;
        public string TaskDescription = string.Empty;

        public MotorDeviceType RequiredDeviceType = MotorDeviceType.Unknown;
        public MotorTaskParameter DevicePosition = new MotorTaskParameter();

        public MotorTaskParameter PreTrialDuration = new MotorTaskParameter();
        public MotorTaskParameter HitWindowDuration = new MotorTaskParameter();
        public MotorTaskParameter PostTrialDuration = new MotorTaskParameter();
        public MotorTaskParameter PostTrialTimeout = new MotorTaskParameter();
        
        public List<MotorTaskParameter> TaskParameters = new List<MotorTaskParameter>();
        public List<string> OutputTriggerOptions = new List<string>();
        
        #endregion
    }
}
