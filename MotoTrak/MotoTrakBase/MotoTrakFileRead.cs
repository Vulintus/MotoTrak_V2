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
    /// This static class handles file input/output operations for MotoTrak.
    /// </summary>
    public static class MotoTrakFileRead
    {
        /// <summary>
        /// This function is intended to read in all sessions the the specified rat has performed
        /// on the specified stage.  Its primary intended use is to load recent performance data
        /// for a rat.  It loads the data based on the primary data path specified in the MotoTrak
        /// configuration file.
        /// </summary>
        /// <param name="rat_name">The rat to load data for</param>
        /// <param name="stage_name">The stage to load data for</param>
        /// <returns>All MotoTrak sessions found for the specified rat and stage</returns>
        public static List<MotoTrakSession> ReadHistory (string rat_name, string stage_name)
        {
            //Create the list that we will return to the caller
            List<MotoTrakSession> session_history = new List<MotoTrakSession>();

            //Get the full path where we will search for files to load
            string full_path = MotoTrakFileRead.ResolveFullPath(rat_name, stage_name);

            //Load all MotoTrak sessions found in the path
            DirectoryInfo folder_info = new DirectoryInfo(full_path);

            if (folder_info.Exists)
            {
                //Get the list of all the files that exist at the path
                List<FileInfo> all_file_info = folder_info.EnumerateFiles("*.MotoTrak").ToList();

                //Load each file
                foreach (var session_file in all_file_info)
                {
                    MotoTrakSession new_session = MotoTrakFileRead.ReadFile(session_file.FullName);
                    if (new_session != null)
                    {
                        session_history.Add(new_session);
                    }
                }
            }
            
            //Return the list of loaded sessions to the caller.
            return session_history;
        }

        /// <summary>
        /// This function is meant to resolve a "load path" (or even a save path)
        /// when a rat and stage name are specified.  This is the path at which data
        /// can be found for this rat.  The path returned is based on the primary
        /// save location in the MotoTrak configuration file.
        /// </summary>
        /// <param name="rat_name">The rat name</param>
        /// <param name="stage_name">The stage name</param>
        /// <returns>The fully resolved path where data may be found</returns>
        public static string ResolveFullPath (string rat_name, string stage_name)
        {
            //Get the base path where files are saved
            string base_path = MotoTrakConfiguration.GetInstance().DataPath;
            
            //Initially set the full path to equal the base path
            string full_path = base_path;

            //If the base path doesn't end with a back-slash, then add one to the end of the full path
            if (!base_path.EndsWith(@"\"))
            {
                full_path = base_path + @"\";
            }

            //Now append the rat folder and stage folder to the full path
            full_path += rat_name + @"\" + stage_name + @"\";
            
            //Return the full path to load files from
            return full_path;
        }

        /// <summary>
        /// This function reads a MotoTrak session.
        /// NO EFFORT has been made to make this function compatible with ArdyMotor version 1.0 files.
        /// However, all ArdyMotor version 2.0 files should be compatible with this function.
        /// </summary>
        /// <param name="fully_qualified_path">The path of the file (including the file name)</param>
        public static MotoTrakSession ReadFile (string fully_qualified_path)
        {
            try
            {
                //Open the file for reading
                byte[] file_bytes = System.IO.File.ReadAllBytes(fully_qualified_path);

                //Determine the file version
                SByte version = (sbyte)file_bytes[0];
                if (version > -5)
                {
                    return ReadArdyMotorVersion2File(file_bytes);
                }
                if (version == -5)
                {
                    return ReadMotoTrakFile(file_bytes);
                }
            }
            catch
            {
                //Inform the user that messaging data could not be loaded
                MotoTrakMessaging.GetInstance().AddMessage("Could not load session data!");
            }

            return null;
        }

        private static MotoTrakSession ReadMotoTrakFile (byte[] file_bytes)
        {
            //Create a session object that will be returned to the caller
            MotoTrakSession session = new MotoTrakSession();

            //Create a stage object within the session
            session.SelectedStage = new MotorStage();

            //Create a device object within the session
            session.Device = new MotorDevice();

            //Create a stream out of the byte array
            MemoryStream stream = new MemoryStream(file_bytes);
            BinaryReader reader = new BinaryReader(stream);

            try
            {
                //Read in the MotoTrak file header
                ReadMotoTrakFileHeader(session, reader);

                //Read in each trial
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    try
                    {
                        ReadMotoTrakFileEvent(session, reader);
                    }
                    catch
                    {
                        MotoTrakMessaging.GetInstance().AddMessage("Unable to read MotoTrak file event!");
                    }
                }
            }
            catch
            {
                //Add an error message to the log for the user to see
                MotoTrakMessaging.GetInstance().AddMessage("Unable to load MotoTrak file!");
            }
            

            //Return the session to the caller
            return session;
        }

        private static void ReadMotoTrakFileHeader (MotoTrakSession session, BinaryReader reader)
        {
            if (reader != null && session != null)
            {
                //First, read the file version
                SByte file_version = reader.ReadSByte();

                if (file_version == -5)
                {
                    //Next, read the session start time
                    double session_start_time = reader.ReadDouble();
                    session.StartTime = MotorMath.ConvertMatlabDatenumToDateTime(session_start_time);

                    //Next, read in the number of characters in the rat's name
                    Byte N = reader.ReadByte();

                    //Next, read in the rat's name
                    session.RatName = new string(reader.ReadChars(N));

                    //Read in the number of characters in the booth title
                    N = reader.ReadByte();

                    //Next, read in the booth title
                    session.BoothLabel = new string(reader.ReadChars(N));

                    //Next, read in the number of characters in the stage name
                    N = reader.ReadByte();

                    //Next, read in the stage name
                    string stage_name = new string(reader.ReadChars(N));

                    //Create a new stage object within the session
                    session.SelectedStage = new MotorStage();
                    session.SelectedStage.StageName = stage_name;

                    //Read in the number of characters in the device description
                    N = reader.ReadByte();

                    //Read in the device description
                    string device_description = new string(reader.ReadChars(N));
                    
                    //Read in the number of coefficients used in the calibration function
                    N = reader.ReadByte();

                    //Read in each coefficient
                    Dictionary<int, double> coeffs = new Dictionary<int, double>();
                    for (Byte i = 0; i < N; i++)
                    {
                        float coeff = reader.ReadSingle();

                        //Set the coefficient in the device
                        coeffs[i] = coeff;
                    }

                    //Create a device within the session
                    session.Device = new MotorDevice(MotorDeviceTypeConverter.ConvertToMotorDeviceType(device_description), 0, coeffs);

                    //Read in the number of streams used in this session
                    N = reader.ReadByte();

                    //Read in the metadata from each stream
                    session.SelectedStage.DataStreamTypes = new List<MotorBoardDataStreamType>();
                    for (Byte i = 0; i < N; i++)
                    {
                        //Read in the stream description
                        Byte n_char_description = reader.ReadByte();
                        string stream_desc = new string(reader.ReadChars(n_char_description));

                        //Read in the stream units
                        Byte n_char_units = reader.ReadByte();
                        string stream_units = new string(reader.ReadChars(n_char_units));

                        //Add the data stream to the session
                        session.SelectedStage.DataStreamTypes.Add(MotorBoardDataStreamTypeConverter.ConvertToMotorBoardDataStreamType(stream_desc));
                    }

                    //Read in the number of stage parameters that exist
                    UInt32 n_params = reader.ReadUInt32();

                    //Read in each stage parameter
                    for (UInt32 i = 0; i < n_params; i++)
                    {
                        //Read in the number of characters in the name of each stage parameter
                        N = reader.ReadByte();

                        //Read in the parameter name itself
                        string param_name = new string(reader.ReadChars(N));

                        //Create a new MotorStageParameter object
                        MotorStageParameter k = new MotorStageParameter();
                        k.ParameterName = param_name;

                        //Add the parameter to the stage
                        session.SelectedStage.StageParameters.TryAdd(param_name, k);
                    }
                }
            }
        }

        private static void ReadMotoTrakFileTrial (MotoTrakSession session, MotorTrial trial, BinaryReader reader)
        {
            if (trial != null && reader != null)
            {
                //Read in the trial number
                UInt32 trial_number = reader.ReadUInt32();

                //Read the start time of the trial
                double start_time = reader.ReadDouble();
                trial.StartTime = MotorMath.ConvertMatlabDatenumToDateTime(start_time);

                //Read the trial result
                Byte result_code = reader.ReadByte();
                trial.Result = MotorTrialResultConverter.ConvertResultCodeToEnumeratedType(result_code);

                //Read the trial end time (for pause trials)
                if (trial.Result == MotorTrialResult.Pause)
                {
                    double end_time = reader.ReadDouble();
                    trial.EndTime = MotorMath.ConvertMatlabDatenumToDateTime(end_time);
                }

                //Read in the hit window duration
                trial.HitWindowDurationInSeconds = reader.ReadSingle();

                //Read in the pre-trial duration
                trial.PreTrialSamplingPeriodInSeconds = reader.ReadSingle();

                //Read in the post-trial duration
                trial.PostTrialSamplingPeriodInSeconds = reader.ReadSingle();

                //Read in the post-trial timeout
                trial.PostTrialTimeOutInSeconds = reader.ReadSingle();

                //Read in the manipulandum position
                trial.DevicePosition = reader.ReadSingle();

                //Read in the number of variable parameters that exist for this trial
                Byte N = reader.ReadByte();

                //Read in each parameter
                for (Byte i = 0; i < N; i++)
                {
                    //Read in the value
                    float variable_param_value = reader.ReadSingle();

                    //Add the value to the trial
                    string stage_parameter_name = session.SelectedStage.StageParameters.Keys.ToList()[i];
                    trial.VariableParameters[stage_parameter_name] = variable_param_value;
                }

                //Read in the number of hits that occurred during this trial
                N = reader.ReadByte();

                //Read in the timestamp for each hit
                for (Byte i = 0; i < N; i++)
                {
                    double hit_time_matlab = reader.ReadDouble();
                    DateTime hit_time = MotorMath.ConvertMatlabDatenumToDateTime(hit_time_matlab);
                    trial.HitTimes.Add(hit_time);
                }

                //Save the number of output trigger events
                N = reader.ReadByte();

                //Read in the timestamp for each output trigger
                for (Byte i = 0; i < N; i++)
                {
                    double output_trigger_timestamp_matlab = reader.ReadDouble();
                    DateTime output_trigger_timestamp = MotorMath.ConvertMatlabDatenumToDateTime(output_trigger_timestamp_matlab);
                    trial.OutputTriggers.Add(output_trigger_timestamp);
                }

                //Read in the number of samples in the signal
                UInt32 n_samples = reader.ReadUInt32();

                //Read in the signal
                trial.TrialData = new List<List<double>>();
                for (int i = 0; i < session.SelectedStage.TotalDataStreams; i++)
                {
                    //Add a new list of doubles for this stream of data
                    trial.TrialData.Add(new List<double>());

                    //Read in this stream of data
                    for (UInt32 x = 0; x < n_samples; x++)
                    {
                        float data_point = reader.ReadSingle();
                        trial.TrialData[i].Add(data_point);
                    }
                }
            }
        }
        
        private static void ReadMotoTrakFileEvent (MotoTrakSession session, BinaryReader reader)
        {
            //Get the event type
            MotoTrakFileSave.BlockType event_type = (MotoTrakFileSave.BlockType)reader.ReadInt32();

            switch (event_type)
            {
                case MotoTrakFileSave.BlockType.Trial:

                    MotorTrial new_trial = new MotorTrial();
                    ReadMotoTrakFileTrial(session, new_trial, reader);
                    session.Trials.Add(new_trial);

                    break;
                case MotoTrakFileSave.BlockType.ManualFeed:

                    double manual_feed_timestamp_matlab = reader.ReadDouble();
                    DateTime manual_feed_timestamp = MotorMath.ConvertMatlabDatenumToDateTime(manual_feed_timestamp_matlab);
                    session.ManualFeeds.Add(manual_feed_timestamp);

                    break;
                case MotoTrakFileSave.BlockType.PauseStart:

                    double pause_start_timestamp_matlab = reader.ReadDouble();
                    DateTime pause_start_timestamp = MotorMath.ConvertMatlabDatenumToDateTime(pause_start_timestamp_matlab);
                    session.Pauses.Add(new Tuple<DateTime, DateTime>(pause_start_timestamp, pause_start_timestamp));

                    break;
                case MotoTrakFileSave.BlockType.PauseFinish:

                    double pause_finish_timestamp_matlab = reader.ReadDouble();
                    DateTime pause_finish_timestamp = MotorMath.ConvertMatlabDatenumToDateTime(pause_finish_timestamp_matlab);
                    Tuple<DateTime, DateTime> last_pause = session.Pauses.LastOrDefault();
                    if (last_pause != null)
                    {
                        session.Pauses.RemoveAt(session.Pauses.Count - 1);
                        Tuple<DateTime, DateTime> new_finished_pause = new Tuple<DateTime, DateTime>(last_pause.Item1, pause_finish_timestamp);
                        session.Pauses.Add(new_finished_pause);
                    }

                    break;
                case MotoTrakFileSave.BlockType.SessionEnd:

                    double session_end_timestamp_matlab = reader.ReadDouble();
                    DateTime session_end_timestamp = MotorMath.ConvertMatlabDatenumToDateTime(session_end_timestamp_matlab);
                    session.EndTime = session_end_timestamp;

                    break;
                case MotoTrakFileSave.BlockType.TimestampedNote:

                    double note_timestamp_matlab = reader.ReadDouble();
                    DateTime note_timestamp = MotorMath.ConvertMatlabDatenumToDateTime(note_timestamp_matlab);

                    UInt16 note_length = reader.ReadUInt16();

                    char[] note_content = reader.ReadChars(note_length);
                    string note_content_string = new string(note_content);

                    session.TimestampedNotes.Add(new Tuple<DateTime, string>(note_timestamp, note_content_string));

                    break;
                case MotoTrakFileSave.BlockType.GeneralSessionNotes:

                    UInt16 session_note_length = reader.ReadUInt16();

                    char[] session_note_content = reader.ReadChars(session_note_length);
                    string session_note_content_string = new string(session_note_content);

                    session.SessionNotes = session_note_content_string;
                    
                    break;
                default:
                    break;
            }
        }

        private static MotoTrakSession ReadArdyMotorVersion2File (byte[] file_bytes)
        {
            //Create a session object which will be returned to the caller
            MotoTrakSession session = new MotoTrakSession();

            //Create an empty stage that will be assigned to this session
            MotorStage session_stage = new MotorStage();
            
            //Attach the stage to the session
            session.SelectedStage = session_stage;

            //Create an empty device that will be assigned to this session
            MotorDevice session_device = new MotorDevice();

            //Attach the device to the session
            session.Device = session_device;

            //Create a stream out of the byte array
            MemoryStream stream = new MemoryStream(file_bytes);
            BinaryReader reader = new BinaryReader(stream);

            //Read in the file version (this information should already be known)
            SByte version = (SByte)reader.ReadByte();

            //Read the old 365-day Matlab daycode from the file
            if (version == -1  || version == -3)
            {
                //For some reason the 365-day day-code was still saved to the file in 2 variants
                //of the ArdyMotor v2 files.  We should read in those bytes here, although we aren't going
                //to use this daycode at all.
                UInt16 old_daycode = BitConverter.ToUInt16(reader.ReadBytes(2), 0);
            }

            //Read in the booth number
            SByte booth_number = (SByte)reader.ReadByte();
            session.BoothLabel = booth_number.ToString();
            
            //Read in the number of characters in the rat's name.
            int N = (SByte)reader.ReadByte();

            //Read in the rat's name
            session.RatName = BitConverter.ToString(reader.ReadBytes(N), 0, N);

            //Read in the device's position
            float peg_location = reader.ReadSingle();
            session.SelectedStage.Position = new MotorStageParameter()
            {
                CurrentValue = peg_location,
                ParameterName = "Position",
                ParameterType = MotorStageParameter.StageParameterType.Fixed
            };

            //Read in the number of characters in the stage description
            N = (SByte)reader.ReadByte();

            //Read in the stage description
            string stage_description = BitConverter.ToString(reader.ReadBytes(N), 0, N);
            session.SelectedStage.Description = stage_description;

            //Read in how many characters are in the device name
            N = (SByte)reader.ReadByte();

            //Read in the name of the device
            string device_name = BitConverter.ToString(reader.ReadBytes(N), 0, N);

            //Convert the string form of a device name to a MotorDeviceType
            MotorDeviceType device_type = MotorDeviceTypeConverter.ConvertToMotorDeviceType(device_name);
            session.Device.DeviceType = device_type;

            //Read in calibration constants
            float[] calibration_constants = new float[2];
            calibration_constants[0] = reader.ReadSingle();
            calibration_constants[1] = reader.ReadSingle();

            //Read in the number of characters in the constraint description
            N = (SByte)reader.ReadByte();

            //Read in the constraint description. This is tossed.  We don't need it.
            BitConverter.ToString(reader.ReadBytes(N), 0, N);

            //Read in the number of characters in the threshold type
            N = (SByte)reader.ReadByte();

            //Read in the threshold description.  This can be used to identify what stage implementation corresponds
            //to the stage for this session.  For now, however, it gets tossed.
            BitConverter.ToString(reader.ReadBytes(N), 0, N);

            //Set the pre-trial sampling duration
            if (version == -1 || version == -3)
            {
                session.SelectedStage.PreTrialSamplingPeriodInSeconds = new MotorStageParameter()
                {
                    CurrentValue = 1.0,
                    ParameterType = MotorStageParameter.StageParameterType.Fixed
                };
            }
            else
            {
                float pre_trial_sampling_in_ms = reader.ReadSingle();
                double resulting_pre_trial_sampling_period = pre_trial_sampling_in_ms / 1000;
                session.SelectedStage.PreTrialSamplingPeriodInSeconds = new MotorStageParameter()
                {
                    CurrentValue = resulting_pre_trial_sampling_period,
                    ParameterType = MotorStageParameter.StageParameterType.Fixed
                };
            }

            //Now loop and read in trials
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {                
                //Attempt to read in data.  If any of the following fail, skip to the next loop iteration
                try
                {
                    //Instantiate a new motor trial.
                    MotorTrial trial = new MotorTrial();
                 
                    //Read in the trial number for this trial   
                    uint trial_number = reader.ReadUInt32();

                    //Read in the start time of the trial
                    double trial_start_time_matlab = reader.ReadDouble();
                    DateTime trial_start_time = MotorMath.ConvertMatlabDatenumToDateTime(trial_start_time_matlab);

                    //Read in the trial's outcome
                    byte trial_outcome = reader.ReadByte();
                    char trial_outcome_ascii = Convert.ToChar(trial_outcome);
                    if (trial_outcome_ascii == 'P')
                    {
                        //take the appropriate action for reading in a "pause" trial
                        //read in the end time of the pause
                        double end_time = reader.ReadDouble();
                    }
                    else if (trial_outcome_ascii == 'F')
                    {
                        //take the necessary actions for reading in a "manual feed" trial
                    }
                    else
                    {
                        //Set the trial start time
                        trial.StartTime = trial_start_time;

                        //Set the duration of the hit window for this trial
                        trial.HitWindowDurationInSeconds = Convert.ToDouble(reader.ReadSingle());

                        //Read in the trial initiation threshold
                        trial.VariableParameters[MotoTrak_V1_CommonParameters.InitiationThreshold] = Convert.ToDouble(reader.ReadSingle());

                        //Read in the trial hit threshold
                        trial.VariableParameters[MotoTrak_V1_CommonParameters.HitThreshold] = Convert.ToDouble(reader.ReadSingle());
                        
                        //If the file is version -4, then read in the hit threshold ceiling (I believe this is primarily for April's code)
                        if (version == -4)
                        {
                            trial.VariableParameters[MotoTrak_V1_CommonParameters.HitThresholdCeiling] = Convert.ToDouble(reader.ReadSingle());
                        }

                        //Read in the number of hits that occurred during this trial
                        N = (SByte)reader.ReadByte();

                        //Read in the timestamp for each hit that occurred
                        for (int i = 0; i < N; i++)
                        {
                            double hit_time = reader.ReadDouble();
                            trial.HitTimes.Add(MotorMath.ConvertMatlabDatenumToDateTime(hit_time));
                        }

                        //Set the result of the trial
                        if (trial.HitTimes.Count > 0)
                        {
                            trial.Result = MotorTrialResult.Hit;
                        }
                        else
                        {
                            trial.Result = MotorTrialResult.Miss;
                        }

                        //Read in the number of VNS events that occurred during this trial
                        N = (SByte)reader.ReadByte();

                        //Read in the timestamps of each VNS event that occurred during this trial
                        for (int i = 0; i < N; i++)
                        {
                            double vns_time = reader.ReadDouble();
                            trial.OutputTriggers.Add(MotorMath.ConvertMatlabDatenumToDateTime(vns_time));
                        }

                        //Read in the number of samples in this trial
                        UInt32 buffer_size = reader.ReadUInt32();

                        //Read in timestamps for each sample
                        UInt16[] sample_timestamps = new UInt16[buffer_size];
                        byte[] sample_timestamps_bytes = reader.ReadBytes(Convert.ToInt32(buffer_size * sizeof(UInt16)));
                        Buffer.BlockCopy(sample_timestamps_bytes, 0, sample_timestamps, 0, sample_timestamps_bytes.Length);
                        List<double> trial_timestamps = sample_timestamps.Select(x => Convert.ToDouble(x)).ToList();

                        //Read in the device signal values for each sample
                        float[] sample_device_values = new float[buffer_size];
                        byte[] sample_device_values_bytes = reader.ReadBytes(Convert.ToInt32(buffer_size * sizeof(float)));
                        Buffer.BlockCopy(sample_device_values_bytes, 0, sample_device_values, 0, sample_device_values_bytes.Length);
                        List<double> trial_signal = sample_device_values.Select(x => Convert.ToDouble(x)).ToList();

                        //Read in the IR signal values for each sample
                        Int16[] sample_ir_values = new Int16[buffer_size];
                        byte[] sample_ir_values_bytes = reader.ReadBytes(Convert.ToInt32(buffer_size * sizeof(Int16)));
                        Buffer.BlockCopy(sample_ir_values_bytes, 0, sample_ir_values, 0, sample_ir_values_bytes.Length);
                        List<double> ir_signal = sample_device_values.Select(x => Convert.ToDouble(x)).ToList();

                        //Subtract the pre-trial sampling duration from the sampling times
                        trial_timestamps = trial_timestamps.Select(x => 
                            x - Convert.ToInt32(session.SelectedStage.PreTrialSamplingPeriodInSeconds.CurrentValue * 1000)).ToList();

                        //Add the data streams to the trial
                        trial.TrialData.Add(trial_timestamps);
                        trial.TrialData.Add(trial_signal);
                        trial.TrialData.Add(ir_signal);
                    }

                    //Add the trial that we just read from the file to the session
                    session.Trials.Add(trial);
                }
                catch
                {
                    //If we failed to read a trial number, go back to the beginning of the loop
                    continue;
                }
            }

            if (version < -1)
            {
                if (session.Trials.Count > 0)
                {
                    session.StartTime = session.Trials[0].StartTime;
                }
            }

            //Return the session that was loaded from the file
            return session;
        }
    }
}
