﻿using HomeAutomation.rooms; // TODO - change
using Phidgets;

namespace HA_COMPONENTS
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    class LightControlOutside : LightControl
    {
        #region CONSTRUCTOR
        public LightControlOutside( double AllOnTime, double SingleOffTime, ref InterfaceKitDigitalOutputCollection outputs )
            : base( AllOnTime, SingleOffTime, ref outputs )
        {
        }
        #endregion
    }
}
