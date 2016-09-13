using MotoTrakUtilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakBase
{
    /// <summary>
    /// This class handles saving MotoTrak sessions to disk.  You can either save a session one trial at a time, or all in one chunk.
    /// </summary>
    public class MotoTrakFileSave
    {
        #region Public static data members

        /// <summary>
        /// The MotoTrak file version
        /// </summary>
        public const int FileVersion = -5;

        #endregion

        #region Public enumerations

        public enum SavePathType
        {
            PrimaryPath,
            SecondaryPath
        }

        #endregion

        #region Private data members
        
        private string _file_path = string.Empty;
        private FileStream _file_stream = null;
        private BinaryWriter _binary_writer = null;
        private List<string> keys = new List<string>();

        #endregion

        #region Constructor

        /// <summary>
        /// Creates an empty MotoTrakFileSave object
        /// </summary>
        public MotoTrakFileSave ( )
        {
            //empty constructor
        }
        
        /// <summary>
        /// Constructor that takes a file name as an input parameter
        /// </summary>
        /// <param name="file_name">The path and file name that will be used to save the session data</param>
        public MotoTrakFileSave ( string file_name )
        {
            _file_path = file_name;
        }

        /// <summary>
        /// Constructor that takes the rat name, stage, and an optional primary/secondary path flag, and constructs
        /// a file path based off of that data
        /// </summary>
        /// <param name="rat_name">Rat name</param>
        /// <param name="stage">Selected stage</param>
        /// <param name="path_type">Primary or secondary data path</param>
        public MotoTrakFileSave ( string rat_name, MotorStage stage, SavePathType path_type = SavePathType.PrimaryPath )
        {
            _file_path = MotoTrakFileSave.ResolveFullFilePathAndName(rat_name, stage, path_type);
        }

        /// <summary>
        /// Constructor that takes a MotoTrak session object as well as a flag that indicates whether to save to the primary
        /// or secondary path.
        /// </summary>
        /// <param name="current_session">The session to be saved</param>
        /// <param name="path_type">Primary or secondary data path</param>
        public MotoTrakFileSave ( MotoTrakSession current_session, SavePathType path_type = SavePathType.PrimaryPath )
        {
            _file_path = MotoTrakFileSave.ResolveFullFilePathAndName(current_session, path_type);
        }

        #endregion

        #region Static public methods

        /// <summary>
        /// This function resolves a full file path and name for a session, given a rat name, a stage, and (optionally) whether to save
        /// to the primary data path or the secondary data path.
        /// </summary>
        /// <param name="rat_name">Rat name</param>
        /// <param name="current_stage">Selected stage for the session</param>
        /// <param name="path_type">Whether to save to the primary or secondary data-path</param>
        /// <returns>The fully-qualified save path and file name</returns>
        public static string ResolveFullFilePathAndName ( string rat_name, MotorStage current_stage, SavePathType path_type = SavePathType.PrimaryPath )
        {
            if (!string.IsNullOrEmpty(rat_name) && current_stage != null)
            {
                string rat_folder = rat_name;
                string stage_folder = current_stage.StageName;

                string base_path = MotoTrakConfiguration.GetInstance().DataPath;
                if (path_type == SavePathType.SecondaryPath)
                {
                    base_path = MotoTrakConfiguration.GetInstance().SecondaryDataPath;
                }

                //Initially set the full path to equal the base path
                string full_path = base_path;

                //If the base path doesn't end with a back-slash, then add one to the end of the full path
                if (!base_path.EndsWith(@"\"))
                {
                    full_path = base_path + @"\";
                }

                //Now append the rat folder and stage folder to the full path
                full_path += rat_folder + @"\" + stage_folder + @"\";

                //Calculate the session's file name
                string session_time = DateTime.Now.ToString("yyyyMMddTHHmmss");
                string file_name = rat_name + "_" + session_time + "_" + stage_folder + ".MotoTrak";

                //Append the file name to the full path
                full_path += file_name;

                //Return the fully qualified path
                return full_path;
            }

            return string.Empty;
        }

        /// <summary>
        /// This function resolves a full file path and name for a session, given current session, and (optionally) whether to save
        /// to the primary data path or the secondary data path.
        /// </summary>
        /// <param name="current_session">The current session to be saved</param>
        /// <param name="path_type">Whether to save to the primary or secondary data-path</param>
        /// <returns>The fully qualified file path and name for saving the session</returns>
        public static string ResolveFullFilePathAndName( MotoTrakSession current_session, SavePathType path_type = SavePathType.PrimaryPath )
        {
            if (current_session != null)
            {
                return ResolveFullFilePathAndName(current_session.RatName, current_session.SelectedStage, path_type);
            }

            return string.Empty;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Opens a writing stream to a file to save the session.  The string parameter is set as the file name to be used.
        /// </summary>
        /// <param name="path">The fully-qualified path to the file</param>
        /// <returns>True if successful, false if unsuccessful</returns>
        public bool OpenFileStream (string path)
        {
            _file_path = path;

            try
            {
                //Create directory if it doesn't exist
                new FileInfo(_file_path).Directory.Create();

                //Open a file at the path location to write to
                _file_stream = new FileStream(_file_path, FileMode.Create);
                _binary_writer = new BinaryWriter(_file_stream, Encoding.ASCII);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Opens a writing stream to a file to save the session.  If this function is used, the file name should
        /// have already been specified by the user.
        /// </summary>
        /// <returns>True if successful, false if unsuccessful</returns>
        public bool OpenFileStream ()
        {
            return OpenFileStream(_file_path);
        }

        /// <summary>
        /// Closes the currently open file stream
        /// </summary>
        public void CloseFileStream ()
        {
            if (_file_stream != null)
            {
                _file_stream.Close();
            }
        }

        /// <summary>
        /// Saves an entire session to a file
        /// </summary>
        /// <param name="current_session">The session currently in memory</param>
        public void SaveEntireSession ( MotoTrakSession current_session )
        {
            if (_file_stream != null && _file_stream.CanWrite && _binary_writer != null && current_session != null)
            {
                //Save the session headers
                SaveSessionHeaders(current_session);

                //Now, save each trial in the session to the file
                for (int i = 0; i < current_session.Trials.Count; i++)
                {
                    SaveTrial(current_session.Trials[i], Convert.ToUInt32(i+1));
                }
            }
        }

        /// <summary>
        /// Saves the session headers to a file
        /// </summary>
        /// <param name="current_session">The session currently in memory</param>
        public void SaveSessionHeaders ( MotoTrakSession current_session )
        {
            if (_file_stream != null && _file_stream.CanWrite && _binary_writer != null && current_session != null)
            {
                //First, save the file version to the file
                _binary_writer.Write(Convert.ToSByte(MotoTrakFileSave.FileVersion));

                //Next, save the session start time (as an 8-byte double, in Matlab datecode format)
                double session_start_time = MotorMath.ConvertDateTimeToMatlabDatenum(current_session.StartTime);
                _binary_writer.Write(session_start_time);

                //Next, save the session end time (as an 8-bute double, in Matlab datecode format)
                double session_end_time = MotorMath.ConvertDateTimeToMatlabDatenum(current_session.EndTime);
                _binary_writer.Write(session_end_time);

                //Save the number of characters in the rat's name
                Byte N = Convert.ToByte(current_session.RatName.Length);
                _binary_writer.Write(N);

                //Save the characters of the rat's name
                _binary_writer.Write(current_session.RatName.ToCharArray());

                //Save the number of characters in the booth title
                N = Convert.ToByte(current_session.BoothLabel.Length);
                _binary_writer.Write(N);

                //Save the characters of the booth title
                _binary_writer.Write(current_session.BoothLabel.ToCharArray());

                //Save the number of characters in the stage title
                N = Convert.ToByte(current_session.SelectedStage.StageName.Length);
                _binary_writer.Write(N);

                //Save the characters of the stage title
                _binary_writer.Write(current_session.SelectedStage.StageName.ToCharArray());

                //Save the number of characters in the device description
                N = Convert.ToByte(current_session.Device.DeviceName.Length);
                _binary_writer.Write(N);

                //Save the device description
                _binary_writer.Write(current_session.Device.DeviceName.ToCharArray());

                //Save the number of characters found in the session notes
                UInt16 n_notes = Convert.ToUInt16(current_session.SessionNotes.Length);
                _binary_writer.Write(n_notes);

                //Save the session notes
                _binary_writer.Write(current_session.SessionNotes.ToCharArray());

                //Save the number of coefficients used in the calibration function
                N = Convert.ToByte(current_session.Device.Coefficients.Keys.Count);
                _binary_writer.Write(N);

                //Save each coefficient used in the calibration function
                foreach (var k in current_session.Device.Coefficients.Keys)
                {
                    float value = Convert.ToSingle(current_session.Device.Coefficients[k]);
                    _binary_writer.Write(value);
                }

                //Save the number of stream signals used in this session
                N = Convert.ToByte(current_session.SelectedStage.TotalDataStreams);
                _binary_writer.Write(N);

                //Save the metadata for each data stream
                for (int i = 0; i < current_session.SelectedStage.TotalDataStreams; i++)
                {
                    //Get the data stream type
                    var data_stream_type = current_session.SelectedStage.DataStreamTypes[i];

                    //Get the name of the data stream and its units
                    string description = MotorBoardDataStreamTypeConverter.ConvertToDescription(data_stream_type);
                    string units = MotorBoardDataStreamTypeConverter.ConvertToUnitsDescription(data_stream_type);

                    //If the data stream is a MotoTrak manipulandum, get the units of the device type for the session
                    if (data_stream_type == MotorBoardDataStreamType.DeviceValue)
                    {
                        units = MotorDeviceTypeConverter.ConvertToUnitsDescription(current_session.Device.DeviceType);
                    }

                    //Save the description of the stream to the file
                    N = Convert.ToByte(description.Length);
                    _binary_writer.Write(N);
                    _binary_writer.Write(description.ToCharArray());

                    //Save the units of the stream to the file
                    N = Convert.ToByte(units.Length);
                    _binary_writer.Write(N);
                    _binary_writer.Write(units.ToCharArray());
                }

                //Save the number of stage parameters that exist
                UInt32 n_stage_params = Convert.ToUInt32(current_session.SelectedStage.StageParameters.Count);
                _binary_writer.Write(n_stage_params);

                //Save each stage parameter to the file
                foreach (var k in current_session.SelectedStage.StageParameters)
                {
                    //Get the parameter name
                    string parameter_name = k.Key;

                    //Add this parameter name to our ordered list of keys
                    keys.Add(parameter_name);

                    //Save the parameter name to the file
                    N = Convert.ToByte(parameter_name.Length);
                    _binary_writer.Write(N);
                    _binary_writer.Write(parameter_name.ToCharArray());
                }

                //Make sure the data is actually written to the file before continuing
                _file_stream.Flush();
            }
        }

        /// <summary>
        /// Saves a trial to the currently open file
        /// </summary>
        /// <param name="trial">An individual MotorTrial object</param>
        public void SaveTrial ( MotorTrial trial, UInt32 trial_number )
        {
            if (_file_stream != null && _file_stream.CanWrite && _binary_writer != null && trial != null)
            {
                //Write the trial number out to the file
                _binary_writer.Write(trial_number);

                //Write the start time of the trial
                double trial_start_time = MotorMath.ConvertDateTimeToMatlabDatenum(trial.StartTime);
                _binary_writer.Write(trial_start_time);

                //Write the outcome of the trial
                Byte result_code = MotorTrialResultConverter.ConvertToTrialResultCode(trial.Result);
                _binary_writer.Write(result_code);

                //If the trial is a "pause" trial
                if (trial.Result == MotorTrialResult.Pause)
                {
                    double trial_end_time = MotorMath.ConvertDateTimeToMatlabDatenum(trial.EndTime);
                    _binary_writer.Write(trial_end_time);
                }

                //Save the hit window duration (in seconds)
                Single hit_win_duration = Convert.ToSingle(trial.HitWindowDurationInSeconds);
                _binary_writer.Write(hit_win_duration);

                //Save the pre-trial sampling window duration (in seconds)
                Single pre_duration = Convert.ToSingle(trial.PreTrialSamplingPeriodInSeconds);
                _binary_writer.Write(pre_duration);

                //Save the post-trial sampling window duration (in seconds)
                Single post_duration = Convert.ToSingle(trial.PostTrialSamplingPeriodInSeconds);
                _binary_writer.Write(post_duration);

                //Save the post-trial time-out (in seconds)
                Single post_timeout = Convert.ToSingle(trial.PostTrialTimeOutInSeconds);
                _binary_writer.Write(post_timeout);

                //Save the manipulandum position for this trial
                Single position = Convert.ToSingle(trial.DevicePosition);
                _binary_writer.Write(position);

                //Save the number of variable parameters that exist for this trial
                Byte N = Convert.ToByte(trial.VariableParameters.Count);
                _binary_writer.Write(N);

                //Save each of the variable parameters for this trial
                foreach (var k in keys)
                {
                    //Save the parameter value
                    float parameter_value = float.NaN;
                    if (trial.VariableParameters.Keys.Contains(k))
                    {
                        parameter_value = Convert.ToSingle(trial.VariableParameters[k]);
                    }
                    
                    _binary_writer.Write(parameter_value);
                }
                
                //Save the number of hits that occurred during this trial
                N = Convert.ToByte(trial.HitTimes.Count);
                _binary_writer.Write(N);

                //Save the timestamp of each hit that occurred
                for (int i = 0; i < trial.HitTimes.Count; i++)
                {
                    double hit_time = MotorMath.ConvertDateTimeToMatlabDatenum(trial.HitTimes[i]);
                    _binary_writer.Write(hit_time);
                }

                //Save the number of output trigger events
                N = Convert.ToByte(trial.OutputTriggers.Count);
                _binary_writer.Write(N);

                //Save the timestamp of each output trigger
                for (int i = 0; i < trial.OutputTriggers.Count; i++)
                {
                    double output_time = MotorMath.ConvertDateTimeToMatlabDatenum(trial.OutputTriggers[i]);
                    _binary_writer.Write(output_time);
                }

                //Save the number of samples in the signal
                UInt32 n_samples = Convert.ToUInt32(trial.TrialData[0].Count);
                _binary_writer.Write(n_samples);

                //Save the data from each stream
                for (int i = 0; i < trial.TrialData.Count; i++)
                {
                    for (int x = 0; x < trial.TrialData[i].Count; x++)
                    {
                        _binary_writer.Write(Convert.ToSingle(trial.TrialData[i][x]));
                    }
                }

                //Make sure the data is actually written to the file before continuing
                _file_stream.Flush();
            }
        }

        #endregion
    }
}
