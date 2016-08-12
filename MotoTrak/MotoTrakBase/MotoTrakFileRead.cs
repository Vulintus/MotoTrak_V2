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
        /// This function reads a MotoTrak session.
        /// NO EFFORT has been made to make this function compatible with ArdyMotor version 1.0 files.
        /// However, all ArdyMotor version 2.0 files should be compatible with this function.
        /// </summary>
        /// <param name="fully_qualified_path">The path of the file (including the file name)</param>
        public static void ReadFile (string fully_qualified_path)
        {
            try
            {
                //Open the file for reading
                byte[] file_bytes = System.IO.File.ReadAllBytes(fully_qualified_path);

                //Determine the file version
                SByte version = (sbyte)file_bytes[0];
                if (version < 0)
                {
                    ReadArdyMotorVersion2File(file_bytes);
                }
            }
            catch
            {
                //Inform the user that messaging data could not be loaded
                MotoTrakMessaging.GetInstance().AddMessage("Could not load session data!");
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
