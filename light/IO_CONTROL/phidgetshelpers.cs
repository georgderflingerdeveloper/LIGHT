using System;
using Phidgets;

namespace PhidgetsHelpers
{
    static class PHIDGET_EXCEPTION_OUT
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Int32.ToString")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.DateTime.ToString")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "System.Console.WriteLine(System.String)")]
        static public void PhidgetExceptionOutput ( PhidgetException phiex, string info )
        {
            Console.WriteLine( DateTime.Now + " " + info + " " + phiex.Code + " " +  phiex.Data + " " + phiex.Description);
        }
    }
}
