using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Management;

namespace MotoTrakBase
{
    /// <summary>
    /// This class handles interfacing with the MotoTrak controller board
    /// </summary>
    public class MotorBoard : NotifyPropertyChangedObject
    {
        #region Private data members

        private char[] seps = new char[4] { ' ', '\t', '\n', '\r' };
        private SerialPort _serialConnection = null;
        private const int MinimumArduinoSketchVersion = 30;
        private double _autopositioner_offset = 48;

        #endregion

        #region Constructors - This is a singleton class

        private static MotorBoard _instance = null;
        private static Object _instance_lock = new object();

        /// <summary>
        /// Constructor
        /// </summary>
        private MotorBoard()
        {
            //constructor is private
        }

        /// <summary>
        /// Gets the one and only instance of this class that is allowed to exist.
        /// </summary>
        /// <returns>Instance of ArdyMotorBoard class</returns>
        public static MotorBoard GetInstance()
        {
            if (_instance == null)
            {
                lock (_instance_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new MotorBoard();
                    }
                }
            }

            return _instance;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The SerialPort object that maintains the serial connection to the controller board.
        /// </summary>
        public SerialPort SerialConnection
        {
            get
            {
                return _serialConnection;
            }
            private set
            {
                _serialConnection = value;
            }
        }

        /// <summary>
        /// The COM port that we are connected to
        /// </summary>
        public string ComPort
        {
            get
            {
                if (IsSerialConnectionValid)
                {
                    return SerialConnection.PortName;
                }

                return string.Empty;
            }
        }

        /// <summary>
        /// Checks the serial connection to see if it is not null and open
        /// </summary>
        public bool IsSerialConnectionValid
        {
            get
            {
                return (SerialConnection != null && SerialConnection.IsOpen);
            }
        }

        /// <summary>
        /// The offset of the autopositioner.
        /// </summary>
        public double AutopositionerOffset
        {
            get
            {
                return _autopositioner_offset;
            }
            set
            {
                _autopositioner_offset = value;
            }
        }
        
        #endregion

        #region Methods

        /// <summary>
        /// Connects the arduino board
        /// </summary>
        /// <param name="portName">Name of the port we want to connect to</param>
        /// <param name="baudRate">Baud rate of the serial connection</param>
        /// <returns>Boolean value telling us whether we were successful</returns>
        public bool ConnectToArduino(string portName)
        {
            try
            {
                SerialConnection = new SerialPort(portName, 115200);
                SerialConnection.DtrEnable = true;

                SerialConnection.Open();
                //SerialConnection.ReadExisting();
                //SerialConnection.Close();
                //SerialConnection.Open();

                bool success = false;
                string ardyResponse = SerialConnection.ReadLine().Trim();
                while (true)
                {
                    if (ardyResponse.Equals("READY"))
                    {
                        success = true;
                        break;
                    }
                    else if (SerialConnection.BytesToRead < 1)
                    {
                        break;
                    }
                    else
                    {
                        ardyResponse = SerialConnection.ReadLine().Trim();
                    }
                }

                NotifyPropertyChanged("IsSerialConnectionValid");
                NotifyPropertyChanged("ComPort");

                return success;
            }
            catch
            {
                //If any exception occurred, return false, indicating that we were
                //not able to connect to the serial port.
                return false;
            }
        }

        /// <summary>
        /// Disconnects from the arduino board
        /// </summary>
        public void DisconnectFromArduino()
        {
            if (IsSerialConnectionValid)
            {
                try
                {
                    //Disable streaming
                    EnableStreaming(0);

                    //Clear the stream
                    ClearStream();

                    //Close the serial connection
                    SerialConnection.Close();
                    NotifyPropertyChanged("IsSerialConnectionValid");
                    NotifyPropertyChanged("ComPort");
                }
                catch
                {
                    //Log any errors
                    MotoTrakMessaging.GetInstance().AddMessage("Error while attempting to disconnect from the controller board.");
                }
            }
        }

