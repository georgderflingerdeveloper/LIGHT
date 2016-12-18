using System;
using System.Collections.Generic;
using System.Timers;
using HomeAutomation.HardConfig;
using HomeAutomation.Controls;
using Phidgets.Events;
using HomeAutomation.rooms;
using BASIC_CONTROL_LOGIC;

namespace HA_COMPONENTS
{
    // is controlling a group or single heater elements - f.e thermo switches mounted at the heater body, infra red heater elements
    // _NG - next generation - business logic is seperated from IO Operation
    // IO handling is now treated within a seperate EVENT
    class HeaterElement_NG : LightControlTimer_
    {
        #region DECLARATIONS
        protected bool           _HeaterWasTurnedOn = false;
        int                      _startindex, _lastindex;  // index digital output heater element
        double                   _AutomaticOffTime;
        int                      _PWMIntensity;
        int                      _TimedIntensityStep;
        Timer                    Tim_StartIntensityPWM;
        Timer                    Tim_PermanentOnTimeWindow;
        UnivPWM                  PWM_Heater;
        UnivPWM                  PWM_ShowHeaterActive;
        double                   _PWM_StayOnTime;
        double                   _PWM_StayOffTime;
        bool                     _PrevHeaterWasTurnedOn;
        bool[]                   _ShowStateDigitalOutput = new bool[GeneralConstants.NumberOfOutputsIOCard];
        List<int>                Match_;
        bool                     Toggle;
        ToggleButtonController   DeviceToggleController;

        public delegate void UpdateOutputs_( object sender, bool[] _DigOut, List<int> match );
        public event         UpdateOutputs_ EUpdateOutputs_;

        enum eHeaterControlState
        {
            eOFF,
            eON,
            ePWM_ACTIVE,
            eTHERMOSTATE,
            eDEFROST,
            eSCHEDULE,
            eNIGHTSETBACK
        }

        eHeaterControlState HeaterControlState;
        #endregion

        #region CONSTRUCTOR
        // additional PWM Parameters
        public HeaterElement_NG( double AllOnTime,
                                 double AutomaticOffTime,
                                 double PWM_StayOnTime,
                                 double PWM_StayOffTime,
                                 int startindex,
                                 int lastindex
                              )
            : base( AllOnTime, GeneralConstants.TimerDisabled, AutomaticOffTime )
        {
            base.AllOn_ += HeaterElement_AllOnOff_;
            // use auto off functionality from base
            base.AutomaticOff_ += HeaterElement_AutomaticOff_;
            Tim_StartIntensityPWM = new Timer( ParametersHeaterControl.TimeDemandForItensityTimer );
            Tim_StartIntensityPWM.Elapsed += StartIntensityPWM_Elapsed;

            if( PWM_StayOnTime > 0 && PWM_StayOffTime > 0 )
            {
                PWM_ShowHeaterActive = new UnivPWM( ParametersHeaterControl.ShowOn, ParametersHeaterControl.ShowOff, true );
                PWM_Heater = new UnivPWM( PWM_StayOnTime, PWM_StayOffTime );
                PWM_Heater.PWM_ += HeaterPWM_PWM_;
                PWM_ShowHeaterActive.PWM_ += PWM_ShowHeaterActive_PWM_;
                PWM_Heater.PwmTimeOn = _PWM_StayOnTime = PWM_StayOnTime;
                PWM_Heater.PwmTimeOff = _PWM_StayOffTime = PWM_StayOffTime;
            }

            _AutomaticOffTime = AutomaticOffTime;
            _startindex = startindex;
            _lastindex = lastindex;
            _TimedIntensityStep = 1;
            Tim_PermanentOnTimeWindow = new Timer( ParametersHeaterControl.TimeDemandForPermanentOnWindow );
            Tim_PermanentOnTimeWindow.Elapsed += PermanentOnTimeWindow_Elapsed;
            ToggleController( );
        }
        #endregion

        #region PUBLIC_METHODS
        public void HeaterOn( InputChangeEventArgs e )
        {
            HeaterOn( e.Value );
        }

