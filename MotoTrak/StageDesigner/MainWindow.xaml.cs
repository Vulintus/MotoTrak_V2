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
using Microsoft.Win32;
using System.IO;

namespace StageDesigner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            //Read in the MotoTrak configuration file
            var mototrak_config = MotoTrakConfiguration.GetInstance();
            mototrak_config.ReadConfigurationFile();
            mototrak_config.InitializeStageImplementations();

            DataContext = new StageDesignerViewModel();
        }

        private void NewStage_Click (object sender, RoutedEventArgs e)
        {
            StageDesignerViewModel vm = DataContext as StageDesignerViewModel;
            if (vm != null)
            {
                vm.CreateNewStage();
            }
        }

        private void OpenStage_Click(object sender, RoutedEventArgs e)
        {
            StageDesignerViewModel vm = DataContext as StageDesignerViewModel;
            if (vm != null)
            {
                vm.OpenStageFile();
            }
        }

        private void SaveStage_Click(object sender, RoutedEventArgs e)
        {
            StageDesignerViewModel vm = DataContext as StageDesignerViewModel;
            if (vm != null)
            {
                vm.SaveSelectedStageFile();
            }
        }

        private void SaveStageAs_Click(object sender, RoutedEventArgs e)
        {
            StageDesignerViewModel vm = DataContext as StageDesignerViewModel;
            if (vm != null)
            {
                vm.SaveStageFileAs();                
            }
        }

        private void CloseStage_Click(object sender, RoutedEventArgs e)
        {
            StageDesignerViewModel vm = DataContext as StageDesignerViewModel;
            if (vm != null)
            {
                vm.CloseSelectedStage();
            }
        }

        private void CloseAllStages_Click(object sender, RoutedEventArgs e)
        {
            StageDesignerViewModel vm = DataContext as StageDesignerViewModel;
            if (vm != null)
            {
                vm.CloseAllStages();
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            //Safely close all the stages in the list of open stages
            StageDesignerViewModel vm = DataContext as StageDesignerViewModel;
            if (vm != null)
            {
                vm.CloseAllStages();
            }

            //Close the window
            this.Close();
        }
    }
}