        /// <summary>
        /// This function takes a string as a command, and then some integer parameter.
        /// Wherever the character "i" is found in the command string, it will be replaced
        /// by the integer parameter.  For example, the command "gi" with a paremeter of 2 would
        /// become "g2".  This new command will then be sent to the Arduino to be executed by
        /// the Arduino.
        /// </summary>
        /// <param name="command">A command string</param>
        /// <param name="parameter">A number to be used as a replacement for all "i" characters in the command</param>
        private void SimpleCommand(string command, int parameter)
        {
            if (IsSerialConnectionValid)
            {
                string parameterString = parameter.ToString();
                string newCommandString = command.Replace("i", parameterString);
                SerialConnection.Write(newCommandString);
            }
            else
            {
                throw new Exception("Unable to execute SimpleCommand because of an invalid serial connection.");
            }
        }

        /// <summary>
        /// This function does the same thing as SimpleCommand, but then waits for a response from the Arduino.
        /// The response is parsed as an integer and returned.
        /// If for some reason there is an error parsing the response as an integer, this function will throw one
        /// of 3 exceptions:
        /// System.ArgumentNullException
        /// System.FormatException
        /// System.OverflowException
        /// </summary>
        /// <param name="command">A command</param>
        /// <param name="parameter">A parameter to be used in the command</param>
        /// <returns>An integer response from the Arduino</returns>
        private int SimpleReturn(string command, int parameter)
        {
            if (IsSerialConnectionValid)
            {
                string parameterString = parameter.ToString();
                string newCommandString = command.Replace("i", parameterString);
                SerialConnection.Write(newCommandString);

                int parsedInt = 0;
                string stringToParse = SerialConnection.ReadLine();
                bool parsedCorrectly = Int32.TryParse(stringToParse, out parsedInt);
                if (!parsedCorrectly)
                {
                    throw new InvalidOperationException();
                }

                return parsedInt;
            }
            else
            {
                throw new Exception("Unable to execute SimpleReturn in MotorBoard because of an invalid connection.");
            }
        }

        /// <summary>
        /// This function takes a command and replaces all "i" characters in the command
        /// with parameter1, and all "nn" strings in the command with parameter2.  It then
        /// sends the command to the Arduino.
        /// </summary>
        /// <param name="command">A command</param>
        /// <param name="parameter1">A parameter to use to replace "i"</param>
        /// <param name="parameter2">A parameter to use to replace "nn"</param>
        private void LongCommand(string command, int parameter1, int parameter2)
        {
            if (IsSerialConnectionValid)
            {
                //Replace any character "i" with parameter 1.  parameter 1 should be a single digit integer.
                string newCommandString = command.Replace("i", parameter1.ToString());

                //Get the bytes of the integer (since it is a 32-bit integer, it will be 4 bytes)
                byte[] bytes = BitConverter.GetBytes(parameter2);

                //We only support 16-bit integer parameters in our communication protocol, so let's take
                //the two lowest order bytes
                bytes = bytes.Take(2).ToArray();

                //Now we need to reverse the order of the two bytes that we have in order to correct
                //for endianness
                Array.Reverse(bytes);

                //Now let's convert the bytes that we have into a character string
                //string charRepresentation = System.Text.Encoding.ASCII.GetString(bytes);
                List<char> list_of_char = new List<char>();
                foreach (byte b in bytes)
                {
                    list_of_char.Add(Convert.ToChar(b));
                }
                string charRepresentation = new string(list_of_char.ToArray());

                //Now replace the string "nn" in the command with the correct bytes
                string finalCommandString = newCommandString.Replace("nn", charRepresentation);

                //Now break down the full command into individual bytes and send it
                //byte[] commandBytes = Encoding.ASCII.GetBytes(finalCommandString);
                List<byte> command_bytes = new List<byte>();
                foreach (char c in finalCommandString)
                {
                    command_bytes.Add(BitConverter.GetBytes(c)[0]);
                }

                byte[] commandBytes = command_bytes.ToArray();

                SerialConnection.Write(commandBytes, 0, commandBytes.Length);
            }
            else
            {
                throw new Exception("Unable to execute LongCommand function because of an invalid serial connection.");
            }
        }

