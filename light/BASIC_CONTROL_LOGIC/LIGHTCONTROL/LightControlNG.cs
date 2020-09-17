using BASIC_COMPONENTS;
using BASIC_CONTROL_LOGIC;
using HomeAutomation.HardConfig_Collected;
using Phidgets.Events;
using System.Collections.Generic;
using System.Data;
using System.Timers;

namespace HA_COMPONENTS
{
    // _NG - next generation - business logic is seperated from IO Operation
    // IO handling is now treated within a seperate EVENT
    class LightControl_NG : LightControlTimer_, System.IDisposable
    {
        #region DECLARATIONS
        bool                                       EnableStepLight;
        bool                                       SelectDevicesOff;
        bool                                       _AllLightsAreOff;
        int                                        previousIndex;
        protected bool                             _AllLightsAreOn;
        protected bool                             SomeLightsAreOn;
        int[]                                      _values;
        protected bool[]                           SelectedPermanentOnDevice;
        bool                                       InitPermanentDeviceSelection;
        int                                        actualindex;
        public const int                           MaxNumberOfOutputs = 16;  // 0...15 this amount ist limited of the used IO card
        int                                        _startindex;
        int                                        _lastindex;
        bool                                       DevicesOffProceeded;
        bool                                       _LightControlSingleOffDone;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        bool _PrimaryIOCardIsAttached;
        List<int>                                  Match_;
        devicestepcontrol                          TimedStepControl;
        Timer                                      AllOutputsOffTimer;
        Timer AliveTimer                         = new Timer( Parameters.TimeIntervallAlive );
        Timer FinalAllAutomaticOff               = new Timer();                                          // all configured devices off
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        bool[] _StateDigitalOutput               = new bool[GeneralConstants.NumberOfOutputsIOCard];     // fill state from outside
        bool[] _ToggleOutput                     = new bool[GeneralConstants.NumberOfOutputsIOCard];
        bool[] _ShowStateDigitalOutput           = new bool[GeneralConstants.NumberOfOutputsIOCard];     // show internal state - reason is to ease testing
        protected bool[] _DigitalOutput          = new bool[GeneralConstants.NumberOfOutputsIOCard];

        public delegate void AllSelectedDevicesOff( object sender, int firstdevice, int lastdevice );
        public event         AllSelectedDevicesOff AllSelectedDevicesOff_;

        public delegate void UpdateOutputs( object sender, bool[] _DigOut, List<int> match );
        public event         UpdateOutputs EUpdateOutputs;

        public delegate void Reset( object sender );
        public event         Reset EReset;
        #endregion

