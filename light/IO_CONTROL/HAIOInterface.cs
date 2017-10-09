using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeAutomation.HardConfig_Collected;

namespace HAHardware
{
    // the world outside is using booleans 
    interface IDigitalIO
    {
         bool[] DigitalInputs        { get; set; }
         bool[] DigitalOutputs       { get; set; }
    }

    // represents index and value of IO primer
    interface IDigitalIndexValue
    {
        int  Index                   { get; set; }
        bool Value                   { get; set; }
    }
}