        public void HeaterOn( bool Value )
        {
            if( Value == true )
            {
                base.StartAllOnTimer( );
            }
            else
            {
                base.StopAllOnTimer( );
                Tim_StartIntensityPWM?.Stop( );
                StopTimers( );
            }
        }

        public void HeaterToggleOnOffRisingEdge( bool edge )
        {
            if( edge )
            {
                if( !Toggle )
                {
                    base.StartAllOnTimer( );
                    Toggle = true;
                }
                else
                {
                    base.StopAllOnTimer( );
                    Tim_StartIntensityPWM?.Stop( );
                    StopTimers( );
                    Toggle = false;
                    HeatersOff( );
                }
            }
        }

        public void HeaterOnFallingEdge( InputChangeEventArgs e )
        {
            HeaterOnFallingEdge( e.Value );
        }

        public void HeaterOnFallingEdge( bool Value )
        {
            if( Value == false )
            {
                base.StartAllOnTimer( );
            }
            else
            {
                base.StopAllOnTimer( );
                StopTimers( );
                Tim_StartIntensityPWM?.Stop( );
            }
        }

        // todo
        public void UseThermostatDigital( InputChangeEventArgs e )
        {
        }

        public int IntensityStep
        {
            set
            {
                _PWMIntensity = value;
            }
        }

        public bool HeaterIsOn
        {
            get
            {
                return ( _HeaterWasTurnedOn );
            }
        }

        public bool WasHeaterSwitched( )
        {
            if( _PrevHeaterWasTurnedOn != _HeaterWasTurnedOn )
            {
                _PrevHeaterWasTurnedOn = _HeaterWasTurnedOn;
                return true;
            }
            return false;
        }

        public void Reset( )
        {
            HeaterControlState = eHeaterControlState.eOFF;
            Tim_StartIntensityPWM?.Stop( );
            Tim_PermanentOnTimeWindow?.Stop( );
            PWM_Heater?.Stop( );
            PWM_ShowHeaterActive?.Stop( );
            _HeaterWasTurnedOn = false;
        }

        public void TurnHeaterOnOffWithCounts( )
        {
            DeviceToggleController?.DeviceToggleOnCounts( );
        }

        public void ConfigOnOffCount( uint requiredcounts, double timewindow )
        {
            DeviceToggleController.Countsrequired = requiredcounts;
            DeviceToggleController.Timewindow = timewindow;
        }


        #endregion

        #region PRIVATE_METHODS
        void ToggleController( )
        {
            DeviceToggleController = new ToggleButtonController( );
            DeviceToggleController.EToggle_ += DeviceToggleController_EToggle_;
        }

        void StopTimers( )
        {
            Tim_PermanentOnTimeWindow?.Stop( );
            Tim_StartIntensityPWM?.Stop( );
        }

        // heater duration is determined with actual light index ( 0 is low ... x is highest )
        // this is a feature - f.e. first light was aktivated - first heating level and so on ...

        // set output only without knowing anyting about the state
        void TurnHeaters( bool value )
        {
            for( int ind = _startindex; ind <= _lastindex; ind++ )
            {
                _ShowStateDigitalOutput[ind] = value ? true : false;
            }

            EUpdateOutputs_?.Invoke( this, _ShowStateDigitalOutput, Match_ );
        }

        void HeatersOff( )
        {
            _HeaterWasTurnedOn = false;
            TurnHeaters( false );
        }

        void HeatersOn( )
        {
            _HeaterWasTurnedOn = true;
            TurnHeaters( true );
        }

        void HeaterElement_AutomaticOff_( object sender )
        {
            HeaterControlState = eHeaterControlState.eOFF;
            HeaterControlStateMachine( ref HeaterControlState );
            _HeaterWasTurnedOn = false;
        }

        void HeaterElement_AllOnOff_( object sender )
        {
            if( _HeaterWasTurnedOn )
            {
                HeaterControlState = eHeaterControlState.eOFF;
                HeaterControlStateMachine( ref HeaterControlState );
            }
            else
            {
                HeaterControlState = eHeaterControlState.eON;
                HeaterControlStateMachine( ref HeaterControlState );
            }
            _HeaterWasTurnedOn = !_HeaterWasTurnedOn;
        }