        /// <summary>
        /// This function sends a message to the Arduino asking for the sketch version that is loaded onto it.
        /// The sketch version we are looking for is 111.  If the Arduino returns the value 111, then this function
        /// will return true.  Otherwise, it will return false.
        /// </summary>
        public bool IsArduinoSketchValid()
        {
            //Indicate what a valid return message is from the controller board
            int validReturnMessage = 111;

            if (IsSerialConnectionValid)
            {
                try
                {
                    //Attempt to retrieve the sketch id from the controller board
                    SerialConnection.Write("A");
                    string returnMessage = SerialConnection.ReadLine();
                    int numberFromMessage = 0;
                    bool parseSuccess = Int32.TryParse(returnMessage, out numberFromMessage);

                    //Return whether or not the message returned from the controller board matches
                    //a valid sketch.
                    return (parseSuccess && (numberFromMessage == validReturnMessage));
                }
                catch
                {
                    //If some exception occurs during the communication and parsing process,
                    //return a false value indicating that the sketch is not valid.
                    return false;
                }
            }

            //Return false if no valid serial connection exists
            return false;
        }

        /// <summary>
        /// Returns the version of the motor board sketch that is running on the Arduino board.
        /// </summary>
        /// <returns></returns>
        public int CheckVersion()
        {
            try
            {
                //Retrieve the sketch version from the controller board
                int version = this.SimpleReturn("Z", 0);
                return version;
            }
            catch
            {
                //Return a version of 0 if an error was encountered, and log the error with the MotoTrakMessaging system.
                MotoTrakMessaging.GetInstance().AddMessage("Unable to identify controller board version.");
                return 0;
            }
        }

        /// <summary>
        /// Indicates whether there are characters that need to be read on the serial line
        /// </summary>
        /// <returns>True or false</returns>
        public bool SerialConnectionHasCharactersToRead ()
        {
            if (IsSerialConnectionValid)
            {
                return SerialConnection.BytesToRead > 0;
            }

            return false;
        }

        /// <summary>
        /// Returns the name or the label of the booth that we are connected to.
        /// In the event that the connection is not valid, it returns the string "Unknown booth".
        /// </summary>
        /// <returns>The booth name as described in the function summary</returns>
        public string GetBoothLabel()
        {
            try
            {
                int booth_number = this.SimpleReturn("BA", 1);
                string booth_label = booth_number.ToString();
                return booth_label;
            }
            catch
            {
                return "Unknown booth";
            }
        }

        /// <summary>
        /// Sets the booth number on the Arduino board.
        /// </summary>
        public void SetBoothNumber(int boothNumber)
        {
            try
            {
                this.LongCommand("Cnn", 0, boothNumber);
            }
            catch
            {
                //If an error occurred, log the error within the MotoTrakMessaging system.
                MotoTrakMessaging.GetInstance().AddMessage("Unable to set booth number on the controller board.");
            }
        }

        /// <summary>
        /// Returns, in integer form, what kind of device is connected to the motor board at the specified 
        /// device index.
        /// </summary>
        /// <param name="deviceIndex">The device index we want to check.</param>
        /// <returns></returns>
        public int GetBoardDeviceValue()
        {
            try
            {
                int device_value = this.SimpleReturn("DA", 0);
                return device_value;
            }
            catch
            {
                //In the event of an error, return 0 and also log the error with the MotoTrakMessaging system.
                MotoTrakMessaging.GetInstance().AddMessage("Unable to retrieve device identification value from controller board.");
                return 0;
            }
        }

