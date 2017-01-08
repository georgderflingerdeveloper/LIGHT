using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BaseControlLogic
{
    [TestClass]
    public class unittest_lightstepcontrol
    {
        const uint NumberOfDevices           = 16;
        const double TimeNext                = 40;
        const double LittleDelay             = 20;
        const uint NumberOfOnOffTests        = 2;

        devicestepcontrol StepControl = new devicestepcontrol( NumberOfDevices, TimeNext );

        void SignalChangeOnOff()
        {
            StepControl.WatchForInputValueChange( true );
            Thread.Sleep(  Convert.ToInt16( LittleDelay ) );
            StepControl.WatchForInputValueChange( false );
        }

        [TestMethod]
        public void Test_DeviceValueSwitch( )
        {
            for( uint i = 0; i < NumberOfOnOffTests; i++ )
            {
                SignalChangeOnOff( ); // f.e. emulation of pushing / pulling a button
                Assert.AreEqual( true, StepControl.Value );
                SignalChangeOnOff( );
                Assert.AreEqual( false, StepControl.Value );
                StepControl.Reset( );
            }
        }

        [TestMethod]
        public void Test_DeviceNumberSwitchNextElement()
        {
            for( uint i = 0; i < NumberOfDevices; i++  )
            {
                // Switch ON next device
                StepControl.WatchForInputValueChange( true );
                Thread.Sleep( Convert.ToInt16( TimeNext ) + Convert.ToInt16( LittleDelay ) );

                Assert.AreEqual( i+1, StepControl.Number );

                StepControl.WatchForInputValueChange( false );
                Thread.Sleep( Convert.ToInt16( LittleDelay ) );
                
            }
            StepControl.Reset( );
        }

        [TestMethod]
        public void Test_DeviceValueSwitchNextElement( )
        {
            for( uint i = 0; i < NumberOfDevices; i++ )
            {
                // Switch ON next device
                StepControl.WatchForInputValueChange( true );
                Thread.Sleep( Convert.ToInt16( TimeNext ) + Convert.ToInt16( LittleDelay ) );

                Assert.AreEqual( i + 1, StepControl.Number );

                StepControl.WatchForInputValueChange( false );
                Thread.Sleep( Convert.ToInt16( LittleDelay ) );

                Assert.AreEqual( false, StepControl.Value );

                SignalChangeOnOff( );

                Assert.AreEqual( true, StepControl.Value );

                SignalChangeOnOff( );

                Assert.AreEqual( false, StepControl.Value );
            }
            StepControl.Reset( );
        }
    }
}
