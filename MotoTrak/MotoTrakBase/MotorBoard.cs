using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;

namespace MotoTrakBase
{
    /// <summary>
    /// This class handles interfacing with the Arduino board
    /// </summary>
    public class MotorBoard : NotifyPropertyChangedObject
    {
        #region Private data members

        private char[] seps = new char[4] { ' ', '\t', '\n', '\r' };
        private SerialPort _serialConnection = null;
        private const int MinimumArduinoSketchVersion = 30;

        #endregion

        #region Constructors - This is a singleton class

        private static MotorBoard _instance = null;

        /// <summary>
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
                _instance = new MotorBoard();
            }

            return _instance;
        }

        #endregion

        #region Properties

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
            SerialConnection = new SerialPort(portName, 115200);
            SerialConnection.DtrEnable = true;

            SerialConnection.Open();
            SerialConnection.ReadExisting();
            SerialConnection.Close();
            SerialConnection.Open();

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

        /// <summary>
        /// Disconnects from the arduino board
        /// </summary>
        public void DisconnectFromArduino()
        {
            if (IsSerialConnectionValid)
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
            string parameterString = parameter.ToString();
            string newCommandString = command.Replace("i", parameterString);
            SerialConnection.Write(newCommandString);
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
            string charRepresentation = System.Text.Encoding.ASCII.GetString(bytes);

            //Now replace the string "nn" in the command with the correct bytes
            string finalCommandString = newCommandString.Replace("nn", charRepresentation);

            //Now break down the full command into individual bytes and send it
            byte[] commandBytes = Encoding.ASCII.GetBytes(finalCommandString);
            SerialConnection.Write(commandBytes, 0, commandBytes.Length);
        }

        /// <summary>
        /// This function sends a message to the Arduino asking for the sketch version that is loaded onto it.
        /// The sketch version we are looking for is 111.  If the Arduino returns the value 111, then this function
        /// will return true.  Otherwise, it will return false.
        /// </summary>
        public bool IsArduinoSketchValid()
        {
            int validReturnMessage = 111;

            if (IsSerialConnectionValid)
            {
                SerialConnection.Write("A");
                string returnMessage = SerialConnection.ReadLine();
                int numberFromMessage = 0;
                bool parseSuccess = Int32.TryParse(returnMessage, out numberFromMessage);
                return (parseSuccess && (numberFromMessage == validReturnMessage));
            }

            return false;
        }

        /// <summary>
        /// Returns the version of the motor board sketch that is running on the Arduino board.
        /// </summary>
        /// <returns></returns>
        public int CheckVersion()
        {
            return this.SimpleReturn("Z", 0);
        }

        /// <summary>
        /// Returns the booth number that we are connected to
        /// </summary>
        /// <returns></returns>
        public int GetBoothNumber()
        {
            return this.SimpleReturn("BA", 1);
        }

        /// <summary>
        /// Sets the booth number on the Arduino board.
        /// </summary>
        public void SetBoothNumber(int boothNumber)
        {
            this.LongCommand("Cnn", 0, boothNumber);
        }

        /// <summary>
        /// Returns, in integer form, what kind of device is connected to the motor board at the specified 
        /// device index.
        /// </summary>
        /// <param name="deviceIndex">The device index we want to check.</param>
        /// <returns></returns>
        public int GetBoardDeviceValue()
        {
            return this.SimpleReturn("DA", 0);
        }

        public int CalGrams()
        {
            return this.SimpleReturn("PA", 1);
        }

        public int NPerCalGrams()
        {
            return this.SimpleReturn("RiAA", 1);
        }

        public int ReadDevice()
        {
            return this.SimpleReturn("MA", 1);
        }

        public int GetBaseline()
        {
            return this.SimpleReturn("NA", 0);
        }

        public void SetBaseline(int baseline)
        {
            this.LongCommand("Onn", 0, baseline);
        }

        public void SetCalGrams(int baseline)
        {
            this.LongCommand("Qnn", 0, baseline);
        }

        public void SetNPerCalGrams(int baseline)
        {
            this.LongCommand("Snn", 0, baseline);
        }

        public void TriggerFeeder()
        {
            this.SimpleCommand("WA", 1);
        }

        public void TriggerStim()
        {
            this.SimpleCommand("XA", 1);
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
            this.SimpleCommand("gi", streamingMode);
        }

        public void SetStreamingPeriod(int period)
        {
            this.LongCommand("enn", 0, period);
        }

        public int GetStreamingPeriod()
        {
            return this.SimpleReturn("f", 0);
        }

        public void SetStreamIR(int input)
        {
            this.SimpleCommand("ci", input);
        }

        public int GetStreamIR()
        {
            return this.SimpleReturn("d", 0);
        }

        public int CheckDigitialIR(int input)
        {
            return this.SimpleReturn("1i", input);
        }

        public int CheckAnalogIR(int input)
        {
            return this.SimpleReturn("2i", input);
        }

        public void Feed()
        {
            this.SimpleCommand("3A", 1);
        }

        public int GetFeedDuration()
        {
            return this.SimpleReturn("4", 0);
        }

        public void SetFeedDuration(int duration)
        {
            this.LongCommand("5nn", 0, duration);
        }

        public void Stimulate()
        {
            this.SimpleCommand("6", 0);
        }

        public int GetStimulusDuration()
        {
            return this.SimpleReturn("7", 0);
        }

        public void SetStimulusDuration(int duration)
        {
            this.LongCommand("8nn", 0, duration);
        }

        public void SetLights(int input)
        {
            this.SimpleCommand("9i", input);
        }

        public List<List<int>> ReadStream()
        {
            List<List<int>> output = new List<List<int>>();

            if (IsSerialConnectionValid)
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

            return output;
        }

        public void ClearStream()
        {
            if (IsSerialConnectionValid)
            {
                SerialConnection.ReadExisting();
            }
        }

        public void KnobToggle (int toggle_value)
        {
            this.SimpleCommand("Ei", toggle_value);
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
            //Grab the device value form the MotoTrak controller
            int device_value = this.GetBoardDeviceValue();

            //Convert the value to a device type
            MotorDeviceType device_type = MotorDevice.ConvertArdyDeviceValueToDeviceType(device_value);

            //Create a new device object for the device
            MotorDevice device = new MotorDevice(device_type, 1);

            //Return the device
            return device;
        }

        #endregion
    }
}
