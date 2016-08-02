using System;
using System.Collections.Generic;
using MotoTrakUtilities;
using System.Collections.Concurrent;

namespace MotoTrakBase
{
    /// <summary>
    /// This class represents a stage within the MotoTrak program.
    /// </summary>
    public class MotorStage
    {
        #region Public enumerated type

        #endregion

        #region Private data members
        
        /// <summary>
        /// Some constants
        /// </summary>
        private const int _defaultPreTrialSamplingPeriodInSeconds = 1;
        private const int _defaultHitWindowDurationInSeconds = 2;
        private const int _defaultPostTrialSamplingPeriodInSeconds = 2;
        private const int _defaultPostTrialTimeoutPeriodInSeconds = 0;

        /// <summary>
        /// This will hold the stage implementation code
        /// </summary>
        private IMotorStageImplementation _stageImplementation = null;

        /// <summary>
        /// Strings that hold the path and file name of where this stage is located
        /// </summary>
        private string _stageFilePath = string.Empty;
        private string _stageFileName = string.Empty;

        private MotorStageParameter _pre_trial_sampling_period_in_seconds = new MotorStageParameter()
        {
            ParameterType = MotorStageParameter.StageParameterType.Fixed,
            InitialValue = _defaultPreTrialSamplingPeriodInSeconds,
            CurrentValue = _defaultPreTrialSamplingPeriodInSeconds
        };

        private MotorStageParameter _post_trial_sampling_period_in_seconds = new MotorStageParameter()
        {
            ParameterType = MotorStageParameter.StageParameterType.Fixed,
            InitialValue = _defaultPostTrialSamplingPeriodInSeconds,
            CurrentValue = _defaultPostTrialSamplingPeriodInSeconds
        };

        private MotorStageParameter _post_trial_timeout_period_in_seconds = new MotorStageParameter()
        {
            ParameterType = MotorStageParameter.StageParameterType.Fixed,
            InitialValue = _defaultPostTrialTimeoutPeriodInSeconds,
            CurrentValue = _defaultPostTrialTimeoutPeriodInSeconds
        };

        private MotorStageParameter _hit_window_in_seconds = new MotorStageParameter()
        {
            ParameterType = MotorStageParameter.StageParameterType.Fixed,
            InitialValue = _defaultHitWindowDurationInSeconds,
            CurrentValue = _defaultHitWindowDurationInSeconds
        };

        private MotorStageParameter _position_of_device = new MotorStageParameter()
        {
            ParameterType = MotorStageParameter.StageParameterType.Fixed,
            InitialValue = 0,
            CurrentValue = 0
        };

        private ConcurrentDictionary<string, MotorStageParameter> _stage_parameters = new ConcurrentDictionary<string, MotorStageParameter>();
        
        private MotorStageStimulationType _output_trigger_type = MotorStageStimulationType.Off;

        private int _trial_count_lookback_for_adaptive_adjustments = 10;

        private List<MotorBoardDataStreamType> _data_streams = new List<MotorBoardDataStreamType>()
        {
            MotorBoardDataStreamType.Timestamp,
            MotorBoardDataStreamType.DeviceValue,
            MotorBoardDataStreamType.IRSensorValue
        };
        
        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an empty MotoTrak stage.
        /// </summary>
        public MotorStage()
        {
            //empty
        }

        #endregion

        #region Properties

        /// <summary>
        /// The number of the stage
        /// </summary>
        public string StageName { get; set; }

        /// <summary>
        /// A text description of the stage
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The type of device the stage uses
        /// </summary>
        public MotorDeviceType DeviceType { get; set; }

        /// <summary>
        /// The number of milliseconds each sample in the signal represents.
        /// </summary>
        public int SamplePeriodInMilliseconds { get; set; }
        
        /// <summary>
        /// The path leading to the stage file
        /// </summary>
        public string StageFilePath
        {
            get
            {
                return _stageFilePath;
            }
            private set
            {
                _stageFilePath = value;
            }
        }

        /// <summary>
        /// The file name (not including the path)
        /// </summary>
        public string StageFileName
        {
            get
            {
                return _stageFileName;
            }
            private set
            {
                _stageFileName = value;
            }
        }

        /// <summary>
        /// The fully qualified stage file: path + file name.
        /// This is read-only.
        /// </summary>
        public string StageFile
        {
            get
            {
                return (StageFilePath + StageFileName);
            }
        }