        #region CONSTRUCTOR
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Mobility", "CA1601:DoNotUseTimersThatPreventPowerStateChanges")]
        public LightControl_NG( double AllOnTime, double SingleOffTime, int deviceindex )
            : base( AllOnTime, SingleOffTime )
        {
            base.AllOn_     += LightControl_AllOn_;
            base.SingleOff_ += LightControl_SingleOff_;
            _startindex      = _lastindex = deviceindex;
            Constructor( );
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Mobility", "CA1601:DoNotUseTimersThatPreventPowerStateChanges")]
        public LightControl_NG( double AllOnTime,
                                double AllOutputsOffTime,
                                double SingleOffTime,
                                double AutomaticOffTime,
                                int startindex,
                                int lastindex
                              )
            : base( AllOnTime, SingleOffTime, AutomaticOffTime )
        {
            Constructor( AllOutputsOffTime, SingleOffTime, AutomaticOffTime, startindex, lastindex );
            EUpdateOutputs += LightControlNG_EUpdateOutputs;
        }



        #endregion

        #region PROPERTIES
        public bool IsPrimaryIOCardAttached
        {
            set
            {
                _PrimaryIOCardIsAttached = value;
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

        #region PUBLIC_METHODS
 
		public void TurnSingleDevice( int index, bool value )
        {
            if( ( _DigitalOutput != null ) && ( index < _DigitalOutput.Length ) )
            {
                _DigitalOutput[index] = value;
            }
            EUpdateOutputs?.Invoke( this, _DigitalOutput, Match_ );
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "elements")]
        public void TurnAllDevices( bool command )
        {
            int index = 0;
            foreach( bool elements in _DigitalOutput )
            {
                _DigitalOutput[index] = command;
                index++;
            }
            EUpdateOutputs?.Invoke( this, _DigitalOutput, Match_ );
        }

        public void ToggleSingleDevice( int index )
        {
            if( !_ToggleOutput[index] )
            {
                TurnSingleDevice( index, GeneralConstants.ON );
            }
            else
            {
                TurnSingleDevice( index, GeneralConstants.OFF );
            }
            _ToggleOutput[index] = !_ToggleOutput[index];
        }

        public void StartAliveSignal( )
        {
            AliveTimer.Start( );
        }

        public void StopAliveSignal( )
        {
            AliveTimer.Stop( );
            _DigitalOutput[CommonRoomIOAssignment.indOutputIsAlive] = false;
            EUpdateOutputs?.Invoke( this, _DigitalOutput, Match_ );
        }

        public void ResetDeviceControl( )
        {
            _LightControlSingleOffDone = false;

			for( int i = 0; i < _ToggleOutput.Length; i++ )
			{
			     _ToggleOutput[i] = false;
			}
		}

        public void StartWaitForAllOff( )
        {
            AllOutputsOffTimer?.Start( );
        }

        public void StopWaitForAllOff( )
        {
            AllOutputsOffTimer?.Stop( );
        }

        public void MakeStep( bool cmd )
        {
            makestep_( cmd );
        }

        public void MakeTimedStep( bool cmd )
        {
            TimedStepControl?.WatchForInputValueChange( cmd );
        }



        public new void AutomaticOff( bool value )
        {
            SelectDevicesOff = false;
            StartAutomaticOff( value );

            if( DevicesOffProceeded )
            {
                CancelAutomaticOff( );
                DevicesOffProceeded = false;
            }

            RestartAutomaticOff( value );
        }

        #endregion

        #region EVENTHANDLERS
        void LightControlNG_EUpdateOutputs( object sender, bool[] _DigOut, List<int> Match )
        {
            DoUpDateIO( _DigOut );
        }

        private void TimedStepControl_EStep( uint number, bool value )
        {
            _DigitalOutput[number] = value;
            EUpdateOutputs?.Invoke( this, _DigitalOutput, Match_ );
        }

        void FinalAllAutomaticOff_Elapsed( object sender, ElapsedEventArgs e )
        {
            TurnAllLightsOff( _startindex, _lastindex );
        }

        void AllOutputsOffTimer_Elapsed( object sender, ElapsedEventArgs e )
        {
            AllOutputsOffTimer.Stop( );
            AllOutputsOff( );
            EReset?.Invoke( this );
        }

        void AliveTimer_Elapsed( object sender, ElapsedEventArgs e )
        {
            _DigitalOutput[CommonRoomIOAssignment.indOutputIsAlive] = !_DigitalOutput[CommonRoomIOAssignment.indOutputIsAlive];
            EUpdateOutputs?.Invoke( this, _DigitalOutput, Match_ );
        }

        void LightControl_ESingleDelayedIndexedOff_( object sender, int index )
        {
            ControlOutput( index, false );
        }

        void LightControl_SingleOff_( object sender )
        {
            // no need for turning off when light is already off
            if( _AllLightsAreOff )
            {
                return;
            }
            _LightControlSingleOffDone = true;
            _AllLightsAreOn = false;
            TurnAllLightsOff( _startindex, _lastindex );
            actualindex = previousIndex;

            if( ( actualindex + _startindex ) > _lastindex )
            {
                actualindex = 0;
            }
        }

        void LightControl_AutomaticOff_( object sender )
        {
            if( ( actualindex + _startindex ) > _lastindex )
            {
                actualindex = 0;
            }
            _AllLightsAreOn = false;
            if( SelectDevicesOff )
            {
                TurnSomeLightsOff( _values );
            }
            else
            {
                TurnAllLightsOff( _startindex, _lastindex );
            }
            base.StopAutomaticOfftimer( );
        }

        void LightControl_AllOn_( object sender )
        {
            actualindex = 0;
            AllLightsOn( _startindex, _lastindex );
            _AllLightsAreOn = true;
        }
        #endregion

        #region PRIVATE_METHODS
        void AllOutputsOff( )
        {
            for( int i = 0; i < _DigitalOutput.Length; i++ )
            {
                _DigitalOutput[i] = false;
            }
            EUpdateOutputs?.Invoke( this, _DigitalOutput, Match_ );
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "lastindex")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "startindex")]
        bool AreAllLightsOff( int startindex, int lastindex )
        {
            // check for all devices (lights) off
            for( int i = _startindex; i <= _lastindex; i++ )
            {
                if( _ShowStateDigitalOutput[i] == true )
                {
                    return false;
                }
            }
            return true;
        }

        void makestep_( bool cmd )
        {
            // f.e PUSH a BUTTON 
            if( cmd )
            {
                base.StartAllTimers( );
                if( SomeLightsAreOn )
                {
                    TurnAllLightsOff( _startindex, _lastindex );
                    SomeLightsAreOn = false;
                    for( int i = 0; i < _values.Length; i++ )
                    {
                        if( actualindex >= 0 )
                        {
                            // last selected device in the "white list - that means the device stays on"
                            if( actualindex - 1 == _values[i] )
                            {
                                actualindex = _startindex;
                            }
                        }
                    }
                    EnableStepLight = true;
                    return;
                }
                if( _AllLightsAreOn )
                {
                    TurnAllLightsOff( _startindex, _lastindex );
                    EnableStepLight = false;
                    _AllLightsAreOn = false;
                }
                else
                {
                    EnableStepLight = true;
                }
            }
            //f.e  PULL a BUTTON
            else
            {
                base.StopAllTimers( );
                if( _LightControlSingleOffDone )
                {
                    _LightControlSingleOffDone = false;
                    return;
                }
                if( EnableStepLight )
                {
                    previousIndex = actualindex;
                    StepLight( _startindex, ref actualindex, _lastindex );
                }
            }
            _AllLightsAreOff = AreAllLightsOff( _startindex, _lastindex );
        }

