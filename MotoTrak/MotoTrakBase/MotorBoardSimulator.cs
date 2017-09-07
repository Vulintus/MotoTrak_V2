using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakBase
{
    /// <summary>
    /// A class that simulates a motor board connection
    /// </summary>
    public class MotorBoardSimulator : NotifyPropertyChangedObject, IMotorBoard
    {
        #region Singleton

        private static MotorBoardSimulator _instance;
        private static object _instance_lock = new object();

        private MotorBoardSimulator()
        {
            //empty
        }

        public static MotorBoardSimulator GetInstance()
        {
            if (_instance == null)
            {
                lock (_instance_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new MotorBoardSimulator();
                    }
                }
            }

            return _instance;
        }

        #endregion

        private int _streaming_mode = 0;

        public double AutopositionerOffset
        {
            get
            {
                return 0;
            }
            set
            {
                //empty
            }
        }

        public string ComPort
        {
            get
            {
                return "SIMULATED";
            }
        }

        public bool IsSerialConnectionValid
        {
            get
            {
                return true;
            }
        }

        public SerialPort SerialConnection
        {
            get
            {
                return null;
            }
        }

        public void Autopositioner(double pos)
        {
            //empty
        }

        public int CalGrams()
        {
            return 32767;
        }

        public int CheckVersion()
        {
            return 30;
        }

        public void ClearStream()
        {
            //empty
        }

        public bool ConnectToArduino(string portName)
        {
            return true;
        }

        public void DisconnectFromArduino()
        {
            //empty
        }

        public bool DoesSketchMeetMinimumRequirements()
        {
            return true;
        }

        public void EnableStreaming(int streamingMode)
        {
            _streaming_mode = streamingMode;
        }

        public void Feed()
        {
            //empty
        }

        public int GetBaseline()
        {
            return 0;
        }

        public int GetBoardDeviceValue()
        {
            return 550;
        }

        public string GetBoothLabel()
        {
            return "SIMULATED";
        }

        public int GetFeedDuration()
        {
            return 10;
        }

        public MotorDevice GetMotorDevice()
        {
            MotorDevice dev = new MotorDevice();
            dev.Baseline = 0;
            dev.Slope = 1;
            dev.DeviceType = MotorDeviceType.Pull;

            return dev;
        }

        public int GetStimulusDuration()
        {
            return 10;
        }

        public int GetStreamingPeriod()
        {
            return 10;
        }

        public bool IsArduinoSketchValid()
        {
            return true;
        }

        public void KnobToggle(int toggle_value)
        {
            //empty
        }

        public int NPerCalGrams()
        {
            return 32767;
        }

        public int ReadDevice()
        {
            return 0;
        }

        public List<List<Int64>> ReadStream()
        {
            Random gen = new Random(DateTime.Now.Millisecond);
            List<List<Int64>> result = new List<List<Int64>>();
            result.Add(new List<Int64>() { 0, Convert.ToInt64(gen.Next(0, 10)), 0 });
            result.Add(new List<Int64>() { 0, Convert.ToInt64(gen.Next(0, 10)), 0 });
            result.Add(new List<Int64>() { 0, Convert.ToInt64(gen.Next(0, 10)), 0 });

            return result;
        }

        public bool SerialConnectionHasCharactersToRead()
        {
            return false;
        }

        public void SetBaseline(int baseline)
        {
            //empty
        }

        public void SetBoothNumber(int boothNumber)
        {
            //empty
        }

        public void SetCalGrams(int cal_grams)
        {
            //empty
        }

        public void SetFeedDuration(int duration)
        {
            //empty
        }

        public void SetLights(int input)
        {
            //empty
        }

        public void SetNPerCalGrams(int n_per_cal_grams)
        {
            //empty
        }

        public void SetStimulusDuration(int duration)
        {
            //empty
        }

        public void SetStreamingPeriod(int period)
        {
            //empty
        }

        public void Stimulate()
        {
            //empty
        }

        public void TriggerFeeder()
        {
            //empty
        }

        public void TriggerStim()
        {
            //empty
        }
    }
}
