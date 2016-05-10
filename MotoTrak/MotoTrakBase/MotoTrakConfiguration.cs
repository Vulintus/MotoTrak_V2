using System;
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
        /// <returns>Instance of ArdyMotorBoard class</returns>
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

        private string ConfigurationFileName = "mototrak.config";

        #endregion

        #region Properties

        public int ConfigurationVersion { get; set; }
        public string VariantName { get; set; }
        public string StagePath { get; set; }
        public string DataPath { get; set; }
        public string SecondaryDataPath { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Reads the MotoTrak configuration file and populates properties of this class accordingly.
        /// </summary>
        public void ReadConfigurationFile ()
        {
            try
            {
                //Open the configuration file using a stream reader.
                StreamReader reader = new StreamReader(ConfigurationFileName);

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
                    }
                    else if (key.Equals("STAGE URL"))
                    {
                        StagePath = value;
                    }
                    else if (key.Equals("MAIN DATA LOCATION"))
                    {
                        DataPath = value;
                    }
                    else if (key.Equals("SECONDARY DATA LOCATION"))
                    {
                        SecondaryDataPath = value;
                    }
                }
            }
            catch (Exception e)
            {
                throw new MotoTrakException("There was an error while attempting to read the MotoTrak configuration file.");
            }
        }

        #endregion
    }
}
