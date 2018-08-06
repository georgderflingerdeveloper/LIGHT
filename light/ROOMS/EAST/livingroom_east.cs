using BASIC_COMPONENTS;
using Communication.CLIENT_Communicator;
using Communication.HAProtocoll;
using Communication.UDP;
using HomeAutomation.HardConfig_Collected;
using HomeAutomation.rooms;
using Phidgets;
using Phidgets.Events;
using System;
using System.Timers;
using SystemServices;
using HomeAutomationProtocoll;

namespace HomeAutomation
{
    class LightControlEast : LightControl
    {
        InterfaceKitDigitalOutputCollection _outputs;

        public LightControlEast(double AllOnTime, double SingleOffTime, double AutomaticOffTime, int startindex, int lastindex, ref InterfaceKitDigitalOutputCollection outputs)
            : base( AllOnTime,  SingleOffTime,  AutomaticOffTime,  startindex,  lastindex, ref outputs)
        {
            _outputs = outputs;
        }

        public void TurnLightsGalleryFloor( string command )
        {
            bool command_ = false;
            if( _outputs == null )
            {
                return;
            }
            switch( command )
            {
                case HomeAutomationCommandos.ALL_LIGHTS_ON:
                     command_= GeneralConstants.ON;
                     break;
                case HomeAutomationCommandos.ALL_LIGHTS_OFF:
                     command_ = GeneralConstants.OFF;
                     break;
                default:
                    break;
           }
           for( int i = EastSideIOAssignment.indSpotGalleryFloor_1_18; i <= EastSideIOAssignment.indBarGallery1_4; i++ )
           {
               _outputs[i] = command_;
           }

        }
    }

    class Livingroom_east
    {
        #region DECLARATIONES
        LightControlEast[]                    LightControlMulti;
        BuildingSection[]                     BuildingMultiCard;
        UdpReceive                            UDPReceive_;
        UdpReceive                            UDPReceiveFromGui_;
        UdpSend                               UDPSendData;
        Timer                                 TurnOffTimer;

        BasicClientComumnicator BasicClientCommunicator_;

        public event DigitalInputChanged  EDigitalInputChanged;

        DigitalInputEventargs  _DigitalInputEventargs  = new DigitalInputEventargs( );
        DigitalOutputEventargs _DigitalOutputEventargs = new DigitalOutputEventargs( );


        int NumberOfAttachedPhidgets;
        int[]                                 _SerialNumbers;
        int                                   ActualPluggedCardId;
        const int FORSEEN_PHIDGET_CARDS   = 2;
        // Testmode for phidget IO card
        bool                                  EnableTestBase = false;
        decimal                               receivedTransactionCounter         = 0;
        decimal                               PreviousreceivedTransactionCounter = 0;
        string                                _SoftwareVersion;
        static class IOCardID
        {
            public const int ID_1 = 0;
            public const int ID_2 = 1;
        }
        #endregion

