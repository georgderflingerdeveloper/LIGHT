using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Communication.HA_COMPONENTS.INTERFACES
{
    public delegate void UpdateOutputs(object sender, bool[] _DigOut, List<int> match);

    interface ICentralControlledElements
    {
        void DelayedDeviceOnRisingEdge(bool Value);
        void DelayedDeviceOnFallingEdge(bool Value);
        void DeviceOnFallingEdgeAutomaticOff(bool Value);

        event UpdateOutputs EUpdateOutputs;

    }
}
