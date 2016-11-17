using System.Windows;
using System.Speech.Synthesis;
using MotoTrakBoothLauncher;
using MotoTrakBase;
using System;

namespace MotoTrakCalibration
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            //Create a window to allow the user to select a port
            PortSelectionUI portSelectorWindow = new PortSelectionUI();

            //Show the dialog
            portSelectorWindow.ShowDialog();

            //Get the result of the port selection window
            PortSelectorViewModel portSelectorResult = PortSelectorViewModel.GetInstance();
            if (portSelectorResult.ResultOK && portSelectorResult.AvailablePortCount > 0)
            {
                //Get the port name the user wants to connect to
                string portName = portSelectorResult.AvailablePorts[portSelectorResult.SelectedPortIndex].Model.DeviceID;

                //Connect to the board
                IMotorBoard motor_board = MotorBoard.GetInstance();
                motor_board.ConnectToArduino(portName);

                //Query the booth number
                string booth_label = motor_board.GetBoothLabel();

                //Query the motor device that is connected to the motor board
                MotorDevice device = motor_board.GetMotorDevice();
                
                //Save the booth pairings
                MotoTrakConfiguration.GetInstance().BoothPairings[portName] = booth_label;
                MotoTrakConfiguration.GetInstance().SaveBoothPairings();
                
                //Check to see which type of device is connected to the board
                if (device.DeviceType == MotorDeviceType.Pull)
                {
                    //Save the device to the calibration model
                    PullCalibrationModel.GetInstance().PullDevice = device;

                    //Create a view-model object
                    PullCalibrationViewModel vm = new PullCalibrationViewModel(booth_label, portName, device, PullCalibrationModel.GetInstance());

                    //Set the data context for the window
                    DataContext = vm;

                    //Set the data context of the pull calibration control
                    this.PullCalibrationControl.DataContext = vm;

                    //Kick off a background thread to start streaming device data from the motor board.
                    DeviceStreamModel.GetInstance().StartStreaming();
                }
                else if (device.DeviceType == MotorDeviceType.Lever)
                {
                    //Save the device to the calibration model
                    LeverCalibrationModel.GetInstance().LeverDevice = device;
                    LeverCalibrationModel.GetInstance().SavedMaxValue = Convert.ToInt32(device.Baseline);
                    int range = -Convert.ToInt32(Convert.ToDouble(MotorDevice.LeverRangeInDegrees / device.Slope));
                    int min_value = Convert.ToInt32(device.Baseline) - range;
                    LeverCalibrationModel.GetInstance().SavedMinValue = min_value;
                    
                    //Create a view-model object
                    LeverCalibrationViewModel vm = new LeverCalibrationViewModel(booth_label, portName, device, LeverCalibrationModel.GetInstance());

                    //Set the data context for the window
                    DataContext = vm;

                    //Set the data context of the lever calibration control
                    this.LeverCalibrationControl.DataContext = vm;

                    //Kick off a background thread to start streaming device data from the motor board
                    DeviceStreamModel.GetInstance().StartStreaming();
                }
                else if (device.DeviceType == MotorDeviceType.Knob)
                {
                    //Create a view-model object
                    KnobCalibrationViewModel vm = new KnobCalibrationViewModel(booth_label, portName, device);

                    //Set the data context for the window
                    DataContext = vm;

                    //Set the data context of the knob calibration control
                    this.KnobCalibrationControl.DataContext = vm;

                    //Kick off a background thread to start streaming device data from the motor board
                    DeviceStreamModel.GetInstance().StartStreaming();
                }
                else
                {
                    //If the device was unrecognized, then close the window.
                    var msg_box_result = MessageBox.Show("Error: Unable to detect device connected to MotoTrak controller! The application will now close.");
                    ErrorLoggingService.GetInstance().LogStringError("Error: Unable to detect device connected to MotoTrak controller!");
                    this.Close();
                }
            }
            else
            {
                //If the user did not choose a port to connect to, go ahead and close the main window
                this.Close();
            }
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            //Cancel device streaming if necessary
            DeviceStreamModel.GetInstance().StopStreaming();

            //Disconnect from the Ardunio board if necessary
            MotorBoard.GetInstance().DisconnectFromArduino();
        }
    }
}