        #region CONSTRUCTOR
        public Livingroom_east( int[] SerialNumbers, string IpAdressServer, string PortServer, string softwareversion ) 
        {
            _SerialNumbers    = SerialNumbers;
            _GivenClientName  = InfoOperationMode.LIVING_ROOM_EAST;
            _IpAdressServer   = IpAdressServer;
            _PortNumberServer = Convert.ToInt16( PortServer );

            TurnOffTimer = new Timer( ParametersLightControl.TimeDemandForSingleOffEastSide );
            TurnOffTimer.Elapsed += TurnOffTimer_Elapsed;

            if ( _SerialNumbers != null )
            {
                BuildingMultiCard = new BuildingSection[SerialNumbers.Length];

                for( int i = 0; i < SerialNumbers.Length; i++ )
                {
                     BuildingMultiCard[i] = new BuildingSection( SerialNumbers[i], EnableTestBase );
                     BuildingMultiCard[i].InputChange += Livingroom_east_InputChange;
                     
                    if( BuildingMultiCard[i].Attached )
                    {
                         NumberOfAttachedPhidgets++;
                    }
                }
             }

            if( (BuildingMultiCard != null) && (NumberOfAttachedPhidgets > 0) )
            {
                AllCardsOutputsOff( );

                LightControlMulti = new LightControlEast[FORSEEN_PHIDGET_CARDS];

                if( BuildingMultiCard[IOCardID.ID_1].Attached )
                {
                    LightControlMulti[IOCardID.ID_1]
                        = new LightControlEast( ParametersLightControlEASTSide.TimeDemandForAllOn,
                                                    ParametersLightControl.TimeDemandForSingleOffEastSide,
                                                    ParametersLightControlEASTSide.TimeDemandForAutomaticOffEastSide,
                                                    EastSideIOAssignment.indDigitalOutput_SpotFrontSide_1_4,
                                                    EastSideIOAssignment.indWindowLEDEastUpside,
                                                    ref BuildingMultiCard[IOCardID.ID_1].outputs );
                }

                if( BuildingMultiCard.Length > 1 )
                {
                    if ( BuildingMultiCard[IOCardID.ID_2] != null )
                    {
                        if ( BuildingMultiCard[IOCardID.ID_2].Attached )
                        {
                            LightControlMulti[IOCardID.ID_2]
                                = new LightControlEast( ParametersLightControlEASTSide.TimeDemandForAllOn,
                                                            ParametersLightControl.TimeDemandForSingleOff,
                                                            ParametersLightControlEASTSide.TimeDemandForAutomaticOffEastSideGalleryUpside,
                                                            EastSideIOAssignment.indSpotGalleryFloor_1_18,
                                                            EastSideIOAssignment.indBarGallery1_4,
                                                            ref BuildingMultiCard[IOCardID.ID_2].outputs );
                        }
                    }
                }

                if( BuildingMultiCard[IOCardID.ID_1].Attached )
                {
                    LightControlMulti[IOCardID.ID_1].IsPrimaryIOCardAttached = true;
                    LightControlMulti[IOCardID.ID_1].StartAliveSignal( );
                }

                try
                {
                    UDPReceive_ = new UdpReceive( IPConfiguration.Port.PORT_UDP_LIVINGROOM_WEST );
                    UDPReceive_.EDataReceived += UDPReceive__EDataReceived;
                    UDPReceiveFromGui_ =  new UdpReceive( IPConfiguration.Port.PORT_UDP_WEB_FORWARDER_CENTER );
                    UDPReceiveFromGui_.EDataReceived += UDPReceiveFromGui__EDataReceived;
                    UDPSendData = new UdpSend( IPConfiguration.Address.IP_ADRESS_BROADCAST, IPConfiguration.Port.PORT_LIGHT_CONTROL_LIVING_ROOM_EAST );
                }
                catch( Exception ex )
                {
                    Services.TraceMessage_( "Failed to establish UDP communication " + ex.Message + " " + ex.Source );
                    LightControlMulti[IOCardID.ID_1].StopAliveSignal( );
                }
            }
            try
            {
                BasicClientCommunicator_ = new BasicClientComumnicator( _GivenClientName, _IpAdressServer, PortServer, softwareversion );
            }
            catch
            {
                if( LightControlMulti != null )
                {
                    LightControlMulti[IOCardID.ID_1].StopAliveSignal();
                }
            }
            if( BasicClientCommunicator_ != null )
            {
                BasicClientCommunicator_.Room = _GivenClientName;
                if( LightControlMulti != null )
                {
                    BasicClientCommunicator_.Primer1IsAttached = BuildingMultiCard[IOCardID.ID_1].Attached;
                }
                BasicClientCommunicator_.EHACommando += BasicClientCommunicator__EHACommando;
            }
        }
        #endregion

        #region IPCONFIGURATION
        string _GivenClientName;
        public string GivenClientName
        {
            get
            {
                return _GivenClientName;
            }
        }

