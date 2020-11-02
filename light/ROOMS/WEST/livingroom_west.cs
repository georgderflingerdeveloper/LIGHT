using Communication.HAProtocoll;
using Communication.UDP;
using HomeAutomation.HardConfig_Collected;
using HomeAutomation.rooms;
using HomeAutomationProtocoll;
using Phidgets;
using Phidgets.Events;
using System;
using SystemServices;

namespace HomeAutomation
{
    class LightControlLivingRoomWest : LightControl
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        int _startindex;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        int _lastindex;
        bool turnedDeviceGroupManuallyOff = false;
        bool turnedAutoOff                = false;

        enum LivingRooWestStep
        {
            eLightWindowDoorEntryLeft,
            eLightKitchenDown_1,
            eLightKitchenDown_2,
            eLightKitchenDown_1_2,
            eLightWindowBoardLeft,
            eLightWindowBoardRight,
            eLightWindowBoardLeftRight,
            eLightWall
        }

        LivingRooWestStep LivingRoomWestStep_;
        LivingRooWestStep LastLivingRoomWest_;

        public LightControlLivingRoomWest( double AllOnTime, double SingleOffTime, double AutomaticOffTime, int startindex, int lastindex, ref InterfaceKitDigitalOutputCollection outputs ) :
            base( AllOnTime, SingleOffTime, AutomaticOffTime, startindex, lastindex, ref outputs )
        {
            _startindex = startindex;
            _lastindex = lastindex;
            LivingRoomWestStep_ = LivingRooWestStep.eLightWindowDoorEntryLeft;
            base.AllSelectedDevicesOff_ += LightControlLivingRoomWest_AllSelectedDevicesOff_;
            base.AutomaticOff_ += LightControlLivingRoomWest_AutomaticOff_;
        }

        void LightControlLivingRoomWest_AutomaticOff_( object sender )
        {
            LivingRoomWestStep_ = LastLivingRoomWest_;
            turnedAutoOff = true;
        }

        void LightControlLivingRoomWest_AllSelectedDevicesOff_( object sender, int firstdevice, int lastdevice )
        {
            if( firstdevice == LivingRoomWestIOAssignment.LivWestDigOutputs.indDigitalOutLightWindowDoorEntryLeft 
                && 
                lastdevice == LivingRoomWestIOAssignment.LivWestDigOutputs.indDigitalOutLightWindowBoardLeft )
            {
                LivingRoomWestStep_ = LastLivingRoomWest_;
                turnedDeviceGroupManuallyOff = true;
            }
        }

