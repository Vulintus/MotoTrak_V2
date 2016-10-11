using System.Collections.Generic;
using System.Linq;
using System.IO.Ports;
using MotoTrakBase;

namespace MotoTrak
{
    /// <summary>
    /// This is a view model class for the port selector UI.  It is a singleton class.
    /// </summary>
    public class PortSelectorViewModel : NotifyPropertyChangedObject
    {
        #region Private data members

        private int _selectedPortIndex = 0;

        #endregion

        #region Constructors - This is a singleton class

        private static PortSelectorViewModel _instance = null;

        /// <summary>
        /// </summary>
        private PortSelectorViewModel()
        {
            //Read in the booth pairings before instantiating this view model
            MotoTrakConfiguration.GetInstance().ReadBoothPairings();
        }

        /// <summary>
        /// Gets the one and only instance of this class that is allowed to exist.
        /// </summary>
        /// <returns>Instance of ArdyMotorBoard class</returns>
        public static PortSelectorViewModel GetInstance()
        {
            if (_instance == null)
            {
                _instance = new PortSelectorViewModel();
            }

            return _instance;
        }


        #endregion

        #region Properties

        /// <summary>
        /// The list of port names that we can connect to
        /// </summary>
        public List<USBDeviceViewModel> AvailablePorts
        {
            get
            {
                var devices = MotorBoard.QueryConnectedArduinoDevices();
                /*string[] portNames = SerialPort.GetPortNames();
                if (portNames != null && portNames.Length > 0)
                {
                    List<string> portNamesList = portNames.ToList();
                    return portNamesList;
                }

                return new List<string>() { "No ports found" };*/
                var device_viewmodels = devices.Select(x => new USBDeviceViewModel(x)).ToList();
                return device_viewmodels;
            }
        }

        /// <summary>
        /// The number of ports that are available to connect to 
        /// </summary>
        public int AvailablePortCount
        {
            get
            {
                var devices = MotorBoard.QueryConnectedArduinoDevices();
                return devices.Count;
                /*string[] portNames = SerialPort.GetPortNames();
                if (portNames != null && portNames.Length > 0)
                {
                    return portNames.Length;
                }

                return 0;*/
            }
        }

        /// <summary>
        /// The index of the selected port in the list of port names
        /// </summary>
        public int SelectedPortIndex
        {
            get
            {
                return _selectedPortIndex;
            }
            set
            {
                _selectedPortIndex = value;
                NotifyPropertyChanged("SelectedPortIndex");
            }
        }

        #endregion
    }
}