        /// <summary>
        /// The total number of streams of data that will be streamed from the MotoTrak controller board based on the settings of this stage.
        /// The default number is 3 (and this is the number of streams that was used in MotoTrak V1).
        /// The 3 default streams (in order) are: { timestamp, pull/knob device value, IR sensor value }
        /// </summary>
        public int TotalDataStreams
        {
            get
            {
                return _data_streams.Count;
            }
        }

        /// <summary>
        /// An ordered list of the data stream types for this stage
        /// </summary>
        public List<MotorBoardDataStreamType> DataStreamTypes
        {
            get
            {
                return _data_streams;
            }
            set
            {
                _data_streams = value;
            }
        }

        /// <summary>
        /// A dictionary of all pertinent parameters for this stage (hit threshold, initiation threshold, etc)
        /// </summary>
        public ConcurrentDictionary<string, MotorStageParameter> StageParameters
        {
            get
            {
                return _stage_parameters;
            }
            set
            {
                _stage_parameters = value;
            }
        }

        /// <summary>
        /// References the functions that know how to run this stage within a session.
        /// </summary>
        public IMotorStageImplementation StageImplementation
        {
            get
            {
                return _stageImplementation;
            }
            private set
            {
                _stageImplementation = value;
            }
        }

        /// <summary>
        /// The number of trials to retain, or rather, the number of trials to "look back" when needing
        /// to make adaptive adjustments at the end of every trial.
        /// </summary>
        public int TrialsToRetainForAdaptiveAdjustments
        {
            get
            {
                return _trial_count_lookback_for_adaptive_adjustments;
            }
            set
            {
                _trial_count_lookback_for_adaptive_adjustments = value;
            }
        }

        /// <summary>
        /// The position of the "peg" for the device on this stage
        /// </summary>
        public MotorStageParameter Position
        {
            get
            {
                return _position_of_device;
            }
            set
            {
                _position_of_device = value;
            }
        }

        /// <summary>
        /// The duration of the hit window (in units of seconds)
        /// </summary>
        public MotorStageParameter HitWindowInSeconds
        {
            get
            {
                return _hit_window_in_seconds;
            }
            set
            {
                _hit_window_in_seconds = value;
            }
        }
        
        /// <summary>
        /// The total amount of time (in seconds) during which we record samples at the beginning of a trial
        /// before the hit window begins.  In V1 of MotoTrak, the value of this variable was fixed at 1.  In V2
        /// of MotoTrak, this variable may be defined by the stage definition file.
        /// </summary>
        public MotorStageParameter PreTrialSamplingPeriodInSeconds
        {
            get
            {
                return _pre_trial_sampling_period_in_seconds;
            }
            set
            {
                _pre_trial_sampling_period_in_seconds = value;
            }
        }
        
        /// <summary>
        /// The total amount of time (in seconds) during which we record samples at the end of a trial (after the
        /// hit window ends).  In V1 of MotoTrak, the value of this variable was fixed at 2.  In V2 of MotoTrak,
        /// this variable may be defined by the stage definition file.
        /// </summary>
        public MotorStageParameter PostTrialSamplingPeriodInSeconds
        {
            get
            {
                return _post_trial_sampling_period_in_seconds;
            }
            set
            {
                _post_trial_sampling_period_in_seconds = value;
            }
        }

        /// <summary>
        /// How many seconds must be between the end of one trial and the beginning of the next trial
        /// </summary>
        public MotorStageParameter PostTrialTimeoutInSeconds
        {
            get
            {
                return _post_trial_timeout_period_in_seconds;
            }
            set
            {
                _post_trial_timeout_period_in_seconds = value;
            }
        }
        
        /// <summary>
        /// The possible hit threshold types that may be available based on the kind of device this stage uses.
        /// </summary>
        public List<MotorTaskTypeV1> PossibleHitThresholdTypes
        {
            get
            {
                List<MotorTaskTypeV1> possibleTypes = new List<MotorTaskTypeV1>();
                switch (DeviceType)
                {
                    case MotorDeviceType.Pull:
                        possibleTypes.Add(MotorTaskTypeV1.PeakForce);
                        possibleTypes.Add(MotorTaskTypeV1.SustainedForce);
                        break;
                    case MotorDeviceType.Knob:
                        possibleTypes.Add(MotorTaskTypeV1.TotalDegrees);
                        break;
                    default:
                        possibleTypes.Add(MotorTaskTypeV1.Undefined);
                        break;
                }

                return possibleTypes;
            }
        }

