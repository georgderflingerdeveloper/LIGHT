using System;
using Communication.UDP;
using HomeAutomation.HardConfig;
using HomeAutomation.rooms;
using Phidgets.Events;
using SystemServices;
using System.Collections.Generic;
using HA_COMPONENTS;

namespace HomeAutomation
{
    class SleepingRoom : CommonRoom
    {
        #region DECLARATIONES
        LightControl  LightSleepingRoom;
        HeaterElement HeaterSleepingRoom;
        UdpSend       UdpSend_;
        bool AliveSignalStopped;
        #endregion

        #region CONSTRUCTOR
        public SleepingRoom( )
            : base( )
        {
            if( base.Attached )
            {
                LightSleepingRoom = new LightControl( ParametersLightControlSleepingRoom.TimeDemandForAllOn,
                                                       ParametersLightControl.TimeDemandForSingleOff,
                                                       ParametersLightControlSleepingRoom.TimeDemandForAutomaticOff,
                                                       SleepingIOLightIndices.indSleepingRoomFirstLight,
                                                       SleepingIOLightIndices.indSleepingRoomLastLight,
                                                       ref base.outputs );

                HeaterSleepingRoom = new HeaterElement( ParametersHeaterControl.TimeDemandForHeatersOnOff,
                                                        ParametersHeaterControlSleepingRoom.TimeDemandForHeatersAutomaticOff,
                                                        ParametersHeaterControl.TimeDemandForHeatersOffSleepingRoomSmall,
                                                        ParametersHeaterControl.TimeDemandForHeatersOnSleepingRoomBig,
                                                        SleepingRoomIODeviceIndices.indDigitalOutputHeater,
                                                        SleepingRoomIODeviceIndices.indDigitalOutputHeater,
                                                        ref base.outputs );

                LightSleepingRoom.IsPrimaryIOCardAttached = base.Attached;
                try
                {
                    UdpSend_ = new UdpSend( IPConfiguration.Address.IP_ADRESS_BROADCAST, IPConfiguration.Port.PORT_UDP_SLEEPINGROOM );
                }
                catch( Exception ex )
                {
                    Services.TraceMessage_( ex.Message );
                    LightSleepingRoom.StartAliveSignal( );
                }
            }
        }
        #endregion

        protected override void TurnNextLightOn_( InputChangeEventArgs e )
        {
            if( LightSleepingRoom != null )
            {
                switch( e.Index ) // the index is the assigned input number
                {
                    // first relase start one light, next relase start neighbor light, turn previous light off
                    // press button longer than ( f.e. 1.. seconds ) - all lights on
                    // press button longer than ( f.e. 2.. seconds ) - all lights off
                    // TODO press button longer than ( f.e. 3.. seconds ) - broadcast all lights off, just default lights on
                    case SleepingRoomIODeviceIndices.indDigitalInputMainButton:
                        // operate light only when there is no demand of manual heater control
                        if( !HeaterSleepingRoom.WasHeaterSwitched( ) )
                        {
                            LightSleepingRoom.MakeStep( e );
                        }
                        else
                        {
                            LightSleepingRoom.StopAllOnTimer( );
                            LightSleepingRoom.ResetLightControl( );
                        }
                        if( !AliveSignalStopped )
                        {
                            LightSleepingRoom.StopAliveSignal( );
                            AliveSignalStopped = true;
                        }
                        LightSleepingRoom.AutomaticOffAllLights( e );
                        break;
                    default:
                        break;
                }
            }
        }

        void HeaterControl( InputChangeEventArgs e )
        {
            switch( e.Index ) // the index is the assigned input number
            {
                case SleepingRoomIODeviceIndices.indDigitalInputMainButton:
                    if( HeaterSleepingRoom != null )
                    {
                        HeaterSleepingRoom.HeaterOn( e );
                    }
                    break;
            }
        }

        protected override void BuildingSection_InputChange( object sender, InputChangeEventArgs e )
        {
            CheckFireAlert( e );

            CheckOpenWindow( e );

            if( LightSleepingRoom != null )
            {
                LightSleepingRoom.StateDigitalOutput = base.StateDigitalOutput;
            }

            TurnNextLightOn_( e );

            if( HeaterSleepingRoom != null && LightSleepingRoom != null )
            {
                HeaterSleepingRoom.IntensityStep = LightSleepingRoom.ActualLightIndexSingleStep;
            }

            HeaterControl( e );
        }
    }

