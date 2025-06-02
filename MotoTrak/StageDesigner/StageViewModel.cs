using MotoTrakBase;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace StageDesigner
{
    /// <summary>
    /// This view-model represents a stage in the StageDesigner app
    /// </summary>
    public class StageViewModel : NotifyPropertyChangedObject
    {
        #region Constructors

        /// <summary>
        /// Constructor for the stage view-model class
        /// </summary>
        public StageViewModel(MotorStage stage_to_edit)
        {
            if (stage_to_edit == null)
            {
                StageModel = new MotorStage();
                StageParameters.CollectionChanged += StageParameters_CollectionChanged;
                RefreshViewAfterNewTaskSelection();
            }
            else
            {
                //Choose a default stage implementation if the MotorStage object doesn't already have one
                if (stage_to_edit.StageImplementation == null)
                {
                    var list_of_tasks = GetOrderedListOfPythonStageImplementations();
                    if (list_of_tasks != null && list_of_tasks.Count > 0)
                    {
                        stage_to_edit.StageImplementation = list_of_tasks.FirstOrDefault().Item2;
                    }
                }

                StageModel = stage_to_edit;

                StageParameters.CollectionChanged += StageParameters_CollectionChanged;

                SetSelectedTaskIndex();

                RefreshView();
            }
        }

        #endregion

        #region Functions that watch for changes on the stage parameters

        private void StageParameters_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            //Notify the UI that it has changed
            NotifyPropertyChanged("StageParameters");
        }
        
        private void StageParameterViewModelPropertyChangedHandler(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged("StageTimingMessage");
        }

        #endregion

        #region Private data members

        private int _selected_task_index = 0;
        private string _stage_name = string.Empty;
        private string _stage_description = string.Empty;

        private StageParameterControlViewModel _pre_trial_recording_duration_vm = null;
        private StageParameterControlViewModel _hit_window_duration_vm = null;
        private StageParameterControlViewModel _post_hit_window_recording_duration_vm = null;
        private StageParameterControlViewModel _post_trial_timeout_duration_vm = null;
        private StageParameterControlViewModel _device_position_vm = null;
        private ObservableCollection<StageParameterControlViewModel> _stage_parameters = new ObservableCollection<StageParameterControlViewModel>();

        private MotorStage _stage = null;

        #endregion

        #region Private methods

        private void RefreshView ()
        {
            //Get the task parameters
            PythonTaskImplementation k = StageModel.StageImplementation as PythonTaskImplementation;
            if (k != null)
            {
                //Create view-models for the basic stage parameters
                PreTrialRecordingDurationViewModel = new StageParameterControlViewModel(_stage.PreTrialSamplingPeriodInSeconds, k.TaskDefinition.PreTrialDuration);
                HitWindowDurationViewModel = new StageParameterControlViewModel(_stage.HitWindowInSeconds, k.TaskDefinition.HitWindowDuration);
                PostHitWindowRecordingDurationViewModel = new StageParameterControlViewModel(_stage.PostTrialSamplingPeriodInSeconds, k.TaskDefinition.PostTrialDuration);
                PostTrialTimeoutDurationViewModel = new StageParameterControlViewModel(_stage.PostTrialTimeoutInSeconds, k.TaskDefinition.PostTrialTimeout);
                DevicePositionViewModel = new StageParameterControlViewModel(_stage.Position, k.TaskDefinition.DevicePosition);

                NotifyPropertyChanged("PreTrialRecordingDurationViewModel");
                NotifyPropertyChanged("HitWindowDurationViewModel");
                NotifyPropertyChanged("PostHitWindowRecordingDurationViewModel");
                NotifyPropertyChanged("PostTrialTimeoutDurationViewModel");
                NotifyPropertyChanged("DevicePositionViewModel");
            }

            //Subscribe to timing parameter view-model changes
            SubscribeToTimingParameterViewModelChanges();

            //Create view-models for each stage parameter
            InstantiateStageParameterViewModels();

            //Set the recommended device for this stage, based on the stage implementation
            SetDeviceToRecommendedDeviceForStageImplementation();
            
            NotifyPropertyChanged("SelectedTaskIndex");
            NotifyPropertyChanged("TaskDescriptionVisibility");
            NotifyPropertyChanged("TaskDescription");
            NotifyPropertyChanged("DevicePositionWarningVisibility");
            NotifyPropertyChanged("OutputTriggerOptions");
            NotifyPropertyChanged("OutputTriggerSelectedIndex");
        }

        private void RefreshViewAfterNewTaskSelection ()
        {
            //Set the stage implementation in the model stage object
            SetStageImplementation();

            //Clear the stage parameters dictionary in preparation to repopulate it with new stage parameters
            StageModel.StageParameters.Clear();

            //Instantiate required stage parameters for this stage, based on the stage implementation
            InstantiateRequiredStageParameters();

            //Reset the output trigger type
            StageModel.OutputTriggerType = string.Empty;
            PythonTaskImplementation k = StageModel.StageImplementation as PythonTaskImplementation;
            if (k != null)
            {
                if (k.TaskDefinition.OutputTriggerOptions != null && k.TaskDefinition.OutputTriggerOptions.Count > 0)
                {
                    StageModel.OutputTriggerType = k.TaskDefinition.OutputTriggerOptions[0];
                }
            }

            RefreshView();
        }

        private List<Tuple<string, PythonTaskImplementation>> GetOrderedListOfPythonStageImplementations ()
        {
            List<Tuple<string, PythonTaskImplementation>> result = new List<Tuple<string, PythonTaskImplementation>>();
            var stage_implementations = MotoTrakConfiguration.GetInstance().PythonStageImplementations.ToList();
            stage_implementations.Sort((x, y) => x.Key.CompareTo(y.Key));

            foreach (var kvp in stage_implementations)
            {
                PythonTaskImplementation this_stage_impl = kvp.Value as PythonTaskImplementation;
                if (this_stage_impl != null)
                {
                    result.Add(new Tuple<string, PythonTaskImplementation>(kvp.Key, this_stage_impl));
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Returns the currently selected PythonStageImplementation object, based on what
        /// the selected index is in the UI.
        /// </summary>
        private PythonTaskImplementation GetCurrentlySelectedPythonStageImplementation()
        {
            var ordered_list_of_stage_impls = GetOrderedListOfPythonStageImplementations();
            if (ordered_list_of_stage_impls != null && SelectedTaskIndex < ordered_list_of_stage_impls.Count)
            {
                return ordered_list_of_stage_impls[SelectedTaskIndex].Item2;
            }

            return null;
        }

        private int GetIndexOfModelStageImplementation ()
        {
            var ordered_list_of_stage_impls = GetOrderedListOfPythonStageImplementations();
            if (ordered_list_of_stage_impls != null)
            {
                int index = ordered_list_of_stage_impls.Select(x => x.Item2).ToList().IndexOf(StageModel.StageImplementation as PythonTaskImplementation);
                return index;
            }

            return -1;
        }

        /// <summary>
        /// Creates the required stage parameters on the StageModel object for the currently selected stage.
        /// </summary>
        private void InstantiateRequiredStageParameters ()
        {
            var currently_selected_stage_impl = GetCurrentlySelectedPythonStageImplementation();
            if (currently_selected_stage_impl != null)
            {
                var parameters = currently_selected_stage_impl.TaskDefinition.TaskParameters;
                if (parameters != null && parameters.Count > 0)
                {
                    foreach (var p in parameters)
                    {
                        //Create a new stage parameter
                        MotorStageParameter new_param = new MotorStageParameter()
                        {
                            ParameterName = p.ParameterName,
                            ParameterUnits = p.ParameterUnits,
                            IsQuantitative = p.IsQuantitative,
                            InitialValue = 0,
                            CurrentValue = 0
                        };

                        //Set a default value for the parameter if it is a nominal parameter
                        if (!p.IsQuantitative)
                        {
                            if (p.PossibleValues.Count > 0)
                            {
                                new_param.NominalValue = p.PossibleValues[0];
                            }
                        }

                        //Add it to our dictionary of stage parameters
                        StageModel.StageParameters[new_param.ParameterName] = new_param;
                    }
                }
            }
        }

        /// <summary>
        /// Creates view-model classes for each stage parameter that exists.
        /// </summary>
        private void InstantiateStageParameterViewModels ()
        {
            //Clear the list of stage parameters
            StageParameters.Clear();

            //Fetch the list of required task parameters
            PythonTaskImplementation k = StageModel.StageImplementation as PythonTaskImplementation;
            if (k != null)
            {
                var task_parameters = k.TaskDefinition.TaskParameters;
                
                //Create default stage parameters if there are any that do not exist
                foreach (var tp in task_parameters)
                {
                    if (!_stage.StageParameters.ContainsKey(tp.ParameterName))
                    {
                        var sp = MotorStageParameter.CreateStageParameterFromTaskParameter(tp);
                        StageParameterControlViewModel spvm = new StageParameterControlViewModel(sp, tp);
                        _stage.StageParameters[tp.ParameterName] = sp;
                    }
                }

                //Create a view-model for each stage parameter
                foreach (var sp in _stage.StageParameters)
                {
                    //Get the corresponding task parameter
                    var tp = task_parameters.Where(x => x.ParameterName.Equals(sp.Value.ParameterName)).FirstOrDefault();

                    //Create the stage parameter view-model
                    StageParameterControlViewModel spvm = new StageParameterControlViewModel(sp.Value, tp);
                    StageParameters.Add(spvm);
                }
            }
        }

        private void SubscribeToTimingParameterViewModelChanges ()
        {
            PreTrialRecordingDurationViewModel.PropertyChanged += StageParameterViewModelPropertyChangedHandler;
            HitWindowDurationViewModel.PropertyChanged += StageParameterViewModelPropertyChangedHandler;
            PostHitWindowRecordingDurationViewModel.PropertyChanged += StageParameterViewModelPropertyChangedHandler;
            PostTrialTimeoutDurationViewModel.PropertyChanged += StageParameterViewModelPropertyChangedHandler;
        }
        
        private void SetDeviceToRecommendedDeviceForStageImplementation ()
        {
            var stage_impl = GetCurrentlySelectedPythonStageImplementation();
            StageModel.DeviceType = stage_impl.TaskDefinition.RequiredDeviceType;

            NotifyPropertyChanged("SelectedDeviceString");
            NotifyPropertyChanged("DeviceSelectedIndex");
            NotifyPropertyChanged("DevicePositionWarningVisibility");
            NotifyPropertyChanged("DeviceTypeWarningVisibility");
        }

        private void SetStageImplementation ()
        {
            var stage_impls = GetOrderedListOfPythonStageImplementations();
            if (stage_impls != null && stage_impls.Count > 0 && _selected_task_index >= 0 && _selected_task_index < stage_impls.Count)
            {
                StageModel.StageImplementation = stage_impls[_selected_task_index].Item2;
            }
        }

        private void SetSelectedTaskIndex ( )
        {
            _selected_task_index = GetIndexOfModelStageImplementation();
            NotifyPropertyChanged("SelectedTaskIndex");
        }

        #endregion

        #region Properties

        /// <summary>
        /// The model MotorStage
        /// </summary>
        public MotorStage StageModel
        {
            get
            {
                return _stage;
            }
            private set
            {
                _stage = value;
            }
        }

        /// <summary>
        /// This is a list of all tasks that are available to choose from
        /// </summary>
        public List<string> TaskNames
        {
            get
            {
                List<string> result = new List<string>();
                var ordered_stage_impls = GetOrderedListOfPythonStageImplementations();
                foreach (var implementation_tuple in ordered_stage_impls)
                {
                    string this_name = implementation_tuple.Item2.TaskDefinition.TaskName + " (" + implementation_tuple.Item1 + ")";
                    result.Add(this_name);
                }

                return result;
            }
        }

        /// <summary>
        /// This is the description of the task being used for this stage
        /// </summary>
        public string TaskDescription
        {
            get
            {
                var currently_selected_stage_impl = GetCurrentlySelectedPythonStageImplementation();
                if (currently_selected_stage_impl != null)
                {
                    return currently_selected_stage_impl.TaskDefinition.TaskDescription;
                }

                return string.Empty;
            }
        }

        /// <summary>
        /// This indicates whether or not the task description in the GUI is visible to the user or not
        /// </summary>
        public Visibility TaskDescriptionVisibility
        {
            get
            {
                if (String.IsNullOrEmpty(TaskDescription))
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
        /// This indicates the index of the selected task in the array of all available tasks
        /// </summary>
        public int SelectedTaskIndex
        {
            get
            {
                return _selected_task_index;
            }
            set
            {
                //Get the newly selected index
                _selected_task_index = value;

                //Refresh the view based on the new selected task's parameters
                RefreshViewAfterNewTaskSelection();
            }
        }

        /// <summary>
        /// This appears as a tool tip when the user enters a new stage name
        /// </summary>
        public string StageNameTooltip
        {
            get
            {
                return "Keep it short and easily identifiable.  Examples: P1, L7, or PullStage10";
            }
        }

        /// <summary>
        /// This appears as a tool tip when the user enters a new stage description
        /// </summary>
        public string StageDescriptionTooltip
        {
            get
            {
                return "This is for a brief description of the stage.  Don't make it too long.";
            }
        }

        /// <summary>
        /// This is the name of the stage currently being created or edited.
        /// </summary>
        public string StageName
        {
            get
            {
                return StageModel.StageName;
            }
            set
            {
                StageModel.StageName = value;
                NotifyPropertyChanged("StageName");
                NotifyPropertyChanged("StageNameForTabHeader");
            }
        }

        /// <summary>
        /// This is the title of the stage that will be displayed in the tab header.
        /// It is usually the same as the stage name itself, but will be "New stage" if the stage has not yet been named.
        /// </summary>
        public string StageNameForTabHeader
        {
            get
            {
                if (!String.IsNullOrEmpty(StageName))
                {
                    return StageName;
                }
                else
                {
                    return "New stage";
                }
            }
        }

        /// <summary>
        /// A description of the stage
        /// </summary>
        public string StageDescription
        {
            get
            {
                return StageModel.Description;
            }
            set
            {
                StageModel.Description = value;
                NotifyPropertyChanged("StageDescription");
            }
        }
        
        /// <summary>
        /// Returns a list of string descriptions of all possible devices the user can use when creating a stage.
        /// </summary>
        public List<string> DevicesAvailable
        {
            get
            {
                List<string> result = MotoTrakBase.MotorDevice.GetAllDeviceTypes().Select(x =>
                    MotorDeviceTypeConverter.ConvertToDescription(x)).ToList();
                return result;
            }
        }

        /// <summary>
        /// The name of the device used for this stage
        /// </summary>
        public string SelectedDeviceString
        {
            get
            {
                return MotorDeviceTypeConverter.ConvertToDescription(StageModel.DeviceType);
            }
        }

        /// <summary>
        /// The index of the selected device for this stage
        /// </summary>
        public int DeviceSelectedIndex
        {
            get
            {
                var device_types = MotoTrakBase.MotorDevice.GetAllDeviceTypes();
                int result = device_types.IndexOf(StageModel.DeviceType);
                if (result == -1 || StageModel.DeviceType == MotorDeviceType.Unknown)
                    result = 0;
                return result;
            }
            set
            {
                int index = value;
                var device_types = MotoTrakBase.MotorDevice.GetAllDeviceTypes();
                if (device_types != null && device_types.Count > 0 && index < device_types.Count)
                {
                    StageModel.DeviceType = device_types[index];
                }

                NotifyPropertyChanged("DeviceSelectedIndex");
                NotifyPropertyChanged("DevicePositionWarningVisibility");
                NotifyPropertyChanged("DeviceTypeWarningVisibility");
            }
        }

        /// <summary>
        /// This represents the number of milliseconds per sample that is read in from the MotoTrak board for this stage
        /// </summary>
        public string SamplingRate
        {
            get
            {
                return StageModel.SamplePeriodInMilliseconds.ToString();
            }
            set
            {
                string sampling_rate_str = value;
                int result = 0;
                bool success = Int32.TryParse(sampling_rate_str, out result);
                if (success)
                {
                    StageModel.SamplePeriodInMilliseconds = result;
                }

                NotifyPropertyChanged("SamplingRate");
            }
        }

        /// <summary>
        /// A view-model class for the pre-trial recording duration StageParameter object.
        /// </summary>
        public StageParameterControlViewModel PreTrialRecordingDurationViewModel
        {
            get
            {
                return _pre_trial_recording_duration_vm;
            }
            private set
            {
                _pre_trial_recording_duration_vm = value;
                NotifyPropertyChanged("PreTrialRecordingDurationViewModel");
            }
        }

        /// <summary>
        /// A view-model object for the hit window duration StageParameter.
        /// </summary>
        public StageParameterControlViewModel HitWindowDurationViewModel
        {
            get
            {
                return _hit_window_duration_vm;
            }
            set
            {
                _hit_window_duration_vm = value;
                NotifyPropertyChanged("HitWindowDurationViewModel");
            }
        }

        /// <summary>
        /// A view-model object for the post-hit-window recording duration.
        /// </summary>
        public StageParameterControlViewModel PostHitWindowRecordingDurationViewModel
        {
            get
            {
                return _post_hit_window_recording_duration_vm;
            }
            set
            {
                _post_hit_window_recording_duration_vm = value;
                NotifyPropertyChanged("PostHitWindowRecordingDurationViewModel");
            }
        }

        /// <summary>
        /// A view-model object for the post-trial-timeout duration.
        /// </summary>
        public StageParameterControlViewModel PostTrialTimeoutDurationViewModel
        {
            get
            {
                return _post_trial_timeout_duration_vm;
            }
            set
            {
                _post_trial_timeout_duration_vm = value;
                NotifyPropertyChanged("PostTrialTimeoutDurationViewModel");
            }
        }

        /// <summary>
        /// A view-model for the device position stage parameter
        /// </summary>
        public StageParameterControlViewModel DevicePositionViewModel
        {
            get
            {
                return _device_position_vm;
            }
            set
            {
                _device_position_vm = value;
                NotifyPropertyChanged("DevicePositionViewModel");
            }
        }

        /// <summary>
        /// A list of stage parameter view-model objects for all of the stage parameters.
        /// </summary>
        public ObservableCollection<StageParameterControlViewModel> StageParameters
        {
            get
            {
                return _stage_parameters;
            }
            set
            {
                _stage_parameters = value;
                NotifyPropertyChanged("StageParameters");
            }
        }

        /// <summary>
        /// Determines the visibility of the knob position warning text in the UI
        /// </summary>
        public Visibility DevicePositionWarningVisibility
        {
            get
            {
                if (StageModel.DeviceType == MotorDeviceType.Knob)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// Determines the visibility of the warning text related to device selection for this stage.
        /// </summary>
        public Visibility DeviceTypeWarningVisibility
        {
            get
            {
                var stage_impl = GetCurrentlySelectedPythonStageImplementation();
                if (stage_impl != null)
                {
                    if (stage_impl.TaskDefinition.RequiredDeviceType != MotorDeviceType.Unknown &&
                        stage_impl.TaskDefinition.RequiredDeviceType != StageModel.DeviceType)
                    {
                        return Visibility.Visible;
                    }
                }
                
                return Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Displays a message about trial timing information to the user.
        /// </summary>
        public string StageTimingMessage
        {
            get
            {
                string result = string.Empty;

                double trial_duration = StageModel.HitWindowInSeconds.InitialValue + StageModel.PostTrialSamplingPeriodInSeconds.InitialValue;
                double lookback_duration = StageModel.PreTrialSamplingPeriodInSeconds.InitialValue;
                double total_trial_duration = lookback_duration + trial_duration;
                int samples_per_trial = StageModel.TotalRecordedSamplesPerTrial;

                if (Double.IsNaN(total_trial_duration))
                {
                    result = "We were unable to calculate a trial duration based on your settings. Please verify that you have entered properly formatted numbers into each field.";
                }
                else
                {
                    result = "Based on your settings, a trial will include a " + trial_duration.ToString("#.##") + " second recording period with a " +
                        lookback_duration.ToString("#.##") + " second lookback.  The full " + total_trial_duration.ToString("#.##") + " seconds will include " +
                        samples_per_trial.ToString() + " recorded samples.";
                }

                return result;
            }
        }

        /// <summary>
        /// The list of output trigger options that we provide to the user
        /// </summary>
        public List<string> OutputTriggerOptions
        {
            get
            {
                List<string> result = new List<string>();
                PythonTaskImplementation k = StageModel.StageImplementation as PythonTaskImplementation;
                if (k != null)
                {
                    result = k.TaskDefinition.OutputTriggerOptions;
                }

                return result;
            }
        }

        /// <summary>
        /// The selected index within the list of output trigger options.
        /// This indicates what the selected type of output trigger is for this stage.
        /// </summary>
        public int OutputTriggerSelectedIndex
        {
            get
            {
                int index = 0;
                PythonTaskImplementation k = StageModel.StageImplementation as PythonTaskImplementation;
                if (k != null)
                {
                    index = k.TaskDefinition.OutputTriggerOptions.IndexOf(StageModel.OutputTriggerType);
                }

                return index;
            }
            set
            {
                PythonTaskImplementation k = StageModel.StageImplementation as PythonTaskImplementation;
                if (k != null)
                {
                    int index = value;
                    if (k.TaskDefinition.OutputTriggerOptions != null &&
                        k.TaskDefinition.OutputTriggerOptions.Count > 0 && 
                        index < k.TaskDefinition.OutputTriggerOptions.Count &&
                        index >= 0)
                    {
                        StageModel.OutputTriggerType = k.TaskDefinition.OutputTriggerOptions[index];
                    }
                }

                NotifyPropertyChanged("OutputTriggerSelectedIndex");
            }
        }

        #endregion
    }
}
