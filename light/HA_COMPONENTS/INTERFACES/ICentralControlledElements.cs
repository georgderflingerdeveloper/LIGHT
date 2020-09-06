using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Communication.HA_COMPONENTS.INTERFACES
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Dig")]
    public delegate void UpdateOutputs(object sender, bool[] _DigOut, List<int> match);

    interface ICentralControlledElements
    {
        void DelayedDeviceOnRisingEdge(bool Value);
        void DelayedDeviceOnFallingEdge(bool Value);
        void DeviceOnFallingEdgeAutomaticOff(bool Value);

        event UpdateOutputs EUpdateOutputs;

    }
}
