using MotoTrakBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MotoTrak
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
            if (portSelectorResult.AvailablePortCount > 0)
            {
                string portName = portSelectorResult.AvailablePorts[portSelectorResult.SelectedPortIndex];
                SessionViewModel viewModel = DataContext as SessionViewModel;
                if (viewModel != null)
                {
                    viewModel.InitializeSession(portName);
                }
            }
        }

        private void StartButtonClick(object sender, RoutedEventArgs e)
        {
            SessionViewModel viewModel = DataContext as SessionViewModel;
            if (viewModel != null)
            {
                if (viewModel.IsSessionRunning)
                {
                    viewModel.StopSession();
                }
                else
                {
                    viewModel.StartSession();
                }
            }
        }

        private void PauseButtonClick(object sender, RoutedEventArgs e)
        {
            SessionViewModel viewModel = DataContext as SessionViewModel;
            if (viewModel != null)
            {
                viewModel.TogglePause();
            }
        }
        
        private void FeedButtonClick(object sender, RoutedEventArgs e)
        {
            SessionViewModel viewModel = DataContext as SessionViewModel;
            if (viewModel != null)
            {
                viewModel.TriggerManualFeed();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            SessionViewModel viewModel = DataContext as SessionViewModel;
            if (viewModel != null)
            {
                viewModel.CancelStreaming();
                MotorBoard.GetInstance().DisconnectFromArduino();
            }
        }
    }
}
