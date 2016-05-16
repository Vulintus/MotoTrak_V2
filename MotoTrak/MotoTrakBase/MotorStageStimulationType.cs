using System.ComponentModel;

namespace MotoTrakBase
{
    /// <summary>
    /// This enumeration defines the different VNS stimulation types within the MotoTrak system.  Although we primarily use these for VNS stimulation at TxBDC,
    /// they of course can be adaptive to other things for other labs.
    /// </summary>
    public enum MotorStageStimulationType
    {
        [Description("OFF")]
        Off,

        [Description("ON")]
        On,

        [Description("RANDOM")]
        Random,

        [Description("BURST")]
        Burst,

        [Description("TOP")]
        Top,

        [Description("ALL")]
        All,

        [Description("M-RANDOM")]
        MRandom
    }
}
