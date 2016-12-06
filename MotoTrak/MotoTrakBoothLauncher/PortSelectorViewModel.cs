using MotoTrakBase;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        private bool _currently_refreshing = false;

        private SimpleCommand _refresh_command;

        private BackgroundWorker _refresh_thread = null;

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
            ToggleRefresh();
            //HardQueryOfDevices();
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

        /// <summary>
        /// The content that appears on the OK button
        /// </summary>
        public string SelectBoothButtonContent
        {
            get
            {
                if (_currently_refreshing)
                {
                    return "Please wait";
                }
                else
                {
                    return "OK";
                }
            }
        }

        /// <summary>
        /// Text that is displayed if no MotoTrak booths are currently detected
        /// </summary>
        public string NoBoothsDetectedText
        {
            get
            {
                if (_currently_refreshing)
                {
                    return "Searching for MotoTrak booths...";
                }
                else
                {
                    return "No MotoTrak booths were detected!";
                }
            }
        }

        /// <summary>
        /// Whether or not to enable the button that allows booth selection
        /// </summary>
        public bool SelectBoothButtonEnabled
        {
            get
            {
                if (_currently_refreshing)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        #endregion

        #region Commands

        public SimpleCommand RefreshCommand
        {
            get
            {
                return _refresh_command ?? (_refresh_command = new SimpleCommand(() => ToggleRefresh(), true));
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Kicks off a refresh of the booth listing in the booth selection box
        /// </summary>
        public void ToggleRefresh ()
        {
            if (_refresh_thread == null  || !_refresh_thread.IsBusy)
            {
                //Set up the necessary code for the background thread to refresh the booth listing
                _refresh_thread = new BackgroundWorker();
                _refresh_thread.WorkerReportsProgress = true;
                _refresh_thread.WorkerSupportsCancellation = true;
                _refresh_thread.DoWork += delegate
                {
                    _available_port_list = MotorBoard.QueryConnectedArduinoDevices();
                };
                _refresh_thread.ProgressChanged += delegate
                {
                    //code goes here
                };
                _refresh_thread.RunWorkerCompleted += delegate
                {
                    _currently_refreshing = false;
                    NotifyPropertyChanged("AvailablePorts");
                    NotifyPropertyChanged("AvailablePortCount");
                    NotifyPropertyChanged("NoBoothsTextVisibility");
                    NotifyPropertyChanged("NoBoothsDetectedText");
                    NotifyPropertyChanged("SelectBoothButtonContent");
                    NotifyPropertyChanged("SelectBoothButtonEnabled");
                };

                //Set a flag indicating that the booth list is being refreshed
                _currently_refreshing = true;

                //Run the background thread to refresh the booth listing
                _refresh_thread.RunWorkerAsync();
            }
        }

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
