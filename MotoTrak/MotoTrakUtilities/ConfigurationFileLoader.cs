using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakUtilities
{
    public static class ConfigurationFileLoader
    {
        /// <summary>
        /// This function is a standardized configuration file loader.  Several different configuration
        /// files are used in MotoTrak.  This file knows how to do some very basic parsing for all of them, such
        /// that it parses out any comments and returns the LINES of the file (containing actual contents that
        /// are not just comments) as a list of strings.
        /// 
        /// Some examples of config files this can be used for: the main MotoTrak config file, the MotoTrak booth pairings file,
        /// and the MotoTrak stage config files.
        /// </summary>
        /// <param name="file_name">The name of the file to load (with path, can be a relative path)</param>
        /// <returns>A list of strings, where each string in the list is a line of the file</returns>
        public static List<string> LoadConfigurationFile(string file_name)
        {
            List<string> result = new List<string>();

            FileInfo file_info = new FileInfo(file_name);
            if (file_info.Exists)
            {
                //Create a stream reader object
                StreamReader reader = null;

                //Try to open a handle to the stage file
                reader = new StreamReader(file_name);

                //Continue loading the stage
                while (!reader.EndOfStream)
                {
                    //Read in a line from the stage file
                    string input_string = reader.ReadLine();

                    //Parse away any comments that are at the end of the line
                    string[] whole_line_parts = input_string.Split(new char[] { '%' }, 2);
                    string line_without_comments = whole_line_parts[0].Trim();

                    //If this line was just an empty line, or a line full of comments, skip it
                    if (!string.IsNullOrEmpty(line_without_comments))
                    {
                        result.Add(line_without_comments);
                    }
                }

                //Close the file stream
                reader.Close();
            }

            return result;
        }
    }
}