        void PermanentOnTimeWindow_Elapsed( object sender, ElapsedEventArgs e )
        {
            HeaterControlState = eHeaterControlState.ePWM_ACTIVE;
            HeaterControlStateMachine( ref HeaterControlState );
        }

        #region PWM
        void PWM_StateMachine( UnivPWM.ePWMStatus pwmstatus )
        {
            switch( pwmstatus )
            {
                case UnivPWM.ePWMStatus.eIsOn:
                     TurnHeaters( true );
                     break;

                case UnivPWM.ePWMStatus.eIsOff:
                     TurnHeaters( false );
                     break;

                case UnivPWM.ePWMStatus.eInactive:
                     break;
            }
        }

        void HeaterPWM_PWM_( object sender, UnivPWM.ePWMStatus pwmstatus )
        {
            PWM_StateMachine( pwmstatus );
        }
        void PWM_ShowHeaterActive_PWM_( object sender, UnivPWM.ePWMStatus pwmstatus )
        {
            PWM_StateMachine( pwmstatus );
        }
        #endregion PWM

        // "heater intensity" control is activated
        void StartIntensityPWM_Elapsed( object sender, ElapsedEventArgs e )
        {
            if( _PWM_StayOnTime <= 0 || _PWM_StayOffTime <= 0 )
            {
                Tim_StartIntensityPWM.Stop( );
                return;
            }

            PWM_Heater.PwmTimeOn  = _PWM_StayOnTime * _TimedIntensityStep;
            PWM_Heater.PwmTimeOff = _PWM_StayOffTime;

            //show heater status
            PWM_ShowHeaterActive.Start( _TimedIntensityStep );

            if( _TimedIntensityStep > ParametersHeaterControl.MaxIntensitySteps )
            {
                Tim_StartIntensityPWM.Stop( );
                PWM_Heater.Stop( );
                PWM_ShowHeaterActive.Stop( );
                HeaterControlState = eHeaterControlState.eTHERMOSTATE;
                HeaterControlStateMachine( ref HeaterControlState );
                _TimedIntensityStep = 1;
                return;
            }
            _TimedIntensityStep++;

            PWM_Heater.Restart( );
        }

        void RestartIntensityTimer( )
        {
            Tim_StartIntensityPWM.Stop( );
            Tim_StartIntensityPWM.Start( );
        }

        void HeaterControlStateMachine( ref eHeaterControlState ControlState )
        {
            switch( ControlState )
            {
                case eHeaterControlState.eOFF:
                     TurnHeaters( false );
                     _TimedIntensityStep = 1;
                     if( _AutomaticOffTime > 0 )
                     {
                         base.StopAutomaticOfftimer( );
                     }
                     PWM_Heater?.Stop( );
                     Tim_StartIntensityPWM?.Stop( );
                     Tim_PermanentOnTimeWindow?.Stop( );
                     break;

                case eHeaterControlState.eON:
                     TurnHeaters( true );
                     if( _AutomaticOffTime > 0 )
                     {
                         base.StartAutomaticOfftimer( );
                     }
                     Tim_PermanentOnTimeWindow?.Start( );
                     break;

                case eHeaterControlState.ePWM_ACTIVE:
                     Tim_StartIntensityPWM?.Start( );
                     Tim_PermanentOnTimeWindow?.Stop( );
                     break;

                case eHeaterControlState.eTHERMOSTATE:
                //HeatersOff( );
                break;

                case eHeaterControlState.eDEFROST:
                     base.ReconfigAutomaticOffTimer( new TimeSpan( 7, 0, 0, 0, 0 ).TotalMilliseconds );
                     PWM_Heater.PwmTimeOn = ParametersHeaterControlLivingRoom.TimeDemandForHeatersOnDefrost;
                     PWM_Heater.PwmTimeOff = ParametersHeaterControlLivingRoom.TimeDemandForHeatersOffDefrost;
                     PWM_Heater.Start( );
                     break;
            }
        }
        #endregion

        #region PROPERTIES
        // representation of digital outpus as BOOLEANS - purpose is to ease testing
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

        #region EVENTHANDLERS
        private void DeviceToggleController_EToggle_( object sender, bool value )
        {
            TurnHeaters( value );
        }
        #endregion
    }
}