    class LightControlSleepingRoom_NG : LightControl_NG
    {
        public LightControlSleepingRoom_NG( double AllOnTime,
                                            double AllOutputsOffTime,
                                            double SingleOffTime,
                                            double AutomaticOffTime,
                                            int startindex,
                                            int lastindex
                                           )
            : base( AllOnTime, AllOutputsOffTime, SingleOffTime, AutomaticOffTime, startindex, lastindex )
        {
        }
    }

    class SleepingRoomNG : CommonRoom
    {
        #region DECLARATIONES
        LightControlSleepingRoom_NG LightSleepingRoom;
 //       HeaterElement_NG        HeaterSleepingRoom;
        UdpSend                 UdpSend_;
        bool                    _Test;
        bool[]                  _StateDigOut = new bool[GeneralConstants.NumberOfOutputsIOCard];
        #endregion

        #region CONSTRUCTOR
        public SleepingRoomNG( ) : base( )
        {
            if( base.Attached )
            {
                LightSleepingRoom = new LightControlSleepingRoom_NG( 
                                                                     ParametersLightControlSleepingRoomNG.TimeDemandForAllOn,
                                                                     ParametersLightControlSleepingRoom.TimeDemandForAllOutputsOff,
                                                                     ParametersLightControl.TimeDemandForSingleOff,
                                                                     ParametersLightControlSleepingRoom.TimeDemandForAutomaticOff,
                                                                     SleepingIOLightIndices.indSleepingRoomFirstLight,
                                                                     SleepingIOLightIndices.indSleepingRoomLastLight
                                                                   );

                LightSleepingRoom.Match = new List<int> {  SleepingIOLightIndices.indSleepingRoomFirstLight,
                                                           SleepingIOLightIndices.indSleepingRoomSecondLight,  
                                                           SleepingIOLightIndices.indSleepingRoomThirdLight,
                                                           SleepingIOLightIndices.indSleepingRoomFourthLight,
                                                           SleepingIOLightIndices.indSleepingRoomLastLight,
                                                           CommonRoomIOAssignment.indOutputIsAlive
                                                        };



                //HeaterSleepingRoom = new HeaterElement_NG( ParametersHeaterControl.TimeDemandForHeatersOnOff,
                //                                           ParametersHeaterControlSleepingRoom.TimeDemandForHeatersAutomaticOff,
                //                                           ParametersHeaterControl.TimeDemandForHeatersOffSleepingRoomSmall,
                //                                           ParametersHeaterControl.TimeDemandForHeatersOnSleepingRoomBig,
                //                                           SleepingRoomIODeviceIndices.indDigitalOutputHeater,
                //                                           SleepingRoomIODeviceIndices.indDigitalOutputHeater );

                //HeaterSleepingRoom.Match = new List<int> { SleepingRoomIODeviceIndices.indDigitalOutputHeater };

                LightSleepingRoom.EUpdateOutputs += EUpdateOutputs;
                //HeaterSleepingRoom.EUpdateOutputs_ += EUpdateOutputs;


                LightSleepingRoom.IsPrimaryIOCardAttached = base.Attached;
                LightSleepingRoom.StartAliveSignal( );
                try
                {
                    UdpSend_ = new UdpSend( IPConfiguration.Address.IP_ADRESS_BROADCAST, IPConfiguration.Port.PORT_UDP_SLEEPINGROOM );
                }
                catch( Exception ex )
                {
                    Services.TraceMessage_( ex.Message );
                    LightSleepingRoom.StopAliveSignal( );
                }
            }
        }
        #endregion

        #region PROPERTIES
        public bool [ ] StateDigOut
        {
            get
            {
                return _StateDigOut;
            }
        }
        #endregion

        private void EUpdateOutputs( object sender, bool[] _DigOut, List<int> match )
        {
            for( int i = 0; i < _DigOut.Length; i++ )
            {
                if( match.Contains( i ) ) // only matching index within the defined list are written into the array
                {
                    _StateDigOut [i] = _DigOut [i];
                    if( !_Test )
                    {
                        if( base.Attached )
                        {
                            // DIGITAL OUTPUT MAPPING
                            base.outputs [i] = _DigOut [i]; // only one location where "real" output is written!
                        }
                    }
                }
            }
        }

        void ControlSequenceOnInputChange( int index, bool Value )
        {
            switch( index )
            {
                case SleepingRoomIODeviceIndices.indDigitalInputMainButton:
                     LightSleepingRoom.MakeTimedStep( Value );
                     break;
            }
        }

        protected override void BuildingSection_InputChange( object sender, InputChangeEventArgs e )
        {
            ControlSequenceOnInputChange( e.Index, e.Value );
        }
    }
}

 