        /// <summary>
        /// This function returns the numerator of the slope calibration coefficient.
        /// </summary>
        /// <returns>CalGrams numerator</returns>
        public int CalGrams()
        {
            try
            {
                int cal_grams = this.SimpleReturn("PA", 1);
                return cal_grams;
            }
            catch
            {
                //In the event of an error, return 0 and also log the error with the MotoTrakMessaging system.
                MotoTrakMessaging.GetInstance().AddMessage("Unable to retrieve calibration value (CalGrams function) from controller board.");
                return 0;
            }
        }

        /// <summary>
        /// This function returns the denominator of the slope calibration coefficient.
        /// </summary>
        /// <returns>NPerCalGrams denominator</returns>
        public int NPerCalGrams()
        {
            try
            {
                int n_per_cal_grams = this.SimpleReturn("RiAA", 1);
                return n_per_cal_grams;
            }
            catch
            {
                //In the event of an error, return 0 and also log the error with the MotoTrakMessaging system.
                MotoTrakMessaging.GetInstance().AddMessage("Unable to retrieve calibration value (NPerCalGrams function) from controller board.");
                return 0;
            }
        }

        /// <summary>
        /// Reads the most recent analog value from the device that is connected to the MotoTrak controller board.
        /// </summary>
        /// <returns>Analog value from the connected MotoTrak device</returns>
        public int ReadDevice()
        {
            try
            {
                int device_val = this.SimpleReturn("MA", 1);
                return device_val;
            }
            catch
            {
                //In the event of an error, return 0 and also log the error with the MotoTrakMessaging system.
                MotoTrakMessaging.GetInstance().AddMessage("Unable to read value from MotoTrak device.");
                return 0;
            }
        }

        /// <summary>
        /// Reads the baseline calibration constant for the MotoTrak device that is connected to the controller board.
        /// </summary>
        /// <returns>The baseline calibration constant</returns>
        public int GetBaseline()
        {
            try
            {
                int baseline = this.SimpleReturn("NA", 0);
                return baseline;
            }
            catch
            {
                //In the event of an error in the SimpleReturn method, we will return a baseline of 0 and
                //we will log the error using the MotoTrakMessaging system
                MotoTrakMessaging.GetInstance().AddMessage("Unable to retrieve device baseline value.");
                return 0;
            }
        }

        /// <summary>
        /// Sets the baseline calibration constant for the connected MotoTrak device.
        /// </summary>
        /// <param name="baseline">The new baseline value</param>
        public void SetBaseline(int baseline)
        {
            try
            {
                this.LongCommand("Onn", 0, baseline);
            }
            catch
            {
                //In the event of an error while executing the LongCommand method, we will log the error
                //using the MotoTrakMessaging system
                MotoTrakMessaging.GetInstance().AddMessage("Unable to set baseline of device on the controller board.");
            }
        }

        /// <summary>
        /// Sets the CalGrams (numerator) calibration coefficient.
        /// </summary>
        /// <param name="cal_grams">cal grams coefficient</param>
        public void SetCalGrams(int cal_grams)
        {
            try
            {
                this.LongCommand("Qnn", 0, cal_grams);
            }
            catch
            {
                //In the event of an error, log the error with the MotoTrakMessaging system.
                MotoTrakMessaging.GetInstance().AddMessage("Unable to set CalGrams calibration coefficient.");
            }
        }

        /// <summary>
        /// Sets the NPerCalGrams (denominator) calibration coefficient.
        /// </summary>
        /// <param name="n_per_cal_grams">n_per_cal_grams denominator coefficient</param>
        public void SetNPerCalGrams(int n_per_cal_grams)
        {
            try
            {
                this.LongCommand("Snn", 0, n_per_cal_grams);
            }
            catch
            {
                //In the event of an error, log the error with the MotoTrakMessagingSystem.
                MotoTrakMessaging.GetInstance().AddMessage("Unable to set the NPerCalGrams calibration coefficient.");
            }
        }