        override protected void StepLight( int startindex, ref int index, int _indexlastdevice )
        {
            if( turnedAutoOff )
            {
                index = 0;
                turnedAutoOff = false;
            }

            // preferred by a connected hardware button
            if( turnedDeviceGroupManuallyOff )
            {
                index = 0;
                turnedDeviceGroupManuallyOff = false;
            }

            switch( LivingRoomWestStep_ )
            {
                case LivingRooWestStep.eLightWindowDoorEntryLeft:
                     if( index > 0 )
                     {
                         index = 0;
                         outputs_[LivingRoomWestIOAssignment.LivWestDigOutputs.indDigitalOutLightWindowDoorEntryLeft]  = false;
                         LivingRoomWestStep_ = LivingRooWestStep.eLightKitchenDown_1;
                         break;
                     }
                     outputs_[LivingRoomWestIOAssignment.LivWestDigOutputs.indDigitalOutLightWindowDoorEntryLeft]  = true;
                     index++;
                     break;

                case LivingRooWestStep.eLightKitchenDown_1:
                     if( index > 0 )
                     {
                         index = 0;
                         outputs_[LivingRoomWestIOAssignment.LivWestDigOutputs.indDigitalOutLightKitchenDown_1]  = false;
                         LivingRoomWestStep_ = LivingRooWestStep.eLightKitchenDown_2;
                         break;
                     }
                     outputs_[LivingRoomWestIOAssignment.LivWestDigOutputs.indDigitalOutLightKitchenDown_1]  = true;
                     index++;
                     break;

                case LivingRooWestStep.eLightKitchenDown_2:
                     if( index > 0 )
                     {
                         index = 0;
                         outputs_[LivingRoomWestIOAssignment.LivWestDigOutputs.indDigitalOutLightKitchenDown_2]  = false;
                         LivingRoomWestStep_ = LivingRooWestStep.eLightKitchenDown_1_2;
                         break;
                     }
                     outputs_[LivingRoomWestIOAssignment.LivWestDigOutputs.indDigitalOutLightKitchenDown_2]  = true;
                     index++;
                     break;

               case LivingRooWestStep.eLightKitchenDown_1_2:
                    if( index > 0 )
                    {
                        index = 0;
                        outputs_ [LivingRoomWestIOAssignment.LivWestDigOutputs.indDigitalOutLightKitchenDown_1] = false;
                        outputs_ [LivingRoomWestIOAssignment.LivWestDigOutputs.indDigitalOutLightKitchenDown_2] = false;
                        LivingRoomWestStep_ = LivingRooWestStep.eLightWindowBoardLeft;
                        break;
                    }
                    outputs_ [LivingRoomWestIOAssignment.LivWestDigOutputs.indDigitalOutLightKitchenDown_1] = true;
                    outputs_ [LivingRoomWestIOAssignment.LivWestDigOutputs.indDigitalOutLightKitchenDown_2] = true;
                    index++;
                    break;

                case LivingRooWestStep.eLightWindowBoardLeft:
                     if( index > 0 )
                     {
                         index = 0;
                         outputs_[LivingRoomWestIOAssignment.LivWestDigOutputs.indDigitalOutLightWindowBoardLeft]  = false;
                         LivingRoomWestStep_ = LivingRooWestStep.eLightWindowBoardRight;
                         break;
                     }
                     outputs_[LivingRoomWestIOAssignment.LivWestDigOutputs.indDigitalOutLightWindowBoardLeft]  = true;
                     index++;
                     break;

                case LivingRooWestStep.eLightWindowBoardRight:
                     if( index > 0 )
                     {
                         index = 0;
                         outputs_[LivingRoomWestIOAssignment.LivWestDigOutputs.indDigitalOutLightWindowBoardRight]  = false;
                         LivingRoomWestStep_ = LivingRooWestStep.eLightWindowBoardLeftRight;
                         break;
                     }
                     outputs_[LivingRoomWestIOAssignment.LivWestDigOutputs.indDigitalOutLightWindowBoardRight]  = true;
                     index++;
                     break;

                case LivingRooWestStep.eLightWindowBoardLeftRight:
                     if( index > 0 )
                     {
                        index = 0;
                        outputs_ [LivingRoomWestIOAssignment.LivWestDigOutputs.indDigitalOutLightWindowBoardLeft] = false;
                        outputs_ [LivingRoomWestIOAssignment.LivWestDigOutputs.indDigitalOutLightWindowBoardRight] = false;
                        LivingRoomWestStep_ = LivingRooWestStep.eLightWall;
                        break;
                     }
                     outputs_ [LivingRoomWestIOAssignment.LivWestDigOutputs.indDigitalOutLightWindowBoardLeft] = true;
                     outputs_ [LivingRoomWestIOAssignment.LivWestDigOutputs.indDigitalOutLightWindowBoardRight] = true;
                     index++;
                     break;

                case LivingRooWestStep.eLightWall:
                     if( index > 0 )
                     {
                         index = 0;
                         outputs_ [LivingRoomWestIOAssignment.LivWestDigOutputs.indDigitalOutLightWall] = false;
                         LivingRoomWestStep_ = LivingRooWestStep.eLightWindowDoorEntryLeft;
                         break;
                     }
                     outputs_ [LivingRoomWestIOAssignment.LivWestDigOutputs.indDigitalOutLightWall] = true;
                     index++;
                     break;

            }
            LastLivingRoomWest_ = LivingRoomWestStep_;
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    class livingroom_west : CommonRoom
    {
        LightControlLivingRoomWest LightControlLivingRoomWest_;
        UdpSend                    UdpSend_, UdpSendEcho;
        UdpReceive                 UDPReceive_;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public livingroom_west( )
            : base( )
        {
            if( base.Attached )
            {
                LightControlLivingRoomWest_ 
                    = new LightControlLivingRoomWest( ParametersLightControlLivingRoomWest.TimeDemandForAllOnWest,
                                                      ParametersLightControl.TimeDemandForSingleOff,
                                                      ParametersLightControlLivingRoomWest.TimeDemandForAutomaticOffWest,
                                                      LivingRoomWestIOAssignment.indFirstLight,
                                                      LivingRoomWestIOAssignment.indLastLight,
                                                      ref base.outputs );
                LightControlLivingRoomWest_.IsPrimaryIOCardAttached = base.Attached;
                LightControlLivingRoomWest_.StartAliveSignal( );
                try
                {
                    UdpSend_    = new UdpSend( IPConfiguration.Address.IP_ADRESS_BROADCAST, IPConfiguration.Port.PORT_UDP_LIVINGROOM_WEST );
                    UdpSendEcho = new UdpSend(  IPConfiguration.Address.IP_ADRESS_BROADCAST, IPConfiguration.Port.PORT_UDP_IO_ECHO ); // use this adress when working under localhost: "127.0.0.255"
                    UDPReceive_ = new UdpReceive( IPConfiguration.Port.PORT_UDP_WEB_FORWARDER_CENTER );
                    UDPReceive_.EDataReceived += UDPReceive__EDataReceived;
               }
                catch( Exception ex )
                {
                    Services.TraceMessage_( ex.Message );
                    LightControlLivingRoomWest_.StopAliveSignal( );
                }
            }
            LightControlLivingRoomWest_.AutomaticOff_ += LightControlLivingRoomWest__AutomaticOff_;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "Communication.UDP.UdpSend.SendString(System.String)")]
        private void LightControlLivingRoomWest__AutomaticOff_( object sender )
        {
            LightControlLivingRoomWest_.ResetDelayedOff( );
            UdpSendEcho.SendString( DeviceStatus.LIGHTS_LIVING_ROOM_WEST_ARE_OFF );
        }

        void TurnWindowLedgeWest( bool command )
        {
            base.outputs[LivingRoomWestIOAssignment.LivWestDigOutputs.indDigitalOutLightWindowBoardLeft]  = command;
            base.outputs[LivingRoomWestIOAssignment.LivWestDigOutputs.indDigitalOutLightWindowBoardRight] = command;
        }

        void TurnLightKitchenBoardDownLights( bool command )
        {
            base.outputs[LivingRoomWestIOAssignment.LivWestDigOutputs.indDigitalOutLightKitchenDown_1] = command;
            base.outputs[LivingRoomWestIOAssignment.LivWestDigOutputs.indDigitalOutLightKitchenDown_2] = command;
        }

        void TurnLightWindowDoorEntryLeft( bool command )
        {
            base.outputs[LivingRoomWestIOAssignment.LivWestDigOutputs.indDigitalOutLightWindowDoorEntryLeft] = command;
        }

        void TurnLightWall( bool command )
        {
            base.outputs[LivingRoomWestIOAssignment.LivWestDigOutputs.indDigitalOutLightWall] = command;
        }

        void TurnTriangle( bool command )
        {
            base.outputs[LivingRoomWestIOAssignment.LivWestDigOutputs.indDigitalOutputWindowTriangleLeftSmall] = command;
        }

        void TurnAllLights( bool command )
        {
            TurnWindowLedgeWest( command );
            TurnLightKitchenBoardDownLights( command );
            TurnLightWindowDoorEntryLeft( command );
            TurnLightWall( command );
            TurnTriangle( command );
        }

        #region REMOTE_CONTROLLED_UDP
        decimal receivedTransactionCounter            = 0;
        decimal PreviousreceivedTransactionCounter    = 0;
        const int ExpectedArrayElementsSignalTelegram = UdpTelegram.DelfaultExpectedArrayElementsSignalTelegram;
        const int ExpectedArrayElementsCommonCommand  = 1;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Convert.ToDecimal(System.String)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "SystemServices.Services.TraceMessage_(System.String,System.String,System.String,System.Int32)")]
        void UDPReceive__EDataReceived( string e )
        {
            string[] DatagrammSplitted = e.Split( ComandoString.Telegram.Seperator );

            if ( DatagrammSplitted.Length == ExpectedArrayElementsCommonCommand )
            {
                switch ( DatagrammSplitted[0] )
                {
                    case ComandoString.TURN_ALL_LIGHTS_ON:
                    case ComandoString.TURN_ALL_LIGHTS_WEST_ON:
                         TurnAllLights( GeneralConstants.ON );
                         LightControlLivingRoomWest_.DelayedOff( );
                         break;

                    case ComandoString.TURN_ALL_LIGHTS_OFF:
                    case ComandoString.TURN_ALL_LIGHTS_WEST_OFF:
                         TurnAllLights( GeneralConstants.OFF );
                         break;

                    case ComandoString.TURN_WINDOW_LEDGE_WEST_ON:
                         TurnWindowLedgeWest( GeneralConstants.ON );
                         break;

                    case ComandoString.TURN_WINDOW_LEDGE_WEST_OFF:
                         TurnWindowLedgeWest( GeneralConstants.OFF );
                         break;

                    case ComandoString.TURN_KITCHEN_BOARD_DOWN_LIGHTS_ON:
                         TurnLightKitchenBoardDownLights( GeneralConstants.ON );
                         break;

                    case ComandoString.TURN_KITCHEN_BOARD_DOWN_LIGHTS_OFF:
                         TurnLightKitchenBoardDownLights( GeneralConstants.OFF );
                         break;

                    case ComandoString.TURN_WINDOW_LIGHT_DOOR_ENTRY_LEFT_ON:
                         TurnLightWindowDoorEntryLeft( GeneralConstants.ON );
                         break;

                    case ComandoString.TURN_WINDOW_LIGHT_DOOR_ENTRY_LEFT_OFF:
                         TurnLightWindowDoorEntryLeft( GeneralConstants.OFF );
                         break;

                    case ComandoString.TURN_LIGHTS_WALL_WEST_ON:
                         TurnLightWall( GeneralConstants.ON );
                         break;

                    case ComandoString.TURN_LIGHTS_WALL_WEST_OFF:
                         TurnLightWall( GeneralConstants.OFF );
                         break;

                    case ComandoString.TURN_LIGHT_TRIANGLE_SMALL_WEST_ON:
                         TurnTriangle( GeneralConstants.ON ); 
                         break;

                    case ComandoString.TURN_LIGHT_TRIANGLE_SMALL_WEST_OFF:
                         TurnTriangle( GeneralConstants.OFF );
                         break;

                }
                return;
            }

            if ( DatagrammSplitted.Length != ExpectedArrayElementsSignalTelegram )
            {
                Services.TraceMessage_( "Wrong datagramm received" );
                return;
            }

            string transactioncounter = DatagrammSplitted[ComandoString.Telegram.IndexTransactionCounter];

            receivedTransactionCounter = Convert.ToDecimal( transactioncounter );

            // basic check wether counter counts up
            if ( receivedTransactionCounter > PreviousreceivedTransactionCounter )
            {
                PreviousreceivedTransactionCounter = receivedTransactionCounter;
            }
            else
            {
                return;
            }


        }
        #endregion

        protected override void TurnNextLightOn_( InputChangeEventArgs e )
        {
            if( LightControlLivingRoomWest_ != null )
            {
                LightControlLivingRoomWest_.StateDigitalOutput = base.StateDigitalOutput;

                switch( e.Index )
                {
                    case LivingRoomWestIOAssignment.LivWestDigInputs.indDigitalInputButtonMainDownRight:
                         LightControlLivingRoomWest_.MakeStep( e );
                         LightControlLivingRoomWest_.AutomaticOffRestartAll( e );
                         break;
                }
            }
        }

        decimal TransactionCounter = 0;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Int32.ToString")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Decimal.ToString")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "Communication.UDP.UdpSend.SendStringSync(System.String)")]
        void SendInputState( InputChangeEventArgs e )
        {
             if( LightControlLivingRoomWest_ == null || UdpSend_ == null )
             {
                 return;
             }
             TransactionCounter++;
             UdpSend_.SendStringSync( TransactionCounter.ToString( )   +
                                           ComandoString.Telegram.Seperator +
                                           e.Index.ToString( )              + 
                                           ComandoString.Telegram.Seperator +
                                           e.Value.ToString( ) );
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        protected override void BuildingSection_InputChange( object sender, InputChangeEventArgs e )
        {
            try
            {
                TurnNextLightOn_( e );
                SendInputState( e );
                LightControlLivingRoomWest_.AutomaticOff( e );
            }
            catch( Exception ex )
            {
                Services.TraceMessage_( ex.Message );
            }
        }
    }
}
