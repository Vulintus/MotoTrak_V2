using MotoTrakBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MotoTrakBoothLauncher
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
            
            //Query the devices
            HardQueryOfDevices();
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

        #region Private data members

        List<USBDeviceInfo> _available_port_list = new List<USBDeviceInfo>();
        private bool _result_ok = false;

        #endregion

        #region Private methods

        private void HardQueryOfDevices()
        {
            _available_port_list = MotorBoard.QueryConnectedArduinoDevices();
            NotifyPropertyChanged("AvailablePorts");
            NotifyPropertyChanged("AvailablePortCount");
            NotifyPropertyChanged("NoBoothsTextVisibility");
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
                var device_viewmodels = _available_port_list.Select(x => new USBDeviceViewModel(x)).ToList();
                device_viewmodels.Sort((x, y) => x.BoothName.CompareTo(y.BoothName));
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
                return _available_port_list.Count;
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

        /// <summary>
        /// Whether or not the user actually chose a port
        /// </summary>
        public bool ResultOK
        {
            get
            {
                return _result_ok;
            }
            set
            {
                _result_ok = value;
            }
        }

        /// <summary>
        /// Indicates whether the message that says whether booths are available is visible to the user or not.
        /// </summary>
        public Visibility NoBoothsTextVisibility
        {
            get
            {
                if (AvailablePortCount > 0)
                {
                    return Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Visible;
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Forces the GUI to refresh the list of available ports
        /// </summary>
        public void RefreshPortListing()
        {
            this.HardQueryOfDevices();
        }

        #endregion
    }
}