        string _IpAdressServer;
        public string IpAdressServer
        {
            set
            {
                _IpAdressServer = value;
            }
        }

        string _PortServer;
        int _PortNumberServer;
        public string PortServer
        {
            set
            {
                _PortServer = value;
                _PortNumberServer = Convert.ToInt16( value );
            }
        }
        #endregion

        #region HELPERFUNCTIONS
        void TurnCeilingGalleryDownside( bool command )
        {
            LightControlMulti[IOCardID.ID_1]?.TurnSingleLight( EastSideIOAssignment.indDigitalOutput_SpotFrontSide_1_4, command );
            LightControlMulti[IOCardID.ID_1]?.TurnSingleLight( EastSideIOAssignment.indDigitalOutput_SpotFrontSide_5_8, command );
            LightControlMulti[IOCardID.ID_1]?.TurnSingleLight( EastSideIOAssignment.indDigitalOutput_SpotBackSide_1_3,  command );
            LightControlMulti[IOCardID.ID_1]?.TurnSingleLight( EastSideIOAssignment.indDigitalOutput_SpotBackSide_4_8,  command );
        }

        void TurnGalleryUpSide( bool command )
        {
            LightControlMulti[IOCardID.ID_2]?.TurnSingleLight( EastSideIOAssignment.indSpotGalleryFloor_1_18,        command );
            LightControlMulti[IOCardID.ID_2]?.TurnSingleLight( EastSideIOAssignment.indSpotGalleryFloor_2_4,         command );
            LightControlMulti[IOCardID.ID_2]?.TurnSingleLight( EastSideIOAssignment.indSpotGalleryFloor_5_6,         command );
            LightControlMulti[IOCardID.ID_2]?.TurnSingleLight( EastSideIOAssignment.indSpotGalleryFloor_7,           command );
            LightControlMulti[IOCardID.ID_2]?.TurnSingleLight( EastSideIOAssignment.indSpotGalleryFloor_8_10,        command );
            LightControlMulti[IOCardID.ID_2]?.TurnSingleLight( EastSideIOAssignment.indSpotGalleryFloor_17_19_20_21, command );
            LightControlMulti[IOCardID.ID_2]?.TurnSingleLight( EastSideIOAssignment.indSpotGalleryFloor_16,          command );
            LightControlMulti[IOCardID.ID_2]?.TurnSingleLight( EastSideIOAssignment.indSpotGalleryFloor_14_15,       command );
            LightControlMulti[IOCardID.ID_2]?.TurnSingleLight( EastSideIOAssignment.indSpotGalleryFloor_13,          command );
            LightControlMulti[IOCardID.ID_2]?.TurnSingleLight( EastSideIOAssignment.indSpotGalleryFloor_11_12,       command );
        }
        #endregion

