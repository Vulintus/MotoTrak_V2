using Microsoft.Win32;
using MotoTrakBase;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace StageDesigner
{
    /// <summary>
    /// A main view-model class for the StageDesigner app
    /// </summary>
    public class StageDesignerViewModel : NotifyPropertyChangedObject
    {
        #region Private data members

        private ObservableCollection<StageViewModel> _open_stages = new ObservableCollection<StageViewModel>();
        private int _selected_stage_index = 0;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        public StageDesignerViewModel()
        {
            _open_stages.CollectionChanged += _open_stages_CollectionChanged;
        }

        #endregion

        #region Event listeners
        
        private void _open_stages_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged("OpenStages");
            NotifyPropertyChanged("StageFileTabControlVisibility");
            NotifyPropertyChanged("NoStagesOpenMessageVisibility");
        }

        #endregion

        #region Properties

        /// <summary>
        /// Returns a list of stages that are open for editing
        /// </summary>
        public ObservableCollection<StageViewModel> OpenStages
        {
            get
            {
                return _open_stages;
            }
        }

        /// <summary>
        /// The index of the selected stage in the list of open stages
        /// </summary>
        public int SelectedStageIndex
        {
            get
            {
                return _selected_stage_index;
            }
            set
            {
                _selected_stage_index = value;
                NotifyPropertyChanged("SelectedStageIndex");
            }
        }

        /// <summary>
        /// The visibility of the stage tab control, determine by whether or not any stages are open
        /// </summary>
        public Visibility StageFileTabControlVisibility
        {
            get
            {
                if (this.OpenStages == null || this.OpenStages.Count == 0)
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
        /// Indicates whether or not to display the message to the user that tells the user that
        /// there are no stages currently open for editing.
        /// </summary>
        public Visibility NoStagesOpenMessageVisibility
        {
            get
            {
                if (this.OpenStages == null || this.OpenStages.Count == 0)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }

        #endregion

        #region Methods to save and open stages

        /// <summary>
        /// Creates a new stage for the user to edit in the GUI
        /// </summary>
        public void CreateNewStage ()
        {
            //Create a new stage, a view-model for the new stage, and add the view-model
            //to the list of "Open Stages" in the GUI.
            this.OpenStages.Add(new StageViewModel(null));

            //Change the selected stage index to be that of the new stage
            this.SelectedStageIndex = this.OpenStages.Count - 1;
        }

        /// <summary>
        /// Allows the user to open an existing stage file
        /// </summary>
        public void OpenStageFile ()
        {
            //Create an open-file dialog to allow the user to open a stage file
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.DefaultExt = ".MotorStage";
            dlg.Filter = "MotoTrak Stage Files |*.MotorStage";

            //Show the dialog to the user
            Nullable<bool> result = dlg.ShowDialog();

            //Get the selected file name and open the stage file
            if (result.HasValue && result.Value)
            {
                //Open the stage file
                string file_name = dlg.FileName;

                //Create a file info object for the selected file
                FileInfo stage_file_info = new FileInfo(file_name);

                //Create the MotorStage object
                MotorStage new_stage = MotorStage.LoadStageFromFile(file_name);

                //Create a view-model for the MotorStage
                StageViewModel stage_vm = new StageViewModel(new_stage);

                //Add the stage view-model to our list of stages that are open
                this.OpenStages.Add(stage_vm);

                //Change the selected stage index to be that of the new stage
                this.SelectedStageIndex = this.OpenStages.Count - 1;
            }
        }

        /// <summary>
        /// Allows the user to save the currently selected stage file
        /// </summary>
        public void SaveSelectedStageFile ()
        {
            if (this.OpenStages.Count > 0 && this.SelectedStageIndex >= 0 && this.SelectedStageIndex < this.OpenStages.Count)
            {
                //Grab the currently selected stage
                MotorStage cur_stage = this.OpenStages[this.SelectedStageIndex].StageModel;

                //Check to see if a file name has previously been defined for this stage
                if (string.IsNullOrEmpty(cur_stage.StageFile))
                {
                    //If not, then go through the "Save-As" process for saving the stage file
                    this.SaveStageFileAs();
                }
                else
                {
                    //If so, then simply save the stage file to the current file name
                    MotorStage.SaveStageToFile(cur_stage, cur_stage.StageFile);
                }
            }
        }

        /// <summary>
        /// Allows the user to choose a name for the currently selected stage file and save it
        /// </summary>
        public void SaveStageFileAs ()
        {
            //Create a save-file dialog so the user can select a file name for this stage
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.DefaultExt = ".MotorStage";
            dlg.Filter = "MotoTrak Stage Files |*.MotorStage";

            //Show the save dialog to the user
            Nullable<bool> result = dlg.ShowDialog();

            //Save the currently selected stage to the file that the user selected
            if (result.HasValue && result.Value)
            {
                //Get the file name
                string file_name = dlg.FileName;

                //Create a file info object for the selected file
                FileInfo stage_file_info = new FileInfo(file_name);

                //Set the file name of the currently selected stage
                if (this.OpenStages.Count > 0 && this.SelectedStageIndex >= 0 && this.SelectedStageIndex < this.OpenStages.Count)
                {
                    this.OpenStages[this.SelectedStageIndex].StageModel.StageFilePath = stage_file_info.DirectoryName;
                    this.OpenStages[this.SelectedStageIndex].StageModel.StageFileName = stage_file_info.Name;

                    //Save the stage file
                    this.SaveSelectedStageFile();
                }
            }
        }

        /// <summary>
        /// Closes the currently selected stage
        /// </summary>
        public void CloseSelectedStage ()
        {
            //Remove the selected stage from the list of open stages to close it.
            this.OpenStages.RemoveAt(this.SelectedStageIndex);

            //Make sure the selected stage index is still valid
            if (this.SelectedStageIndex >= this.OpenStages.Count)
            {
                this.SelectedStageIndex = Math.Max(0, this.OpenStages.Count - 1);
            }
        }

        /// <summary>
        /// Closes all open stages in the GUI
        /// </summary>
        public void CloseAllStages ()
        {
            //Clear the list of open stages to close all stages
            this.OpenStages.Clear();

            //Update the selected stage index
            this.SelectedStageIndex = 0;
        }

        /// <summary>
        /// Closes a specific tab at a specified index
        /// </summary>
        /// <param name="index">The index of the tab to close</param>
        public void CloseStageTabAtIndex (int index)
        {
            this.OpenStages.RemoveAt(index);

            //Make sure the selected stage index is still valid
            if (this.SelectedStageIndex >= this.OpenStages.Count)
            {
                this.SelectedStageIndex = Math.Max(0, this.OpenStages.Count - 1);
            }
        }

        #endregion
    }
}
