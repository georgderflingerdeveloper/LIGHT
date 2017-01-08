using System;
using System.Collections.Generic;
using System.Timers;
using Communication.Server_;
using HomeAutomation.Controls;
using HomeAutomation.HardConfig;
using Phidgets;
using Phidgets.Events;
using PhidgetsHelpers;
using SystemServices;


namespace HomeAutomation
{
    namespace rooms
    {
        // represents one IO card integrated in a "building room / rooms"
        public class BuildingSection : InterfaceKit
        {
            #region DECLARATIONS
            Timer                        BlinkOnOffTimer;
            Timer                        TimedFeatureActivate;
            Timer                        TimedActivateOutSequence;
            bool                         _enableBase;
            bool[]                       _StateDigitalOutput = new bool[GeneralConstants.NumberOfOutputsIOCard];
            bool                         _ButtonLightBarOn;
            int                          NextOutputIndex  = 0;
            bool                         _InhibitButtonComands;
            bool                         blink = false;
            bool                         _TimedOutput_FeatureActivate;
            protected bool               _PrimaryIOCardIsAttached;
            public delegate void TimedOutputFeature ( ElapsedEventArgs e );
            public event TimedOutputFeature TimedOutputFeature_;
            #endregion

            #region CONSTRUCTOR
            void Constructor( int serialnumber_standardio )
            {
                try
                {
                    base.open( serialnumber_standardio );
                    Console.WriteLine( TimeUtil.GetTimestamp() + " " + "Waiting for InterfaceKit with serial number " + serialnumber_standardio.ToString( ) +" to be attached..." );
                    base.waitForAttachment( Parameters.AttachWaitTime );
                    base.InputChange  += BuildingSection_InputChange;
                    base.OutputChange += BuildingSection_OutputChange;
                }
                catch( PhidgetException phiex_ )
                {
                    Services.TraceMessage( TimeUtil.GetTimestamp( ) + " " + phiex_.Message );
                    PHIDGET_EXCEPTION_OUT.PhidgetExceptionOutput( phiex_, InfoString.InfoPhidgetException );
                }

                if( base.Attached )
                {
                    Console.WriteLine( TimeUtil.GetTimestamp( ) + " " + "Attached IO Card   TYPE:" + base.Type.ToString( ) );
                    Console.WriteLine( TimeUtil.GetTimestamp( ) + " " + "Attached IO Card SERIAL:" + base.SerialNumber.ToString( ) );
                    _PrimaryIOCardIsAttached = true;
                }

            }

            public BuildingSection ( )
                : base( )
            {
                try
                {
                    base.open( );
                    Console.WriteLine( TimeUtil.GetTimestamp( ) + " " +  "Waiting for InterfaceKit to be attached..." );
                    base.waitForAttachment( Parameters.AttachWaitTime );
                    base.InputChange += BuildingSection_InputChange;
                    base.OutputChange += BuildingSection_OutputChange;
                }
                catch( PhidgetException phiex_ )
                {
                    Console.WriteLine( TimeUtil.GetTimestamp( ) + " " + "Attaching Phidget IO card failed!" );
                    PHIDGET_EXCEPTION_OUT.PhidgetExceptionOutput( phiex_, InfoString.InfoPhidgetException );
                    Services.TraceMessage( TimeUtil.GetTimestamp( ) + " " + phiex_.Message );
                    Console.WriteLine( );
                }

                if( base.Attached )
                {
                    Console.WriteLine( TimeUtil.GetTimestamp( ) + " " + "Attaching was successfull" );
                    Console.WriteLine( TimeUtil.GetTimestamp( ) + " " + "Attached IO Card   TYPE:" + base.Type.ToString( ) );
                    Console.WriteLine( TimeUtil.GetTimestamp( ) + " " + "Attached IO Card SERIAL:" + base.SerialNumber.ToString( ) );
                    _PrimaryIOCardIsAttached = true;
                }

                BlinkOnOffTimer = new Timer( );
                BlinkOnOffTimer.Elapsed += BlinkOnOffTimer_Elapsed;
                _ButtonLightBarOn = false;
                TimedFeatureActivate = new Timer( Parameters.TimeActiveateFeature );
                TimedFeatureActivate.Elapsed += TimedFeatureActivate_Elapsed;
                TurnAllLightsOff( );
                NextOutputIndex = 0;
                TimedActivateOutSequence = new Timer( Parameters.TimedActivateSequence );
                TimedActivateOutSequence.Elapsed += TimedActivateOutSequence_Elapsed;
                base.Detach += BuildingSection_Detach;
                base.Attach += BuildingSection_Attach;
            }
 
            public BuildingSection ( int serialnumber_standardio )
                : base( )
            {
                Constructor( serialnumber_standardio );
            }

            public BuildingSection( int serialnumber_standardio, bool enableBase )
                : base( )
            {
                Constructor( serialnumber_standardio );
                _enableBase = enableBase;
            }
            #endregion

            #region PROPERTIES

            public bool[] StateDigitalOutput 
            {
                 get
                 {
                     return ( _StateDigitalOutput );
                 }
            }

            public bool PrimaryIOCardIsAttached
            {
                get
                {
                    return( _PrimaryIOCardIsAttached );
                }

            }
 
            public bool ButtonLightBarOn
            {
                get { return _ButtonLightBarOn; }
                set { _ButtonLightBarOn = value; }
            }

            public bool InhibitButtonComands
            {
                set { _InhibitButtonComands = value; }
            }

            public bool TimedOutput_FeatureActivate
            {
                get
                {
                    return _TimedOutput_FeatureActivate;
                }
            }

            public bool AllLightsOn
            {
                set
                {
                    if( value )
                    {
                        TurnAllLightsOn( );
                    }
                    else
                    {
                        TurnAllLightsOff( );
                    }
                }
            }

            #endregion

            #region PUBLIC_METHODS

            public void StartBlink ( double intervall )
            {
                BlinkOnOffTimer.Interval = intervall;
                BlinkOnOffTimer.Start( );
            }

            public void StopBlink ( )
            {
                BlinkOnOffTimer.Stop( );
            }

            #endregion

            #region PROTOECTED_METHODS

            protected virtual void BuildingSection_OutputChange( object sender, OutputChangeEventArgs e )
            {
                _StateDigitalOutput[e.Index] =  e.Value ? true : false;
            }

            virtual protected void BuildingSection_InputChange ( object sender, InputChangeEventArgs e )
            {
                if( !_enableBase )
                    return;

                // rising edge
                if( e.Value == true )
                {
                    if( TimedFeatureActivate != null )
                    {
                        TimedFeatureActivate.Start( );
                        _TimedOutput_FeatureActivate = false;
                    }
                }
                else // falling edge
                {
                    if( TimedFeatureActivate != null )
                    {
                        TimedFeatureActivate.Stop( );
                    }
                }

                if( _InhibitButtonComands )
                {
                    return;
                }

                switch( e.Index )
                {
                    // turns all outputs ON
                    case HardwareIOAssignment.Input_ALL_ON:
                        if( e.Value == true )
                        {
                            if( !_ButtonLightBarOn )
                            {
                                AllLights( true );
                            }
                            else
                            {
                                AllLights( false );
                            }
                            _ButtonLightBarOn = !_ButtonLightBarOn;
                        }
                        break;
                    // every button tip turn next output on
                    case HardwareIOAssignment.Input_TipNext:
                        if( e.Value == true )
                        {
                            if( NextOutputIndex >= base.outputs.Count )
                            {
                                TurnAllLightsOff( );
                                NextOutputIndex = 0;
                                return;
                            }
                            base.outputs[NextOutputIndex++] = true;
                        }
                        break;
                }
            }

            protected void AllLights ( bool on )
            {
                if( base.outputs != null )
                {
                    for( int elements = 0; elements < base.outputs.Count; elements++ )
                    {
                        base.outputs[elements] = on;
                    }
                }
            }

            #endregion

            #region PRIVATE_MEHTODS

            void TimedActivateOutSequence_Elapsed ( object sender, ElapsedEventArgs e )
            {
            }

            void BlinkOnOffTimer_Elapsed ( object sender, ElapsedEventArgs e )
            {
                if( !blink )
                {
                    TurnAllLightsOn( );
                }
                else
                {
                    TurnAllLightsOff( );
                }
                blink = !blink;
            }

            void BuildingSection_Attach( object sender, AttachEventArgs e )
            {
                _PrimaryIOCardIsAttached = true;
            }

            void BuildingSection_Detach( object sender, DetachEventArgs e )
            {
                _PrimaryIOCardIsAttached = false;
            }

            void TurnAllLightsOn ( )
            {
                AllLights( true );
            }

            void TurnAllLightsOff ( )
            {
                AllLights( false );
            }

            void TimedFeatureActivate_Elapsed ( object sender, ElapsedEventArgs e )
            {
                _TimedOutput_FeatureActivate = true;
                if( TimedOutputFeature_ != null )
                {
                    TimedOutputFeature_( e );
                }
            }

            #endregion
        }
       
        // this timer class is also used for heater control - that can sometimes be confusing - TODO refactor or try to make it better understandable
        class LightControlTimer_
        {
            #region DECLARATIONS
            Timer DelayAllOnTimer                  = new Timer( );       // idea ist to push f.e a button for a certain time, after that all lights go on
            Timer SingleLightOffTimer              = new Timer();        // "single" group of devices can be turned off manually
            Timer AutomaticTurnSelectedLightsOffTimer;                   // used for a Light Grpoup - f.e. 1-4,   3-7, ..... aso.... 
            List<Timer> SelectedAutomaticLightOffTimerList  = new List<Timer>();  // turn a single selected device automatic off
            Dictionary<int,int> DigIndexDic                 = new Dictionary<int, int>();  // contains digital output index used for timer elapsed event handler
            int PrevSelectionIndex                       = 0;
            int IndTimList  = 0;
            bool InitSingleAutomaticTimerDone = false;
            
            public delegate void AllOn ( object sender );
            public event         AllOn AllOn_;
            public delegate void SingleOff ( object sender );     // "single" group of devices can be turned off manually
            public event         SingleOff SingleOff_;
            public delegate void AutomaticOff ( object sender );  // selected devices turn outomatic off with optional preselecting
            public event         AutomaticOff AutomaticOff_;
            public delegate void SingleDelayedIndexedAutomaticOff( object sender, int index );   // one device can be turned off 
            public event         SingleDelayedIndexedAutomaticOff ESingleDelayedIndexedOff_;
            #endregion

            #region CONSTRUCTOR
            public LightControlTimer_()
            {
            }

            public LightControlTimer_ ( double AllOnTime, double SingleOffTime )
            {
                DelayAllOnTimer.Elapsed                     += DelayAllOnTimer_Elapsed;
                SingleLightOffTimer.Elapsed                 += SingleLightOffTimer_Elapsed;
                DelayAllOnTimer.Interval                     = AllOnTime;
                SingleLightOffTimer.Interval                 = SingleOffTime;
            }

            public LightControlTimer_ ( double AllOnTime, double SingleOffTime, double AutomaticOffTime )
            {
                AutomaticTurnSelectedLightsOffTimer          = new Timer( );
                DelayAllOnTimer.Elapsed                     += DelayAllOnTimer_Elapsed;
                SingleLightOffTimer.Elapsed                 += SingleLightOffTimer_Elapsed;
                AutomaticTurnSelectedLightsOffTimer.Elapsed += AutomaticTurnSelectedLightsOffTimer_Elapsed;

                if( AllOnTime > 0 )
                {
                    DelayAllOnTimer.Interval = AllOnTime;
                }

                if( SingleOffTime > 0 )
                {
                    SingleLightOffTimer.Interval = SingleOffTime;
                }

                if( AutomaticOffTime > 0 )
                {
                    AutomaticTurnSelectedLightsOffTimer.Interval = AutomaticOffTime;
                }
            }
            #endregion

            #region PUBLIC_METHODS
            public void ReconfigAutomaticOffTimer( double time )
            {
                if( time > 0 )
                {
                    AutomaticTurnSelectedLightsOffTimer.Interval = time;
                    AutomaticTurnSelectedLightsOffTimer.Stop( );
                    AutomaticTurnSelectedLightsOffTimer.Start( );
                }
            }

            public void StartAllOnTimer( )
            {
                DelayAllOnTimer.Start( );
            }

            public void StopAllOnTimer( )
            {
                DelayAllOnTimer.Stop( );
            }
 
            public void StartAutomaticOfftimer( )
            {
                AutomaticTurnSelectedLightsOffTimer.Start( );
            }

            public void StopAutomaticOfftimer( )
            {
                AutomaticTurnSelectedLightsOffTimer.Stop( );
            }

            public void RestartAutomaticOfftimer( )
            {
                AutomaticTurnSelectedLightsOffTimer.Stop( );
                AutomaticTurnSelectedLightsOffTimer.Start( );
            }

            public void StartAllTimers( )
            {
                DelayAllOnTimer.Start( );
                SingleLightOffTimer.Start( );
            }

            public void StopAllTimers( )
            {
                DelayAllOnTimer.Stop( );
                SingleLightOffTimer.Stop( );
                if( AutomaticTurnSelectedLightsOffTimer != null )
                {
                    AutomaticTurnSelectedLightsOffTimer.Stop( );
                }
            }
            
