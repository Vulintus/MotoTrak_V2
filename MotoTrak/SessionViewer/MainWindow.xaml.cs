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

namespace SessionViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            //Set the data context of the class
            DataContext = new SessionViewerViewModel();
        }
        
        private void TrialViewerScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SessionViewerViewModel vm = DataContext as SessionViewerViewModel;
            if (vm != null)
            {
                vm.SetCurrentTrial(Convert.ToInt32(e.NewValue));
            }
        }
    }
}
