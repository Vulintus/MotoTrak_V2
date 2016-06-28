using System;
using System.Collections.Generic;
using MotoTrakUtilities;

namespace MotoTrakBase
{
    /// <summary>
    /// This class represents a stage within the MotoTrak program.
    /// </summary>
    public class MotorStage
    {
        #region Private data members

        private int _stageVersion = 1;

        private const int _defaultPreTrialSamplingPeriodInSeconds = 1;
        private const int _defaultPostTrialSamplingPeriodInSeconds = 2;
        private const int _defaultTotalDataStreams = 3;

        private int _preTrialSamplingPeriodInSeconds = _defaultPreTrialSamplingPeriodInSeconds;
        private int _postTrialSamplingPeriodInSeconds = _defaultPostTrialSamplingPeriodInSeconds;
        private int _totalDataStreams = _defaultTotalDataStreams;

        private List<MotorBoardDataStreamType> _dataStreamTypes = new List<MotorBoardDataStreamType>()
            {  MotorBoardDataStreamType.Timestamp, MotorBoardDataStreamType.DeviceValue, MotorBoardDataStreamType.IRSensorValue };

        private IMotorStageImplementation _stageImplementation = null;

        private double _hitThreshold = 0;
        
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

        #region Properties - V2

        /// <summary>
        /// The stage version being used.  A value of 1 indicates that this stage is a MotoTrak V1 style of stage.
        /// A value of 2 indicates that this stage was designed for exclusively for MotoTrak V2.
        /// </summary>
        public int StageVersion
        {
            get
            {
                return _stageVersion;
            }
            set
            {
                _stageVersion = value;
            }
        }

        /// <summary>
        /// The total number of streams of data that will be streamed from the Arduino based on the settings of this stage.
        /// The default number is 3 (and this is the number of streams that was used in MotoTrak V1).
        /// The 3 default streams (in order) are: { timestamp, device value, IR sensor value }
        /// </summary>
        public int TotalDataStreams
        {
            get
            {
                return _totalDataStreams;
            }
            set
            {
                _totalDataStreams = value;
            }
        }