            public void StartSingleAutomaticOffTimer( int IOIndex, double timeOff )
            {
                int listindex = 0;
                if( IOIndex < 0 )
                {
                    throw new Exception( EXCEPTIONMessages.IndexMustNotBeNegative );
                }
                if( SelectedAutomaticLightOffTimerList != null )
                {
                    // every change there is a new entry in the list and the index dictionary
                    if( (IOIndex != PrevSelectionIndex) || !InitSingleAutomaticTimerDone )
                    {
                        SelectedAutomaticLightOffTimerList.Add( new Timer( timeOff ) );
                        // from the list, store the index into a dictionary
                        DigIndexDic.Add( IOIndex, IndTimList );
                        IndTimList++;
                        PrevSelectionIndex = IOIndex;
                        InitSingleAutomaticTimerDone = true;
                    }

                    if( IndTimList <= SelectedAutomaticLightOffTimerList.Count &&
                        IndTimList > 0 )
                    {
                        // now get the proper index for the list out of the dictionary
                        DigIndexDic.TryGetValue( IOIndex, out listindex );

                        if( listindex >= 0 && listindex < SelectedAutomaticLightOffTimerList.Count )
                        {
                            SelectedAutomaticLightOffTimerList[listindex].Stop();
                            SelectedAutomaticLightOffTimerList[listindex].Elapsed += ( sender, args ) => LightControlTimer__Elapsed( sender, IOIndex );
                            SelectedAutomaticLightOffTimerList[listindex].Interval = timeOff;
                            SelectedAutomaticLightOffTimerList[listindex].Enabled = true;
                            SelectedAutomaticLightOffTimerList[listindex].Start();
                        }
                    }
                }
            }

            public void StopSingleAutomaticOffTimer( int Index )
            {
                if( Index < 0 )
                {
                    throw new Exception( EXCEPTIONMessages.IndexMustNotBeNegative );
                }
                if( SelectedAutomaticLightOffTimerList != null )
                {
                    if( Index < SelectedAutomaticLightOffTimerList.Count )
                    {
                        SelectedAutomaticLightOffTimerList[Index].Stop();
                    }
                }
            }

            void LightControlTimer__Elapsed( object sender, int ioindex )
            {
                int listindex = 0;
                if( ESingleDelayedIndexedOff_ != null )
                {
                    ESingleDelayedIndexedOff_( sender, ioindex );
                }
                DigIndexDic.TryGetValue( ioindex, out listindex );
                if( listindex >= 0 && listindex < SelectedAutomaticLightOffTimerList.Count )
                {
                    SelectedAutomaticLightOffTimerList[listindex].Stop();
                }
            }
            #endregion

            #region PRIVATEMETHODS
            private void StartSingleLightOffTimer( )
            {
                SingleLightOffTimer.Start( );
            }

            private void StopSingleLightOffTimer()
            {
                SingleLightOffTimer.Stop( );
            }

            void SingleLightOffTimer_Elapsed( object sender, ElapsedEventArgs e )
            {
                SingleLightOffTimer.Stop( );
                if( SingleOff_ != null )
                {
                    SingleOff_( this );
                }
            }

            void DelayAllOnTimer_Elapsed( object sender, ElapsedEventArgs e )
            {
                DelayAllOnTimer.Stop( );
                if( AllOn_ != null )
                {
                    AllOn_( this );
                }
            }                                          

            void AutomaticTurnSelectedLightsOffTimer_Elapsed( object sender, ElapsedEventArgs e )
            {
                if( AutomaticOff_ != null )
                {
                    AutomaticOff_( this );
                }
            }
            #endregion
        }

        class LightControl : LightControlTimer_
        {
            #region DECLARATIONS
            bool                         EnableStepLight;
            bool                         SelectDevicesOff;
            int                          previousIndex;
            protected bool               _AllRoomLightsAreOn;
            bool                         _AllRoomLightsAreOff;
            protected bool               SomeRoomLightsAreOn;
            int[]                        _values;
            protected bool[]             SelectedPermanentOnDevice;
            bool                         InitPermanentDeviceSelection;
            int                          actualindex        = 0;
            public const int             MaxNumberOfOutputs = 16;  // 0...15 this amount ist limited of the used IO card
            int                          _startindex;
            int                          _lastindex;
            bool                         LightsOffProceeded = false;
            bool                         _LightControlSingleOffDone;
            bool                         _PrimaryIOCardIsAttached;
            Timer                        AllOutputsOffTimer;
            Timer AliveTimer           = new Timer( Parameters.TimeIntervallAlive );
            Timer FinalAllAutomaticOff = new Timer();                                              // all configured devices off
            bool[] _StateDigitalOutput = new bool[GeneralConstants.NumberOfOutputsIOCard];         // fill state from outside
            bool[] _ToggleOutput       = new bool[GeneralConstants.NumberOfOutputsIOCard];
            bool[] _ShowStateDigitalOutput = new bool[GeneralConstants.NumberOfOutputsIOCard];     // show internal state - reason is to ease testing
            protected                    InterfaceKitDigitalOutputCollection outputs_;

            public delegate void AllSelectedDevicesOff( object sender, int firstdevice, int lastdevice );
            public event AllSelectedDevicesOff AllSelectedDevicesOff_;

            public delegate void Reset( object sender );
            public event Reset EReset;
            #endregion

            #region CONSTRUCTOR

            public LightControl( double AllOnTime, double SingleOffTime, ref InterfaceKitDigitalOutputCollection outputs )
                : base( AllOnTime, SingleOffTime )
            {
                outputs_ = outputs;
            }

            public LightControl( double AllOnTime, double SingleOffTime, int startindex, int lastindex, ref InterfaceKitDigitalOutputCollection outputs )
                : base( AllOnTime, SingleOffTime )
            {
                base.AllOn_     += LightControl_AllOn_;
                base.SingleOff_ += LightControl_SingleOff_;
                _startindex      = startindex;
                _lastindex       = lastindex;
                outputs_         = outputs;
            }

            public LightControl( double AllOnTime, double SingleOffTime, double AutomaticOffTime, int startindex, int lastindex, ref InterfaceKitDigitalOutputCollection outputs )
                : base( AllOnTime, SingleOffTime, AutomaticOffTime )
            {
                base.AllOn_        += LightControl_AllOn_;
                base.SingleOff_    += LightControl_SingleOff_;
                base.AutomaticOff_ += LightControl_AutomaticOff_;
                AliveTimer.Elapsed += AliveTimer_Elapsed;
                _startindex         = startindex;
                _lastindex          = lastindex;
                outputs_            = outputs;
                SelectedPermanentOnDevice = new bool[GeneralConstants.NumberOfOutputsIOCard];
                InitPermanentDeviceSelection = false;
            }

            public LightControl( double AllOnTime, double AllOutputsOffTime, double SingleOffTime, double AutomaticOffTime, int startindex, int lastindex, ref InterfaceKitDigitalOutputCollection outputs )
                : base( AllOnTime, SingleOffTime, AutomaticOffTime )
            {
                 Constructor(  AllOutputsOffTime,  SingleOffTime,  AutomaticOffTime,  startindex,  lastindex, ref  outputs );
            }

            // extended functionality - all devices off - even the desired remaining ones after timer elapsed
            public LightControl( double AllOnTime, double AllOutputsOffTime, double SingleOffTime, double AutomaticOffTime, double AllAutomaticOffTime, 
                                 int startindex, int lastindex, ref InterfaceKitDigitalOutputCollection outputs )
                : base( AllOnTime, SingleOffTime, AutomaticOffTime )
            {
                Constructor( AllOutputsOffTime, SingleOffTime, AutomaticOffTime, startindex, lastindex, ref  outputs );
                FinalAllAutomaticOff.Interval   = AllAutomaticOffTime;
                FinalAllAutomaticOff.Elapsed   += FinalAllAutomaticOff_Elapsed;
                base.ESingleDelayedIndexedOff_ += LightControl_ESingleDelayedIndexedOff_;
            }
            #endregion

            #region PROPERTIES

            public bool IsPrimaryIOCardAttached
            {
                set
                {
                    _PrimaryIOCardIsAttached =  value;
                }
            }

            public int ActualLightIndexSingleStep
            {
                get
                {
                    return ( actualindex );
                }
            }

            public bool AllRoomLightsAreOn
            {
                get
                {
                    return ( _AllRoomLightsAreOn );
                }
            }

            public bool AllRoomLightsAreOff
            {
                get
                {
                    return ( _AllRoomLightsAreOff );
                }
            }

            // fill state from outside
            public bool[] StateDigitalOutput
            {
                set
                {
                    _StateDigitalOutput = value;
                }
            }

            // representation of digital outpus as BOOLEANS - purpose is to ease testing
            public bool[] ShowStateDigitalOutput
            {
                get
                {
                    return ( _ShowStateDigitalOutput );
                }
            }

    
            #endregion

            #region PUBLIC_METHODS
            public void FinalAutomaticOff( InputChangeEventArgs e )
            {
                if( e.Value == false )
                {
                    FinalAllAutomaticOff.Stop();
                    FinalAllAutomaticOff.Start();
                }
            }

            public void TurnSingleLight( int index, bool value )
            {
                if( outputs_ != null )
                {
                    if( (index < outputs_.Count) && index > 0 )
                    {
                        outputs_[index] = value;
                    }
                }
            }