        #region UDP_RECEIVE_DATA
        private void UDPReceiveFromGui__EDataReceived(string e)
        {
            string[] DatagrammSplitted = e.Split( ComandoString.Telegram.Seperator );

            switch( DatagrammSplitted[0] )
            {
                case ComandoString.TURN_ALL_LIGHTS_ON:
                     TurnCeilingGalleryDownside( GeneralConstants.ON );
                     TurnGalleryUpSide( GeneralConstants.ON );
                     break;

                case ComandoString.TURN_ALL_LIGHTS_OFF:
                     TurnCeilingGalleryDownside( GeneralConstants.OFF );
                     TurnGalleryUpSide( GeneralConstants.OFF );
                     LightControlMulti[IOCardID.ID_1]?.TurnSingleLight( EastSideIOAssignment.indLightsTriangleGalleryBack, false );
                     LightControlMulti[IOCardID.ID_1]?.TurnSingleLight( EastSideIOAssignment.indWindowLEDEastUpside, false );
                     LightControlMulti[IOCardID.ID_1]?.TurnSingleLight( EastSideIOAssignment.indDoorEntry_Window_Right, false );
                     LightControlMulti[IOCardID.ID_1]?.TurnSingleLight( EastSideIOAssignment.indWindowBesideDoorRight, false );
                     LightControlMulti[IOCardID.ID_1]?.ResetForSyncingWithRemoteControl( );
                     LightControlMulti[IOCardID.ID_2]?.ResetForSyncingWithRemoteControl( );
                     break;

                case ComandoString.TURN_GALLERY_DOWN_ON:
                     TurnCeilingGalleryDownside( GeneralConstants.ON );
                     break;

                case ComandoString.TURN_GALLERY_DOWN_OFF:
                     TurnCeilingGalleryDownside( GeneralConstants.OFF );
                     break;

                case ComandoString.TURN_GALLERY_UP_ON:
                     TurnGalleryUpSide( GeneralConstants.ON );
                     break;

                case ComandoString.TURN_GALLERY_UP_OFF:
                     TurnGalleryUpSide( GeneralConstants.OFF );
                     break;

                case ComandoString.TURN_TRIANGLE_UPSTAIRS_ON:
                     LightControlMulti[IOCardID.ID_1]?.TurnSingleLight( EastSideIOAssignment.indLightsTriangleGalleryBack, true );
                     break;

                case ComandoString.TURN_TRIANGLE_UPSTAIRS_OFF:
                     LightControlMulti[IOCardID.ID_1]?.TurnSingleLight( EastSideIOAssignment.indLightsTriangleGalleryBack, false );
                     break;

                case ComandoString.TURN_LIGHT_WINDOW_SOUTHEAST_UPSIDE_ON:
                     LightControlMulti[IOCardID.ID_1]?.TurnSingleLight( EastSideIOAssignment.indWindowLEDEastUpside, true );
                     break;

                case ComandoString.TURN_LIGHT_WINDOW_SOUTHEAST_UPSIDE_OFF:
                     LightControlMulti[IOCardID.ID_1]?.TurnSingleLight( EastSideIOAssignment.indWindowLEDEastUpside, false );
                     break;

                case ComandoString.TURN_LIGHTBAR_OVER_DOOR_ENTRY_ON:
                     LightControlMulti[IOCardID.ID_1]?.TurnSingleLight( EastSideIOAssignment.indDoorEntry_Window_Right, true );
                     break;

                case ComandoString.TURN_LIGHTBAR_OVER_DOOR_ENTRY_OFF:
                     LightControlMulti[IOCardID.ID_1]?.TurnSingleLight( EastSideIOAssignment.indDoorEntry_Window_Right, false );
                     break;

                case ComandoString.TURN_LIGHTBAR_OVER_RIGHTWINDOW_BESIDE_DOOR_ON:
                     LightControlMulti[IOCardID.ID_1]?.TurnSingleLight( EastSideIOAssignment.indWindowBesideDoorRight, true );
                     break;

                case ComandoString.TURN_LIGHTBAR_OVER_RIGHTWINDOW_BESIDE_DOOR_OFF:
                     LightControlMulti[IOCardID.ID_1]?.TurnSingleLight( EastSideIOAssignment.indWindowBesideDoorRight, false );
                     break;

                case ComandoString.TURN_GALLERY_DOWN_1_ON:
                     LightControlMulti[IOCardID.ID_1]?.TurnSingleLight( EastSideIOAssignment.indDigitalOutput_SpotFrontSide_1_4, true );
                     break;

                case ComandoString.TURN_GALLERY_DOWN_1_OFF:
                     LightControlMulti[IOCardID.ID_1]?.TurnSingleLight( EastSideIOAssignment.indDigitalOutput_SpotFrontSide_1_4, false );
                     break;

                case ComandoString.TURN_GALLERY_DOWN_2_ON:
                     LightControlMulti[IOCardID.ID_1]?.TurnSingleLight( EastSideIOAssignment.indDigitalOutput_SpotFrontSide_5_8, true );
                     break;

                case ComandoString.TURN_GALLERY_DOWN_2_OFF:
                     LightControlMulti[IOCardID.ID_1]?.TurnSingleLight( EastSideIOAssignment.indDigitalOutput_SpotFrontSide_5_8, false );
                     break;

                case ComandoString.TURN_GALLERY_DOWN_3_ON:
                     LightControlMulti[IOCardID.ID_1]?.TurnSingleLight( EastSideIOAssignment.indDigitalOutput_SpotBackSide_1_3, true );
                     break;

                case ComandoString.TURN_GALLERY_DOWN_3_OFF:
                     LightControlMulti[IOCardID.ID_1]?.TurnSingleLight( EastSideIOAssignment.indDigitalOutput_SpotBackSide_1_3, false );
                     break;

                case ComandoString.TURN_GALLERY_DOWN_4_ON:
                     LightControlMulti[IOCardID.ID_1]?.TurnSingleLight( EastSideIOAssignment.indDigitalOutput_SpotBackSide_4_8, true );
                     break;

                case ComandoString.TURN_GALLERY_DOWN_4_OFF:
                     LightControlMulti[IOCardID.ID_1]?.TurnSingleLight( EastSideIOAssignment.indDigitalOutput_SpotBackSide_4_8, false );
                     break;

                case ComandoString.TURN_GALLERY_UP1_ON:
                     LightControlMulti[IOCardID.ID_2]?.TurnSingleLight( EastSideIOAssignment.indSpotGalleryFloor_1_18, true );
                     break;

                case ComandoString.TURN_GALLERY_UP2_ON:
                     LightControlMulti[IOCardID.ID_2]?.TurnSingleLight( EastSideIOAssignment.indSpotGalleryFloor_2_4, true );
                     break;

                case ComandoString.TURN_GALLERY_UP3_ON:
                     LightControlMulti[IOCardID.ID_2]?.TurnSingleLight( EastSideIOAssignment.indSpotGalleryFloor_5_6, true );
                     break;

                case ComandoString.TURN_GALLERY_UP4_ON:
                     LightControlMulti[IOCardID.ID_2]?.TurnSingleLight( EastSideIOAssignment.indSpotGalleryFloor_7, true );
                     break;

                case ComandoString.TURN_GALLERY_UP5_ON:
                     LightControlMulti[IOCardID.ID_2]?.TurnSingleLight( EastSideIOAssignment.indSpotGalleryFloor_8_10, true );
                     break;

                case ComandoString.TURN_GALLERY_UP6_ON:
                     LightControlMulti[IOCardID.ID_2]?.TurnSingleLight( EastSideIOAssignment.indSpotGalleryFloor_11_12, true );
                     break;

                case ComandoString.TURN_GALLERY_UP7_ON:
                     LightControlMulti[IOCardID.ID_2]?.TurnSingleLight( EastSideIOAssignment.indSpotGalleryFloor_13, true );
                     break;

                case ComandoString.TURN_GALLERY_UP8_ON:
                     LightControlMulti[IOCardID.ID_2]?.TurnSingleLight( EastSideIOAssignment.indSpotGalleryFloor_14_15, true );
                     break;

                case ComandoString.TURN_GALLERY_UP9_ON:
                     LightControlMulti[IOCardID.ID_2]?.TurnSingleLight( EastSideIOAssignment.indSpotGalleryFloor_16, true );
                     break;

                case ComandoString.TURN_GALLERY_UP10_ON:
                     LightControlMulti[IOCardID.ID_2]?.TurnSingleLight( EastSideIOAssignment.indSpotGalleryFloor_17_19_20_21, true );
                     break;

                case ComandoString.TURN_GALLERY_UP11_ON:
                     LightControlMulti[IOCardID.ID_2]?.TurnSingleLight( EastSideIOAssignment.indBarGallery1_4, true );
                     break;


                case ComandoString.TURN_GALLERY_UP1_OFF:
                     LightControlMulti[IOCardID.ID_2]?.TurnSingleLight( EastSideIOAssignment.indSpotGalleryFloor_1_18, false );
                     break;

                case ComandoString.TURN_GALLERY_UP2_OFF:
                     LightControlMulti[IOCardID.ID_2]?.TurnSingleLight( EastSideIOAssignment.indSpotGalleryFloor_2_4, false );
                     break;

                case ComandoString.TURN_GALLERY_UP3_OFF:
                     LightControlMulti[IOCardID.ID_2]?.TurnSingleLight( EastSideIOAssignment.indSpotGalleryFloor_5_6, false );
                     break;

                case ComandoString.TURN_GALLERY_UP4_OFF:
                     LightControlMulti[IOCardID.ID_2]?.TurnSingleLight( EastSideIOAssignment.indSpotGalleryFloor_7, false );
                     break;

                case ComandoString.TURN_GALLERY_UP5_OFF:
                     LightControlMulti[IOCardID.ID_2]?.TurnSingleLight( EastSideIOAssignment.indSpotGalleryFloor_8_10, false );
                     break;

                case ComandoString.TURN_GALLERY_UP6_OFF:
                     LightControlMulti[IOCardID.ID_2]?.TurnSingleLight( EastSideIOAssignment.indSpotGalleryFloor_11_12, false );
                     break;

                case ComandoString.TURN_GALLERY_UP7_OFF:
                     LightControlMulti[IOCardID.ID_2]?.TurnSingleLight( EastSideIOAssignment.indSpotGalleryFloor_13, false );
                     break;

                case ComandoString.TURN_GALLERY_UP8_OFF:
                     LightControlMulti[IOCardID.ID_2]?.TurnSingleLight( EastSideIOAssignment.indSpotGalleryFloor_14_15, false );
                     break;

                case ComandoString.TURN_GALLERY_UP9_OFF:
                     LightControlMulti[IOCardID.ID_2]?.TurnSingleLight( EastSideIOAssignment.indSpotGalleryFloor_16, false );
                     break;

                case ComandoString.TURN_GALLERY_UP10_OFF:
                     LightControlMulti[IOCardID.ID_2]?.TurnSingleLight( EastSideIOAssignment.indSpotGalleryFloor_17_19_20_21, false );
                     break;

                case ComandoString.TURN_GALLERY_UP11_OFF:
                     LightControlMulti[IOCardID.ID_2]?.TurnSingleLight( EastSideIOAssignment.indBarGallery1_4, false );
                     break;

                default:
                     break;

            }
        }

