using System;
using Communication.CLIENT_Communicator;
using Communication.HAProtocoll;
using Communication.UDP;
using HomeAutomation.HardConfig;
using HomeAutomation.rooms;
using Phidgets;
using Phidgets.Events;

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

    class livingroom_east
    {
        #region DECLARATIONES
        LightControlEast[]                    LightControlMulti;
        BuildingSection[]                     BuildingMultiCard;
        UdpReceive                            UDPReceive_;
        BasicClientComumnicator               BasicClientCommunicator_;

        int                                   NumberOfAttachedPhidgets;
        int[]                                 _SerialNumbers;
        int                                   ActualPluggedCardId;
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
        public livingroom_east( int[] SerialNumbers, string IpAdressServer, string PortServer, string softwareversion ) 
        {
            _SerialNumbers    = SerialNumbers;
            _GivenClientName  = InfoOperationMode.LIVING_ROOM_EAST;
            _IpAdressServer   = IpAdressServer;
            _PortNumberServer = Convert.ToInt16( PortServer );

            if( _SerialNumbers != null )
            {
                BuildingMultiCard = new BuildingSection[SerialNumbers.Length];

                for( int i = 0; i < SerialNumbers.Length; i++ )
                {
                     BuildingMultiCard[i] = new BuildingSection( SerialNumbers[i], EnableTestBase );  
                     BuildingMultiCard[i].InputChange += livingroom_east_InputChange;
                    
                     if( BuildingMultiCard[i].Attached )
                     {
                         NumberOfAttachedPhidgets++;
                     }
                }
             }

            if( (BuildingMultiCard != null) && (NumberOfAttachedPhidgets > 0) )
            {
                AllCardsOutputsOff( );

                LightControlMulti = new LightControlEast[NumberOfAttachedPhidgets];

                if( BuildingMultiCard[IOCardID.ID_1].Attached )
                {
                    LightControlMulti[IOCardID.ID_1]
                        = new LightControlEast( ParametersLightControlEASTSide.TimeDemandForAllOn,
                                                    ParametersLightControl.TimeDemandForSingleOff,
                                                    ParametersLightControlEASTSide.TimeDemandForAutomaticOffEastSide,
                                                    EastSideIOAssignment.indSpotFrontSide1_4,
                                                    EastSideIOAssignment.indWindowLEDEastUpside,
                                                    ref BuildingMultiCard[IOCardID.ID_1].outputs );
                }

                if( BuildingMultiCard.Length > 1 )
                {
                    if (BuildingMultiCard[IOCardID.ID_2].Attached)
                    {
                        LightControlMulti[IOCardID.ID_2]
                            = new LightControlEast( ParametersLightControlEASTSide.TimeDemandForAllOn,
                                                        ParametersLightControl.TimeDemandForSingleOff,
                                                        ParametersLightControlEASTSide.TimeDemandForAutomaticOffEastSide,
                                                        EastSideIOAssignment.indSpotGalleryFloor_1_18,
                                                        EastSideIOAssignment.indBarGallery1_4,
                                                        ref BuildingMultiCard[IOCardID.ID_2].outputs );
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
                }
                catch
                {
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

        #region UDP_RECEIVE_DATA
        void UDPReceive__EDataReceived( string e )
        {
            string[] DatagrammSplitted = e.Split( ComandoString.Telegram.Seperator );

            receivedTransactionCounter = Convert.ToDecimal( DatagrammSplitted[ComandoString.Telegram.IndexTransactionCounter] );

            // basic check wether counter counts up
            if( receivedTransactionCounter > PreviousreceivedTransactionCounter )
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
                     LightControlMulti[IOCardID.ID_1].MakeStep( ReceivedValue );
                     break;

                case LivingRoomWestIOAssignment.LivWestDigInputs.indDigitalInputButtonMainUpLeft:
                     LightControlMulti[IOCardID.ID_2].AutomaticOff( ReceivedValue );
                     LightControlMulti[IOCardID.ID_2].MakeStep( ReceivedValue );
                     break;

                case LivingRoomWestIOAssignment.LivWestDigInputs.indDigitalInputPresenceDetector:
                     LightControlMulti[IOCardID.ID_1].AutomaticOff( ReceivedValue );
                     // TODO - Automatic off light gallery - condition - relative long time ( f.e. 3hours )
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

        void livingroom_east_InputChange( object sender, InputChangeEventArgs e )
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

            if( LightControlMulti[ActualPluggedCardId] != null )
            {
                switch( e.Index )
                {
                    case EastSideIOAssignment.indTestButton:
                         LightControlMulti[ActualPluggedCardId]?.MakeStep( e );
                         break;

                    case EastSideIOAssignment.indDigitalInput_PresenceDetector:
                         LightControlMulti[IOCardID.ID_1]?.AutomaticOff( e );
                         break;

                    case EastSideIOAssignment.indDigitalInput_MainDoorWingRight:
                         LightControlMulti[ActualPluggedCardId].TurnSingleLight( EastSideIOAssignment.indDoorEntry_Window_Right, true );
                         break;
                }
            }
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
    }
}