        /// <summary>
        /// Triggers the feeder
        /// </summary>
        public void TriggerFeeder()
        {
            try
            {
                this.SimpleCommand("WA", 1);
            }
            catch
            {
                //In the event of an error, log the error with the MotoTrakMessagingSystem.
                MotoTrakMessaging.GetInstance().AddMessage("Unable to trigger the feeder!");
            }
        }

        /// <summary>
        /// Fires an output trigger (typically a VNS trigger in many labs).
        /// </summary>
        public void TriggerStim()
        {
            try
            {
                this.SimpleCommand("XA", 1);
            }
            catch
            {
                //In the event of an error, log the error with the MotoTrakMessagingSystem.
                MotoTrakMessaging.GetInstance().AddMessage("Unable to send output trigger!");
            }
        }

        /// <summary>
        /// Sends an autopositioner command to the MotoTrak controller board
        /// </summary>
        /// <param name="pos">The position to which the autopositioner should move the device</param>
        public void Autopositioner ( double pos )
        {
            int position = Convert.ToInt32(Math.Round(pos));
            this.LongCommand("0nn", 0, position);
        }

        /// <summary>
        /// This function enables streaming on the Arduino.  The streaming mode can be 0, 1, or 2.
        /// 0 = disabled
        /// 1 = periodic
        /// 2 = on an event
        /// </summary>
        /// <param name="streamingMode"></param>
        public void EnableStreaming(int streamingMode)
        {
            try
            {
                this.SimpleCommand("gi", streamingMode);
            }
            catch
            {
                //If an error occurred, we must assume streaming was not successfully enabled.
                //Let's log the error with the MotoTrakMessaging system.
                MotoTrakMessaging.GetInstance().AddMessage("Streaming was not successfully enabled!");
            }
        }

        /// <summary>
        /// Sets the stream period for MotoTrak streaming.  This indicates how often (in units of ms) a new sample
        /// will arrive.  The standard period is 10, so that new samples occur each 10 ms, which is a sampling
        /// rate of 100 Hz.
        /// </summary>
        /// <param name="period">The new sampling period in units of ms</param>
        public void SetStreamingPeriod(int period)
        {
            try
            {
                this.LongCommand("enn", 0, period);
            }
            catch
            {
                //Log any errors to the MotoTrakMessaging system.
                MotoTrakMessaging.GetInstance().AddMessage("Unable to successfully set the streaming period.");
            }
        }

        /// <summary>
        /// Return the current streaming period, in units of ms.
        /// This represents how much time elapses per sample.
        /// </summary>
        /// <returns>The amount of time that elapses per sample, in units of ms.</returns>
        public int GetStreamingPeriod()
        {
            try
            {
                return this.SimpleReturn("f", 0);
            }
            catch
            {
                //In the event of an error, return 0 and also log the error with the MotoTrakMessaging system.
                MotoTrakMessaging.GetInstance().AddMessage("Unable to get streaming period from the MotoTrak controller board.");
                return 0;
            }
        }
        
        /// <summary>
        /// This method is deprecated and should not be used.
        /// Some older version of MotoTrak used this command to trigger the feeder.
        /// All known versions of MotoTrak that are currently in use are using the
        /// TriggerFeeder command which uses a different command over the serial line.
        /// </summary>
        [Obsolete("Feed is deprecated. Please use TriggerFeeder instead.")]
        public void Feed()
        {
            try
            {
                this.SimpleCommand("3A", 1);
            }
            catch
            {
                //Log any error with the MotoTrakMessaging system.
                MotoTrakMessaging.GetInstance().AddMessage("Error while calling the Feed method on MotorBoard class.");
            }
        }

        /// <summary>
        /// Returns the length of the signal that is sent to the feeder when a feeder command is issued.
        /// </summary>
        /// <returns>The length of the feed signal that is sent to the feeder.</returns>
        public int GetFeedDuration()
        {
            try
            {
                int feed_dur = this.SimpleReturn("4", 0);
                return feed_dur;
            }
            catch
            {
                //Log an error message and return 0
                MotoTrakMessaging.GetInstance().AddMessage("Unable to retrieve feed duration");
                return 0;
            }
        }

