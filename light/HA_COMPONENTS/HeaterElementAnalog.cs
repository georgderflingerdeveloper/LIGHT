using BASIC_COMPONENTS;
using System.Collections.Generic;


namespace HA_COMPONENTS
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    class HeaterElementAnalog : HeaterElement_NG
    {
        AnalogHeaterControl AnalogController = new AnalogHeaterControl();
        bool _UseOneOutput;
        int _Index;

        public HeaterElementAnalog( double AllOnTime,
                                    double AutomaticOffTime,
                                    double PWM_StayOnTime,
                                    double PWM_StayOffTime,
                                    int    startindex,
                                    int    lastindex ) 
                                    : base( AllOnTime, AutomaticOffTime, PWM_StayOnTime, PWM_StayOffTime, startindex, lastindex )
        {
            this.EUpdateOutputs_ += HeaterElementAnalog_EUpdateOutputs_;
            if( startindex == lastindex )
            {
                _UseOneOutput = true;
                _Index = startindex;
            }
        }

        private void HeaterElementAnalog_EUpdateOutputs_( object sender, bool[] _DigOut, List<int> match )
        {
            if( _UseOneOutput )
            {
                if( _DigOut[_Index] )
                {
                    AnalogController?.On( ); 
                }
                else
                {
                    AnalogController?.Off( );
                }
            }
        }
    }
}
