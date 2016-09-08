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
            StageModel = stage_to_edit;
            StageParameters.CollectionChanged += StageParameters_CollectionChanged;

            InstantiateRequiredStageParameters();
            SetDeviceToRecommendedDeviceForStageImplementation();
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

        private List<Tuple<string, PythonStageImplementation>> GetOrderedListOfPythonStageImplementations ()
        {
            List<Tuple<string, PythonStageImplementation>> result = new List<Tuple<string, PythonStageImplementation>>();
            var stage_implementations = MotoTrakConfiguration.GetInstance().PythonStageImplementations;
            PythonStageImplementation unknown_task_basic_stage = null;
            foreach (var impl in stage_implementations)
            {
                PythonStageImplementation this_stage_impl = impl.Value as PythonStageImplementation;
                if (this_stage_impl != null)
                {
                    if (this_stage_impl.TaskName.Equals("Unknown"))
                    {
                        unknown_task_basic_stage = this_stage_impl;
                    }
                    else
                    {
                        result.Add(new Tuple<string, PythonStageImplementation>(impl.Key, this_stage_impl));
                    }
                }
            }

            if (unknown_task_basic_stage != null)
            {
                result.Add(new Tuple<string, PythonStageImplementation>("Unknown", unknown_task_basic_stage));
            }

            return result;
        }
        
        /// <summary>
        /// Returns the currently selected PythonStageImplementation object, based on what
        /// the selected index is in the UI.
        /// </summary>
        private PythonStageImplementation GetCurrentlySelectedPythonStageImplementation()
        {
            var ordered_list_of_stage_impls = GetOrderedListOfPythonStageImplementations();
            if (ordered_list_of_stage_impls != null && SelectedTaskIndex < ordered_list_of_stage_impls.Count)
            {
                return ordered_list_of_stage_impls[SelectedTaskIndex].Item2;
            }

            return null;
        }

        /// <summary>
        /// Creates the required stage parameters on the StageModel object for the currently selected stage.
        /// </summary>
        private void InstantiateRequiredStageParameters ()
        {
            //Clear the stage parameters dictionary in preparation to repopulate it with new stage parameters
            StageModel.StageParameters.Clear();

            var currently_selected_stage_impl = GetCurrentlySelectedPythonStageImplementation();
            if (currently_selected_stage_impl != null)
            {
                var parameters = currently_selected_stage_impl.RequiredStageParameters.Values.ToList();
                if (parameters != null && parameters.Count > 0)
                {
                    foreach (var p in parameters)
                    {
                        //Create a new stage parameter
                        MotorStageParameter new_param = new MotorStageParameter()
                        {
                            ParameterName = p.Item1,
                            ParameterUnits = p.Item2
                        };

                        //Add it to our dictionary of stage parameters
                        StageModel.StageParameters[new_param.ParameterName] = new_param;
                    }
                }
            }

            //Create view-models for each stage parameter
            InstantiateStageParameterViewModels();
        }

        /// <summary>
        /// Creates view-model classes for each stage parameter that exists.
        /// </summary>
        private void InstantiateStageParameterViewModels ()
        {
            StageParameters.Clear();
            foreach (var sp in _stage.StageParameters)
            {
                StageParameterControlViewModel spvm = new StageParameterControlViewModel(sp.Value);
                StageParameters.Add(spvm);
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
            StageModel.DeviceType = stage_impl.RecommendedDevice;
            NotifyPropertyChanged("DeviceSelectedIndex");
            NotifyPropertyChanged("DevicePositionWarningVisibility");
            NotifyPropertyChanged("DeviceTypeWarningVisibility");
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

                if (_stage != null)
                {
                    //Create view-models for the basic stage parameters
                    PreTrialRecordingDurationViewModel = new StageParameterControlViewModel(_stage.PreTrialSamplingPeriodInSeconds);
                    HitWindowDurationViewModel = new StageParameterControlViewModel(_stage.HitWindowInSeconds);
                    PostHitWindowRecordingDurationViewModel = new StageParameterControlViewModel(_stage.PostTrialSamplingPeriodInSeconds);
                    PostTrialTimeoutDurationViewModel = new StageParameterControlViewModel(_stage.PostTrialTimeoutInSeconds);
                    DevicePositionViewModel = new StageParameterControlViewModel(_stage.Position);

                    //Subscribe to timing parameter view-model changes
                    SubscribeToTimingParameterViewModelChanges();

                    //Create view-models for each stage parameter
                    InstantiateStageParameterViewModels();
                }
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
                    string this_name = implementation_tuple.Item2.TaskName + " (" + implementation_tuple.Item1 + ")";
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
                    return currently_selected_stage_impl.TaskDescription;
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
                _selected_task_index = value;

                InstantiateRequiredStageParameters();
                SetDeviceToRecommendedDeviceForStageImplementation();

                NotifyPropertyChanged("SelectedTaskIndex");
                NotifyPropertyChanged("TaskDescriptionVisibility");
                NotifyPropertyChanged("TaskDescription");
                NotifyPropertyChanged("DevicePositionWarningVisibility");
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
                    if (stage_impl.RecommendedDevice != MotorDeviceType.Unknown && StageModel.DeviceType != stage_impl.RecommendedDevice)
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
        
        #endregion
    }
}
