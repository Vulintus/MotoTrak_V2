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
using System.Windows.Shapes;

namespace MotoTrakBoothLauncher
{
    /// <summary>
    /// Interaction logic for PortSelectionUI.xaml
    /// </summary>
    public partial class PortSelectionUI : Window
    {
        public PortSelectionUI()
        {
            InitializeComponent();
            
            PortSelectorViewModel viewModel = PortSelectorViewModel.GetInstance();
            DataContext = viewModel;
        }

        private void OK_Button_Click(object sender, RoutedEventArgs e)
        {
            PortSelectorViewModel vm = DataContext as PortSelectorViewModel;
            if (vm != null)
            {
                vm.ResultOK = true;
            }

            //Close the port selector window
            this.Close();
        }

        private void RefreshButtonClick(object sender, RoutedEventArgs e)
        {
            PortSelectorViewModel vm = DataContext as PortSelectorViewModel;
            if (vm != null)
            {
                vm.RefreshPortListing();
            }
        }
    }
}
