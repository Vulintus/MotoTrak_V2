using System;
using System.Collections.Generic;
using MotoTrakUtilities;

namespace MotoTrakBase
{
    /// <summary>
    /// This class represents a stage within the MotoTrak program.
    /// </summary>
    public class MotorStage
    {
        #region Constructors

        /// <summary>
        /// Constructs an empty MotoTrak stage.
        /// </summary>
        public MotorStage()
        {
            //empty
        }

        #endregion

        #region Properties - V1

        public string StageNumber { get; set; }
        public string Description { get; set; }
        public MotorDeviceType DeviceType { get; set; }
        public double Position { get; set; }
        public MotorStageAdaptiveThresholdType AdaptiveThresholdType { get; set; }

        public double HitThresholdMinimum { get; set; }
        public double HitThresholdMaximum { get; set; }
        public double HitThresholdIncrement { get; set; }
        public double HitThreshold { get; set; }

        public double TrialInitiationThreshold { get; set; }
        public MotorStageHitThresholdType HitThresholdType { get; set; }
        public double HitWindowInSeconds { get; set; }
        public int SamplePeriodInMilliseconds { get; set; }
        public MotorStageStimulationType StimulationType { get; set; }

        #endregion

        #region Read-only properties - V1

        public List<MotorStageHitThresholdType> PossibleHitThresholdTypes
        {
            get
            {
                List<MotorStageHitThresholdType> possibleTypes = new List<MotorStageHitThresholdType>();
                switch (DeviceType)
                {
                    case MotorDeviceType.Pull:
                        possibleTypes.Add(MotorStageHitThresholdType.PeakForce);
                        possibleTypes.Add(MotorStageHitThresholdType.SustainedForce);
                        break;
                    case MotorDeviceType.Knob:
                        possibleTypes.Add(MotorStageHitThresholdType.TotalDegrees);
                        break;
                    default:
                        possibleTypes.Add(MotorStageHitThresholdType.Undefined);
                        break;
                }

                return possibleTypes;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns a list of all motor stages found in a spreadsheet.  The spreadsheet location is the URI that is
        /// passed as a parameter to this function.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static List<MotorStage> RetrieveAllStages (Uri address)
        {
            //Read in all the stages from Google Docs
            List<List<string>> stageDocument = ReadGoogleSpreadsheet.Read(address);

            //Remove the first line of the file because it is just column headers
            stageDocument.RemoveAt(0);

            //This is how many columns should be in the file
            int numberOfColumns = 14;

            //Create a list we will use to store all the stages
            List<MotorStage> stages = new List<MotorStage>();

            //Go through each stage from Google Docs and create an object for it
            foreach (List<string> stageLine in stageDocument)
            {
                //If for some reason this line doesn't have the correct number of columns, skip it
                if (stageLine.Count != numberOfColumns)
                {
                    continue;
                }
                else
                {
                    //Otherwise, set the variables for the new stage appropriately.
                    MotorStage stage = new MotorStage();
                    try
                    {
                        stage.StageNumber = stageLine[0];
                        stage.Description = stageLine[1];
                        stage.DeviceType = MotorDeviceTypeConverter.ConvertToMotorDeviceType(stageLine[2]);
                        
                        //Read the constraint from the stage file and throw it away
                        int trash = Int32.Parse(stageLine[3]);

                        stage.Position = Double.Parse(stageLine[4]);
                        stage.AdaptiveThresholdType = MotorStageAdaptiveThresholdTypeConverter.ConvertToMotorStageAdaptiveThresholdType(stageLine[5]);
                        stage.HitThresholdMinimum = Double.Parse(stageLine[6]);
                        stage.HitThresholdMaximum = Double.Parse(stageLine[7]);
                        stage.HitThresholdIncrement = Double.Parse(stageLine[8]);
                        stage.TrialInitiationThreshold = Double.Parse(stageLine[9]);
                        stage.HitThresholdType = MotorStageHitThresholdTypeConverter.ConvertToMotorStageHitThresholdType(stageLine[10]);
                        stage.HitWindowInSeconds = Double.Parse(stageLine[11]);
                        stage.SamplePeriodInMilliseconds = Int32.Parse(stageLine[12]);
                        stage.StimulationType = MotorStageStimulationTypeConverter.ConvertToMotorStageStimulationType(stageLine[13]);
                        
                        //Add the stage to our list of stages
                        stages.Add(stage);
                    }
                    catch (System.FormatException)
                    {
                        //do nothing
                    }
                    catch (System.ArgumentException)
                    {
                        //do nothing
                    }
                    catch (System.OverflowException)
                    {
                        //do nothing
                    }
                }
            }

            return stages;
        }

        #endregion
    }
}