        void StartAutomaticOff( bool value )
        {
            if( value == false )
            {
                base.StartAutomaticOfftimer( );
            }
        }

        void CancelAutomaticOff( )
        {
            base.StopAutomaticOfftimer( );
        }


        void RestartAutomaticOff( bool value )
        {
            if( value == true )
            {
                base.RestartAutomaticOfftimer( );
            }
        }

        void TurnSomeLightsOff( int[] value )
        {
            if( value == null )
            {
                return;
            }

            SomeLightsAreOn = true;
            DevicesOffProceeded = true;
            for( int i = 0; i < value.Length; i++ )
            {
                if( value[i] >= _DigitalOutput.Length )
                {
                    return;
                }

                _DigitalOutput[value[i]] = false;

                if( value.Length <= _DigitalOutput.Length )
                {
                    continue;
                }
                else
                {
                    break;
                }
            }
            EUpdateOutputs?.Invoke( this, _DigitalOutput, Match_ );
        }

        void ControlOutput( int index, bool value )
        {
            if( ( index < GeneralConstants.NumberOfInputsIOCard ) && ( index > 0 ) )
            {
                _DigitalOutput[index] = value;
                EUpdateOutputs?.Invoke( this, _DigitalOutput, Match_ );
            }
        }

        void Constructor( )
        {
            AliveTimer.Elapsed += AliveTimer_Elapsed;
            EUpdateOutputs     += LightControlNG_EUpdateOutputs;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "AutomaticOffTime")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Objekte verwerfen, bevor Bereich verloren geht")]
        void Constructor( double AllOutputsOffTime, double SingleOffTime, double AutomaticOffTime, int firstindex, int lastindex )
        {
            base.AllOn_                    += LightControl_AllOn_;
            base.SingleOff_                += LightControl_SingleOff_;
            base.AutomaticOff_             += LightControl_AutomaticOff_;
            base.ESingleDelayedIndexedOff_ += LightControl_ESingleDelayedIndexedOff_;
            AliveTimer.Elapsed             += AliveTimer_Elapsed;
            _startindex                     = firstindex;
            _lastindex                      = lastindex;
            SelectedPermanentOnDevice       = new bool[GeneralConstants.NumberOfOutputsIOCard];
            InitPermanentDeviceSelection    = false;
            AllOutputsOffTimer              = new Timer( AllOutputsOffTime );
            AllOutputsOffTimer.Elapsed     += AllOutputsOffTimer_Elapsed;
            TimedStepControl                = new devicestepcontrol( (uint) lastindex, new Timer_( SingleOffTime ) );
            TimedStepControl.EStep         += TimedStepControl_EStep;
        }
		#endregion

        #region PROTECTED_METHODS
        protected void DoUpDateIO( bool[] _DigOut )
        {
            _ShowStateDigitalOutput = _DigOut;
        }


 

 

        // TODO BUGFIX !!!!
        protected bool TurnAllLightsOff( int startindex, int lastindex )
        {
            for( int ind = startindex; ind <= lastindex; ind++ )
            {
                _DigitalOutput[ind] = false;
            }
            EUpdateOutputs?.Invoke( this, _DigitalOutput, Match_ );
            AllSelectedDevicesOff_?.Invoke( this, startindex, _lastindex );
            DevicesOffProceeded = true;
            return ( true );
        }

        protected bool AllLightsOn( int startindex, int lastindex )
        {
            for( int ind = startindex; ind <= lastindex; ind++ )
            {
                _DigitalOutput[ind] = true;
            }
            EUpdateOutputs?.Invoke( this, _DigitalOutput, Match_ );
            return ( true );
        }

        virtual protected void StepLight( int startindex, ref int index, int _indexlastdevice )
        {
            int _index = startindex + index;

            if( _index <= _indexlastdevice )
            {
                _DigitalOutput[_index] = true;
                if( index > 0 )
                {
                    _DigitalOutput[_index - 1] = false;
                }
                index++;
            }
            else
            {
                _DigitalOutput[_index - 1] = false;
                index = 0;
            }
            EUpdateOutputs?.Invoke( this, _DigitalOutput, Match_ );
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "AliveTimer")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "AllOutputsOffTimer")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "FinalAllAutomaticOff")]
        public void Dispose()
        {
            throw new System.NotImplementedException();
        }
        #endregion
    }
}
