using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakBase
{
    public enum MotoTrak_V1_StageParameters
    {
        [MotoTrak_V1_SpreadsheetColumnHeading(new string[] { "stage number" })]
        StageNumber,

        [MotoTrak_V1_SpreadsheetColumnHeading(new string[] { "description" })]
        Description,

        [MotoTrak_V1_SpreadsheetColumnHeading(new string[] { "device", "primary input device", "input device" })]
        InputDevice,

        [MotoTrak_V1_SpreadsheetColumnHeading(new string[] { "position", "position (cm)" })]
        Position,

        [MotoTrak_V1_SpreadsheetColumnHeading(new string[] { "hit threshold - minimum", "hit threshold" })]
        HitThresholdMinimum,

        [MotoTrak_V1_SpreadsheetColumnHeading(new string[] { "constraint" })]
        Constraint,

        [MotoTrak_V1_SpreadsheetColumnHeading(new string[] { "hit threshold - type", "Hit Threshold - Type (\"STATIC\", \"LINEAR\", or \"MEDIAN\")" })]
        HitThresholdType,

        [MotoTrak_V1_SpreadsheetColumnHeading(new string[] { "hit threshold - maximum" })]
        HitThresholdMaximum,

        [MotoTrak_V1_SpreadsheetColumnHeading(new string[] { "hit threshold - ceiling" })]
        HitThresholdCeiling,

        [MotoTrak_V1_SpreadsheetColumnHeading(new string[] { "hit threshold - increment" })]
        HitThresholdIncrement,

        [MotoTrak_V1_SpreadsheetColumnHeading(new string[] { "threshold units" })]
        ThresholdUnits,

        [MotoTrak_V1_SpreadsheetColumnHeading(new string[] { "ir trial initiation", "IR Trial Initiation (\"YES\" or \"NO\")" })]
        IRTrialInitiation,

        [MotoTrak_V1_SpreadsheetColumnHeading(new string[] { "sample period", "sample period (ms)" })]
        SamplePeriod,

        [MotoTrak_V1_SpreadsheetColumnHeading(new string[] { "pre-trial sampling time" })]
        PreTrialSamplingTime,

        [MotoTrak_V1_SpreadsheetColumnHeading(new string[] { "post-trial sampling time" })]
        PostTrialSamplingTime,

        [MotoTrak_V1_SpreadsheetColumnHeading(new string[] { "hit window", "hit window (s)" })]
        HitWindow,

        [MotoTrak_V1_SpreadsheetColumnHeading(new string[] { "session duration" })]
        SessionDuration,

        [MotoTrak_V1_SpreadsheetColumnHeading(new string[] { "force stop at end of session" })]
        ForceStopAtEndOfSession,

        [MotoTrak_V1_SpreadsheetColumnHeading(new string[] { "vns default", "stimulation", "VNS Default (\"ON\", \"OFF\", or \"RANDOM\")", "Stimulation (\"ON\", \"OFF\", or \"RANDOM\")" })]
        StimulateOnHit,

        [MotoTrak_V1_SpreadsheetColumnHeading(new string[] { "trial initiation threshold" })]
        TrialInitiationThreshold
    }
}
