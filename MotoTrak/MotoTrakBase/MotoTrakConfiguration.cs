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

        public ConcurrentDictionary<string, IMotorStageImplementation> PythonStageImplementations = new ConcurrentDictionary<string, IMotorStageImplementation>();
        public ConcurrentDictionary<string, string> BoothPairings = new ConcurrentDictionary<string, string>();

        public int ConfigurationVersion { get; set; }
        public string VariantName { get; set; }
        public string StageWebPath { get; set; }
        public string StageLocalPath { get; set; }
        public string DataPath { get; set; }
        public string SecondaryDataPath { get; set; }
        public bool DebuggingMode = false;

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
                StreamWriter writer = new StreamWriter(booth_pairings_file_name);

                //Write each booth pairing to the file
                foreach (var kvp in BoothPairings)
                {
                    if (!string.IsNullOrEmpty(kvp.Value))
                    {
                        writer.WriteLine(kvp.Value + ", " + kvp.Key);
                    }
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
                    StreamReader reader = new StreamReader(booth_pairings_file_name);

                    //Read all the lines from the file
                    List<string> lines = new List<string>();
                    while (!reader.EndOfStream)
                    {
                        lines.Add(reader.ReadLine());
                    }

                    //Close the stream
                    reader.Close();

                    //Now parse the input
                    for (int i = 0; i < lines.Count; i++)
                    {
                        string thisLine = lines[i];
                        string[] splitString = thisLine.Split(new char[] { ',' }, 2);

                        string booth_name = splitString[0].Trim();
                        string com_port = splitString[1].Trim();

                        //Add the booth pairing to our dictionary
                        BoothPairings.TryAdd(com_port, booth_name);
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

                //Open the configuration file using a stream reader.
                StreamReader reader = new StreamReader(configuration_file_name);

                //Read all the lines from the configuration file, and then close the file.
                List<string> lines = new List<string>();
                while (!reader.EndOfStream)
                {
                    string newLine = reader.ReadLine();
                    lines.Add(newLine);
                }

                reader.Close();

                //Now parse the input
                for (int i = 0; i < lines.Count; i++)
                {
                    string thisLine = lines[i];
                    string[] splitString = thisLine.Split(new char[] { ':' }, 2);

                    string key = splitString[0].Trim();
                    string value = splitString[1].Trim();

                    if (key.Equals("VARIANT"))
                    {
                        VariantName = value;
                    }
                    else if (key.Equals("CONFIGURATION VERSION"))
                    {
                        ConfigurationVersion = Int32.Parse(value);
                        isConfigVersionSet = true;
                    }
                    else if (key.Equals("STAGE URL"))
                    {
                        StageWebPath = value;
                    }
                    else if (key.Equals("STAGE FOLDER"))
                    {
                        StageLocalPath = value;
                    }
                    else if (key.Equals("MAIN DATA LOCATION"))
                    {
                        DataPath = value;
                    }
                    else if (key.Equals("SECONDARY DATA LOCATION"))
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
                    PythonStageImplementation new_stage_implementation = new PythonStageImplementation(f);
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
