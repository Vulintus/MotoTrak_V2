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
            //session.BoothNumber = (SByte)reader.ReadByte();

            //Read in the number of characters in the rat's name.
            int N = (SByte)reader.ReadByte();

            //Read in the rat's name
            session.RatName = BitConverter.ToString(reader.ReadBytes(N), 0, N);

            //Read in the device's position
            
            

            //Return the session that was loaded from the file
            return session;
        }
    }
}