        /// <summary>
        /// The number of samples per second as defined by this stage's timing parameters.
        /// </summary>
        public int SamplesPerSecond
        {
            get
            {
                return Convert.ToInt32(1000 / SamplePeriodInMilliseconds);
            }
        }

        /// <summary>
        /// The number of total samples that occur in the signal before the hit window begins for a trial according
        /// to the parameters of this stage.
        /// </summary>
        public int TotalRecordedSamplesBeforeHitWindow
        {
            get
            {
                return Convert.ToInt32(Math.Round(PreTrialSamplingPeriodInSeconds.CurrentValue * SamplesPerSecond));
            }
        }

        /// <summary>
        /// The number of total samples that occur in the signal after the hit window ends for a trial according
        /// to the parameters of this stage.
        /// </summary>
        public int TotalRecordedSamplesAfterHitWindow
        {
            get
            {
                return Convert.ToInt32(Math.Round(PostTrialSamplingPeriodInSeconds.CurrentValue * SamplesPerSecond));
            }
        }

        /// <summary>
        /// The total number of samples that occur within the hit window of trials according to the parameters
        /// of this stage.
        /// </summary>
        public int TotalRecordedSamplesDuringHitWindow
        {
            get
            {
                return Convert.ToInt32(Math.Round(HitWindowInSeconds.CurrentValue * SamplesPerSecond));
            }
        }

        /// <summary>
        /// The total number of samples that occur within an entire recorded trial according to the parameters
        /// of this stage.
        /// </summary>
        public int TotalRecordedSamplesPerTrial
        {
            get
            {
                return (TotalRecordedSamplesBeforeHitWindow + TotalRecordedSamplesDuringHitWindow + TotalRecordedSamplesAfterHitWindow);
            }
        }

