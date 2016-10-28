using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace MotoTrakLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void RunSession_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(@"MotoTrak.exe");
            this.Close();
        }

        private void Calibrate_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(@"MotoTrakCalibration.exe");
            this.Close();
        }

        private void StageDesigner_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(@"StageDesigner.exe");
            this.Close();
        }

        private void Analysis_Click(object sender, RoutedEventArgs e)
        {
            //not yet implemented
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
