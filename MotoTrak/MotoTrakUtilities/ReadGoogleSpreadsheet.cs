using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace MotoTrakUtilities
{
    /// <summary>
    /// Reads a spreadsheet from Google Docs
    /// </summary>
    public class ReadGoogleSpreadsheet
    {
        /// <summary>
        /// Reads a page given a web address, and parses it as a tab-separated-value file.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static List<List<string>> Read(Uri address)
        {
            //Declare the matrix that will be used to return the data
            List<List<string>> fileContent = new List<List<string>>();

            //Create a web request
            WebClient web = new WebClient();
            web.UseDefaultCredentials = true;
            string page = web.DownloadString(address);

            //Declare some whitespace separators which we will use to parse the page
            char[] newLineSep = new char[1] { '\n' };
            char[] tabSep = new char[1] { '\t' };

            //Split the page into many lines
            List<string> linesOfFile = page.Split(newLineSep).ToList();

            //Now iterate through each line and split it based on tab whitespaces
            foreach (string line in linesOfFile)
            {
                List<string> tabSeparatedLine = line.Split(tabSep).ToList();
                fileContent.Add(tabSeparatedLine);
            }

            return fileContent;
        }
    }
}
