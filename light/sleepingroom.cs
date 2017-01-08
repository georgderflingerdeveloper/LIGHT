using System;
using Communication.UDP;
using HomeAutomation.HardConfig;
using HomeAutomation.rooms;
using Phidgets.Events;
using SystemServices;

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
        public SleepingRoom ( )
            : base( )
        {
            if( base.Attached )
            {
                LightSleepingRoom = new LightControl(  ParametersLightControlSleepingRoom.TimeDemandForAllOn,
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

        protected override void TurnNextLightOn_ ( InputChangeEventArgs e )
        {
            if( LightSleepingRoom != null )
            {
                switch( e.Index ) // the index is the assigned input number
                {
                    // first relase start one light, next relase start neighbor light, turn previous light off
                    // press button longer than ( f.e. 1.. seconds ) - all lights on
                    // press button longer than ( f.e. 2.. seconds ) - all lights off
                    // TODO press button longer than ( f.e. 3.. seconds ) - broadcast all lights off, just default lights on
                    case SleepingRoomIOAssignment.indMainButton:
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
                case SleepingRoomIOAssignment.indMainButton:
                     if( HeaterSleepingRoom != null )
                     {
                         HeaterSleepingRoom.HeaterOn( e );
                     }
                     break;
            }
        }

        protected override void BuildingSection_InputChange ( object sender, InputChangeEventArgs e )
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
}
