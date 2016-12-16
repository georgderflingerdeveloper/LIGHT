using IO_Control;

namespace BASIC_COMPONENTS
{
    class AnalogHeaterControl : AnalogControl 
    {
        const int DefaultChannel       =  0;
        const double DefaultVoltageOn  =  10.0;
        const double DefaultVoltageOff =  0.0;

        public AnalogHeaterControl( int serialnumber) : base( serialnumber ) {}
        public AnalogHeaterControl( ) : base(  ) { }

        public void On( )
        {
            SetOutput( DefaultChannel, DefaultVoltageOn );
        }

        public void Off( )
        {
            SetOutput( DefaultChannel, DefaultVoltageOff );
        }
    }
}