        /// <summary>
        /// An ordered list that contains the type of each data-stream that will be received from the MotoTrak controller board
        /// for this stage.  For MotoTrak V1 stages, this list is the same for all stages.  MotoTrak V2 stages, however,
        /// can define the order and number of streams that they want to receive from the MotoTrak controller board.
        /// The 3 default streams (in order) for V1 are: { timestamp, device value, IR sensor value }
        /// </summary>
        public List<MotorBoardDataStreamType> DataStreamTypes
        {
            get
            {
                return _dataStreamTypes;
            }
            set
            {
                _dataStreamTypes = value;
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

        public int TrialsToRetainForAdaptiveAdjustments { get; set; }

        #endregion

        #region Properties - V1

        /// <summary>
        /// The number of the stage
        /// </summary>
        public string StageNumber { get; set; }

        /// <summary>
        /// A text description of the stage
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The type of device the stage uses
        /// </summary>
        public MotorDeviceType DeviceType { get; set; }

        /// <summary>
        /// The position of the "peg" for the device on this stage
        /// </summary>
        public double Position { get; set; }

        /// <summary>
        /// The type of adaptive threshold being used for this stage
        /// </summary>
        public MotorStageAdaptiveThresholdType AdaptiveThresholdType { get; set; }

        /// <summary>
        /// The minimum hit threshold for this stage
        /// </summary>
        public double HitThresholdMinimum { get; set; }

        /// <summary>
        /// The maximum hit threshold for this stage
        /// </summary>
        public double HitThresholdMaximum { get; set; }

        /// <summary>
        /// The increment by which the hit threshold changes for this stage
        /// </summary>
        public double HitThresholdIncrement { get; set; }

        /// <summary>
        /// The fixed hit threshold for this stage
        /// </summary>
        public double HitThreshold
        {
            get
            {
                return _hitThreshold;
            }
            set
            {
                _hitThreshold = value;
            }
        }

        /// <summary>
        /// The trial initiation threshold for this stage
        /// </summary>
        public double TrialInitiationThreshold { get; set; }

        /// <summary>
        /// The type of hit threshold that this stage uses
        /// </summary>
        public MotorStageHitThresholdType HitThresholdType { get; set; }

        /// <summary>
        /// The total duration of the hit window (in seconds)
        /// </summary>
        public double HitWindowInSeconds { get; set; }

        /// <summary>
        /// The number of milliseconds each sample in the signal represents.
        /// </summary>
        public int SamplePeriodInMilliseconds { get; set; }
        
        /// <summary>
        /// The type of stimulation that is delivered on this stage.
        /// </summary>
        public MotorStageStimulationType StimulationType { get; set; }

        /// <summary>
        /// The total amount of time (in seconds) during which we record samples at the beginning of a trial
        /// before the hit window begins.  In V1 of MotoTrak, the value of this variable was fixed at 1.  In V2
        /// of MotoTrak, this variable may be defined by the stage definition file.
        /// </summary>
        public int PreTrialSamplingPeriodInSeconds
        {
            get
            {
                if (StageVersion == 1)
                {
                    return _defaultPreTrialSamplingPeriodInSeconds;
                }
                else
                {
                    return _preTrialSamplingPeriodInSeconds;
                }
            }
            set
            {
                //In V1, this was a read-only property.
                //In V2, this can be set by the stage definition.
                if (StageVersion > 1)
                {
                    _preTrialSamplingPeriodInSeconds = value;
                }
            }
        }

        /// <summary>
        /// The total amount of time (in seconds) during which we record samples at the end of a trial (after the
        /// hit window ends).  In V1 of MotoTrak, the value of this variable was fixed at 2.  In V2 of MotoTrak,
        /// this variable may be defined by the stage definition file.
        /// </summary>
        public int PostTrialSamplingPeriodInSeconds
        {
            get
            {
                if (StageVersion == 1)
                { 
                    return _defaultPostTrialSamplingPeriodInSeconds;
                }
                else
                {
                    return _postTrialSamplingPeriodInSeconds;
                }
            }
            set
            {
                //In V1, this was a read-only property.
                //In V2, this can be set by the stage definition.
                if (StageVersion > 1)
                {
                    _postTrialSamplingPeriodInSeconds = value;
                }
            }
        }

        #endregion

        #region Read-only properties - V1

        /// <summary>
        /// The possible hit threshold types that may be available based on the kind of device this stage uses.
        /// </summary>
        public List<MotorStageHitThresholdType> PossibleHitThresholdTypes
        {
            get
            {
                List<MotorStageHitThresholdType> possibleTypes = new List<MotorStageHitThresholdType>();
                switch (DeviceType)
                {
                    case MotorDeviceType.Pull:
                        possibleTypes.Add(MotorStageHitThresholdType.PeakForce);
                        possibleTypes.Add(MotorStageHitThresholdType.SustainedForce);
                        break;
                    case MotorDeviceType.Knob:
                        possibleTypes.Add(MotorStageHitThresholdType.TotalDegrees);
                        break;
                    default:
                        possibleTypes.Add(MotorStageHitThresholdType.Undefined);
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
                return (PreTrialSamplingPeriodInSeconds * SamplesPerSecond);
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
                return (PostTrialSamplingPeriodInSeconds * SamplesPerSecond);
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
                return Convert.ToInt32(HitWindowInSeconds * SamplesPerSecond);
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
            List<List<string>> stageDocument = ReadGoogleSpreadsheet.Read(address);

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
                        stage.StageNumber = stageLine[0];
                        stage.Description = stageLine[1];
                        stage.DeviceType = MotorDeviceTypeConverter.ConvertToMotorDeviceType(stageLine[2]);
                        
                        //Read the constraint from the stage file and throw it away
                        int trash = Int32.Parse(stageLine[3]);

                        stage.Position = Double.Parse(stageLine[4]);
                        stage.AdaptiveThresholdType = MotorStageAdaptiveThresholdTypeConverter.ConvertToMotorStageAdaptiveThresholdType(stageLine[5]);

                        double hit_min = double.NaN;
                        bool success = Double.TryParse(stageLine[6], out hit_min);
                        stage.HitThresholdMinimum = hit_min;

                        double hit_max = double.NaN;
                        success = Double.TryParse(stageLine[7], out hit_max);
                        stage.HitThresholdMaximum = hit_max;

                        double hit_inc = double.NaN;
                        success = Double.TryParse(stageLine[8], out hit_inc);
                        stage.HitThresholdIncrement = hit_inc;

                        double trial_init = double.NaN;
                        success = Double.TryParse(stageLine[9], out trial_init);
                        stage.TrialInitiationThreshold = trial_init;
                        
                        stage.HitThresholdType = MotorStageHitThresholdTypeConverter.ConvertToMotorStageHitThresholdType(stageLine[10]);
                        
                        stage.HitWindowInSeconds = Double.Parse(stageLine[11]);
                        stage.SamplePeriodInMilliseconds = Int32.Parse(stageLine[12]);
                        
                        stage.StimulationType = MotorStageStimulationTypeConverter.ConvertToMotorStageStimulationType(stageLine[13]);

                        //For all "version 1" stages, this value will be 10.
                        stage.TrialsToRetainForAdaptiveAdjustments = 10;

                        if (stage.AdaptiveThresholdType == MotorStageAdaptiveThresholdType.Static)
                        {
                            //Set the "current" hit threshold to the maximum hit threshold for static stages
                            stage.HitThreshold = stage.HitThresholdMaximum;
                        }
                        else
                        {
                            //Set the "current" hit threshold to the minimum hit threshold for adaptive stages
                            stage.HitThreshold = stage.HitThresholdMinimum;
                        }

                        //Set the implementation of this stage
                        if (stage.HitThresholdType == MotorStageHitThresholdType.PeakForce)
                        {
                            //stage.StageImplementation = new PullStageImplementation();
                            string stage_file = @"C:\Users\dtp110020\Documents\mototrak-2.0\MotoTrak\MotoTrakPythonCode\PythonBasicStageImplementation.py";
                            stage.StageImplementation = new PythonStageImplementation(stage_file);
                            //stage.StageImplementation = new PythonStageImplementation("PythonBasicStageImplementation.py");
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
