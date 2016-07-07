using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakBase
{
    /// <summary>
    /// A central place to store all kinds of exceptions that we may generate within MotoTrak, as well as their user-readable string counterparts.
    /// </summary>
    public enum MotoTrakExceptionType
    {
        [Description("An unknown error occurred within MotoTrak.")]
        Unknown,

        [Description("Unable to connect to the MotoTrak controller board.")]
        UnableToConnectToControllerBoard,

        [Description("The connected MotoTrak controller does not meet the minimum requirements to run MotoTrak.")]
        ControllerBoardNotCompatible,

        [Description("The device that is connected is unrecognized by MotoTrak.")]
        UnrecognizedDevice,

        [Description("Unable to load previous session data.")]
        CouldNotLoadSessionData,

        [Description("Unabled to load MotoTrak stage.")]
        CouldNotLoadMotoTrakStage
    }
}