            public void ToggleSingleLight( int index )
            {
                if( !_ToggleOutput[index] )
                {
                    TurnSingleLight( index, GeneralConstants.ON );
                }
                else
                {
                    TurnSingleLight( index, GeneralConstants.OFF );
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
                outputs_[CommonRoomIOAssignment.indOutputIsAlive] = false;
            }

            public void ResetLightControl( )
            {
                _LightControlSingleOffDone = false;
            }

            public void StartWaitForAllOff( )
            {
                if( AllOutputsOffTimer != null )
                {
                    AllOutputsOffTimer.Start( );
                }
            }

            public void StopWaitForAllOff( )
            {
                if( AllOutputsOffTimer != null )
                {
                    AllOutputsOffTimer.Stop( );
                }
            }

            public void MakeStep( InputChangeEventArgs e )
            {
                makestep_( e.Value );
            }

            public void MakeStep( bool cmd )
            {
                makestep_( cmd );
            }

            public new void AutomaticOff ( InputChangeEventArgs e )
            {
                SelectDevicesOff = false;
                StartAutomaticOff( e );
                if( LightsOffProceeded  )
                {
                   CancelAutomaticOff();
                   LightsOffProceeded = false;
                }
                RestartAutomaticOff( e );
            }

            public new void AutomaticOff( bool value )
            {
                SelectDevicesOff = false;
                StartAutomaticOff( value );
                if( LightsOffProceeded )
                {
                    CancelAutomaticOff( );
                    LightsOffProceeded = false;
                }
                RestartAutomaticOff( value );
            }
 
            // starts auto OFF once until finished
            public void AutomaticOffAllLights( InputChangeEventArgs e )
            {
                SelectDevicesOff = false;
                StartAutomaticOff( e );
            }

            // restarts auto off each falling edge 
            public void AutomaticOffRestartAll( InputChangeEventArgs e )
            {
                SelectDevicesOff = false;
                RestartAutomaticOffFallingEdge( e );
            }

            // automatic off triggered via digital inputs with the option that devices which want to be turned off can be selected
            public void AutomaticOffSelect( InputChangeEventArgs e, params int[] values )
            {
                // no need to execute when lights are already off
                if( AreAllLightsOff( _startindex, _lastindex ) )
                {
                    return;
                }
                _values                   = values;
                SelectDevicesOff          = true;
                bool readyForAutomaticOff = false;
                // feeds a array with the selected permanent on devices - this devices will stay on when 
                // others were comanded for turning off
                if( !InitPermanentDeviceSelection )
                {
                    for( int i = 0; i <= _lastindex; i++ )
                    {
                        SelectedPermanentOnDevice[i] = true;
                    }
                    for( int i = 0; i < _values.Length; i++ )
                    {
                        if( _values[i] < GeneralConstants.NumberOfOutputsIOCard )
                        {
                            SelectedPermanentOnDevice[_values[i]] = false;
                        }
                    }
                    InitPermanentDeviceSelection = true;
                }

                for( int i = 0; i <= _lastindex; i++ )
                {
                    if( !SelectedPermanentOnDevice[i] && outputs_[i] )
                    {
                        readyForAutomaticOff = true;
                        break;
                    }
                }

                if( readyForAutomaticOff )
                {
                    StartAutomaticOff( e );
                }

                if( LightsOffProceeded )
                {
                    CancelAutomaticOff( );
                    LightsOffProceeded = false;
                }

                RestartAutomaticOff( e );
            }

            public void AutomaticOffSelect( bool command, params int[] values )
            {
                // no need to execute when lights are already off
                if( AreAllLightsOff( _startindex, _lastindex ) )
                {
                    return;
                }
                _values = values;
                SelectDevicesOff = true;
                bool readyForAutomaticOff = false;
                // feeds a array with the selected permanent on devices - this devices will stay on when 
                // others were comanded for turning off
                if( !InitPermanentDeviceSelection )
                {
                    for( int i = 0; i <= _lastindex; i++ )
                    {
                        SelectedPermanentOnDevice[i] = true;
                    }
                    for( int i = 0; i < _values.Length; i++ )
                    {
                        if( _values[i] < GeneralConstants.NumberOfOutputsIOCard )
                        {
                            SelectedPermanentOnDevice[_values[i]] = false;
                        }
                    }
                    InitPermanentDeviceSelection = true;
                }

                for( int i = 0; i <= _lastindex; i++ )
                {
                    if( !SelectedPermanentOnDevice[i] && outputs_[i] )
                    {
                        readyForAutomaticOff = true;
                        break;
                    }
                }

                if( readyForAutomaticOff )
                {
                    StartAutomaticOff( command );
                }

                if( LightsOffProceeded )
                {
                    CancelAutomaticOff();
                    LightsOffProceeded = false;
                }

                RestartAutomaticOff( command );
            }
            #endregion
 
            #region EVENTHANDLERS
            void FinalAllAutomaticOff_Elapsed( object sender, ElapsedEventArgs e )
            {
                TurnAllLightsOff( _startindex, _lastindex );
            }

            void AllOutputsOffTimer_Elapsed( object sender, ElapsedEventArgs e )
            {
                AllOutputsOffTimer.Stop( );
                AllOutputsOff( );
                if( EReset != null )
                {
                    EReset( this );
                }
            }

            void AliveTimer_Elapsed( object sender, ElapsedEventArgs e )
            {
                 if( outputs_ != null && _PrimaryIOCardIsAttached )
                 {
                     try
                     {
                         outputs_[CommonRoomIOAssignment.indOutputIsAlive] = !outputs_[CommonRoomIOAssignment.indOutputIsAlive];
                     }
                     catch
                     {
                         // so far idle until device is attached again
                     }
                 }
            }

            void LightControl_ESingleDelayedIndexedOff_( object sender, int index )
            {
                ControlOutput( index, false );
            }

            void LightControl_SingleOff_( object sender )
            {
                // no need for turning off when light is already off
                if( _AllRoomLightsAreOff )
                {
                    return;
                }
                _LightControlSingleOffDone = true;
                _AllRoomLightsAreOn = false;
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
                _AllRoomLightsAreOn = false;
                if( SelectDevicesOff )
                {
                    TurnSomeLightsOff( _values );
                }
                else
                {
                   TurnAllLightsOff( _startindex, _lastindex );
                }
            }            
            
            void LightControl_AllOn_( object sender )
            {
                actualindex = 0;
                AllLightsOn( _startindex, _lastindex );
                _AllRoomLightsAreOn = true;
            }
            #endregion

            #region PRIVATE_METHODS
            void AllOutputsOff( )
            {
                for( int i = 0; i < outputs_.Count; i++ )
                {
                    outputs_[i] = false;
                }
            }

            bool AreAllLightsOff( int startindex, int lastindex )
            {
                // check for all devices (lights) off
                for( int i = _startindex; i <= _lastindex; i++ )
                {
                    if( _StateDigitalOutput[i] == true )  
                    {                                                        
                        return false;
                    }
                }
                return true;
            }

            void makestep_( bool cmd )
            {
                // PUSH BUTTON 
                if( cmd == true )
                {
                    base.StartAllTimers( );
                    if( SomeRoomLightsAreOn )
                    {
                        TurnAllLightsOff( _startindex, _lastindex );
                        SomeRoomLightsAreOn = false;
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
                    if( _AllRoomLightsAreOn )
                    {
                        TurnAllLightsOff( _startindex, _lastindex );
                        EnableStepLight = false;
                        _AllRoomLightsAreOn = false;
                    }
                    else
                    {
                        EnableStepLight = true;
                    }
                }
                // PULL BUTTON
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
                // check for all devices (lights) off
                _AllRoomLightsAreOff = AreAllLightsOff( _startindex, _lastindex);
            }

            void StartAutomaticOff( bool value )
            {
                if( value == false )
                {
                    base.StartAutomaticOfftimer( );
                }
            }

            void StartAutomaticOff( InputChangeEventArgs e )
            {
                if( e.Value == false )
                {
                    base.StartAutomaticOfftimer( );
                }
            }
 
            void CancelAutomaticOff(  )
            {
                 base.StopAutomaticOfftimer( );
            }

            void RestartAutomaticOff( InputChangeEventArgs e )
            {
                if( e.Value == true )
                {
                    base.RestartAutomaticOfftimer( );
                }
            }

            void RestartAutomaticOff( bool value )
            {
                if( value == true )
                {
                    base.RestartAutomaticOfftimer( );
                }
            }

            void RestartAutomaticOffFallingEdge( InputChangeEventArgs e )
            {
                if( e.Value == false )
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

                SomeRoomLightsAreOn = true;
                LightsOffProceeded = true;
                for( int i = 0; i < value.Length; i++ )
                {
                    if( value[i] >= outputs_.Count )
                    {
                        return;
                    }
                    
                    outputs_[value[i]] = false;
                    
                    if( value.Length <= outputs_.Count )
                    {
                        continue;
                    }
                    else
                        break;
                }
            }

            void ControlOutput( int index, bool value )
            {
                if( (index < GeneralConstants.NumberOfInputsIOCard) && (index > 0) )
                {
                    outputs_[index] = value;
                }
            }

            void Constructor( double AllOutputsOffTime, double SingleOffTime, double AutomaticOffTime, int startindex, int lastindex, ref InterfaceKitDigitalOutputCollection outputs )
            {
                base.AllOn_ += LightControl_AllOn_;
                base.SingleOff_ += LightControl_SingleOff_;
                base.AutomaticOff_ += LightControl_AutomaticOff_;
                base.ESingleDelayedIndexedOff_ += LightControl_ESingleDelayedIndexedOff_;
                AliveTimer.Elapsed += AliveTimer_Elapsed;
                _startindex = startindex;
                _lastindex = lastindex;
                outputs_ = outputs;
                SelectedPermanentOnDevice = new bool[GeneralConstants.NumberOfOutputsIOCard];
                InitPermanentDeviceSelection = false;
                AllOutputsOffTimer = new Timer( AllOutputsOffTime );
                AllOutputsOffTimer.Elapsed += AllOutputsOffTimer_Elapsed;
            }
            
            #endregion

            #region PROTECTED_METHODS
            protected void TurnOnWithDelayedOffSingleLight( InputChangeEventArgs e, double delayedofftime, int index )
            {
                if( e.Value == false )
                {
                    ControlOutput( index, true );
                    base.StartSingleAutomaticOffTimer( index, delayedofftime );
                }
            }

            protected void TurnOnWithDelayedOffSingleLight( InputChangeEventArgs e, bool enable, double delayedofftime, int index )
            {
                if( enable == false )
                {
                    return;
                }
                TurnOnWithDelayedOffSingleLight(  e, delayedofftime,  index );
            }
             
            protected bool TurnAllLightsOff( int startindex, int lastindex )
            {
                for( int ind = startindex; ind <= lastindex; ind++ )
                {
                    outputs_[ind] = false;
                }
                LightsOffProceeded = true;
                // fire event which tells that all lights are off
                if( AllSelectedDevicesOff_ != null )
                {
                    AllSelectedDevicesOff_( this, startindex, _lastindex );
                }
                return ( true );
            }

            protected bool AllLightsOn( int startindex, int lastindex )
            {
                for( int ind = startindex; ind <= lastindex; ind++ )
                {
                    outputs_[ind] = true;
                }
                return ( true );
            }

            virtual protected void StepLight( int startindex, ref int index, int _indexlastdevice )
            {
                int _index = startindex + index;

                if( _index <= _indexlastdevice )
                {
                    outputs_[_index] = true;
                    if( index > 0 )
                    {
                        outputs_[_index - 1] = false;
                    }
                    index++;
                }
                else
                {
                    outputs_[_index - 1] = false;
                    index = 0;
                }
            }
            #endregion
        }

        // lightcontrol refactored - for easier unit tests
        class LightControlNG : LightControlTimer_
        {
            #region DECLARATIONS
            bool                         EnableStepLight;
            bool                         SelectDevicesOff;
            int                          previousIndex;
            protected bool               _AllRoomLightsAreOn;
            bool                         _AllRoomLightsAreOff;
            protected bool               SomeRoomLightsAreOn;
            int[]                        _values;
            protected bool[]             SelectedPermanentOnDevice;
            bool                         InitPermanentDeviceSelection;
            int                          actualindex        = 0;
            public const int             MaxNumberOfOutputs = 16;  // 0...15 this amount ist limited of the used IO card
            int                          _startindex;
            int                          _lastindex;
            bool                         LightsOffProceeded = false;
            bool                         _LightControlSingleOffDone;
            bool                         _PrimaryIOCardIsAttached;
            Timer                        AllOutputsOffTimer;
            Timer AliveTimer                         = new Timer( Parameters.TimeIntervallAlive );
            Timer FinalAllAutomaticOff               = new Timer();                                              // all configured devices off
            bool[] _StateDigitalOutput               = new bool[GeneralConstants.NumberOfOutputsIOCard];         // fill state from outside
            bool[] _ToggleOutput                     = new bool[GeneralConstants.NumberOfOutputsIOCard];
            bool[] _ShowStateDigitalOutput           = new bool[GeneralConstants.NumberOfOutputsIOCard];     // show internal state - reason is to ease testing
            protected bool[] _DigitalOutput          = new bool[GeneralConstants.NumberOfOutputsIOCard];     // 
            protected                    InterfaceKitDigitalOutputCollection outputs_;

            public delegate void    AllSelectedDevicesOff( object sender, int firstdevice, int lastdevice );
            public event            AllSelectedDevicesOff AllSelectedDevicesOff_;

            protected delegate void UpdateOutputs( object sender, bool[] _DigOut  );
            protected event         UpdateOutputs EUpdateOutputs;

            public delegate void    Reset( object sender );
            public event            Reset EReset;
            #endregion

            #region CONSTRUCTOR

            public LightControlNG( double AllOnTime, double SingleOffTime, ref InterfaceKitDigitalOutputCollection outputs )
                : base( AllOnTime, SingleOffTime )
            {
                outputs_ = outputs;
                EUpdateOutputs += LightControlNG_EUpdateOutputs;
            }

            public LightControlNG( double AllOnTime, double SingleOffTime, int startindex, int lastindex, ref InterfaceKitDigitalOutputCollection outputs )
                : base( AllOnTime, SingleOffTime )
            {
                base.AllOn_ += LightControl_AllOn_;
                base.SingleOff_ += LightControl_SingleOff_;
                _startindex = startindex;
                _lastindex = lastindex;
                outputs_ = outputs;
                EUpdateOutputs += LightControlNG_EUpdateOutputs;
            }

            public LightControlNG( double AllOnTime, double SingleOffTime, double AutomaticOffTime, int startindex, int lastindex, ref InterfaceKitDigitalOutputCollection outputs )
                : base( AllOnTime, SingleOffTime, AutomaticOffTime )
            {
                base.AllOn_ += LightControl_AllOn_;
                base.SingleOff_ += LightControl_SingleOff_;
                base.AutomaticOff_ += LightControl_AutomaticOff_;
                AliveTimer.Elapsed += AliveTimer_Elapsed;
                _startindex = startindex;
                _lastindex = lastindex;
                outputs_ = outputs;
                SelectedPermanentOnDevice = new bool[GeneralConstants.NumberOfOutputsIOCard];
                InitPermanentDeviceSelection = false;
                EUpdateOutputs += LightControlNG_EUpdateOutputs;
            }

            public LightControlNG( double AllOnTime, 
                                   double AllOutputsOffTime, 
                                   double SingleOffTime, 
                                   double AutomaticOffTime, 
                                   int startindex, 
                                   int lastindex, 
                                   ref InterfaceKitDigitalOutputCollection outputs )
                : base( AllOnTime, SingleOffTime, AutomaticOffTime )
            {
                Constructor( AllOutputsOffTime, SingleOffTime, AutomaticOffTime, startindex, lastindex, ref  outputs );
                EUpdateOutputs += LightControlNG_EUpdateOutputs;
            }

   
            // extended functionality - all devices off - even the desired remaining ones after timer elapsed
            public LightControlNG( double AllOnTime,
                                   double AllOutputsOffTime, 
                                   double SingleOffTime, 
                                   double AutomaticOffTime, 
                                   double AllFinalOffTime,
                                   int startindex, 
                                   int lastindex,
                                   ref InterfaceKitDigitalOutputCollection outputs )
                : base( AllOnTime, SingleOffTime, AutomaticOffTime )
            {
                Constructor( AllOutputsOffTime, SingleOffTime, AutomaticOffTime, startindex, lastindex, ref  outputs );
                FinalAllAutomaticOff.Interval         = AllFinalOffTime;
                FinalAllAutomaticOff.Elapsed         += FinalAllAutomaticOff_Elapsed;
                base.ESingleDelayedIndexedOff_       += LightControl_ESingleDelayedIndexedOff_;
                EUpdateOutputs                       += LightControlNG_EUpdateOutputs;
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

            public int ActualLightIndexSingleStep
            {
                get
                {
                    return ( actualindex );
                }
            }

            public bool AllRoomLightsAreOn
            {
                get
                {
                    return ( _AllRoomLightsAreOn );
                }
            }

            public bool AllRoomLightsAreOff
            {
                get
                {
                    return ( _AllRoomLightsAreOff );
                }
            }

            // fill state from outside
            public bool[] StateDigitalOutput
            {
                set
                {
                    _StateDigitalOutput = value;
                }
            }

            // representation of digital outpus as BOOLEANS - purpose is to ease testing
            public bool[] ShowStateDigitalOutput
            {
                get
                {
                    return ( _ShowStateDigitalOutput );
                }
            }


            #endregion

            #region PUBLIC_METHODS
            public void FinalAutomaticOff( InputChangeEventArgs e )
            {
                if( _AllRoomLightsAreOff )
                {
                    return;
                }

                if( e.Value == false )
                {
                    FinalAllAutomaticOff.Stop();
                    FinalAllAutomaticOff.Start();
                }
            }

            public void FinalAutomaticOff( bool command )
            {
                if( _AllRoomLightsAreOff )
                {
                    return;
                }

                if( command == false )
                {
                    FinalAllAutomaticOff.Enabled = true;
                    FinalAllAutomaticOff.Stop();
                    FinalAllAutomaticOff.Start();
                }
            }

            public void TurnSingleLight( int index, bool value )
            {
                if( (_DigitalOutput != null) && (index < _DigitalOutput.Length) )
                {
                    _DigitalOutput[index] = value;
                }

                if( EUpdateOutputs != null )
                {
                    EUpdateOutputs( this, _DigitalOutput );
                }
            }

            public void ToggleSingleLight( int index )
            {
                if( !_ToggleOutput[index] )
                {
                    TurnSingleLight( index, GeneralConstants.ON );
                }
                else
                {
                    TurnSingleLight( index, GeneralConstants.OFF );
                }
                _ToggleOutput[index] = !_ToggleOutput[index];
            }

            public void StartAliveSignal()
            {
                AliveTimer.Start();
            }

            public void StopAliveSignal()
            {
                AliveTimer.Stop();
                _DigitalOutput[CommonRoomIOAssignment.indOutputIsAlive] = false;
                if( EUpdateOutputs != null )
                {
                    EUpdateOutputs( this, _DigitalOutput );
                }
            }

            public void ResetLightControl()
            {
                _LightControlSingleOffDone = false;
            }

            public void StartWaitForAllOff()
            {
                if( AllOutputsOffTimer != null )
                {
                    AllOutputsOffTimer.Start();
                }
            }

            public void StopWaitForAllOff()
            {
                if( AllOutputsOffTimer != null )
                {
                    AllOutputsOffTimer.Stop();
                }
            }

            public void MakeStep( InputChangeEventArgs e )
            {
                makestep_( e.Value );
            }

            public void MakeStep( bool cmd )
            {
                makestep_( cmd );
            }

            public new void AutomaticOff( InputChangeEventArgs e )
            {
                SelectDevicesOff = false;
                StartAutomaticOff( e );
                if( LightsOffProceeded )
                {
                    CancelAutomaticOff();
                    LightsOffProceeded = false;
                }
                RestartAutomaticOff( e );
            }

            public new void AutomaticOff( bool value )
            {
                SelectDevicesOff = false;
                StartAutomaticOff( value );
                if( LightsOffProceeded )
                {
                    CancelAutomaticOff();
                    LightsOffProceeded = false;
                }
                RestartAutomaticOff( value );
            }

            public void AutomaticOffAllLights( InputChangeEventArgs e )
            {
                SelectDevicesOff = false;
                StartAutomaticOff( e );
                if( _AllRoomLightsAreOff )
                {
                    CancelAutomaticOff();
                }
            }

            public void AutomaticOffAllLights( )
            {
                SelectDevicesOff = false;
                StartAutomaticOff( );
                if( _AllRoomLightsAreOff )
                {
                    CancelAutomaticOff();
                }
            }

            // restarts auto off each falling edge 
            public void AutomaticOffRestartAll( InputChangeEventArgs e )
            {
                SelectDevicesOff = false;
                RestartAutomaticOffFallingEdge( e );
            }

            public void AutomaticOffSelect( InputChangeEventArgs e, params int[] values )
            {
                // no need to execute when lights are already off
                if( AreAllLightsOff( _startindex, _lastindex ) )
                {
                    return;
                }
                _values = values;
                SelectDevicesOff = true;
                bool readyForAutomaticOff = false;
                // feeds a array with the selected permanent on devices - this devices will stay on when 
                // others were comanded for turning off
                if( !InitPermanentDeviceSelection )
                {
                    for( int i = 0; i <= _lastindex; i++ )
                    {
                        SelectedPermanentOnDevice[i] = true;
                    }
                    for( int i = 0; i < _values.Length; i++ )
                    {
                        if( _values[i] < GeneralConstants.NumberOfOutputsIOCard )
                        {
                            SelectedPermanentOnDevice[_values[i]] = false;
                        }
                    }
                    InitPermanentDeviceSelection = true;
                }

                for( int i = 0; i <= _lastindex; i++ )
                {
                    if( !SelectedPermanentOnDevice[i] && outputs_[i] )
                    {
                        readyForAutomaticOff = true;
                        break;
                    }
                }

                if( readyForAutomaticOff )
                {
                    StartAutomaticOff( e );
                }

                if( LightsOffProceeded )
                {
                    CancelAutomaticOff();
                    LightsOffProceeded = false;
                }

                RestartAutomaticOff( e );
            }

            public void AutomaticOffSelect( bool command, params int[] values )
            {
                // no need to execute when lights are already off
                if( AreAllLightsOff( _startindex, _lastindex ) )
                {
                    return;
                }
                _values = values;
                SelectDevicesOff = true;
                bool readyForAutomaticOff = false;
                // feeds a array with the selected permanent on devices - this devices will stay on when 
                // others were comanded for turning off
                if( !InitPermanentDeviceSelection )
                {
                    for( int i = 0; i <= _lastindex; i++ )
                    {
                        SelectedPermanentOnDevice[i] = true;
                    }
                    for( int i = 0; i < _values.Length; i++ )
                    {
                        if( _values[i] < GeneralConstants.NumberOfOutputsIOCard )
                        {
                            SelectedPermanentOnDevice[_values[i]] = false;
                        }
                    }
                    InitPermanentDeviceSelection = true;
                }

                for( int i = 0; i <= _lastindex; i++ )
                {
                    if( !SelectedPermanentOnDevice[i] && _ShowStateDigitalOutput[i] )
                    {
                        readyForAutomaticOff = true;
                        break;
                    }
                }

                if( readyForAutomaticOff )
                {
                    StartAutomaticOff( command );
                }

                if( LightsOffProceeded )
                {
                    CancelAutomaticOff();
                    LightsOffProceeded = false;
                }

                RestartAutomaticOff( command );
            }
            #endregion

            #region EVENTHANDLERS
            // direct acess to output reference
            void LightControlNG_EUpdateOutputs( object sender, bool[] _DigOut )
            {
                DoUpDateIO( _DigOut );
            }

            void FinalAllAutomaticOff_Elapsed( object sender, ElapsedEventArgs e )
            {
                TurnAllLightsOff( _startindex, _lastindex );
            }

            void AllOutputsOffTimer_Elapsed( object sender, ElapsedEventArgs e )
            {
                AllOutputsOffTimer.Stop();
                AllOutputsOff();
                if( EReset != null )
                {
                    EReset( this );
                }
            }

            void AliveTimer_Elapsed( object sender, ElapsedEventArgs e )
            {
                if( outputs_ != null && _PrimaryIOCardIsAttached )
                {
                    try
                    {
                        _DigitalOutput[CommonRoomIOAssignment.indOutputIsAlive] = !_DigitalOutput[CommonRoomIOAssignment.indOutputIsAlive];
                        if( EUpdateOutputs != null )
                        {
                            EUpdateOutputs( this, _DigitalOutput );
                        }
                    }
                    catch
                    {
                        // so far idle until device is attached again
                    }
                }
            }

            void LightControl_ESingleDelayedIndexedOff_( object sender, int index )
            {
                ControlOutput( index, false );
            }

            void LightControl_SingleOff_( object sender )
            {
                // no need for turning off when light is already off
                if( _AllRoomLightsAreOff )
                {
                    return;
                }
                _LightControlSingleOffDone = true;
                _AllRoomLightsAreOn = false;
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
                _AllRoomLightsAreOn = false;
                if( SelectDevicesOff )
                {
                    TurnSomeLightsOff( _values );
                }
                else
                {
                    TurnAllLightsOff( _startindex, _lastindex );
                }
            }

            void LightControl_AllOn_( object sender )
            {
                actualindex = 0;
                AllLightsOn( _startindex, _lastindex );
                _AllRoomLightsAreOn = true;
            }
            #endregion

            #region PRIVATE_METHODS
            void AllOutputsOff()
            {
                for( int i = 0; i < _DigitalOutput.Length; i++ )
                {
                    _DigitalOutput[i] = false;
                }
                if( EUpdateOutputs != null )
                {
                    EUpdateOutputs( this, _DigitalOutput );
                }
            }

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
                // PUSH BUTTON 
                if( cmd == true )
                {
                    base.StartAllTimers();
                    if( SomeRoomLightsAreOn )
                    {
                        TurnAllLightsOff( _startindex, _lastindex );
                        SomeRoomLightsAreOn = false;
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
                    if( _AllRoomLightsAreOn )
                    {
                        TurnAllLightsOff( _startindex, _lastindex );
                        EnableStepLight = false;
                        _AllRoomLightsAreOn = false;
                    }
                    else
                    {
                        EnableStepLight = true;
                    }
                }
                // PULL BUTTON
                else
                {
                    base.StopAllTimers();
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
                // check for all devices (lights) off
                _AllRoomLightsAreOff = AreAllLightsOff( _startindex, _lastindex );
            }

            void StartAutomaticOff( bool value )
            {
                if( value == false )
                {
                    base.StartAutomaticOfftimer();
                }
            }

            void StartAutomaticOff( )
            {
                 base.StartAutomaticOfftimer();
            }

            void StartAutomaticOff( InputChangeEventArgs e )
            {
                if( e.Value == false )
                {
                    base.StartAutomaticOfftimer();
                }
            }

            void CancelAutomaticOff()
            {
                base.StopAutomaticOfftimer();
            }

            void RestartAutomaticOff( InputChangeEventArgs e )
            {
                if( e.Value == true )
                {
                    base.RestartAutomaticOfftimer();
                }
            }

            void RestartAutomaticOff( bool value )
            {
                if( value == true )
                {
                    base.RestartAutomaticOfftimer();
                }
            }

            void RestartAutomaticOffFallingEdge( InputChangeEventArgs e )
            {
                if( e.Value == false )
                {
                    base.RestartAutomaticOfftimer();
                }
            }

            void TurnSomeLightsOff( int[] value )
            {
                if( value == null )
                {
                    return;
                }

                SomeRoomLightsAreOn = true;
                LightsOffProceeded = true;
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
                        break;
                }

                if( EUpdateOutputs != null )
                {
                    EUpdateOutputs( this, _DigitalOutput );
                }
            }

            void ControlOutput( int index, bool value )
            {
                if( ( index < GeneralConstants.NumberOfInputsIOCard ) && ( index > 0 ) )
                {
                    _DigitalOutput[index] = value;
                    if( EUpdateOutputs != null )
                    {
                        EUpdateOutputs( this, _DigitalOutput );
                    }
                }
            }

            void Constructor( double AllOutputsOffTime, double SingleOffTime, double AutomaticOffTime, int startindex, int lastindex, ref InterfaceKitDigitalOutputCollection outputs )
            {
                base.AllOn_                      += LightControl_AllOn_;
                base.SingleOff_                  += LightControl_SingleOff_;
                base.AutomaticOff_               += LightControl_AutomaticOff_;
                base.ESingleDelayedIndexedOff_   += LightControl_ESingleDelayedIndexedOff_;
                AliveTimer.Elapsed               += AliveTimer_Elapsed;
                _startindex                       = startindex;
                _lastindex                        = lastindex;
                outputs_                          = outputs;
                SelectedPermanentOnDevice    = new bool[GeneralConstants.NumberOfOutputsIOCard];
                InitPermanentDeviceSelection = false;
                AllOutputsOffTimer           = new Timer( AllOutputsOffTime );
                AllOutputsOffTimer.Elapsed   += AllOutputsOffTimer_Elapsed;
            }

            #endregion

            #region PROTECTED_METHODS

            protected void DoUpDateIO( bool[] _DigOut )
            {
                _ShowStateDigitalOutput = _DigOut;
                if( outputs_ != null )
                {
                    for( int i = 0; i < _DigOut.Length; i++ )
                    {
                        if( i < outputs_.Count )
                        {
                            outputs_[i] = _DigitalOutput[i];
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            protected void TurnOnWithDelayedOffSingleLight( InputChangeEventArgs e, double delayedofftime, int index )
            {
                if( e.Value == false )
                {
                    ControlOutput( index, true );
                    base.StartSingleAutomaticOffTimer( index, delayedofftime );
                }
            }

            protected void TurnOnWithDelayedOffSingleLight( InputChangeEventArgs e, bool enable, double delayedofftime, int index )
            {
                if( enable == false )
                {
                    return;
                }
                TurnOnWithDelayedOffSingleLight( e, delayedofftime, index );
            }

            protected void TurnOnWithDelayedOffSingleLight( bool command, double delayedofftime, int index )
            {
                if( command == false )
                {
                    ControlOutput( index, true );
                    base.StartSingleAutomaticOffTimer( index, delayedofftime );
                }
            }

            protected void TurnOnWithDelayedOffSingleLight( bool command, bool enable, double delayedofftime, int index )
            {
                if( enable == false )
                {
                    return;
                }
                TurnOnWithDelayedOffSingleLight( command, delayedofftime, index );
            }

            protected bool TurnAllLightsOff( int startindex, int lastindex )
            {
                for( int ind = startindex; ind <= lastindex; ind++ )
                {
                    _DigitalOutput[ind] = false;
                }
                if( EUpdateOutputs != null )
                {
                    EUpdateOutputs( this, _DigitalOutput );
                }
                LightsOffProceeded = true;
                // fire event which tells that all lights are off
                if( AllSelectedDevicesOff_ != null )
                {
                    AllSelectedDevicesOff_( this, startindex, _lastindex );
                }
                return ( true );
            }

            protected bool AllLightsOn( int startindex, int lastindex )
            {
                for( int ind = startindex; ind <= lastindex; ind++ )
                {
                    _DigitalOutput[ind] = true;
                    if( EUpdateOutputs != null )
                    {
                        EUpdateOutputs( this, _DigitalOutput );
                    }
                }
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
                if( EUpdateOutputs != null )
                {
                    EUpdateOutputs( this, _DigitalOutput );
                }
            }
            #endregion
        }

        // _NG - next generation - business logic is seperated from IO Operation
        // IO handling is now treated within a seperate EVENT
        class LightControl_NG : LightControlTimer_
        {
            #region DECLARATIONS
            bool                                       EnableStepLight;
            bool                                       SelectDevicesOff;
            int                                        previousIndex;
            protected bool                             _AllRoomLightsAreOn;
            bool                                       _AllRoomLightsAreOff;
            protected bool                             SomeRoomLightsAreOn;
            int[]                                      _values;
            protected bool[]                           SelectedPermanentOnDevice;
            bool                                       InitPermanentDeviceSelection;
            int                                        actualindex        = 0;
            public const int                           MaxNumberOfOutputs = 16;  // 0...15 this amount ist limited of the used IO card
            int                                        _startindex;
            int                                        _lastindex;
            bool                                       LightsOffProceeded = false;
            bool                                       _LightControlSingleOffDone;
            bool                                       _PrimaryIOCardIsAttached;
            List<int>                                  Match_;
            Timer                                      AllOutputsOffTimer;
            Timer AliveTimer                         = new Timer( Parameters.TimeIntervallAlive );
            Timer FinalAllAutomaticOff               = new Timer();                                              // all configured devices off
            bool[] _StateDigitalOutput               = new bool[GeneralConstants.NumberOfOutputsIOCard];         // fill state from outside
            bool[] _ToggleOutput                     = new bool[GeneralConstants.NumberOfOutputsIOCard];
            bool[] _ShowStateDigitalOutput           = new bool[GeneralConstants.NumberOfOutputsIOCard];     // show internal state - reason is to ease testing
            protected bool[] _DigitalOutput          = new bool[GeneralConstants.NumberOfOutputsIOCard];    


            public delegate void    AllSelectedDevicesOff( object sender, int firstdevice, int lastdevice );
            public event            AllSelectedDevicesOff AllSelectedDevicesOff_;

            public delegate void    UpdateOutputs( object sender, bool[] _DigOut, List<int> match );
            public event            UpdateOutputs EUpdateOutputs;

            public delegate void    Reset( object sender );
            public event            Reset EReset;

            #endregion

            #region CONSTRUCTOR


            public LightControl_NG( )
                : base(  )
            {
                Constructor( );
            }

            public LightControl_NG( double AllOnTime, double SingleOffTime )
                : base( AllOnTime, SingleOffTime )
            {
                Constructor();
            }

            public LightControl_NG( double AllOnTime, double SingleOffTime, int startindex, int lastindex )
                : base( AllOnTime, SingleOffTime )
            {
                base.AllOn_     += LightControl_AllOn_;
                base.SingleOff_ += LightControl_SingleOff_;
                _startindex      = startindex;
                _lastindex       = lastindex;
                Constructor();
            }

            public LightControl_NG( double AllOnTime, double SingleOffTime, int deviceindex )
                : base( AllOnTime, SingleOffTime )
            {
                base.AllOn_ += LightControl_AllOn_;
                base.SingleOff_ += LightControl_SingleOff_;
                _startindex = _lastindex = deviceindex;
                Constructor();
            }

            public LightControl_NG( double AllOnTime, double SingleOffTime, double AutomaticOffTime, int startindex, int lastindex )
                : base( AllOnTime, SingleOffTime, AutomaticOffTime )
            {
                base.AllOn_        += LightControl_AllOn_;
                base.SingleOff_    += LightControl_SingleOff_;
                base.AutomaticOff_ += LightControl_AutomaticOff_;
                _startindex         = startindex;
                _lastindex          = lastindex;
                SelectedPermanentOnDevice    = new bool[GeneralConstants.NumberOfOutputsIOCard];
                InitPermanentDeviceSelection = false;
                Constructor();
            }

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


            // extended functionality - all devices off - even the desired remaining ones after timer elapsed
            public LightControl_NG( double AllOnTime,
                                    double AllOutputsOffTime,
                                    double SingleOffTime,
                                    double AutomaticOffTime,
                                    double AllFinalOffTime,
                                    int    startindex,
                                    int    lastindex
                                  )
                : base( AllOnTime, SingleOffTime, AutomaticOffTime )
            {
                Constructor( AllOutputsOffTime, SingleOffTime, AutomaticOffTime, startindex, lastindex );
                FinalAllAutomaticOff.Interval   = AllFinalOffTime;
                FinalAllAutomaticOff.Elapsed   += FinalAllAutomaticOff_Elapsed;
                base.ESingleDelayedIndexedOff_ += LightControl_ESingleDelayedIndexedOff_;
                EUpdateOutputs                 += LightControlNG_EUpdateOutputs;
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

            public int ActualLightIndexSingleStep
            {
                get
                {
                    return ( actualindex );
                }
            }

            public bool AllRoomLightsAreOn
            {
                get
                {
                    return ( _AllRoomLightsAreOn );
                }
            }

            public bool AllRoomLightsAreOff
            {
                get
                {
                    return ( _AllRoomLightsAreOff );
                }
            }

            // fill state from outside
            public bool[] StateDigitalOutput
            {
                set
                {
                    _StateDigitalOutput = value;
                }
            }

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

            #region PUBLIC_METHODS
            public void FinalAutomaticOff( InputChangeEventArgs e )
            {
                if( _AllRoomLightsAreOff )
                {
                    return;
                }

                if( e.Value == false )
                {
                    FinalAllAutomaticOff.Stop();
                    FinalAllAutomaticOff.Start();
                }
            }

            public void FinalAutomaticOff( bool command )
            {
                if( _AllRoomLightsAreOff )
                {
                    return;
                }

                if( command == false )
                {
                    FinalAllAutomaticOff.Enabled = true;
                    FinalAllAutomaticOff.Stop();
                    FinalAllAutomaticOff.Start();
                }
            }

            public void TurnSingleLight( int index, bool value )
            {
                if( ( _DigitalOutput != null ) && ( index < _DigitalOutput.Length ) )
                {
                    _DigitalOutput[index] = value;
                }

                if( EUpdateOutputs != null )
                {
                    EUpdateOutputs( this, _DigitalOutput, Match_ );
                }
            }

            public void TurnAllLights( bool command )
            {
                int index = 0;
                foreach( bool elements in _DigitalOutput ) 
                {
                    _DigitalOutput[index] = command;
                    index++;
                }

                if( EUpdateOutputs != null )
                {
                    EUpdateOutputs( this, _DigitalOutput, Match_ );
                }
            }

            public void ToggleSingleLight( int index )
            {
                if( !_ToggleOutput[index] )
                {
                    TurnSingleLight( index, GeneralConstants.ON );
                }
                else
                {
                    TurnSingleLight( index, GeneralConstants.OFF );
                }
                _ToggleOutput[index] = !_ToggleOutput[index];
            }

            public void StartAliveSignal()
            {
                AliveTimer.Start();
            }

            public void StopAliveSignal()
            {
                AliveTimer.Stop();
                _DigitalOutput[CommonRoomIOAssignment.indOutputIsAlive] = false;
                if( EUpdateOutputs != null )
                {
                    EUpdateOutputs( this, _DigitalOutput, Match_ );
                }
            }

            public void ResetLightControl()
            {
                _LightControlSingleOffDone = false;
            }

            public void StartWaitForAllOff()
            {
                if( AllOutputsOffTimer != null )
                {
                    AllOutputsOffTimer.Start();
                }
            }

            public void StopWaitForAllOff()
            {
                if( AllOutputsOffTimer != null )
                {
                    AllOutputsOffTimer.Stop();
                }
            }

            public void MakeStep( InputChangeEventArgs e )
            {
                makestep_( e.Value );
            }

            public void MakeStep( bool cmd )
            {
                makestep_( cmd );
            }

            public new void AutomaticOff( InputChangeEventArgs e )
            {
                SelectDevicesOff = false;
                StartAutomaticOff( e );
                if( LightsOffProceeded )
                {
                    CancelAutomaticOff();
                    LightsOffProceeded = false;
                }
                RestartAutomaticOff( e );
            }

            public new void AutomaticOff( bool value )
            {
                SelectDevicesOff = false;
                StartAutomaticOff( value );
                if( LightsOffProceeded )
                {
                    CancelAutomaticOff();
                    LightsOffProceeded = false;
                }
                RestartAutomaticOff( value );
            }

            public void AutomaticOffAllLights( InputChangeEventArgs e )
            {
                SelectDevicesOff = false;
                StartAutomaticOff( e );
                if( _AllRoomLightsAreOff )
                {
                    CancelAutomaticOff();
                }
            }

            public void AutomaticOffAllLights()
            {
                SelectDevicesOff = false;
                StartAutomaticOff();
                if( _AllRoomLightsAreOff )
                {
                    CancelAutomaticOff();
                }
            }

            // restarts auto off each falling edge 
            public void AutomaticOffRestartAll( InputChangeEventArgs e )
            {
                SelectDevicesOff = false;
                RestartAutomaticOffFallingEdge( e );
            }

            public void AutomaticOffSelect( InputChangeEventArgs e, params int[] values )
            {
                // no need to execute when lights are already off
                if( AreAllLightsOff( _startindex, _lastindex ) )
                {
                    return;
                }
                _values = values;
                SelectDevicesOff = true;
                bool readyForAutomaticOff = false;
                // feeds a array with the selected permanent on devices - this devices will stay on when 
                // others were comanded for turning off
                if( !InitPermanentDeviceSelection )
                {
                    for( int i = 0; i <= _lastindex; i++ )
                    {
                        SelectedPermanentOnDevice[i] = true;
                    }
                    for( int i = 0; i < _values.Length; i++ )
                    {
                        if( _values[i] < GeneralConstants.NumberOfOutputsIOCard )
                        {
                            SelectedPermanentOnDevice[_values[i]] = false;
                        }
                    }
                    InitPermanentDeviceSelection = true;
                }

                for( int i = 0; i <= _lastindex; i++ )
                {
                    if( !SelectedPermanentOnDevice[i] && _ShowStateDigitalOutput[i] )
                    {
                        readyForAutomaticOff = true;
                        break;
                    }
                }

                if( readyForAutomaticOff )
                {
                    StartAutomaticOff( e );
                }

                if( LightsOffProceeded )
                {
                    CancelAutomaticOff();
                    LightsOffProceeded = false;
                }

                RestartAutomaticOff( e );
            }

            public void AutomaticOffSelect( bool command, params int[] values )
            {
                // no need to execute when lights are already off
                if( AreAllLightsOff( _startindex, _lastindex ) )
                {
                    return;
                }
                _values = values;
                SelectDevicesOff = true;
                bool readyForAutomaticOff = false;
                // feeds a array with the selected permanent on devices - this devices will stay on when 
                // others were comanded for turning off
                if( !InitPermanentDeviceSelection )
                {
                    for( int i = 0; i <= _lastindex; i++ )
                    {
                        SelectedPermanentOnDevice[i] = true;
                    }
                    for( int i = 0; i < _values.Length; i++ )
                    {
                        if( _values[i] < GeneralConstants.NumberOfOutputsIOCard )
                        {
                            SelectedPermanentOnDevice[_values[i]] = false;
                        }
                    }
                    InitPermanentDeviceSelection = true;
                }

                for( int i = 0; i <= _lastindex; i++ )
                {
                    if( !SelectedPermanentOnDevice[i] && _ShowStateDigitalOutput[i] )
                    {
                        readyForAutomaticOff = true;
                        break;
                    }
                }

                if( readyForAutomaticOff )
                {
                    StartAutomaticOff( command );
                }

                if( LightsOffProceeded )
                {
                    CancelAutomaticOff();
                    LightsOffProceeded = false;
                }

                RestartAutomaticOff( command );
            }
            #endregion

            #region EVENTHANDLERS

            void LightControlNG_EUpdateOutputs( object sender, bool[] _DigOut, List<int> Match )
            {
                 DoUpDateIO( _DigOut );
            }

            void FinalAllAutomaticOff_Elapsed( object sender, ElapsedEventArgs e )
            {
                TurnAllLightsOff( _startindex, _lastindex );
            }

            void AllOutputsOffTimer_Elapsed( object sender, ElapsedEventArgs e )
            {
                AllOutputsOffTimer.Stop();
                AllOutputsOff();
                if( EReset != null )
                {
                    EReset( this );
                }
            }

            void AliveTimer_Elapsed( object sender, ElapsedEventArgs e )
            {
                _DigitalOutput[CommonRoomIOAssignment.indOutputIsAlive] = !_DigitalOutput[CommonRoomIOAssignment.indOutputIsAlive];
                if( EUpdateOutputs != null )
                {
                    EUpdateOutputs( this, _DigitalOutput, Match_ );
                }
            }

            void LightControl_ESingleDelayedIndexedOff_( object sender, int index )
            {
                ControlOutput( index, false );
            }

            void LightControl_SingleOff_( object sender )
            {
                // no need for turning off when light is already off
                if( _AllRoomLightsAreOff )
                {
                    return;
                }
                _LightControlSingleOffDone = true;
                _AllRoomLightsAreOn = false;
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
                _AllRoomLightsAreOn = false;
                if( SelectDevicesOff )
                {
                    TurnSomeLightsOff( _values );
                }
                else
                {
                    TurnAllLightsOff( _startindex, _lastindex );
                }
            }

            void LightControl_AllOn_( object sender )
            {
                actualindex = 0;
                AllLightsOn( _startindex, _lastindex );
                _AllRoomLightsAreOn = true;
            }
            #endregion

            #region PRIVATE_METHODS
            void AllOutputsOff()
            {
                for( int i = 0; i < _DigitalOutput.Length; i++ )
                {
                    _DigitalOutput[i] = false;
                }
                if( EUpdateOutputs != null )
                {
                    EUpdateOutputs( this, _DigitalOutput, Match_ );
                }
            }

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
                // PUSH BUTTON 
                if( cmd == true )
                {
                    base.StartAllTimers();
                    if( SomeRoomLightsAreOn )
                    {
                        TurnAllLightsOff( _startindex, _lastindex );
                        SomeRoomLightsAreOn = false;
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
                    if( _AllRoomLightsAreOn )
                    {
                        TurnAllLightsOff( _startindex, _lastindex );
                        EnableStepLight = false;
                        _AllRoomLightsAreOn = false;
                    }
                    else
                    {
                        EnableStepLight = true;
                    }
                }
                // PULL BUTTON
                else
                {
                    base.StopAllTimers();
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
                // check for all devices (lights) off
                _AllRoomLightsAreOff = AreAllLightsOff( _startindex, _lastindex );
            }

            void StartAutomaticOff( bool value )
            {
                if( value == false )
                {
                    base.StartAutomaticOfftimer();
                }
            }

            void StartAutomaticOff()
            {
                base.StartAutomaticOfftimer();
            }

            void StartAutomaticOff( InputChangeEventArgs e )
            {
                if( e.Value == false )
                {
                    base.StartAutomaticOfftimer();
                }
            }

            void CancelAutomaticOff()
            {
                base.StopAutomaticOfftimer();
            }

            void RestartAutomaticOff( InputChangeEventArgs e )
            {
                if( e.Value == true )
                {
                    base.RestartAutomaticOfftimer();
                }
            }

            void RestartAutomaticOff( bool value )
            {
                if( value == true )
                {
                    base.RestartAutomaticOfftimer();
                }
            }

            void RestartAutomaticOffFallingEdge( InputChangeEventArgs e )
            {
                if( e.Value == false )
                {
                    base.RestartAutomaticOfftimer();
                }
            }

            void TurnSomeLightsOff( int[] value )
            {
                if( value == null )
                {
                    return;
                }

                SomeRoomLightsAreOn = true;
                LightsOffProceeded = true;
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
                        break;
                }

                if( EUpdateOutputs != null )
                {
                    EUpdateOutputs( this, _DigitalOutput, Match_ );
                }
            }

            void ControlOutput( int index, bool value )
            {
                if( ( index < GeneralConstants.NumberOfInputsIOCard ) && ( index > 0 ) )
                {
                    _DigitalOutput[index] = value;
                    if( EUpdateOutputs != null )
                    {
                        EUpdateOutputs( this, _DigitalOutput, Match_ );
                    }
                }
            }

            void Constructor()
            {
                AliveTimer.Elapsed += AliveTimer_Elapsed;
                EUpdateOutputs += LightControlNG_EUpdateOutputs;
            }

            void Constructor( double AllOutputsOffTime, double SingleOffTime, double AutomaticOffTime, int startindex, int lastindex )
            {
                base.AllOn_                    += LightControl_AllOn_;
                base.SingleOff_                += LightControl_SingleOff_;
                base.AutomaticOff_             += LightControl_AutomaticOff_;
                base.ESingleDelayedIndexedOff_ += LightControl_ESingleDelayedIndexedOff_;
                AliveTimer.Elapsed             += AliveTimer_Elapsed;
                _startindex                     = startindex;
                _lastindex                      = lastindex;
                SelectedPermanentOnDevice       = new bool[GeneralConstants.NumberOfOutputsIOCard];
                InitPermanentDeviceSelection    = false;
                AllOutputsOffTimer              = new Timer( AllOutputsOffTime );
                AllOutputsOffTimer.Elapsed     += AllOutputsOffTimer_Elapsed;
            }
            #endregion

            #region PROTECTED_METHODS

            protected void DoUpDateIO( bool[] _DigOut )
            {
                _ShowStateDigitalOutput = _DigOut;
            }

            protected void TurnOnWithDelayedOffSingleLight( InputChangeEventArgs e, double delayedofftime, int index )
            {
                if( e.Value == false )
                {
                    ControlOutput( index, true );
                    base.StartSingleAutomaticOffTimer( index, delayedofftime );
                }
            }

            protected void TurnOnWithDelayedOffSingleLight( InputChangeEventArgs e, bool enable, double delayedofftime, int index )
            {
                if( enable == false )
                {
                    return;
                }
                TurnOnWithDelayedOffSingleLight( e, delayedofftime, index );
            }

            protected void TurnOnWithDelayedOffSingleLight( bool command, double delayedofftime, int index )
            {
                if( command == false )
                {
                    ControlOutput( index, true );
                    base.StartSingleAutomaticOffTimer( index, delayedofftime );
                }
            }

            protected void TurnOnWithDelayedOffSingleLight( bool command, bool enable, double delayedofftime, int index )
            {
                if( enable == false )
                {
                    return;
                }
                TurnOnWithDelayedOffSingleLight( command, delayedofftime, index );
            }

            protected bool TurnAllLightsOff( int startindex, int lastindex )
            {
                for( int ind = startindex; ind <= lastindex; ind++ )
                {
                    _DigitalOutput[ind] = false;
                }
                if( EUpdateOutputs != null )
                {
                    EUpdateOutputs( this, _DigitalOutput, Match_ );
                }
                LightsOffProceeded = true;
                // fire event which tells that all lights are off
                if( AllSelectedDevicesOff_ != null )
                {
                    AllSelectedDevicesOff_( this, startindex, _lastindex );
                }
                return ( true );
            }

            protected bool AllLightsOn( int startindex, int lastindex )
            {
                for( int ind = startindex; ind <= lastindex; ind++ )
                {
                    _DigitalOutput[ind] = true;
                    if( EUpdateOutputs != null )
                    {
                        EUpdateOutputs( this, _DigitalOutput, Match_ );
                    }
                }
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
                if( EUpdateOutputs != null )
                {
                    EUpdateOutputs( this, _DigitalOutput, Match_ );
                }
            }
            #endregion
        }

        // is controlling a group or single heater elements - mainly thermo switches mounted at the heater body
        class HeaterElement : LightControlTimer_
        {
            #region DECLARATIONS
            protected                InterfaceKitDigitalOutputCollection outputs_;
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
            bool                     _Test;


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
            // turn heater element on / off and automatic down
            public HeaterElement( double AllOnTime,
                                  double AutomaticOffTime,
                                  int startindex,
                                  int lastindex,
                                  ref InterfaceKitDigitalOutputCollection outputs )
                : base( AllOnTime, GeneralConstants.TimerDisabled, AutomaticOffTime )
            {
                base.AllOn_        += HeaterElement_AllOnOff_;
                base.AutomaticOff_ += HeaterElement_AutomaticOff_;
                _AutomaticOffTime   = AutomaticOffTime;
                _startindex = startindex;
                _lastindex  = lastindex;
                outputs_    = outputs;
            }

            // additional PWM Parameters
            public HeaterElement( double AllOnTime,
                                  double AutomaticOffTime,
                                  double PWM_StayOnTime,
                                  double PWM_StayOffTime,
                                  int startindex,
                                  int lastindex,
                                  ref InterfaceKitDigitalOutputCollection outputs )
                : base( AllOnTime, GeneralConstants.TimerDisabled, AutomaticOffTime )
            {
                base.AllOn_ += HeaterElement_AllOnOff_;
                // use auto off functionality from base
                base.AutomaticOff_ += HeaterElement_AutomaticOff_;
                Tim_StartIntensityPWM = new Timer( ParametersHeaterControl.TimeDemandForItensityTimer );
                Tim_StartIntensityPWM.Elapsed += StartIntensityPWM_Elapsed;
                if( PWM_StayOnTime > 0 &&  PWM_StayOffTime > 0 )
                {
                    PWM_ShowHeaterActive = new UnivPWM( ParametersHeaterControl.ShowOn, ParametersHeaterControl.ShowOff, true );
                    PWM_Heater = new UnivPWM( PWM_StayOnTime, PWM_StayOffTime );
                    PWM_Heater.PWM_                         += HeaterPWM_PWM_;
                    PWM_ShowHeaterActive.PWM_               += PWM_ShowHeaterActive_PWM_;
                    PWM_Heater.PwmTimeOn  = _PWM_StayOnTime  = PWM_StayOnTime;
                    PWM_Heater.PwmTimeOff = _PWM_StayOffTime = PWM_StayOffTime;
                }
                _AutomaticOffTime = AutomaticOffTime;
                _startindex = startindex;
                _lastindex  = lastindex;
                outputs_    = outputs;
                _TimedIntensityStep = 1;
                Tim_PermanentOnTimeWindow = new Timer( ParametersHeaterControl.TimeDemandForPermanentOnWindow );
                Tim_PermanentOnTimeWindow.Elapsed += PermanentOnTimeWindow_Elapsed;
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
                    if( Tim_StartIntensityPWM != null )
                    {
                        Tim_StartIntensityPWM.Stop( );
                    }
                    StopTimers( );
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
                    if( Tim_StartIntensityPWM != null )
                    {
                        Tim_StartIntensityPWM.Stop( );
                    }
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
                if( Tim_StartIntensityPWM     != null &&
                    Tim_PermanentOnTimeWindow != null && 
                    PWM_Heater                != null && 
                    PWM_ShowHeaterActive      != null )
                {
                    Tim_StartIntensityPWM.Stop( );
                    Tim_PermanentOnTimeWindow.Stop( );
                    PWM_Heater.Stop( );
                    PWM_ShowHeaterActive.Stop( );
                    _HeaterWasTurnedOn = false;
                }
            }
            #endregion

            #region PRIVATE_METHODS
            void StopTimers( )
            {
                if( Tim_PermanentOnTimeWindow != null )
                {
                    Tim_PermanentOnTimeWindow.Stop( );
                }
                if( Tim_StartIntensityPWM != null )
                {
                    Tim_StartIntensityPWM.Stop( );
                }
            }

            // heater duration is determined with actual light index ( 0 is low ... x is highest )
            // this is a feature - f.e. first light was aktivated - first heating level and so on ...

            // set output only without knowing anyting about the state
            void TurnHeaters( bool value )
            {
                for( int ind = _startindex; ind <= _lastindex; ind++ )
                {
                    if( _Test == false )
                    {
                        outputs_[ind] = value ? true : false;
                    }
                    _ShowStateDigitalOutput[ind] = value;
                }
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
                if( outputs_ != null || _Test )
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
                         if( PWM_Heater != null )
                         {
                             PWM_Heater.Stop( );
                         }
                         if( Tim_StartIntensityPWM != null )
                         {
                             Tim_StartIntensityPWM.Stop( );
                         }
                         if( Tim_PermanentOnTimeWindow != null )
                         {
                             Tim_PermanentOnTimeWindow.Stop( );
                         }
                         break;

                    case eHeaterControlState.eON:
                         TurnHeaters( true );
                         if( _AutomaticOffTime > 0 )
                         {
                             base.StartAutomaticOfftimer( );
                         }
                         if( Tim_PermanentOnTimeWindow != null )
                         {
                             Tim_PermanentOnTimeWindow.Start( );
                         } 
                         break;

                    case eHeaterControlState.ePWM_ACTIVE:
                         if( Tim_StartIntensityPWM != null )
                         {
                             Tim_StartIntensityPWM.Start( );
                         }
                         Tim_PermanentOnTimeWindow.Stop( );
                         break;

                    case eHeaterControlState.eTHERMOSTATE:
                         //HeatersOff( );
                         break;

                    case eHeaterControlState.eDEFROST:
                         base.ReconfigAutomaticOffTimer( new TimeSpan( 7, 0, 0, 0, 0 ).TotalMilliseconds );
                         PWM_Heater.PwmTimeOn  =  ParametersHeaterControlLivingRoom.TimeDemandForHeatersOnDefrost;
                         PWM_Heater.PwmTimeOff =  ParametersHeaterControlLivingRoom.TimeDemandForHeatersOffDefrost;
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
            public bool Test
            {
                set
                {
                    _Test = value;
                }
            }

            #endregion
        }

        class HMIController
        {
            const uint     INITIAL_COUNT_VALUE          = 1;
            const uint     DEFAULT_REQUIRED_COUNT_VALUE = 2;
            const uint     DEFAULT_TIME_WINDOW          = 1000;
            uint           _ToggleCounter;
            uint           _countsrequired;
            bool           _toggleflag;
            double         _timewindow;
            Timer          _OperateWithinWindowTimer;
            public delegate void Toggle_( object sender, bool value );
            public event         Toggle_ EToggle_;

            public HMIController()
            {
                _countsrequired = DEFAULT_REQUIRED_COUNT_VALUE;
                _timewindow     = DEFAULT_TIME_WINDOW;
                Constructor( );
            }

            public HMIController( uint countsrequired, double timewindow )
            {
                _countsrequired = countsrequired;
                _timewindow    = timewindow;
                Constructor( );
            }

            #region PROPERTIES
            public uint Countsrequired
            {
                get
                {
                    return _countsrequired;
                }

                set
                {
                    _countsrequired = value;
                }
            }

            public double Timewindow
            {
                get
                {
                    return _timewindow;
                }

                set
                {
                    _timewindow = value;
                }
            }
            #endregion

            #region PRIVATE_METHODS
            void Constructor()
            {
                _OperateWithinWindowTimer = new Timer( _timewindow );
                _OperateWithinWindowTimer.Elapsed += _OperateWithinWindowTimer_Elapsed;
                _ToggleCounter = INITIAL_COUNT_VALUE;
            }

            void DeviceToggleOnCounts_( uint countsrequired, double timewindow )
            {
                if( timewindow > 0.0 )
                { 
                    _OperateWithinWindowTimer.Interval = timewindow;
                    _OperateWithinWindowTimer?.Start( );
                }
                else
                {
                    return;
                }

                if( 0 == (_ToggleCounter % countsrequired) )
                {
                    _ToggleCounter = INITIAL_COUNT_VALUE;
                    _OperateWithinWindowTimer?.Stop();

                    if( !_toggleflag )
                    {
                        EToggle_( this, true );
                    }
                    else
                    {
                       EToggle_( this, false );
                    }

                    _toggleflag = !_toggleflag;
                    return;
                }
                _ToggleCounter++;
            }
            #endregion

            public void DeviceToggleOnCounts(  )
            {
                DeviceToggleOnCounts_( _countsrequired, _timewindow );
            }

            public void DeviceToggleOnCounts( uint countsrequired, double timewindow )
            {
                DeviceToggleOnCounts_( countsrequired, timewindow );
            }

            #region EVENTHANDLERS
            private void _OperateWithinWindowTimer_Elapsed( object sender, ElapsedEventArgs e )
            {
                _ToggleCounter = INITIAL_COUNT_VALUE;
                _OperateWithinWindowTimer?.Stop( );
            }
            #endregion
        }

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
            HMIController            DeviceToggleController;

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
            // turn heater element on / off and automatic down
            public HeaterElement_NG( double AllOnTime,
                                     double AutomaticOffTime,
                                     int    startindex,
                                     int    lastindex
                                   )
                : base( AllOnTime, GeneralConstants.TimerDisabled, AutomaticOffTime )
            {
                base.AllOn_        += HeaterElement_AllOnOff_;
                base.AutomaticOff_ += HeaterElement_AutomaticOff_;
                _AutomaticOffTime   = AutomaticOffTime;
                _startindex         = startindex;
                _lastindex          = lastindex;
                ToggleController();
            }


            // additional PWM Parameters
            public HeaterElement_NG( double AllOnTime,
                                     double AutomaticOffTime,
                                     double PWM_StayOnTime,
                                     double PWM_StayOffTime,
                                     int    startindex,
                                     int    lastindex
                                  )
                : base( AllOnTime, GeneralConstants.TimerDisabled, AutomaticOffTime )
            {
                base.AllOn_ += HeaterElement_AllOnOff_;
                // use auto off functionality from base
                base.AutomaticOff_            += HeaterElement_AutomaticOff_;
                Tim_StartIntensityPWM          = new Timer( ParametersHeaterControl.TimeDemandForItensityTimer );
                Tim_StartIntensityPWM.Elapsed += StartIntensityPWM_Elapsed;
                
                if( PWM_StayOnTime > 0 && PWM_StayOffTime > 0 )
                {
                    PWM_ShowHeaterActive       = new UnivPWM( ParametersHeaterControl.ShowOn, ParametersHeaterControl.ShowOff, true );
                    PWM_Heater                 = new UnivPWM( PWM_StayOnTime, PWM_StayOffTime );
                    PWM_Heater.PWM_           += HeaterPWM_PWM_;
                    PWM_ShowHeaterActive.PWM_ += PWM_ShowHeaterActive_PWM_;
                    PWM_Heater.PwmTimeOn       = _PWM_StayOnTime = PWM_StayOnTime;
                    PWM_Heater.PwmTimeOff      = _PWM_StayOffTime = PWM_StayOffTime;
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
                    base.StartAllOnTimer();
                }
                else
                {
                    base.StopAllOnTimer();
                    if( Tim_StartIntensityPWM != null )
                    {
                        Tim_StartIntensityPWM.Stop();
                    }
                    StopTimers();
                }
            }

            public void HeaterToggleOnOffRisingEdge( bool edge )
            {
                if( edge )
                {
                    if( !Toggle )
                    {
                        base.StartAllOnTimer();
                        Toggle = true;
                    }
                    else
                    {
                        base.StopAllOnTimer();
                        if( Tim_StartIntensityPWM != null )
                        {
                            Tim_StartIntensityPWM.Stop();
                        }
                        StopTimers();
                        Toggle = false;
                        HeatersOff();
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
                    base.StartAllOnTimer();
                }
                else
                {
                    base.StopAllOnTimer();
                    StopTimers();
                    if( Tim_StartIntensityPWM != null )
                    {
                        Tim_StartIntensityPWM.Stop();
                    }
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

            public bool WasHeaterSwitched()
            {
                if( _PrevHeaterWasTurnedOn != _HeaterWasTurnedOn )
                {
                    _PrevHeaterWasTurnedOn = _HeaterWasTurnedOn;
                    return true;
                }
                return false;
            }

            public void Reset()
            {
                HeaterControlState = eHeaterControlState.eOFF;
                if( Tim_StartIntensityPWM != null &&
                    Tim_PermanentOnTimeWindow != null &&
                    PWM_Heater != null &&
                    PWM_ShowHeaterActive != null )
                {
                    Tim_StartIntensityPWM.Stop();
                    Tim_PermanentOnTimeWindow.Stop();
                    PWM_Heater.Stop();
                    PWM_ShowHeaterActive.Stop();
                    _HeaterWasTurnedOn = false;
                }
            }

            public void TurnHeaterOnOffWithCounts()
            {
                DeviceToggleController?.DeviceToggleOnCounts();
            }

            public void ConfigOnOffCount( uint requiredcounts, double timewindow )
            {
                DeviceToggleController.Countsrequired = requiredcounts;
                DeviceToggleController.Timewindow = timewindow;
            }


            #endregion

            #region PRIVATE_METHODS
            void ToggleController()
            {
                DeviceToggleController = new HMIController( );
                DeviceToggleController.EToggle_ += DeviceToggleController_EToggle_;
            }

            void StopTimers()
            {
                if( Tim_PermanentOnTimeWindow != null )
                {
                    Tim_PermanentOnTimeWindow.Stop();
                }
                if( Tim_StartIntensityPWM != null )
                {
                    Tim_StartIntensityPWM.Stop();
                }
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

                this?.EUpdateOutputs_( this, _ShowStateDigitalOutput, Match_ );
            }

            void HeatersOff()
            {
                _HeaterWasTurnedOn = false;
                TurnHeaters( false );
            }

            void HeatersOn()
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
                    Tim_StartIntensityPWM.Stop();
                    return;
                }

                PWM_Heater.PwmTimeOn = _PWM_StayOnTime * _TimedIntensityStep;
                PWM_Heater.PwmTimeOff = _PWM_StayOffTime;

                //show heater status
                PWM_ShowHeaterActive.Start( _TimedIntensityStep );

                if( _TimedIntensityStep > ParametersHeaterControl.MaxIntensitySteps )
                {
                    Tim_StartIntensityPWM.Stop();
                    PWM_Heater.Stop();
                    PWM_ShowHeaterActive.Stop();
                    HeaterControlState = eHeaterControlState.eTHERMOSTATE;
                    HeaterControlStateMachine( ref HeaterControlState );
                    _TimedIntensityStep = 1;
                    return;
                }
                _TimedIntensityStep++;

                PWM_Heater.Restart();
            }

            void RestartIntensityTimer()
            {
                Tim_StartIntensityPWM.Stop();
                Tim_StartIntensityPWM.Start();
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
                            base.StopAutomaticOfftimer();
                        }
                        if( PWM_Heater != null )
                        {
                            PWM_Heater.Stop();
                        }
                        if( Tim_StartIntensityPWM != null )
                        {
                            Tim_StartIntensityPWM.Stop();
                        }
                        if( Tim_PermanentOnTimeWindow != null )
                        {
                            Tim_PermanentOnTimeWindow.Stop();
                        }
                        break;

                   case eHeaterControlState.eON:
                        TurnHeaters( true );
                        if( _AutomaticOffTime > 0 )
                        {
                            base.StartAutomaticOfftimer();
                        }
                        if( Tim_PermanentOnTimeWindow != null )
                        {
                            Tim_PermanentOnTimeWindow.Start();
                        }
                        break;

                   case eHeaterControlState.ePWM_ACTIVE:
                        if( Tim_StartIntensityPWM != null )
                        {
                            Tim_StartIntensityPWM.Start();
                        }
                        Tim_PermanentOnTimeWindow.Stop();
                        break;

                   case eHeaterControlState.eTHERMOSTATE:
                        //HeatersOff( );
                        break;

                   case eHeaterControlState.eDEFROST:
                        base.ReconfigAutomaticOffTimer( new TimeSpan( 7, 0, 0, 0, 0 ).TotalMilliseconds );
                        PWM_Heater.PwmTimeOn = ParametersHeaterControlLivingRoom.TimeDemandForHeatersOnDefrost;
                        PWM_Heater.PwmTimeOff = ParametersHeaterControlLivingRoom.TimeDemandForHeatersOffDefrost;
                        PWM_Heater.Start();
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

        // some inputs and actuators are controlled at "center" because cabeling required this
        class CentralControlledElements : LightControlTimer_
        {
            #region DECLARATIONS
            protected InterfaceKitDigitalOutputCollection outputs_;
            int _startindex, _lastindex, _deviceindex;
            bool[] _ShowStateDigitalOutput   = new bool[GeneralConstants.NumberOfOutputsIOCard];         // fill state from outside
            bool _Test;

            List<int> Match_ = new List<int>();
            public delegate void UpdateOutputs_( object sender, bool[] _DigOut, List<int> match );
            public event         UpdateOutputs_ EUpdateOutputs_;

            #endregion

            #region CONSTRUCTOR 
            // different "configurations" are via constructor possible
            public CentralControlledElements ( double AllOnTime, double AutomaticOffTime, int startindex, int lastindex, ref InterfaceKitDigitalOutputCollection outputs )
                : base( AllOnTime, GeneralConstants.TimerDisabled, AutomaticOffTime )
            {
                _startindex           = startindex;
                _lastindex            = lastindex;
                for( int i = startindex; i < lastindex; i++ )
                {
                    Match_.Add( i );
                }
                outputs_              = outputs;
                base.AllOn_          += CentralControlledElements_AllOn_;
                base.AutomaticOff_   += CentralControlledElements_AutomaticOff_;
            }

            public CentralControlledElements ( double AllOnTime, double AutomaticOffTime, int deviceindex,  ref InterfaceKitDigitalOutputCollection outputs )
                : base( AllOnTime, GeneralConstants.TimerDisabled, AutomaticOffTime )
            {
                _deviceindex         = _startindex = _lastindex = deviceindex;
                Match_.Add( deviceindex );
                outputs_ = outputs;
                base.AllOn_         += CentralControlledElements_AllOn_;
                base.AutomaticOff_  += CentralControlledElements_AutomaticOff_;
            }

            public CentralControlledElements( double AutomaticOffTime, int deviceindex, ref InterfaceKitDigitalOutputCollection outputs )
                : base( GeneralConstants.TimerDisabled, GeneralConstants.TimerDisabled, AutomaticOffTime )
            {
                _deviceindex          = _startindex
                                      = _lastindex 
                                      = deviceindex;
                outputs_              = outputs;
                Match_.Add( deviceindex );
                base.AutomaticOff_   += CentralControlledElements_AutomaticOff_;
            }
            #endregion

            #region PUBLIC_METHODS
            public void DelayedDeviceOnRisingEdge ( InputChangeEventArgs e )
            {
                DelayedDeviceOnRisingEdge( e.Value );
            }

            public void DelayedDeviceOnRisingEdge( bool Value )
            {
                if( Value == true )
                {
                    base.StartAllOnTimer();
                }
                else
                {
                    base.StopAllOnTimer();
                }
            }

            public void DelayedDeviceOnFallingEdge ( InputChangeEventArgs e )
            {
                DelayedDeviceOnFallingEdge( e.Value );
            }

            public void DelayedDeviceOnFallingEdge( bool Value )
            {
                if( Value == false )
                {
                    base.StartAllOnTimer();
                }
                else
                {
                    base.StopAllOnTimer();
                }
            }

            public void DeviceOnFallingEdgeAutomaticOff( InputChangeEventArgs e )
            {
                DeviceOnFallingEdgeAutomaticOff( e.Value );          
            }

            public void DeviceOnFallingEdgeAutomaticOff( bool Value )
            {
                if( Value == false )
                {
                    base.RestartAutomaticOfftimer();
                    if( _deviceindex < GeneralConstants.NumberOfOutputsIOCard && _deviceindex >= 0 && (outputs_ != null) || _Test )
                    {
                        if( !_Test )
                        {
                            if( outputs_.Count > 0 )
                            {
                                outputs_[_deviceindex] = true;
                            }
                        }
                        _ShowStateDigitalOutput[_deviceindex] = true;
                        if( EUpdateOutputs_ != null && Match_.Count > 0 )
                        {
                            EUpdateOutputs_( this, _ShowStateDigitalOutput, Match_ );
                        }
                    }
                }
            }
            #endregion

            #region EVENTHANDLERS
            void CentralControlledElements_AutomaticOff_ ( object sender )
            {
                base.StopAutomaticOfftimer( );
                // output ON
                for( int ind = _startindex; ind <= _lastindex; ind++ )
                {
                    if( outputs_.Count > 0 )
                    {
                        outputs_[ind] = false;  // TODO - Refactor direct acess!
                    }
                    _ShowStateDigitalOutput[_deviceindex] = false;
                }
                if( EUpdateOutputs_ != null && Match_.Count > 0 )
                {
                    EUpdateOutputs_( this, _ShowStateDigitalOutput, Match_ );
                }
            }

            void CentralControlledElements_AllOn_ ( object sender )
            {
                if( ( outputs_ != null ) || _Test )
                {
                    base.StartAutomaticOfftimer( );
                    // output ON
                    for( int ind = _startindex; ind <= _lastindex; ind++ )
                    {
                        if( outputs_.Count > 0 )
                        {
                            outputs_[ind] = true;
                        }
                        _ShowStateDigitalOutput[ind] = true;
                    }
                    if( EUpdateOutputs_ != null && Match_.Count > 0 )
                    {
                        EUpdateOutputs_( this, _ShowStateDigitalOutput, Match_ );
                    }
                }
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
  
            public bool Test
            {
                set
                {
                    _Test = value;
                }
            }
            #endregion
        }

 

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
                _startindex     = startindex;
                _lastindex      = lastindex;

                base.AllOn_               += CentralControlledElements_AllOn_;
                base.AutomaticOff_        += CentralControlledElements_AutomaticOff_;
            }

            public CentralControlledElements_NG( double AllOnTime, double AutomaticOffTime, int deviceindex )
                : base( AllOnTime, GeneralConstants.TimerDisabled, AutomaticOffTime )
            {
                _deviceindex = _startindex = _lastindex = deviceindex;
                base.AllOn_                += CentralControlledElements_AllOn_;
                base.AutomaticOff_         += CentralControlledElements_AutomaticOff_;
            }

            public CentralControlledElements_NG( double AutomaticOffTime, int deviceindex )
                : base( GeneralConstants.TimerDisabled, GeneralConstants.TimerDisabled, AutomaticOffTime )
            {
                _deviceindex = _startindex = _lastindex  = deviceindex;
                base.AutomaticOff_ += CentralControlledElements_AutomaticOff_;
            }
            #endregion

            #region PUBLIC_METHODS
            public void DelayedDeviceOnRisingEdge( InputChangeEventArgs e )
            {
                DelayedDeviceOnRisingEdge( e.Value );
            }

            public void DelayedDeviceOnRisingEdge( bool Value )
            {
                if( Value == true )
                {
                    base.StartAllOnTimer();
                }
                else
                {
                    base.StopAllOnTimer();
                }
            }

            public void DelayedDeviceOnFallingEdge( InputChangeEventArgs e )
            {
                DelayedDeviceOnFallingEdge( e.Value );
            }

            public void DelayedDeviceOnFallingEdge( bool Value )
            {
                if( Value == false )
                {
                    base.StartAllOnTimer();
                }
                else
                {
                    base.StopAllOnTimer();
                }
            }

            public void DeviceOnFallingEdgeAutomaticOff( InputChangeEventArgs e )
            {
                DeviceOnFallingEdgeAutomaticOff( e.Value );
            }

            public void DeviceOnFallingEdgeAutomaticOff( bool Value )
            {
                if( Value == false )
                {
                    base.RestartAutomaticOfftimer();
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
                base.StopAutomaticOfftimer();
                // output ON
                for( int ind = _startindex; ind <= _lastindex; ind++ )
                {
                    _ShowStateDigitalOutput[ind] = false;
                }
                if( EUpdateOutputs_ != null )
                {
                    EUpdateOutputs_( this, _ShowStateDigitalOutput, Match_ );
                }
            }

            void CentralControlledElements_AllOn_( object sender )
            {
                base.StartAutomaticOfftimer();
                // output ON
                for( int ind = _startindex; ind <= _lastindex; ind++ )
                {
                    _ShowStateDigitalOutput[ind] = true;
                }
                if( EUpdateOutputs_ != null  )
                {
                    EUpdateOutputs_( this, _ShowStateDigitalOutput, Match_ );
                }
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

        // basic functional equipment for any room
        class CommonRoom : BuildingSection
        {
            #region DECLARATIONES
            protected int            LightDeviceIndex                       = 0; // this is assigned to the digital output number
            int                      _StartWithDefaultDeviceIndex           = 0;
            const int                TotalNumberOfLightGroupsInRoom         = 5;
            protected bool           AutoWalkStartToggle                    = false;
            double                   _DelayTimeAutoNexTimer                 = 1000;
            double                   _DelayTimeAllOnTimer                   = 1200;
            double                   _DelayTimeSlingleLightOff              = 700;
            protected bool           EnableStepLight                        = true;
            bool                     _Toggle                                = false;
            protected bool           _AllLightsAreOn                        = false;
            LightControl             SelectLightControl;
            string                   _SoftwareVersion;

            protected Timer AutoNextTimer              = new Timer( );
            protected Timer DelayAllOnTimer            = new Timer( );
            protected Timer SingleLightOffTimer        = new Timer( );
            enum LightControl
            {
                eNextLightOn,
            }; 

            #endregion

            #region CONSTRUCTOR

            void Constructor( )
            {
                AutoNextTimer.Elapsed       += AutoNextTimer_Elapsed;
                AutoNextTimer.Interval       = _DelayTimeAutoNexTimer;
                DelayAllOnTimer.Elapsed     += DelayAllOnTimer_Elapsed;
                DelayAllOnTimer.Interval     = _DelayTimeAllOnTimer;
                SingleLightOffTimer.Elapsed += SingleLightOffTimer_Elapsed;
                SingleLightOffTimer.Interval = _DelayTimeSlingleLightOff;
                if( Attached )
                {
                    AllLightsOff( );
                }
            }

            public CommonRoom ( )
                : base( )
            {
                Constructor( );
            }

            public CommonRoom( bool disable )
            {
                if( disable )
                {
                    return;
                }
                Constructor( );
            }

            #endregion

            #region PROPERTIES

            public int StartWithDefaultDeviceIndex
            {
                set
                {
                    _StartWithDefaultDeviceIndex = value;
                }
            }

            public string SoftwareVersion
            {
                set
                {
                    _SoftwareVersion = value;
                }

                get
                {
                    return ( _SoftwareVersion );
                }
            }

            #endregion

            #region PUBLIC_METHODS

            public void TurnNextLightOn ( )
            {
                SelectLightControl = LightControl.eNextLightOn;
            }

            public void AllLightsOff ( )
            {
                for( int ind = 0; ind < TotalNumberOfLightGroupsInRoom; ind++ )
                {
                    base.outputs[ind] = false;
                }
                LightDeviceIndex = 0;
            }

            public void AllOutputsOff ( )
            {
                if( outputs != null )
                {
                    for( int ind = 0; ind < base.outputs.Count; ind++ )
                    {
                        base.outputs[ind] = false;
                    }
                }
                LightDeviceIndex = 0;
            }

            public bool AllLightsOff ( int startindex, int lastindex )
            {
                for( int ind = startindex; ind <= lastindex; ind++ )
                {
                    base.outputs[ind] = false;
                }
                return ( true );
            }

            new public bool AllLightsOn ( int startindex, int lastindex )
            {
                for( int ind = startindex; ind <= lastindex; ind++ )
                {
                    base.outputs[ind] = true;
                }
                return ( true );
            }

            public void AllOutputs ( bool demand )
            {
                for( int ind = 0; ind < base.outputs.Count; ind++ )
                {
                    base.outputs[ind] = demand;
                }
  
            }

            public void AllLightsOff ( out bool alllightsareon )
            {
                for( int ind = 0; ind < TotalNumberOfLightGroupsInRoom; ind++ )
                {
                    base.outputs[ind] = false;
                }
                LightDeviceIndex = 0;
                alllightsareon = false;
            }

            new public void AllLightsOn ( )
            {
                for( int ind = 0; ind < TotalNumberOfLightGroupsInRoom; ind++ )
                {
                    base.outputs[ind] = true;
                }
                LightDeviceIndex = 0;
            }

            new public void AllLightsOn ( out bool alllightsareon )
            {
                for( int ind = 0; ind < TotalNumberOfLightGroupsInRoom; ind++ )
                {
                    base.outputs[ind] = true;
                }
                LightDeviceIndex = 0;
                alllightsareon = true;
            }

            #endregion

            #region PRIVATE_METHODS

            void SingleLightOffTimer_Elapsed( object sender, ElapsedEventArgs e )
            {
                AllLightsOff( );
                SingleLightOffTimer.Stop( );
                EnableStepLight = false;
            }

            void DelayAllOnTimer_Elapsed ( object sender, ElapsedEventArgs e )
            {
                AllLightsOn( out _AllLightsAreOn );
                DelayAllOnTimer.Stop( );
            }

            void AutoNextTimer_Elapsed ( object sender, ElapsedEventArgs e )
            {
                if( LightDeviceIndex < TotalNumberOfLightGroupsInRoom )
                {
                    base.outputs[LightDeviceIndex++] = true;
                }
                else
                {
                    AutoNextTimer.Stop( );
                }
            }

            #endregion

            #region PROTECTED_METHODS

            protected void AllOnOff ( InputChangeEventArgs e )
            {
                // positive edge
                if( e.Value == true )
                {
                    if( !_Toggle )
                    {
                        AllLights( true );
                    }
                    else
                    {
                        AllLights( false );
                    }
                    _Toggle = !_Toggle;
                }
            }

            protected void NextLight ( )
            {
                if( LightDeviceIndex < TotalNumberOfLightGroupsInRoom )
                {
                    base.outputs[LightDeviceIndex++] = true;
                }
                else
                {
                    AllLightsOff( );
                }
            }

            protected void StepLight ( )
            {
                if( LightDeviceIndex < TotalNumberOfLightGroupsInRoom )
                {
                    base.outputs[LightDeviceIndex] = true;
                    if( LightDeviceIndex > 0 )
                    {
                        base.outputs[LightDeviceIndex - 1] = false;
                    }
                    LightDeviceIndex++;
                }
                else
                {
                    base.outputs[LightDeviceIndex - 1] = false;
                    base.outputs[LightDeviceIndex] = false;
                    LightDeviceIndex = 0;
                }
            }

            protected void StepLight ( int startindex, ref int index, int _indexlastdevice )
            {
                int _index = startindex + index;
                
                if( _index <= _indexlastdevice )
                {
                    base.outputs[_index] = true;
                    if( index > 0 )
                    {
                        base.outputs[_index - 1] = false;
                    }
                    index++;
                }
                else
                {
                    base.outputs[_index - 1] = false;
                    index = 0;
                }
            }

            virtual protected void TurnNextLightOn_ ( InputChangeEventArgs e )
            {
                switch( e.Index ) // the index is the assigned input number
                {
                    // turns all outputs ON - increasing - each push will activate next light
                    case CommonRoomIOAssignment.indMainButton:
                        if( e.Value == true )
                        {
                            NextLight( );
                        }
                        else
                        {
                        }
                        break;

                    // first push - next by next light will turn on, second push -  all lights off again
                    case CommonRoomIOAssignment.indOptionalWalk:
                        if( e.Value == true )
                        {
                            if( !AutoWalkStartToggle )
                            {
                                AutoNextTimer.Start( );
                            }
                            else
                            {
                                AllLightsOff( );
                            }
                        }
                        else
                        {
                            AutoWalkStartToggle = !AutoWalkStartToggle;
                        }
                        break;

                    // first relase start one light, next relase start neighbor light, turn previous light off
                    // press button longer than ( f.e. 1.. seconds ) - all lights on
                    // press button longer than ( f.e. 2.. seconds ) - all lights off
                    // TODO press button longer than ( f.e. 3.. seconds ) - broadcast all lights off, just default lights on
                    case CommonRoomIOAssignment.indOptionalMakeStep:
                        // PUSH BUTTON 
                        if( e.Value == true )
                        {
                            DelayAllOnTimer.Start( );

                            SingleLightOffTimer.Start( );

                            // all lights are already on - turn it off!
                            if( _AllLightsAreOn )
                            {
                                AllLightsOff( );
                                EnableStepLight = false;
                                _AllLightsAreOn = !_AllLightsAreOn;
                            }
                            else
                            {
                                EnableStepLight = true;
                            }
                        }
                        // PULL BUTTON
                        else
                        {
                            SingleLightOffTimer.Stop( );
                            DelayAllOnTimer.Stop( );
                            if( EnableStepLight )
                            {
                                StepLight( );
                            }
                        }
                        break;

                    default:
                        break;

                }
            }

            // TODO - fill fire alert datagramm
            virtual protected void CheckFireAlert ( InputChangeEventArgs e )
            {
            }

            // TODO - fill datagramm, heaterelement handling 
            virtual protected void CheckOpenWindow ( InputChangeEventArgs e )
            {
            }

            // ************* I N P U T S *****************
            protected override void BuildingSection_InputChange ( object sender, InputChangeEventArgs e )
            {
                CheckFireAlert( e );

                CheckOpenWindow( e );

                switch( SelectLightControl )
                {
                    case LightControl.eNextLightOn:
                         TurnNextLightOn_( e );
                         break;

                    default:
                         break;
                }
            }

            #endregion
        }

        class CommonRoomAsServer : CommonRoom
        {
              protected Server TCPServer;
              public CommonRoomAsServer ( int port)
                : base( )
              {
                TCPServer = new Server( Convert.ToInt16( port ) );
              }
        }
    }
}