        /// <summary>
        /// Sets the duration of the feed signal that is sent to the feeder when a feed command is issued.
        /// </summary>
        /// <param name="duration">The duration of the feed signal that is sent to the feeder.</param>
        public void SetFeedDuration(int duration)
        {
            try
            {
                this.LongCommand("5nn", 0, duration);
            }
            catch
            {
                MotoTrakMessaging.GetInstance().AddMessage("Unable to set the feed duration on the controller board.");
            }
        }

        /// <summary>
        /// This method is deprecated and should not be used.
        /// Some older versions of MotoTrak used this command to trigger the A-M Systems stimulator.
        /// All known versions of MotoTrak that are currently in use are using the TriggerStim
        /// command which uses a different command over the serial line.
        /// </summary>
        [Obsolete("Stimulate is deprecated. Please use TriggerStim instead.")]
        public void Stimulate()
        {
            try
            {
                this.SimpleCommand("6", 0);
            }
            catch
            {
                //Log any error with the MotoTrakMessaging system.
                MotoTrakMessaging.GetInstance().AddMessage("Error while calling the Stimulate method on MotorBoard class.");
            }
        }

        /// <summary>
        /// Retrieves the duration of the stimulus trigger sent to the A-M Systems stimulator.
        /// </summary>
        /// <returns>Duration of the stim trigger.</returns>
        public int GetStimulusDuration()
        {
            try
            {
                int stim_dur = this.SimpleReturn("7", 0);
                return stim_dur;
            }
            catch
            {
                //Log the error and return 0
                MotoTrakMessaging.GetInstance().AddMessage("Unable to retrieve output trigger duration from controller board.");
                return 0;
            }
        }

        /// <summary>
        /// Sets the duration of the stimulus trigger that gets sent to the A-M Systems stimulator for VNS projects
        /// This is also the duration of an output trigger for other non-VNS projects.
        /// </summary>
        /// <param name="duration">Duration (in units of ms) of the output trigger</param>
        public void SetStimulusDuration(int duration)
        {
            try
            {
                this.LongCommand("8nn", 0, duration);
            }
            catch
            {
                //Log any errors
                MotoTrakMessaging.GetInstance().AddMessage("Unable to set the output trigger duration on the MotoTrak controller board.");
            }
        }
        
        /// <summary>
        /// Enables or disables the cage lights in the MotoTrak booth.
        /// </summary>
        /// <param name="input">1 to turn them on, 0 to turn them off.</param>
        public void SetLights(int input)
        {
            try
            {
                this.SimpleCommand("9i", input);
            }
            catch
            {
                //Log any errors
                MotoTrakMessaging.GetInstance().AddMessage("Unable to toggle the cage lights.");
            }
        }

        /// <summary>
        /// Reads the streaming data from the controller board.
        /// The data is returned in the following format.
        /// Assuming you have 3 data streams called A, B, and C, the returned result looks like:
        /// [ [a1 b1 c1] [a2 b2 c2] [a3 b3 c3] ... [a_n b_n c_n] ]
        /// </summary>
        /// <returns>The streaming data as described in the function summary</returns>
        public List<List<int>> ReadStream()
        {
            List<List<int>> output = new List<List<int>>();

            if (IsSerialConnectionValid)
            {
                try
                {
                    while (SerialConnection.BytesToRead > 0)
                    {
                        string lineOfData = _serialConnection.ReadLine();
                        if (!string.IsNullOrEmpty(lineOfData))
                        {
                            string[] splitString = lineOfData.Split(seps);
                            if (splitString.Length == 3)
                            {
                                List<int> integerData = new List<int>();

                                int parsedNumber = 0;
                                for (int i = 0; i < 3; i++)
                                {
                                    Int32.TryParse(splitString[i], out parsedNumber);
                                    integerData.Add(parsedNumber);
                                }

                                output.Add(integerData);
                            }
                        }
                    }
                }
                catch
                {
                    //Log the error
                    MotoTrakMessaging.GetInstance().AddMessage("Error while attempting to read streaming data from controller board.");
                }
            }

            return output;
        }

