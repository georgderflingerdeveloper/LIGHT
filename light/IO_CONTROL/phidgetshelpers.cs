using System;
using Phidgets;

namespace PhidgetsHelpers
{
    static class PHIDGET_EXCEPTION_OUT
    {
        static public void PhidgetExceptionOutput ( PhidgetException phiex, string info )
        {
            Console.WriteLine( DateTime.Now + " " + info + " " + phiex.Code + " " +  phiex.Data + " " + phiex.Description);
        }
    }
}
