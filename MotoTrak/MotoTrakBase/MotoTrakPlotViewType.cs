using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakBase
{
    public enum MotoTrakPlotViewType
    {
        [Description("Undefined view")]
        Undefined,

        [Description("Data stream")]
        DataStream,

        [Description("Recent performance")]
        RecentPerformance,

        [Description("Session overview")]
        SessionOverview
    }
}
