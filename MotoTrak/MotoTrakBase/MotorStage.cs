using System;
using System.Collections.Generic;
using MotoTrakUtilities;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;

namespace MotoTrakBase
{
    /// <summary>
    /// This class represents a stage within the MotoTrak program.
    /// </summary>
    public class MotorStage
    {
        #region Private data members
        
        /// <summary>
        /// Some constants
        /// </summary>
        private const int _defaultPreTrialSamplingPeriodInSeconds = 1;
        private const int _defaultHitWindowDurationInSeconds = 2;
        private const int _defaultPostTrialSamplingPeriodInSeconds = 2;
        private const int _defaultPostTrialTimeoutPeriodInSeconds = 0;

        private int _sampling_period_in_milliseconds = 10;
        private MotorDeviceType _device_type = MotorDeviceType.Pull;

        /// <summary>
        /// This will hold the stage implementation code
        /// </summary>
        private IMotorTaskImplementation _stageImplementation = null;

        /// <summary>
        /// Strings that hold the path and file name of where this stage is located
        /// </summary>
        private string _stageFilePath = string.Empty;
        private string _stageFileName = string.Empty;

        private MotorStageParameter _pre_trial_sampling_period_in_seconds = new MotorStageParameter()
        {
            ParameterName = "Pre-Trial Duration",
            ParameterUnits = "seconds",
            ParameterType = MotorStageParameter.StageParameterType.Fixed,
            InitialValue = _defaultPreTrialSamplingPeriodInSeconds,
            CurrentValue = _defaultPreTrialSamplingPeriodInSeconds
        };

        private MotorStageParameter _post_trial_sampling_period_in_seconds = new MotorStageParameter()
        {
            ParameterName = "Post-Trial Duration",
            ParameterUnits = "seconds",
            ParameterType = MotorStageParameter.StageParameterType.Fixed,
            InitialValue = _defaultPostTrialSamplingPeriodInSeconds,
            CurrentValue = _defaultPostTrialSamplingPeriodInSeconds
        };

        private MotorStageParameter _post_trial_timeout_period_in_seconds = new MotorStageParameter()
        {
            ParameterName = "Post-Trial Timeout Period",
            ParameterUnits = "seconds",
            ParameterType = MotorStageParameter.StageParameterType.Fixed,
            InitialValue = _defaultPostTrialTimeoutPeriodInSeconds,
            CurrentValue = _defaultPostTrialTimeoutPeriodInSeconds
        };

        private MotorStageParameter _hit_window_in_seconds = new MotorStageParameter()
        {
            ParameterName = "Hit Window Duration",
            ParameterUnits = "seconds",
            ParameterType = MotorStageParameter.StageParameterType.Fixed,
            InitialValue = _defaultHitWindowDurationInSeconds,
            CurrentValue = _defaultHitWindowDurationInSeconds
        };

        private MotorStageParameter _position_of_device = new MotorStageParameter()
        {
            ParameterName = "Device Position",
            ParameterUnits = "centimeters",
            ParameterType = MotorStageParameter.StageParameterType.Fixed,
            InitialValue = 0,
            CurrentValue = 0
        };

        private ConcurrentDictionary<string, MotorStageParameter> _stage_parameters = new ConcurrentDictionary<string, MotorStageParameter>();
        private List<MotorStageParameterTone> _tone_stage_parameters = new List<MotorStageParameterTone>();

        private string _output_trigger_type = string.Empty;
        
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
        public MotorDeviceType DeviceType
        {
            get
            {
                return _device_type;
            }
            set
            {
                _device_type = value;
            }
        }

        /// <summary>
        /// The number of milliseconds each sample in the signal represents.
        /// </summary>
        public int SamplePeriodInMilliseconds
        {
            get
            {
                return _sampling_period_in_milliseconds;
            }
            set
            {
                _sampling_period_in_milliseconds = value;
            }
        }
        
