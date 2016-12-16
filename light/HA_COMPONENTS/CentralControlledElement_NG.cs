
using System.Collections.Generic;
using BASIC_CONTROL_LOGIC;
using HomeAutomation.HardConfig;

namespace HA_COMPONENTS
{
    // _NG - next generation - business logic is seperated from IO Operation
    // IO handling is now treated within a seperate EVENT
    class CentralControlledElements_NG : LightControlTimer_
    {
        #region DECLARATIONS
        int            _startindex;
        int            _lastindex;
        int            _deviceindex;
        bool[]         _ShowStateDigitalOutput   = new bool[GeneralConstants.NumberOfOutputsIOCard];         // fill state from outside

        List<int> Match_ = new List<int>();
        public delegate void UpdateOutputs_( object sender, bool[] _DigOut, List<int> match );
        public event         UpdateOutputs_ EUpdateOutputs_;

        #endregion

        #region CONSTRUCTOR
        // different "configurations" are via constructor possible
        public CentralControlledElements_NG( double AllOnTime, double AutomaticOffTime, int startindex, int lastindex )
            : base( AllOnTime, GeneralConstants.TimerDisabled, AutomaticOffTime )
        {
            _startindex = startindex;
            _lastindex = lastindex;

            base.AllOn_ += CentralControlledElements_AllOn_;
            base.AutomaticOff_ += CentralControlledElements_AutomaticOff_;
        }

        public CentralControlledElements_NG( double AllOnTime, double AutomaticOffTime, int deviceindex )
            : base( AllOnTime, GeneralConstants.TimerDisabled, AutomaticOffTime )
        {
            _deviceindex = _startindex = _lastindex = deviceindex;
            base.AllOn_ += CentralControlledElements_AllOn_;
            base.AutomaticOff_ += CentralControlledElements_AutomaticOff_;
        }

        public CentralControlledElements_NG( double AutomaticOffTime, int deviceindex )
            : base( GeneralConstants.TimerDisabled, GeneralConstants.TimerDisabled, AutomaticOffTime )
        {
            _deviceindex = _startindex = _lastindex = deviceindex;
            base.AutomaticOff_ += CentralControlledElements_AutomaticOff_;
        }
        #endregion

        #region PUBLIC_METHODS
 
        public void DelayedDeviceOnRisingEdge( bool Value )
        {
            if( Value == true )
            {
                base.StartAllOnTimer( );
            }
            else
            {
                base.StopAllOnTimer( );
            }
        }

        public void DelayedDeviceOnFallingEdge( bool Value )
        {
            if( Value == false )
            {
                base.StartAllOnTimer( );
            }
            else
            {
                base.StopAllOnTimer( );
            }
        }

        public void DeviceOnFallingEdgeAutomaticOff( bool Value )
        {
            if( Value == false )
            {
                base.RestartAutomaticOfftimer( );
                if( _deviceindex < GeneralConstants.NumberOfOutputsIOCard && _deviceindex >= 0 )
                {
                    _ShowStateDigitalOutput[_deviceindex] = true;
                    if( EUpdateOutputs_ != null )
                    {
                        EUpdateOutputs_( this, _ShowStateDigitalOutput, Match_ );
                    }
                }
            }
        }

        #endregion

        #region EVENTHANDLERS
        void CentralControlledElements_AutomaticOff_( object sender )
        {
            base.StopAutomaticOfftimer( );
            // output ON
            for( int ind = _startindex; ind <= _lastindex; ind++ )
            {
                _ShowStateDigitalOutput[ind] = false;
            }
            EUpdateOutputs_?.Invoke( this, _ShowStateDigitalOutput, Match_ );
        }

        void CentralControlledElements_AllOn_( object sender )
        {
            base.StartAutomaticOfftimer( );
            // output ON
            for( int ind = _startindex; ind <= _lastindex; ind++ )
            {
                _ShowStateDigitalOutput[ind] = true;
            }
            EUpdateOutputs_?.Invoke( this, _ShowStateDigitalOutput, Match_ );
        }
        #endregion

        #region PROPERTIES

        public bool[] ShowStateDigitalOutput
        {
            get
            {
                return ( _ShowStateDigitalOutput );
            }
        }

        public List<int> Match
        {
            set
            {
                Match_ = value;
            }
        }

        #endregion
    }
}