        /// <summary>
        /// The type of output trigger for this stage.
        /// </summary>
        public MotorStageStimulationType OutputTriggerType
        {
            get
            {
                return _output_trigger_type;
            }
            set
            {
                _output_trigger_type = value;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns a list of all motor stages found.  Before calling this function, you MUST read in the MotoTrak configuration
        /// file.  The MotoTrak configuration file defines WHERE to find the stages (whether they are on a Google Doc, a 
        /// spreadsheet, or individual files for each stage).
        /// </summary>
        /// <returns>A list of MotorStage objects representing each available stage.</returns>
        public static List<MotorStage> RetrieveAllStages ()
        {
            MotoTrakConfiguration config = MotoTrakConfiguration.GetInstance();
            string stagePath = config.StagePath;
            
            if (config.ConfigurationVersion == 1)
            {
                //If the configuration version is "1" (all previous versions of MotoTrak), then load in the Google Sheets document that defines stages.
                Uri google_sheets_url = new Uri(stagePath);
                List<MotorStage> stages = RetrieveAllStages_V1(google_sheets_url);

                return stages;
            }

            //If no proper way to load stages was used, then let's just return an empty list of stages.
            return new List<MotorStage>();
        }

        /// <summary>
        /// Returns a list of all motor stages found in a spreadsheet.  The spreadsheet location is the URI that is
        /// passed as a parameter to this function.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        private static List<MotorStage> RetrieveAllStages_V1 (Uri address)
        {
            //Read in all the stages from Google Docs
            List<List<string>> stageDocument = null; 

            try
            {
                //Read the list of stages
                stageDocument = ReadGoogleSpreadsheet.Read(address);
            }
            catch
            {
                //If an error occurred, notify the user using the MotoTrak messaging system
                MotoTrakMessaging.GetInstance().AddMessage("Unable to read Stage spreadsheet!");

                //Return an empty list of stages
                return new List<MotorStage>();
            }
            
            //Remove the first line of the file because it is just column headers
            stageDocument.RemoveAt(0);

            //This is how many columns should be in the file
            int numberOfColumns = 14;

            //Create a list we will use to store all the stages
            List<MotorStage> stages = new List<MotorStage>();

            //Go through each stage from Google Docs and create an object for it
            foreach (List<string> stageLine in stageDocument)
            {
                //If for some reason this line doesn't have the correct number of columns, skip it
                if (stageLine.Count != numberOfColumns)
                {
                    continue;
                }
                else
                {
                    //Otherwise, set the variables for the new stage appropriately.
                    MotorStage stage = new MotorStage();
                    
                    try
                    {
                        stage.StageName = stageLine[0];
                        stage.Description = stageLine[1];
                        stage.DeviceType = MotorDeviceTypeConverter.ConvertToMotorDeviceType(stageLine[2]);
                        
                        //Read the constraint from the stage file and throw it away
                        int trash = Int32.Parse(stageLine[3]);

                        //Read the manipulandum position from the stage file
                        MotorStageParameter position = new MotorStageParameter();
                        position.InitialValue = Double.Parse(stageLine[4]);
                        position.CurrentValue = position.InitialValue;
                        position.AdaptiveThresholdType = MotorStageAdaptiveThresholdType.Static;
                        stage.Position = position;

                        //Create objects for the hit threshold and initiation threshold for this stage
                        MotorStageParameter hit_thresh = new MotorStageParameter();
                        MotorStageParameter init_thresh = new MotorStageParameter();

                        //Read in the hit threshold adaptive type
                        hit_thresh.AdaptiveThresholdType = MotorStageAdaptiveThresholdTypeConverter.ConvertToMotorStageAdaptiveThresholdType(stageLine[5]);
                        
                        //Read in the hit threshold minimum value
                        double hit_min = double.NaN;
                        bool success = Double.TryParse(stageLine[6], out hit_min);
                        hit_thresh.MinimumValue = hit_min;
                        
                        //Read in the hit threshold maximum value
                        double hit_max = double.NaN;
                        success = Double.TryParse(stageLine[7], out hit_max);
                        hit_thresh.MaximumValue = hit_max;

                        //Read in the hit threshold incremental value
                        double hit_inc = double.NaN;
                        success = Double.TryParse(stageLine[8], out hit_inc);
                        hit_thresh.Increment = hit_inc;

                        //Read in the trial initiation threshold
                        double trial_init = double.NaN;
                        success = Double.TryParse(stageLine[9], out trial_init);
                        init_thresh.InitialValue = trial_init;
                        init_thresh.CurrentValue = init_thresh.InitialValue;
                        init_thresh.AdaptiveThresholdType = MotorStageAdaptiveThresholdType.Static;
                        
                        
                        //Read in the hit threshold type
                        var hit_thresh_type = MotorTaskTypeV1Converter.ConvertToMotorStageHitThresholdType(stageLine[10]);

                        //Read in the duration of the hit window
                        MotorStageParameter hit_win = new MotorStageParameter();
                        hit_win.InitialValue = Double.Parse(stageLine[11]);
                        hit_win.CurrentValue = hit_win.InitialValue;
                        hit_win.AdaptiveThresholdType = MotorStageAdaptiveThresholdType.Static;
                        stage.HitWindowInSeconds = hit_win;

                        //Read in the sampling rate (the number of milliseconds inbetween each sample we read from the MotoTrak controller board)
                        stage.SamplePeriodInMilliseconds = Int32.Parse(stageLine[12]);
                        
                        //Read in the output trigger type
                        //hit_thresh.OutputTriggerType = MotorStageStimulationTypeConverter.ConvertToMotorStageStimulationType(stageLine[13]);

                        //For all "version 1" stages, this value will be 10.
                        stage.TrialsToRetainForAdaptiveAdjustments = 10;

                        if (hit_thresh.AdaptiveThresholdType == MotorStageAdaptiveThresholdType.Static)
                        {
                            hit_thresh.CurrentValue = hit_thresh.MaximumValue;
                        }
                        else
                        {
                            hit_thresh.CurrentValue = hit_thresh.MinimumValue;
                        }

                        
                        if (hit_thresh_type == MotorTaskTypeV1.PeakForce)
                        {
                            //Set the implementation of this stage
                            stage.StageImplementation = new PullStageImplementation();

                            //Set the parameters of this stage
                            stage.StageParameters.Clear();
                            stage.StageParameters["Hit Threshold"] = hit_thresh;
                            stage.StageParameters["Initiation Threshold"] = init_thresh;
                        }
                        
                        //Add the stage to our list of stages
                        stages.Add(stage);
                    }
                    catch (System.FormatException)
                    {
                        //do nothing
                    }
                    catch (System.ArgumentException)
                    {
                        //do nothing
                    }
                    catch (System.OverflowException)
                    {
                        //do nothing
                    }
                }
            }

            return stages;
        }

        #endregion
    }
}