        /// <summary>
        /// The path leading to the stage file
        /// </summary>
        public string StageFilePath
        {
            get
            {
                return _stageFilePath;
            }
            set
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
            set
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
                if (!String.IsNullOrEmpty(StageFilePath) && !String.IsNullOrEmpty(StageFileName))
                {
                    return (StageFilePath + @"\" + StageFileName);
                }
                else
                {
                    return string.Empty;
                }
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

        public List<MotorStageParameterTone> ToneStageParameters
        {
            get
            {
                return _tone_stage_parameters;
            }
            set
            {
                _tone_stage_parameters = value;
            }
        }

        /// <summary>
        /// References the functions that know how to run this stage within a session.
        /// </summary>
        public IMotorTaskImplementation StageImplementation
        {
            get
            {
                return _stageImplementation;
            }
            set
            {
                _stageImplementation = value;
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
        public string OutputTriggerType
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

        #region Public methods

        /// <summary>
        /// Resets all stage parameters to their initial values
        /// </summary>
        public void ResetStateToInitialValues ()
        {
            Position.ResetParameterToInitialValue();
            PreTrialSamplingPeriodInSeconds.ResetParameterToInitialValue();
            HitWindowInSeconds.ResetParameterToInitialValue();
            PostTrialSamplingPeriodInSeconds.ResetParameterToInitialValue();
            PostTrialTimeoutInSeconds.ResetParameterToInitialValue();
            
            foreach (var sp in StageParameters)
            {
                sp.Value.ResetParameterToInitialValue();
            }
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Returns a list of all motor stages found.  Before calling this function, you MUST read in the MotoTrak configuration
        /// file.  The MotoTrak configuration file defines WHERE to find the stages (whether they are on a Google Doc, a 
        /// spreadsheet, or individual files for each stage).
        /// </summary>
        /// <returns>A list of MotorStage objects representing each available stage.</returns>
        public static List<MotorStage> RetrieveAllStages ()
        {
            MotoTrakConfiguration config = MotoTrakConfiguration.GetInstance();

            List<MotorStage> stages = new List<MotorStage>();

            if (config.ConfigurationVersion == 1)
            {
                if (!string.IsNullOrEmpty(config.StageLocalPath))
                {
                    //If we are loading stages from a local source...
                    try
                    {
                        string full_stage_path = config.GetLocalApplicationDataFolder() + config.StageLocalPath + @"\";
                        stages = RetrieveAllLocalStages(full_stage_path);
                    }
                    catch (Exception e)
                    {
                        ErrorLoggingService.GetInstance().LogExceptionError(e);
                    }
                }
                else if (!string.IsNullOrEmpty(config.StageWebPath))
                {
                    //Otherwise, if we are loading stages from a web source...
                    try
                    {
                        Uri google_sheets_url = new Uri(config.StageWebPath);
                        stages = RetrieveAllStage_Web(google_sheets_url);
                    }
                    catch (Exception e)
                    {
                        ErrorLoggingService.GetInstance().LogExceptionError(e);
                    }
                }
            }

            //If no proper way to load stages was used, then let's just return an empty list of stages.
            return stages;
        }

        /// <summary>
        /// Saves a MotorStage object out to a file
        /// </summary>
        /// <param name="stage">The stage to save</param>
        /// <param name="full_path_and_filename">The filename</param>
        public static void SaveStageToFile (MotorStage stage, string full_path_and_filename)
        {
            FileInfo file_info = new FileInfo(full_path_and_filename);
            
            //Create the directory if it needs to be made
            if (file_info.Directory != null && !file_info.Directory.Exists)
            {
                file_info.Directory.Create();
            }

            //Attempt to open the file for writing
            StreamWriter writer = null;
            bool successful_open = true;
            
            try
            {
                writer = new StreamWriter(full_path_and_filename);
            }
            catch (Exception e)
            {
                successful_open = false;
                ErrorLoggingService.GetInstance().LogExceptionError(e);
            }

            if (successful_open)
            {
                //First, save the file version.
                writer.WriteLine("Stage File Version: 1");

                //Next, save the stage name
                writer.WriteLine("Stage Name: " + stage.StageName);

                //Save the stage description
                writer.WriteLine("Stage Description: " + stage.Description);

                //Save the name of the stage implementation file
                var stage_impls = MotoTrakConfiguration.GetInstance().PythonStageImplementations;
                string stage_impl_name = string.Empty;
                foreach (var kvp in stage_impls)
                {
                    if (kvp.Value == stage.StageImplementation)
                    {
                        stage_impl_name = kvp.Key;
                    }
                }
                
                writer.WriteLine("Stage Implementation: " + stage_impl_name);

                //Save the stage output trigger type
                writer.WriteLine("Stage Output Trigger: " + stage.OutputTriggerType);

                //Write the stage device name
                writer.WriteLine("Stage Device: " + MotorDeviceTypeConverter.ConvertToDescription(stage.DeviceType));

                //Write the sampling period of the stage (units of ms)
                writer.WriteLine("Stage Sampling Rate: " + stage.SamplePeriodInMilliseconds.ToString());

                //Save the pre-trial sampling duration
                writer.WriteLine("Stage Parameter: " + MotorStage.FormMotorStageParameter(stage.PreTrialSamplingPeriodInSeconds, true));

                //Save the hit window duration
                writer.WriteLine("Stage Parameter: " + MotorStage.FormMotorStageParameter(stage.HitWindowInSeconds, true));

                //Save the post-trial sampling duration
                writer.WriteLine("Stage Parameter: " + MotorStage.FormMotorStageParameter(stage.PostTrialSamplingPeriodInSeconds, true));

                //Save the post-trial timeout duration
                writer.WriteLine("Stage Parameter: " + MotorStage.FormMotorStageParameter(stage.PostTrialTimeoutInSeconds, true));

                //Save the device position
                writer.WriteLine("Stage Parameter: " + MotorStage.FormMotorStageParameter(stage.Position, true));

                //Save each individual stage parameter
                foreach (var sp in stage.StageParameters)
                {
                    writer.WriteLine("Stage Parameter: " + MotorStage.FormMotorStageParameter(sp.Value, true));
                }

                //We are done. Close the file.
                writer.Close();
            }
        }

        /// <summary>
        /// Loads a MotorStage object from a file
        /// </summary>
        /// <param name="full_path_and_filename">The file name</param>
        /// <returns>The MotoTrak stage</returns>
        public static MotorStage LoadStageFromFile (string full_path_and_filename)
        {
            //Create an empty motor stage to use as we load in the stage file
            MotorStage stage = new MotorStage();

            int file_version = 0;

            //Load the lines of the file (excluding comments)
            try
            {
                List<string> file_lines = MotoTrakUtilities.ConfigurationFileLoader.LoadConfigurationFile(full_path_and_filename);

                if (file_lines.Count > 0)
                {
                    //Pop the first line from the collection of lines from the file
                    string first_line = file_lines[0];
                    file_lines.RemoveAt(0);

                    //Look to see if the first line contains the file version
                    string[] parameter_string_parts_first_line = first_line.Split(new char[] { ':' }, 2);
                    string parameter_first_line = parameter_string_parts_first_line[0].Trim();
                    if (parameter_first_line.Equals("Stage File Version"))
                    {
                        
                        bool success = Int32.TryParse(parameter_string_parts_first_line[1].Trim(), out file_version);
                        if (!success)
                        {
                            ErrorLoggingService.GetInstance().LogStringError("Unable to read stage file version.");
                            return null;
                        }
                    }

                    if (file_version == 1)
                    {
                        //Iterate over each line of the file
                        foreach (var line in file_lines)
                        {
                            //At this point, we have found the file version, so we can read in parameters
                            string[] parameter_string_parts = line.Split(new char[] { ':' }, 2);
                            string parameter = parameter_string_parts[0].Trim();

                            //Check the parameter and read it in
                            if (parameter.Equals("Stage Name"))
                            {
                                stage.StageName = parameter_string_parts[1].Trim();
                            }
                            else if (parameter.Equals("Stage Description"))
                            {
                                stage.Description = parameter_string_parts[1].Trim();
                            }
                            else if (parameter.Equals("Stage Implementation"))
                            {
                                stage.StageImplementation = MotoTrakConfiguration.GetInstance().PythonStageImplementations[parameter_string_parts[1].Trim()];
                            }
                            else if (parameter.Equals("Stage Output Trigger"))
                            {
                                stage.OutputTriggerType = parameter_string_parts[1].Trim();
                            }
                            else if (parameter.Equals("Stage Device"))
                            {
                                stage.DeviceType = MotorDeviceTypeConverter.ConvertToMotorDeviceType(parameter_string_parts[1].Trim());
                            }
                            else if (parameter.Equals("Stage Sampling Rate"))
                            {
                                int sampling_rate = 0;
                                bool success = Int32.TryParse(parameter_string_parts[1].Trim(), out sampling_rate);
                                if (success)
                                {
                                    stage.SamplePeriodInMilliseconds = sampling_rate;
                                }
                            }
                            else if (parameter.Equals("Stage Parameter"))
                            {
                                if (parameter_string_parts.Length > 1)
                                {
                                    MotorStageParameter p = MotorStage.ParseMotorStageParameterWithName(parameter_string_parts[1]);

                                    if (p.ParameterName.Equals(stage.PreTrialSamplingPeriodInSeconds.ParameterName))
                                    {
                                        stage.PreTrialSamplingPeriodInSeconds = p;
                                    }
                                    else if (p.ParameterName.Equals(stage.HitWindowInSeconds.ParameterName))
                                    {
                                        stage.HitWindowInSeconds = p;
                                    }
                                    else if (p.ParameterName.Equals(stage.PostTrialSamplingPeriodInSeconds.ParameterName))
                                    {
                                        stage.PostTrialSamplingPeriodInSeconds = p;
                                    }
                                    else if (p.ParameterName.Equals(stage.PostTrialTimeoutInSeconds.ParameterName))
                                    {
                                        stage.PostTrialTimeoutInSeconds = p;
                                    }
                                    else if (p.ParameterName.Equals(stage.Position.ParameterName))
                                    {
                                        stage.Position = p;
                                    }
                                    else
                                    {
                                        stage.StageParameters[p.ParameterName] = p;
                                    }
                                }
                            }
                            else if (parameter.Equals("Stage Tone"))
                            {
                                if (parameter_string_parts.Length > 1)
                                {
                                    MotorStageParameterTone t = MotorStage.ParseMotorStageTone(parameter_string_parts[1]);
                                    if (t != null)
                                    {
                                        stage.ToneStageParameters.Add(t);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        ErrorLoggingService.GetInstance().LogStringError("Stage has an incompatible file version.");
                        return null;
                    }
                }
            }
            catch (Exception e)
            {
                ErrorLoggingService.GetInstance().LogExceptionError(e);
            }
            
            return stage;
        }
        
        #endregion

        #region Private static methods

        /// <summary>
        /// Returns a list of all motor stages found in a spreadsheet.  The spreadsheet location is the URI that is
        /// passed as a parameter to this function.
        /// THIS FUNCTION IS DEPRECATED AND NO LONGER USED.
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
                        stage.StageName = stageLine[0].Trim(new char[] { '\'', ' ', '\n', '\r', '\t' });
                        stage.Description = stageLine[1].Trim(new char[] { '\'', ' ', '\n', '\r', '\t' });
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
                        
                        if (hit_thresh.AdaptiveThresholdType == MotorStageAdaptiveThresholdType.Static)
                        {
                            hit_thresh.InitialValue = hit_thresh.MaximumValue;
                            hit_thresh.CurrentValue = hit_thresh.InitialValue;
                        }
                        else
                        {
                            hit_thresh.InitialValue = hit_thresh.MinimumValue;
                            hit_thresh.CurrentValue = hit_thresh.InitialValue;
                        }

                        switch (hit_thresh_type)
                        {
                            case MotorTaskTypeV1.PeakForce:

                                //Set the implementation of this stage
                                stage.StageImplementation = MotoTrakConfiguration.GetInstance().PythonStageImplementations["PythonPullStageImplementation.py"];
                                
                                //Set the parameters of this stage
                                stage.StageParameters.Clear();
                                stage.StageParameters["Hit Threshold"] = hit_thresh;
                                stage.StageParameters["Initiation Threshold"] = init_thresh;

                                break;
                            case MotorTaskTypeV1.SustainedForce:

                                //Set the implementation of this stage
                                stage.StageImplementation = MotoTrakConfiguration.GetInstance().PythonStageImplementations["PythonSustainedPullStageImplementation.py"];

                                //Set the parameters of this stage
                                stage.StageParameters.Clear();
                                stage.StageParameters["Initiation Threshold"] = init_thresh;
                                stage.StageParameters["Minimum Force"] = new MotorStageParameter() { InitialValue = 35, CurrentValue = 35 };
                                stage.StageParameters["Hold Duration"] = hit_thresh;

                                break;
                            case MotorTaskTypeV1.TotalDegrees:

                                //Set the implementation of this stage
                                stage.StageImplementation = MotoTrakConfiguration.GetInstance().PythonStageImplementations["PythonKnobStageImplementation.py"];

                                //Set the parameters of this stage
                                stage.StageParameters.Clear();
                                stage.StageParameters["Hit Threshold"] = hit_thresh;
                                stage.StageParameters["Initiation Threshold"] = init_thresh;

                                break;
                            case MotorTaskTypeV1.LeverPresses:

                                //Set the implementation of this stage
                                stage.StageImplementation = MotoTrakConfiguration.GetInstance().PythonStageImplementations["PythonLeverStageImplementation.py"];

                                //Set the parameters for this stage
                                stage.StageParameters.Clear();
                                stage.StageParameters["Full Press"] = new MotorStageParameter() { CurrentValue = 9.75, InitialValue = 9.75 };
                                stage.StageParameters["Release Point"] = new MotorStageParameter() { CurrentValue = 6.5, InitialValue = 6.5 };
                                stage.StageParameters["Initiation Threshold"] = new MotorStageParameter() { CurrentValue = 3, InitialValue = 3 };
                                stage.StageParameters["Hit Threshold"] = hit_thresh;

                                break;
                            default:

                                //Set the implementation of this stage
                                stage.StageImplementation = null;

                                //Set the parameters of this stage
                                stage.StageParameters.Clear();
                                stage.StageParameters["Hit Threshold"] = hit_thresh;
                                stage.StageParameters["Initiation Threshold"] = init_thresh;

                                break;
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

        private static List<MotorStage> RetrieveAllStage_Web (Uri address)
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

            //Store the first line in an array called "headers", and then remove it from the stageDocument variable
            List<string> headers = stageDocument[0];
            stageDocument.RemoveAt(0);
            
            //Create a list we will use to store all the stages
            List<MotorStage> stages = new List<MotorStage>();

            foreach (var line in stageDocument)
            {
                try
                { 
                    MotorStage new_stage = ReadSingleStage_Web(headers, line);
                    stages.Add(new_stage);
                }
                catch (Exception e)
                {
                    //empty
                }
            }

            return stages;
        }

        private static MotorStage ReadSingleStage_Web (List<string> headers, List<string> stage_params)
        {
            //Create a string variable to hold the task/stage definition if defined by the user
            string user_defined_task_definition = string.Empty;

            //Otherwise, set the variables for the new stage appropriately.
            MotorStage stage = new MotorStage();

            //Create a variable that will be used often in this function
            bool success = false;

            //Create stage parameter objects that will be needed by the end
            bool use_hit_ceiling = false;
            bool use_ir = false;
            MotorTaskTypeV1 hit_thresh_type = MotorTaskTypeV1.Undefined;
            MotorStageParameter hit_thresh = new MotorStageParameter();
            MotorStageParameter init_thresh = new MotorStageParameter();
            MotorStageParameter hit_ceiling = new MotorStageParameter();

            for (int i = 0; i < headers.Count; i++)
            {
                //Get the key and value
                var h = headers[i];
                var val = stage_params[i];

                if (h.StartsWith("tone", StringComparison.InvariantCultureIgnoreCase))
                {
                    var tone_parameter_name_parts = h.Split(new char[] { ' ' });
                    if (tone_parameter_name_parts.Length >= 3)
                    {
                        string tone_number_string = tone_parameter_name_parts[1].Trim();
                        string tone_parameter = tone_parameter_name_parts[2].Trim();

                        bool tone_number_parse_success = byte.TryParse(tone_number_string, out byte tone_number);
                        if (tone_number_parse_success)
                        {
                            //Instantiate the tone stage parameter object if necessary
                            var tone_stage_parameter = stage.ToneStageParameters.Where(x => x.ToneIndex == tone_number).FirstOrDefault();
                            if (tone_stage_parameter == null)
                            {
                                tone_stage_parameter = new MotorStageParameterTone()
                                {
                                    ToneIndex = tone_number
                                };

                                stage.ToneStageParameters.Add(tone_stage_parameter);
                            }

                            //Set the appropriate sub-parameter
                            if (tone_parameter.Contains("frequency", StringComparison.InvariantCultureIgnoreCase))
                            {
                                bool frequency_parse_success = UInt16.TryParse(val, out UInt16 tone_freq);
                                if (frequency_parse_success)
                                {
                                    tone_stage_parameter.ToneFrequency = tone_freq;
                                }
                            }
                            else if (tone_parameter.Contains("duration", StringComparison.InvariantCultureIgnoreCase))
                            {
                                bool duration_parse_success = Int32.TryParse(val, out int duration_ms);
                                if (duration_parse_success)
                                {
                                    tone_stage_parameter.ToneDuration = TimeSpan.FromMilliseconds(duration_ms);
                                }
                            }
                            else if (tone_parameter.Contains("event", StringComparison.InvariantCultureIgnoreCase))
                            {
                                MotorStageParameterTone.ToneEventType tone_event = MotorStageParameterTone.ToneEventType.Unknown;
                                if (val.Contains("hit", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    tone_event = MotorStageParameterTone.ToneEventType.Hit;
                                }
                                else if (val.Contains("miss", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    tone_event = MotorStageParameterTone.ToneEventType.Miss;
                                }
                                else if (val.Contains("hitwin", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    tone_event = MotorStageParameterTone.ToneEventType.HitWindow;
                                }
                                else if (val.Contains("rising", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    tone_event = MotorStageParameterTone.ToneEventType.Rising;
                                }
                                else if (val.Contains("falling", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    tone_event = MotorStageParameterTone.ToneEventType.Falling;
                                }
                                else if (val.Contains("cue", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    tone_event = MotorStageParameterTone.ToneEventType.Cue;
                                }

                                tone_stage_parameter.ToneEvent = tone_event;
                            }
                            else if (tone_parameter.Contains("thresh", StringComparison.InvariantCultureIgnoreCase))
                            {
                                bool thresh_parse_success = Int16.TryParse(val, out Int16 tone_thresh);
                                if (thresh_parse_success)
                                {
                                    tone_stage_parameter.ToneThreshold = tone_thresh;
                                }
                            }
                        }
                    }

                    //Now skip the rest of the loop body and go to the next iteration of the loop
                    continue;
                }

                //Figure out which parameter is being set
                MotoTrak_V1_StageParameters p = MotoTrak_V1_StageParameters_Converter.ConvertToMotorStageParameterType(h);

                switch (p)
                {
                    case MotoTrak_V1_StageParameters.StageNumber:
                        stage.StageName = val.Trim(new char[] {'\'', ' ', '\n', '\r', '\t' });
                        break;
                    case MotoTrak_V1_StageParameters.Description:
                        stage.Description = val.Trim(new char[] { '\'', ' ', '\n', '\r', '\t' });
                        break;
                    case MotoTrak_V1_StageParameters.InputDevice:
                        stage.DeviceType = MotorDeviceTypeConverter.ConvertToMotorDeviceType(val);
                        break;
                    case MotoTrak_V1_StageParameters.Constraint:
                        //Do nothing with this.  It is deprecated.
                        break;
                    case MotoTrak_V1_StageParameters.Position:
                        MotorStageParameter position = new MotorStageParameter();
                        position.InitialValue = Double.Parse(val);
                        position.CurrentValue = position.InitialValue;
                        position.AdaptiveThresholdType = MotorStageAdaptiveThresholdType.Static;
                        stage.Position = position;
                        break;
                    case MotoTrak_V1_StageParameters.HitThresholdType:

                        //Get the hit threshold type from the spreadsheet
                        hit_thresh.AdaptiveThresholdType = MotorStageAdaptiveThresholdTypeConverter.ConvertToMotorStageAdaptiveThresholdTypeFromSpreadsheetDescription(val);

                        //If the hit threshold type could not properly be parsed, then set it to be static
                        if (hit_thresh.AdaptiveThresholdType == MotorStageAdaptiveThresholdType.Undefined)
                        {
                            hit_thresh.AdaptiveThresholdType = MotorStageAdaptiveThresholdType.Static;
                        }

                        //If the hit threshold type is an adaptive threshold type, then set the parameter type to be "variable" instead of "fixed"
                        if (hit_thresh.AdaptiveThresholdType != MotorStageAdaptiveThresholdType.Static)
                        {
                            hit_thresh.ParameterType = MotorStageParameter.StageParameterType.Variable;
                        }

                        break;
                    case MotoTrak_V1_StageParameters.HitThresholdMinimum:
                        //Read in the hit threshold minimum value
                        double hit_min = double.NaN;
                        success = Double.TryParse(val, out hit_min);
                        hit_thresh.MinimumValue = hit_min;
                        break;
                    case MotoTrak_V1_StageParameters.HitThresholdMaximum:
                        //Read in the hit threshold maximum value
                        double hit_max = double.NaN;
                        success = Double.TryParse(val, out hit_max);
                        hit_thresh.MaximumValue = hit_max;
                        break;
                    case MotoTrak_V1_StageParameters.HitThresholdIncrement:
                        //Read in the hit threshold incremental value
                        double hit_inc = double.NaN;
                        success = Double.TryParse(val, out hit_inc);
                        hit_thresh.Increment = hit_inc;
                        break;
                    case MotoTrak_V1_StageParameters.TrialInitiationThreshold:
                        //Read in the trial initiation threshold
                        double trial_init = double.NaN;
                        success = Double.TryParse(val, out trial_init);
                        init_thresh.InitialValue = trial_init;
                        init_thresh.CurrentValue = init_thresh.InitialValue;
                        init_thresh.AdaptiveThresholdType = MotorStageAdaptiveThresholdType.Static;
                        break;
                    case MotoTrak_V1_StageParameters.ThresholdUnits:
                        //Read in the hit threshold type
                        hit_thresh_type = MotorTaskTypeV1Converter.ConvertToMotorStageHitThresholdType(val);
                        break;
                    case MotoTrak_V1_StageParameters.HitWindow:
                        //Read in the duration of the hit window
                        MotorStageParameter hit_win = new MotorStageParameter();
                        hit_win.InitialValue = Double.Parse(val);
                        hit_win.CurrentValue = hit_win.InitialValue;
                        hit_win.AdaptiveThresholdType = MotorStageAdaptiveThresholdType.Static;
                        stage.HitWindowInSeconds = hit_win;
                        break;
                    case MotoTrak_V1_StageParameters.SamplePeriod:
                        //Read in the sampling rate (the number of milliseconds inbetween each sample we read from the MotoTrak controller board)
                        stage.SamplePeriodInMilliseconds = Int32.Parse(val);
                        break;
                    case MotoTrak_V1_StageParameters.HitThresholdCeiling:

                        double ceiling = double.NaN;
                        success = Double.TryParse(val, out ceiling);
                        if (success)
                        {
                            use_hit_ceiling = true;
                            hit_ceiling.InitialValue = ceiling;
                            hit_ceiling.CurrentValue = hit_ceiling.InitialValue;
                            hit_ceiling.AdaptiveThresholdType = MotorStageAdaptiveThresholdType.Static;
                        }
                        else
                        {
                            use_hit_ceiling = false;
                        }

                        break;
                    case MotoTrak_V1_StageParameters.IRTrialInitiation:

                        string nominal_ir_value = val.Trim();
                        if (nominal_ir_value.Equals("YES", StringComparison.InvariantCultureIgnoreCase))
                        {
                            use_ir = true;
                        }
                        else
                        {
                            use_ir = false;
                        }

                        break;
                    case MotoTrak_V1_StageParameters.StimulateOnHit:

                        stage.OutputTriggerType = val.Trim();

                        break;
                    case MotoTrak_V1_StageParameters.TaskDefinitionFile:

                        user_defined_task_definition = val.Trim();

                        break;

                    default:
                        break;
                }
            }
            
            //Set the initial and current value of the hit threshold
            hit_thresh.InitialValue = hit_thresh.MinimumValue;
            hit_thresh.CurrentValue = hit_thresh.InitialValue;

            //Set the "task implementation" if it is defined by the user
            if (!string.IsNullOrEmpty(user_defined_task_definition))
            {
                if(MotoTrakConfiguration.GetInstance().PythonStageImplementations.ContainsKey(user_defined_task_definition))
                {
                    stage.StageImplementation = MotoTrakConfiguration.GetInstance().PythonStageImplementations[user_defined_task_definition];
                }
                else
                {
                    user_defined_task_definition = string.Empty;
                }
            }
            
            //Decide on a "task implementation" / "stage implementation" based on the parameters found in the spreadsheet.
            switch (hit_thresh_type)
            {
                case MotorTaskTypeV1.PeakForce:

                    if (use_hit_ceiling || use_ir)
                    {
                        //Set the parameters of this stage
                        stage.StageParameters.Clear();
                        stage.StageParameters["Lower bound force threshold"] = hit_thresh;
                        stage.StageParameters["Initiation Threshold"] = init_thresh;
                        
                        MotorStageParameter use_hit_ceiling_param = new MotorStageParameter();
                        use_hit_ceiling_param.ParameterName = "Use upper force boundary";
                        use_hit_ceiling_param.IsQuantitative = false;
                        use_hit_ceiling_param.NominalValue = use_hit_ceiling ? "Yes" : "No";

                        MotorStageParameter ir_sensor_param = new MotorStageParameter();
                        ir_sensor_param.ParameterName = "Use swipe sensor for trial initiations";
                        ir_sensor_param.IsQuantitative = false;
                        ir_sensor_param.NominalValue = use_ir ? "Yes" : "No";

                        hit_ceiling.ParameterName = "Upper bound force threshold";
                        use_hit_ceiling_param.ParameterName = "Use upper force boundary";
                        ir_sensor_param.ParameterName = "Use swipe sensor for trial initiations";

                        stage.StageParameters["Upper bound force threshold"] = hit_ceiling;
                        stage.StageParameters["Use upper force boundary"] = use_hit_ceiling_param;
                        stage.StageParameters["Use swipe sensor for trial initiations"] = ir_sensor_param;

                        //Set the implementation of this stage
                        if (string.IsNullOrEmpty(user_defined_task_definition))
                        {
                            stage.StageImplementation = MotoTrakConfiguration.GetInstance().PythonStageImplementations["PythonPullStageImplementation_FWIR.py"];
                        }
                        else if (user_defined_task_definition.Equals("PythonPullStageImplementation_TXBDC_PullWindowEric.py"))
                        {
                            MotorStageParameter lower_bound_temp = new MotorStageParameter()
                            {
                                InitialValue = hit_thresh.MinimumValue,
                                CurrentValue = hit_thresh.MinimumValue,
                                ParameterName = "Lower bound force threshold"
                            };

                            MotorStageParameter mean_temp = new MotorStageParameter()
                            {
                                InitialValue = hit_thresh.MaximumValue,
                                CurrentValue = hit_thresh.MaximumValue,
                                ParameterName = "Mean force target"
                            };

                            MotorStageParameter hit_thresh_inc_temp = new MotorStageParameter()
                            {
                                InitialValue = hit_thresh.Increment,
                                CurrentValue = hit_thresh.Increment,
                                ParameterName = "Percent of standard deviation"
                            };
                            
                            stage.StageParameters["Lower bound force threshold"] = lower_bound_temp;
                            stage.StageParameters["Mean force target"] = mean_temp;
                            stage.StageParameters["Percent of standard deviation"] = hit_thresh_inc_temp;
                        }
                    }
                    else
                    {
                        //Set the implementation of this stage
                        if (string.IsNullOrEmpty(user_defined_task_definition))
                        {
                            stage.StageImplementation = MotoTrakConfiguration.GetInstance().PythonStageImplementations["PythonPullStageImplementation.py"];
                        }
                            
                        //Set the parameters of this stage
                        stage.StageParameters.Clear();

                        hit_thresh.ParameterName = "Hit Threshold";
                        init_thresh.ParameterName = "Initiation Threshold";

                        stage.StageParameters["Hit Threshold"] = hit_thresh;
                        stage.StageParameters["Initiation Threshold"] = init_thresh;
                    }
                    
                    break;
                case MotorTaskTypeV1.SustainedForce:

                    //Set the implementation of this stage
                    if (string.IsNullOrEmpty(user_defined_task_definition))
                    {
                        stage.StageImplementation = MotoTrakConfiguration.GetInstance().PythonStageImplementations["PythonSustainedPullStageImplementation.py"];
                    }
                        
                    hit_thresh.ParameterName = "Hold Duration";
                    init_thresh.ParameterName = "Initiation Threshold";
                    
                    //Set the parameters of this stage
                    stage.StageParameters.Clear();
                    stage.StageParameters["Initiation Threshold"] = init_thresh;
                    stage.StageParameters["Minimum Force"] = new MotorStageParameter() { InitialValue = 35, CurrentValue = 35, ParameterName = "Minimum Force" };
                    stage.StageParameters["Hold Duration"] = hit_thresh;

                    break;
                case MotorTaskTypeV1.TotalDegrees:

                    hit_thresh.ParameterName = "Hit Threshold";
                    init_thresh.ParameterName = "Initiation Threshold";
                    
                    //Set the parameters of this stage
                    stage.StageParameters.Clear();
                    stage.StageParameters["Initiation Threshold"] = init_thresh;

                    //Set the implementation of this stage
                    if (string.IsNullOrEmpty(user_defined_task_definition))
                    {
                        //Set the hit threshold parameter
                        stage.StageParameters["Hit Threshold"] = hit_thresh;

                        //Set the base knob implementation as the task definition
                        stage.StageImplementation = MotoTrakConfiguration.GetInstance().PythonStageImplementations["PythonKnobStageImplementation.py"];
                    }
                    else if (user_defined_task_definition.Equals("PythonKnobStageImplementation_TXBDC_KnobWindow.py"))
                    {
                        //If the knob window task has been defined as the task definition...

                        MotorStageParameter lower_bound_temp = new MotorStageParameter()
                        {
                            InitialValue = hit_thresh.MinimumValue,
                            CurrentValue = hit_thresh.MinimumValue
                        };

                        MotorStageParameter mean_temp = new MotorStageParameter()
                        {
                            InitialValue = hit_thresh.MaximumValue,
                            CurrentValue = hit_thresh.MaximumValue
                        };

                        MotorStageParameter hit_thresh_inc_temp = new MotorStageParameter()
                        {
                            InitialValue = hit_thresh.Increment,
                            CurrentValue = hit_thresh.Increment
                        };

                        stage.StageParameters["Upper bound turn angle threshold"] = hit_ceiling;
                        stage.StageParameters["Lower bound turn angle threshold"] = lower_bound_temp;
                        stage.StageParameters["Mean turn angle target"] = mean_temp;
                        stage.StageParameters["Percent of standard deviation"] = hit_thresh_inc_temp;
                    }
                    
                    break;
                case MotorTaskTypeV1.LeverPresses:

                    //Set the implementation of this stage
                    if (string.IsNullOrEmpty(user_defined_task_definition))
                    {
                        stage.StageImplementation = MotoTrakConfiguration.GetInstance().PythonStageImplementations["PythonLeverStageImplementation.py"];
                    }
                        
                    //Set the parameters for this stage
                    stage.StageParameters.Clear();
                    stage.StageParameters["Full Press"] = new MotorStageParameter() { CurrentValue = 9.75, InitialValue = 9.75, ParameterName = "Full Press" };
                    stage.StageParameters["Release Point"] = new MotorStageParameter() { CurrentValue = 6.5, InitialValue = 6.5, ParameterName = "Release Point" };
                    stage.StageParameters["Initiation Threshold"] = new MotorStageParameter() { CurrentValue = 3, InitialValue = 3, ParameterName = "Initiation Threshold" };
                    stage.StageParameters["Hit Threshold"] = hit_thresh;

                    break;
                default:

                    //Set the implementation of this stage
                    stage.StageImplementation = null;

                    hit_thresh.ParameterName = "Hit Threshold";

                    //Set the parameters of this stage
                    stage.StageParameters.Clear();
                    stage.StageParameters["Hit Threshold"] = hit_thresh;
                    stage.StageParameters["Initiation Threshold"] = init_thresh;

                    break;
            }
            
            return stage;
        }

        private static MotorStageParameter SimpleParseDouble(string val)
        {
            MotorStageParameter result = new MotorStageParameter();
            bool success = Double.TryParse(val, out double double_val);
            result.InitialValue = double_val;
            result.CurrentValue = result.InitialValue;
            result.AdaptiveThresholdType = MotorStageAdaptiveThresholdType.Static;

            return result;
        }

        private static MotorStageParameter SimpleParseString(string val)
        {
            MotorStageParameter result = new MotorStageParameter();
            result.NominalValue = val;
            result.IsQuantitative = false;
            return result;
        }

        /// <summary>
        /// Retrieves all motor stages on the local disk at the specified location
        /// </summary>
        /// <param name="path">The path where stages are located</param>
        /// <returns>A list of MotoTrak stages</returns>
        private static List<MotorStage> RetrieveAllLocalStages (string path)
        {
            List<MotorStage> result = new List<MotorStage>();

            //First, list all stage files that are found
            DirectoryInfo dir_info = new DirectoryInfo(path);

            //If the directory exists
            if (dir_info.Exists)
            {
                //Find all files in the directory that are MotorStage files
                var file_list = dir_info.EnumerateFiles("*.MotorStage");

                //Attempt to load each file as a stage
                foreach (var f in file_list)
                {
                    MotorStage new_stage = MotorStage.LoadStageFromFile(f.FullName);

                    //Add the new stage to the resulting list of stages
                    result.Add(new_stage);
                }
            }

            //Return the list of stages to the caller
            return result;
        }

        private static MotorStageParameter ParseMotorStageParameterWithName(string parts)
        {
            string[] parts_array = parts.Split(new char[] { ',' }, 8);

            MotorStageParameter p = new MotorStageParameter()
            {
                ParameterName = parts_array[0].Trim(),
                ParameterUnits = parts_array[1].Trim(),
                ParameterType = (parts_array[2].Trim().Equals("Fixed")) ?
                    MotorStageParameter.StageParameterType.Fixed : MotorStageParameter.StageParameterType.Variable,
                AdaptiveThresholdType =
                    MotorStageAdaptiveThresholdTypeConverter.ConvertToMotorStageAdaptiveThresholdType(parts_array[3].Trim()),
                MinimumValue = Double.Parse(parts_array[5]),
                MaximumValue = Double.Parse(parts_array[6]),
                Increment = Double.Parse(parts_array[7]),
            };

            if (p.ParameterUnits.Equals("nominal", StringComparison.OrdinalIgnoreCase))
            {
                p.IsQuantitative = false;
                p.NominalValue = parts_array[4].Trim();
                p.InitialValue = double.NaN;
            }
            else
            {
                p.IsQuantitative = true;
                p.NominalValue = string.Empty;
                p.InitialValue = Double.Parse(parts_array[4]);
            }
            
            p.CurrentValue = p.InitialValue;

            return p;
        }

        private static string FormMotorStageParameter (MotorStageParameter p, bool save_name_as_part_of_it = true)
        {
            if (!p.IsQuantitative)
            {
                p.ParameterUnits = "nominal";
            }

            string output = string.Empty;
            if (save_name_as_part_of_it)
            {
                output = p.ParameterName + ", ";
                output += p.ParameterUnits + ", ";
            }

            output += (p.ParameterType == MotorStageParameter.StageParameterType.Fixed) ? "Fixed" : "Variable";
            output += ", ";
            output += MotorStageAdaptiveThresholdTypeConverter.ConvertToDescription(p.AdaptiveThresholdType) + ", ";

            if (!p.IsQuantitative)
            {
                output += p.NominalValue + ", ";
            }
            else
            {
                output += p.InitialValue.ToString() + ", ";
            }
            
            output += p.MinimumValue.ToString() + ", ";
            output += p.MaximumValue.ToString() + ", ";
            output += p.Increment.ToString();

            return output;
        }

        private static MotorStageParameterTone ParseMotorStageTone (string tone_parameters)
        {
            MotorStageParameterTone result = null;

            //String format:
            //Tone number, frequency, duration, event, threshold
            var parts = tone_parameters.Split(new char[] { ',' }).ToList();

            if (parts.Count >= 4)
            {
                bool success = false;

                //Parse the tone index
                success = byte.TryParse(parts[0], out byte tone_index);

                //Parse the tone frequency
                success &= UInt16.TryParse(parts[1], out UInt16 tone_frequency);

                //Parse the tone duration
                success &= Int32.TryParse(parts[2], out int tone_duration);

                //Parse the tone event
                var val = parts[3].Trim();
                MotorStageParameterTone.ToneEventType tone_event = MotorStageParameterTone.ToneEventType.Unknown;
                if (val.Contains("hit", StringComparison.InvariantCultureIgnoreCase))
                {
                    tone_event = MotorStageParameterTone.ToneEventType.Hit;
                }
                else if (val.Contains("miss", StringComparison.InvariantCultureIgnoreCase))
                {
                    tone_event = MotorStageParameterTone.ToneEventType.Miss;
                }
                else if (val.Contains("hitwin", StringComparison.InvariantCultureIgnoreCase))
                {
                    tone_event = MotorStageParameterTone.ToneEventType.HitWindow;
                }
                else if (val.Contains("rising", StringComparison.InvariantCultureIgnoreCase))
                {
                    tone_event = MotorStageParameterTone.ToneEventType.Rising;
                }
                else if (val.Contains("falling", StringComparison.InvariantCultureIgnoreCase))
                {
                    tone_event = MotorStageParameterTone.ToneEventType.Falling;
                }
                else if (val.Contains("cue", StringComparison.InvariantCultureIgnoreCase))
                {
                    tone_event = MotorStageParameterTone.ToneEventType.Cue;
                }
                else
                {
                    success &= false;
                }

                if (success)
                {
                    result = new MotorStageParameterTone()
                    {
                        ToneIndex = tone_index,
                        ToneFrequency = tone_frequency,
                        ToneDuration = TimeSpan.FromMilliseconds(tone_duration),
                        ToneEvent = tone_event
                    };

                    //Optional 5th parameter - tone threshold
                    if (parts.Count >= 5)
                    {
                        success = Int32.TryParse(parts[4], out int tone_threshold);
                        if (success)
                        {
                            result.ToneThreshold = tone_threshold;
                        }
                    }
                }
            }

            return result;
        }

        #endregion
    }
}
