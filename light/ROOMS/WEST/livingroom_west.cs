using System;
using Phidgets;
using Phidgets.Events;
using HomeAutomation.HardConfig;
using HomeAutomation.rooms;
using Communication.UDP;
using SystemServices;

namespace HomeAutomation
{
    class LightControlLivingRoomWest : LightControl
    {
        int _startindex;
        int _lastindex;
        bool turnedDeviceGroupManuallyOff = false;
        bool turnedAutoOff = false;

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
    class livingroom_west : CommonRoom
    {
        LightControlLivingRoomWest LightControlLivingRoomWest_;
        UdpSend                    UdpSend_;

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
                    UdpSend_ = new UdpSend( IPConfiguration.Address.IP_ADRESS_BROADCAST, IPConfiguration.Port.PORT_UDP_LIVINGROOM_WEST );
                }
                catch( Exception ex )
                {
                    Services.TraceMessage_( ex.Message );
                    LightControlLivingRoomWest_.StopAliveSignal( );
                }
            }
        }

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
        void SendInputState( InputChangeEventArgs e )
        {
             if( LightControlLivingRoomWest_ == null || UdpSend_ == null )
             {
                 return;
             }
             TransactionCounter++;
             UdpSend_.SendString( TransactionCounter.ToString( )   +
                                  ComandoString.Telegram.Seperator +
                                  e.Index.ToString( )              + 
                                  ComandoString.Telegram.Seperator +
                                  e.Value.ToString( ) );
        }

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
