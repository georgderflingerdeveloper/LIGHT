using System;
using Phidgets;
using HomeAutomation.HardConfig_Collected;
using PhidgetsHelpers;

namespace HardwareDevices
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    class LedControl : Phidgets.LED
    {
        public LedControl ( )
            : base( )
        {
            try
            {
                base.open( Phidget_ID.ID_LED_CARD_1 );
                base.waitForAttachment( Parameters.AttachWaitTime );
            }
            catch( PhidgetException phiex_ )
            {
                PHIDGET_EXCEPTION_OUT.PhidgetExceptionOutput( phiex_, InfoString.InfoPhidgetException );
            }

        }
    }
}