        /// <summary>
        /// Flushes the stream of the MotoTrak controller board to clear any existing data.
        /// </summary>
        public void ClearStream()
        {
            if (IsSerialConnectionValid)
            {
                try
                {
                    SerialConnection.ReadExisting();
                }
                catch
                {
                    MotoTrakMessaging.GetInstance().AddMessage("Unable to clear the stream of the MotoTrak controller board.");
                }
            }
        }

        /// <summary>
        /// Toggles the knob device flag on the controller board.
        /// </summary>
        /// <param name="toggle_value">Toggle or not</param>
        public void KnobToggle (int toggle_value)
        {
            try
            {
                this.SimpleCommand("Ei", toggle_value);
            }
            catch
            {
                //Log any errors
                MotoTrakMessaging.GetInstance().AddMessage("Unable to toggle knob device on the controller board.");
            }
        }

        #endregion

        #region V2 Methods

        /// <summary>
        /// Queries the MotoTrak controller board version and returns a true/false value indicating whether the board is supported
        /// by this version of MotoTrak or not.
        /// </summary>
        /// <returns>Boolean value indicating whether the connected MotoTrak board is supported.</returns>
        public bool DoesSketchMeetMinimumRequirements ()
        {
            bool valid = false;

            if (IsSerialConnectionValid)
            {
                //The CheckVersion function should handle all exceptions that occur at the board communication level, and it will
                //log an error message if something occurs.  Here we just need to check and make sure the version is returned is
                //at least the minimum version required.
                int version = this.CheckVersion();
                valid = (version >= MinimumArduinoSketchVersion);
            }

            return valid;
        }

        /// <summary>
        /// Queries the MotoTrak controller for the device that is connected to it.  This version of MotoTrak supports only one device
        /// being connected to a controller.
        /// </summary>
        /// <returns>A MotoDevice object that reperesents the device connected to the MotoTrak controller.</returns>
        public MotorDevice GetMotorDevice ()
        {
            try
            {
                //Grab the device value form the MotoTrak controller
                int device_value = this.GetBoardDeviceValue();

                //Convert the value to a device type
                MotorDeviceType device_type = MotorDevice.ConvertAnalogDeviceValueToDeviceType(device_value);

                //Create a new device object for the device
                MotorDevice device = new MotorDevice(device_type, 1);

                //Return the device
                return device;
            }
            catch
            {
                //In the event that a serial connection to the controller board is invalid, the GetBoardDeviceValue function
                //may throw an exception. In this case, we will return a device with an unknown type.
                return new MotorDevice();
            }
        }

        #endregion

        #region Static methods

        public static List<USBDeviceInfo> QueryConnectedArduinoDevices ()
        {
            //Create a list to hold information from USB devices
            List<USBDeviceInfo> devices = new List<USBDeviceInfo>();

            //Query all connected devices
            var searcher = new ManagementObjectSearcher(@"SELECT * FROM WIN32_SerialPort");
            var collection = searcher.Get();
            
            //Grab the information we need
            foreach (var device in collection)
            {
                string id = (string)device.GetPropertyValue("DeviceID");
                string desc = (string)device.GetPropertyValue("Description");
                USBDeviceInfo d = new USBDeviceInfo(desc, id);
                
                //Check to see if the available serial port is a connected Arduino device
                if (d.Description.Contains("Arduino"))
                {
                    //If so, add the Arduino to our list of devices
                    devices.Add(d);
                }
            }
            
            //Dispose of the collection of queried devices
            collection.Dispose();
            
            //Return the list of devices that were found
            return devices;
        }

        #endregion
    }
}
