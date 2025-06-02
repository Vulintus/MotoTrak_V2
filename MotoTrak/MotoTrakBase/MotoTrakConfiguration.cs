using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakBase
{
    /// <summary>
    /// Singleton class that handles reading the MotoTrak configuration file and storing the variables that were loaded from the file.
    /// </summary>
    public class MotoTrakConfiguration
    {
        #region Constructors - This is a singleton class

        private static MotoTrakConfiguration _instance = null;
        
        /// <summary>
        /// </summary>
        private MotoTrakConfiguration()
        {
            //constructor is private
        }

        /// <summary>
        /// Gets the one and only instance of this class that is allowed to exist.
        /// </summary>
        /// <returns>Instance of MotoTrakConfiguration class</returns>
        public static MotoTrakConfiguration GetInstance()
        {
            if (_instance == null)
            {
                _instance = new MotoTrakConfiguration();
            }

            return _instance;
        }

        #endregion

        #region Private data members

        private string BoothPairingsFileName = "mototrak_booth_pairings.config";
        private string ConfigurationFileName = "mototrak.config";
        private string StageImplementationsPath = "StageImplementations";
        private string DefaultLocalStagePath = "Stages";
        private string CompanyName = "Vulintus";
        private string MotoTrakAppName = "MotoTrak";

        #endregion

        #region Properties

        public ConcurrentDictionary<string, IMotorTaskImplementation> PythonStageImplementations = new ConcurrentDictionary<string, IMotorTaskImplementation>();
        public ConcurrentBag<MotoTrakBoothPairing> BoothPairings = new ConcurrentBag<MotoTrakBoothPairing>();
        
        public int ConfigurationVersion { get; set; }
        public string VariantName { get; set; }
        public string StageWebPath { get; set; }
        public string StageLocalPath { get; set; }
        public string DataPath { get; set; }
        public string SecondaryDataPath { get; set; }
        public bool DebuggingMode = false;
        public double TimeLimitInMinutes = Double.NaN;

        public string PreSpecifiedComPort = string.Empty;

        #endregion

        #region Private methods

        public string GetLocalApplicationDataFolder ()
        {
            var path_name = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            path_name = path_name + @"\" + CompanyName + @"\" + MotoTrakAppName + @"\";

            //Create the path if necessary
            DirectoryInfo dir_info = new DirectoryInfo(path_name);
            if (!dir_info.Exists)
            {
                dir_info.Create();
            }

            //Return the path to the caller
            return path_name;
        }
        
        #endregion

        #region Methods

        /// <summary>
        /// Saves the booth pairings to a file
        /// </summary>
        public void SaveBoothPairings ()
        {
            try
            {
                string booth_pairings_file_name = GetLocalApplicationDataFolder() + BoothPairingsFileName;

                //Open a stream to write to the file
                StreamWriter writer = new StreamWriter(booth_pairings_file_name, false);

                //Write each booth pairing to the file
                foreach (var pairing in BoothPairings)
                {
                    string device_type_string = MotorDeviceTypeConverter.ConvertToDescription(pairing.DeviceConnected);
                    string last_updated_string = pairing.LastUpdated.ToString();
                    string output_string = pairing.BoothLabel + ", " + pairing.ComPort + ", " + last_updated_string + ", " + device_type_string;
                    writer.WriteLine(output_string);
                }

                //Close the file handle
                writer.Close();
            }
            catch
            {
                ErrorLoggingService.GetInstance().LogStringError("Unable to save booth pairings!");
            }
        }

        /// <summary>
        /// Updates an existing booth pairing in our collection of booth pairings, OR creates a new booth pairing
        /// if it doesn't already exist.
        /// </summary>
        /// <param name="com_port">The com port is used as the key to the booth pairings.  It is how we find an existing booth pairing, or determine whether to create a new one.</param>
        /// <param name="booth_label">The booth label to update for the com port.</param>
        /// <param name="device_type">The device currently connected to the booth.</param>
        public void UpdateBoothPairing (string com_port, string booth_label, MotorDeviceType device_type)
        {
            var pairing = BoothPairings.Where(x => x.ComPort.Equals(com_port)).FirstOrDefault();
            if (pairing != null)
            {
                //If the booth pairing already exists in our collection, let's update it.
                pairing.BoothLabel = booth_label;
                pairing.DeviceConnected = device_type;
                pairing.LastUpdated = DateTime.Now;
            }
            else
            {
                //Otherwise, create a new booth pairing to be stored.
                MotoTrakBoothPairing new_pairing = new MotoTrakBoothPairing(booth_label, com_port, device_type, DateTime.Now);
                BoothPairings.Add(new_pairing);
            }
        }

        /// <summary>
        /// Reads the booth pairings file
        /// </summary>
        public void ReadBoothPairings ()
        {
            string booth_pairings_file_name = GetLocalApplicationDataFolder() + BoothPairingsFileName;

            FileInfo booth_pairings_file_info = new FileInfo(booth_pairings_file_name);
            if (booth_pairings_file_info.Exists)
            {
                //Open a stream to read the booth pairings configuration file
                try
                {
                    //Load the configuration file
                    List<string> lines = MotoTrakUtilities.ConfigurationFileLoader.LoadConfigurationFile(booth_pairings_file_name);
                    
                    //Now parse the input
                    for (int i = 0; i < lines.Count; i++)
                    {
                        string thisLine = lines[i];
                        string[] splitString = thisLine.Split(new char[] { ',' }, 4);

                        string booth_name = splitString[0].Trim();
                        string com_port = splitString[1].Trim();
                        DateTime last_updated = DateTime.MinValue;
                        MotorDeviceType device_type = MotorDeviceType.Unknown;

                        //Parse out the "last updated" date and time if it exists
                        if (splitString.Length >= 3)
                        {
                            string last_updated_string = splitString[2].Trim();
                            last_updated = DateTime.Parse(last_updated_string);
                        }

                        //Parse out the connected device if it exists
                        if (splitString.Length >= 4)
                        {
                            string device_type_string = splitString[3].Trim();
                            device_type = MotorDeviceTypeConverter.ConvertToMotorDeviceType(device_type_string);
                        }

                        //Instantiate a booth pairing object
                        MotoTrakBoothPairing pairing = new MotoTrakBoothPairing(booth_name, com_port, device_type, last_updated);

                        //Add the booth pairing to our set
                        BoothPairings.Add(pairing);
                    }
                }
                catch
                {
                    ErrorLoggingService.GetInstance().LogStringError("Unable to read booth pairings file!");
                }
            }
        }

        /// <summary>
        /// Reads the MotoTrak configuration file and populates properties of this class accordingly.
        /// </summary>
        public void ReadConfigurationFile ()
        {
            string configuration_file_name = GetLocalApplicationDataFolder() + ConfigurationFileName;
            
            try
            {
                bool isConfigVersionSet = false;

                //Check to see if the configuration file exists
                FileInfo config_file_info = new FileInfo(configuration_file_name);

                //Generate a default configuration file if one does not exist
                if (!config_file_info.Exists)
                {
                    GenerateDefaultConfigurationFile();
                }

                //Read the configuration file
                List<string> lines = MotoTrakUtilities.ConfigurationFileLoader.LoadConfigurationFile(configuration_file_name);
                
                //Now parse the input
                for (int i = 0; i < lines.Count; i++)
                {
                    string thisLine = lines[i];
                    string[] splitString = thisLine.Split(new char[] { ':' }, 2);

                    string key = splitString[0].Trim();
                    string value = splitString[1].Trim();

                    if (key.Equals("VARIANT", StringComparison.InvariantCultureIgnoreCase))
                    {
                        VariantName = value;
                    }
                    else if (key.Equals("CONFIGURATION VERSION", StringComparison.InvariantCultureIgnoreCase))
                    {
                        ConfigurationVersion = Int32.Parse(value);
                        isConfigVersionSet = true;
                    }
                    else if (key.Equals("STAGE URL", StringComparison.InvariantCultureIgnoreCase))
                    {
                        StageWebPath = value;
                    }
                    else if (key.Equals("STAGE FOLDER"))
                    {
                        StageLocalPath = value;
                    }
                    else if (key.Equals("MAIN DATA LOCATION", StringComparison.InvariantCultureIgnoreCase))
                    {
                        DataPath = value;
                    }
                    else if (key.Equals("SECONDARY DATA LOCATION", StringComparison.InvariantCultureIgnoreCase))
                    {
                        SecondaryDataPath = value;
                    }
                    else if (key.Equals("DEBUG"))
                    {
                        if (value.Equals("True", StringComparison.OrdinalIgnoreCase))
                        {
                            DebuggingMode = true;
                        }
                        else
                        {
                            DebuggingMode = false;
                        }
                    }
                    else if (key.Equals("COM"))
                    {
                        PreSpecifiedComPort = value;
                    }
                    else if (key.Equals("TIME LIMIT"))
                    {
                        double time_limit = double.NaN;
                        bool success = Double.TryParse(value, out time_limit);
                        if (success)
                        {
                            TimeLimitInMinutes = time_limit;
                        }
                    }
                }

                if (!isConfigVersionSet)
                {
                    ConfigurationVersion = 1;
                }
            }
            catch
            {
                MotoTrakMessaging m = MotoTrakMessaging.GetInstance();
                m.AddMessage("Unable to read MotoTrak configuration file!");
            }
        }

        /// <summary>
        /// Generates a default configuration file if one does not exist
        /// </summary>
        public void GenerateDefaultConfigurationFile ()
        {
            string configuration_file_name = GetLocalApplicationDataFolder() + ConfigurationFileName;

            try
            {
                StreamWriter writer = new StreamWriter(configuration_file_name);

                writer.WriteLine("CONFIGURATION VERSION: 1");
                writer.WriteLine("STAGE FOLDER: " + DefaultLocalStagePath);
                writer.WriteLine(@"MAIN DATA LOCATION: C:\MotoTrak Files\");

                writer.Close();
            }
            catch (Exception e)
            {
                ErrorLoggingService.GetInstance().LogExceptionError(e);
            }
        }

        /// <summary>
        /// This method loads in all stage implementations found in the standard folder containing stage implementations.
        /// </summary>
        public void InitializeStageImplementations ()
        {
            //First, find all Python files in the stage implementations folder
            List<string> files = new List<string>();

            try
            {
                //Get all files in the stage implementations folder
                files = Directory.GetFiles(StageImplementationsPath, "*.py").ToList();

                //Load in each file
                foreach (string f in files)
                {
                    PythonTaskImplementation new_stage_implementation = new PythonTaskImplementation(f);
                    string file_name_only = Path.GetFileName(f);
                    PythonStageImplementations[file_name_only] = new_stage_implementation;
                }
            }
            catch (Exception e)
            {
                ErrorLoggingService.GetInstance().LogExceptionError(e);
                ErrorLoggingService.GetInstance().LogStringError("Error while attempting to load Python stage implementations!");
                MotoTrakMessaging.GetInstance().AddMessage("Error while attempting to load Python stage implementations!");
            }
        }

        #endregion
    }
}