        void UDPReceive__EDataReceived( string e )
        {
            string[] DatagrammSplitted = e.Split( ComandoString.Telegram.Seperator );

            receivedTransactionCounter = Convert.ToDecimal( DatagrammSplitted[ComandoString.Telegram.IndexTransactionCounter] );

            // basic check wether counter counts up
            if ( receivedTransactionCounter > PreviousreceivedTransactionCounter )
            {
                PreviousreceivedTransactionCounter = receivedTransactionCounter;
            }
            else
            {
                return;
            }

            // received actual fired digital input as index
            int ReceivedIndex = Convert.ToInt16( DatagrammSplitted[ComandoString.Telegram.IndexDigitalInputs] );
            // received acutal fired value of digital input
            bool ReceivedValue = Convert.ToBoolean( DatagrammSplitted[ComandoString.Telegram.IndexValueDigitalInputs] );

            switch( ReceivedIndex )
            {
                case LivingRoomWestIOAssignment.LivWestDigInputs.indDigitalInputButtonMainDownLeft:
                     LightControlMulti[IOCardID.ID_1]?.MakeStep( ReceivedValue );
                     break;

                case LivingRoomWestIOAssignment.LivWestDigInputs.indDigitalInputButtonMainUpLeft:
                     LightControlMulti[IOCardID.ID_2]?.AutomaticOff( ReceivedValue );
                     LightControlMulti[IOCardID.ID_2]?.MakeStep( ReceivedValue );
                     break;

                case LivingRoomWestIOAssignment.LivWestDigInputs.indDigitalInputPresenceDetector:
                     LightControlMulti[IOCardID.ID_1]?.AutomaticOff( ReceivedValue );
                     LightControlMulti[IOCardID.ID_2]?.AutomaticOff( ReceivedValue );
                     break;
            }
        }
        #endregion

