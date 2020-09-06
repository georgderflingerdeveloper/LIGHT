using System;
using Phidgets;
using SystemServices;

namespace IO_Control
{
    class AnalogControl : Analog
    {
        const int ChannelIndexMax        = 3;
        const int TimeWaitForAttachement = 2000;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public AnalogControl( int SerialNumber ) : base( )
        {
            try
            {
                open( SerialNumber );
                waitForAttachment( TimeWaitForAttachement );
            }
            catch( Exception ex )
            {
                Services.TraceMessage_( ex.Message );
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public AnalogControl( ) : base( )
        {
            try
            {
                open( );
                waitForAttachment( TimeWaitForAttachement );
            }
            catch( Exception ex )
            {
                Services.TraceMessage_( ex.Message );
            }
        }

        public void SetOutput( int channel, double voltage )
        {
            if( Attached )
            {
                if( ( channel >= 0 ) && ( channel <= ChannelIndexMax ) )
                {
                    outputs[channel].Enabled = true;
                    outputs[channel].Voltage = voltage;
                }
            }
        }

        public void Reset()
        {
            if( Attached )
            {
                for( int channel = 0; channel < outputs.Count; channel++ )
                {
                    outputs[channel].Enabled = true;
                    outputs[channel].Voltage = 0;
                }
            }
        }

        ~AnalogControl( )
        {
            if( Attached )
            {
                Reset( );
            }
        }
    }
}
