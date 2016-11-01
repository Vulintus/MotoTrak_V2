using MahApps.Metro.Controls;
using MotoTrakBase;
using MotoTrakBoothLauncher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.DataVisualization.Charting;
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
            
            portSelectorWindow.ShowDialog();

            //Get the result of the port selection window
            PortSelectorViewModel portSelectorResult = PortSelectorViewModel.GetInstance();
            
            if (portSelectorResult.ResultOK && portSelectorResult.AvailablePortCount > 0)
            {
                string portName = portSelectorResult.AvailablePorts[portSelectorResult.SelectedPortIndex].Model.DeviceID;
                MotoTrakViewModel viewModel = DataContext as MotoTrakViewModel;
                if (viewModel != null)
                {
                    //Assign the win-forms plot
                    //viewModel.PlotViewModel.WinFormsPlot = WinFormsPlot;
                    //viewModel.PlotViewModel.InitializePlot();

                    //Subscribe to events from the session notes view
                    MainWindowSessionNotesView.CloseSessionNotes += MainWindowSessionNotesView_CloseSessionNotes;

                    //Initialize MotoTrak
                    viewModel.InitializeMotoTrak(portName);
                }
            }
            else
            {
                //If the user did not choose a port to connect to, go ahead and close the main window
                this.Close();
            }
        }
        
        private void StartButtonClick(object sender, RoutedEventArgs e)
        {
            MotoTrakViewModel viewModel = DataContext as MotoTrakViewModel;
            if (viewModel != null)
            {
                if (viewModel.IsSessionRunning)
                {
                    viewModel.StopSession();

                    SessionNotesViewModel vm = MainWindowSessionNotesView.DataContext as SessionNotesViewModel;
                    if (vm != null)
                    {
                        vm.UpdateView();
                    }
                }
                else
                {
                    viewModel.StartSession();

                    //Set the data context of the notes view
                    MainWindowSessionNotesView.DataContext = new SessionNotesViewModel(MotoTrakModel.GetInstance().CurrentSession);
                }
            }
        }

        private void PauseButtonClick(object sender, RoutedEventArgs e)
        {
            MotoTrakViewModel viewModel = DataContext as MotoTrakViewModel;
            if (viewModel != null)
            {
                viewModel.TogglePause();
            }
        }
        
        private void FeedButtonClick(object sender, RoutedEventArgs e)
        {
            MotoTrakViewModel viewModel = DataContext as MotoTrakViewModel;
            if (viewModel != null)
            {
                viewModel.TriggerManualFeed();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            MotoTrakViewModel viewModel = DataContext as MotoTrakViewModel;
            if (viewModel != null)
            {
                viewModel.ShutdownMotoTrak();
                MotorBoard.GetInstance().DisconnectFromArduino();
            }
        }

        private void MessagesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MessagesListBox.Items != null && MessagesListBox.Items.Count > 0)
            {
                //MessagesListBox.ScrollIntoView(MessagesListBox.Items[MessagesListBox.Items.Count - 1]);
                ListBoxAutomationPeer svAutomation = (ListBoxAutomationPeer)ScrollViewerAutomationPeer.CreatePeerForElement(MessagesListBox);

                IScrollProvider scrollInterface = (IScrollProvider)svAutomation.GetPattern(PatternInterface.Scroll);
                System.Windows.Automation.ScrollAmount scrollVertical = System.Windows.Automation.ScrollAmount.LargeIncrement;
                System.Windows.Automation.ScrollAmount scrollHorizontal = System.Windows.Automation.ScrollAmount.NoAmount;
                //If the vertical scroller is not available, the operation cannot be performed, which will raise an exception. 
                if (scrollInterface.VerticallyScrollable)
                    scrollInterface.Scroll(scrollHorizontal, scrollVertical);
            }
        }

        private void ResetBaselineButtonClick(object sender, RoutedEventArgs e)
        {
            MotoTrakViewModel viewModel = DataContext as MotoTrakViewModel;
            if (viewModel != null)
            {
                viewModel.ResetBaseline();
            }
        }

        private void PlotView_MouseEnter(object sender, MouseEventArgs e)
        {
            MotoTrakViewModel viewModel = DataContext as MotoTrakViewModel;
            if (viewModel != null)
            {
                if (viewModel.PlotViewModel != null)
                {
                    viewModel.PlotViewModel.HandleMouseHover(true);
                }
            }
        }

        private void PlotView_MouseLeave(object sender, MouseEventArgs e)
        {
            MotoTrakViewModel viewModel = DataContext as MotoTrakViewModel;
            if (viewModel != null)
            {
                if (viewModel.PlotViewModel != null)
                {
                    viewModel.PlotViewModel.HandleMouseHover(false);
                }
            }
        }

        private void AddNoteButtonClick (object sender, RoutedEventArgs e)
        {
            MotoTrakViewModel viewModel = DataContext as MotoTrakViewModel;
            if (viewModel != null)
            {
                viewModel.ShowAddNotePanel();
            }
        }

        private void CancelAddNote(object sender, RoutedEventArgs e)
        {
            MotoTrakViewModel viewModel = DataContext as MotoTrakViewModel;
            if (viewModel != null)
            {
                viewModel.CloseAddNotePanel(true);
            }
        }

        private void SaveNewNoteClick(object sender, RoutedEventArgs e)
        {
            MotoTrakViewModel viewModel = DataContext as MotoTrakViewModel;
            if (viewModel != null)
            {
                viewModel.CloseAddNotePanel(false);
            }
        }

        private void MainWindowSessionNotesView_CloseSessionNotes(object sender, RoutedEventArgs e)
        {
            MotoTrakViewModel viewModel = DataContext as MotoTrakViewModel;
            if (viewModel != null)
            {
                viewModel.FinalizeSession();
            }
        }

        private void StageSelectionComboBox_DropDownClosed(object sender, EventArgs e)
        {
            MotoTrakViewModel viewModel = DataContext as MotoTrakViewModel;
            if (viewModel != null)
            {
                viewModel.SetStageSelection();
            }
        }
    }
}