        #region EVENT_HANDLERS
        void BasicClientCommunicator__EHACommando( object sender, string section, string commando )
        {
            if( LightControlMulti == null )
            {
                return;
            }

            switch( section )
            {
                case Section.GalleryFloor:
                     LightControlMulti[IOCardID.ID_2].TurnLightsGalleryFloor( commando );
                     break;

                case Section.GalleryCeiling:
                     break;

                default:
                     break;
            }
        }

        void Livingroom_east_InputChange( object sender, InputChangeEventArgs e )
        {
            // identify which card raised the event
            int EventComesFromCardWithSerial = ( sender as BuildingSection ).SerialNumber;
            for( int i = 0; i < _SerialNumbers.Length; i++ )
            {
                if( EventComesFromCardWithSerial == _SerialNumbers[i] )
                {
                    ActualPluggedCardId = i;
                    break;
                }
            }

            EastSideIOAssignment.SerialCard1 = Convert.ToUInt32(_SerialNumbers[0]);

            if ( LightControlMulti != null )
            {
                switch ( e.Index )
                {
                   case EastSideIOAssignment.indTestButton:
                        LightControlMulti[ActualPluggedCardId]?.MakeStep( e );
                        LightControlMulti[IOCardID.ID_1]?.AutomaticOff( e );
                        break;

                   case EastSideIOAssignment.indDigitalInput_PresenceDetector:
                        LightControlMulti[IOCardID.ID_1]?.AutomaticOff( e );
                        LightControlMulti[IOCardID.ID_2]?.AutomaticOff( e );
                        break;

                   case EastSideIOAssignment.indDigitalInput_DoorContactMainRight:
                        TurnOffTimer?.Stop( );
                        TurnOffTimer?.Start( );
                        LightControlMulti[IOCardID.ID_1]?.TurnSingleLight( EastSideIOAssignment.indDoorEntry_Window_Right, true );
                        UDPSendData.SendStringSync( ComandoString.TURN_LIGHT_OUTSIDE_BY_OPEN_DOOR_CONTACT_ON );
                        break;

                    default:
                        break;
                }
            }


            _DigitalInputEventargs.Index        = e.Index;
            _DigitalInputEventargs.Value        = e.Value;
            _DigitalInputEventargs.SerialNumber = EventComesFromCardWithSerial;
             
            EDigitalInputChanged?.Invoke( this, _DigitalInputEventargs );
        }

        private void TurnOffTimer_Elapsed( object sender, ElapsedEventArgs e )
        {
            LightControlMulti[IOCardID.ID_1]?.TurnSingleLight( EastSideIOAssignment.indDoorEntry_Window_Right, false );
            TurnOffTimer?.Stop( );
            UDPSendData.SendStringSync( ComandoString.TURN_LIGHT_OUTSIDE_BY_OPEN_DOOR_CONTACT_OFF );
        }

        #endregion

        #region PUBLIC_METHODS

        public void AllCardsOutputsOff( )
        {
            for( int i = 0; i < _SerialNumbers.Length; i++ )
            {
                for( int j = 0; j < BuildingMultiCard[i]?.outputs.Count; j++ )
                {
                    BuildingMultiCard[i].outputs[j] = false;
                }
            }
        }

        public void Close( )
        {
            if( _SerialNumbers != null )
            {
                for( int i = 0; i < _SerialNumbers.Length; i++ )
                {
                    BuildingMultiCard[i]?.close();
                }
            }
        }

        #endregion

        #region PROPERTIES
        public string SoftwareVersion
        {
            set => _SoftwareVersion = value;
            get => ( _SoftwareVersion );
        }
        #endregion
    }
}
