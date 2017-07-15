using System;
using System.Collections.Generic;
using Phidgets;
using HomeAutomation.HardConfig;
using Communication.Client_;
using Communication.UDP;
using Communication.HAProtocoll;
using SystemServices;
using HAHardware;
using Scheduler;


namespace Communication
{
    namespace CLIENT_Communicator
    {
        // handles comunication client/server with app protocoll
        // - exchanges IO signals
        // - exchanges Time Data which feed scheduler
        class BasicClientComumnicator 
        {
            #region COMMUNICATION_DECLARATION

            ClientTalktive_                         TCPClient;
            UdpReceive                              UDPReceiveInvitation;
            MessageAnalyzer                         MessageAnalyzer_;

            #endregion

            #region EVENT_DECLARATION

            public delegate void ProcessHACommando( object sender, string section, string commando );
            public event         ProcessHACommando EHACommando;

            #endregion

            #region COMMON_DECLARATION

            string _SoftwareVersion;

            #endregion

            #region IO_DATA_DECLARATION
            IOSelection                             ReceivedSingleIOMessage;
            IOSelection                             GetSingleIOMessageForAnswering;
            InterfaceKitDigitalOutputCollection     outputs_;
            Dictionary<string, int>                 _DeviceDictionaryInputs;
            Dictionary<string, int>                 _DeviceDictionaryOutputs;
            bool[]                                  DigitalOutputStateBeforeProcessComandosFromServer;
            bool                                    firstReceivedDigitalOutputComando;
            #endregion

            #region SCHEDULER_DECLARATION
            FeedData FeedData                                           = new Scheduler.FeedData();
            public  delegate void FeedScheduler( object sender, FeedData e );
            public  event         FeedScheduler EFeedScheduler;

            public delegate void AskSchedulerForStatus( object sender, string Job );
            public event         AskSchedulerForStatus EAskSchedulerForStatus;
            #endregion

            #region CONSTRUCTOR
            // controls one 16/16 primer
            void ConstructorIO( string givenname, string ipserver, string portserver,
                                             ref InterfaceKitDigitalOutputCollection outputs,
                                             ref Dictionary<string, int> dicoutputs,
                                             ref Dictionary<string, int> dicinputs )
            {
                 _GivenClientName         = givenname;
                _IpAdressServer          = ipserver;
                _PortNumberServer        = Convert.ToInt16( portserver );
                outputs_                 = outputs;
                _DeviceDictionaryOutputs = dicoutputs;
                _DeviceDictionaryInputs  = dicinputs;
                DigitalOutputStateBeforeProcessComandosFromServer  = new bool[GeneralConstants.NumberOfOutputsIOCard];
                _DigitalInputState                                 = new bool[GeneralConstants.NumberOfInputsIOCard];
                _DigitalOutputState                                = new bool[GeneralConstants.NumberOfOutputsIOCard];

                // receive servers invitation - this is a UDP packet
                try
                {
                    UDPReceiveInvitation = new UdpReceive( IPConfiguration.Port.PORT_CLIENT_INVITE );
                    UDPReceiveInvitation.EDataReceived += UDPReceiveInvitation_EDataReceived;
                }
                catch( Exception ex )
                {
                    Services.TraceMessage( InfoString.ReceiveInvitationNotPossible );
                    Console.WriteLine( ex.Message );
                }
                // establish client
                try
                {
                    TCPClient = new ClientTalktive_( _IpAdressServer,
                                                     _PortNumberServer,
                                                      InfoString.GreetingToServer                      + 
                                                      IPConfiguration.Prefix.TCPCLIENT                 + 
                                                      _GivenClientName                                 + 
                                                      InfoString.MyComputernameIs 
                                                    );

                    TCPClient.MessageReceivedFromServer += TCPClient_MessageReceivedFromServer;
                    TCPClient.EndpointDisconnected      += TCPClient_EndpointDisconnected;
                }
                catch( Exception ex )
                {
                    Services.TraceMessage( InfoString.FailedToEstablishClient );
                    Console.WriteLine( ex.Data );
                }

                MessageAnalyzer_                               = new MessageAnalyzer( );
                ReceivedSingleIOMessage                        = new IOSelection( );
                GetSingleIOMessageForAnswering                 = new IOSelection( );
                GetSingleIOMessageForAnswering.EDigitalInput  += GetSingleIOMessageForAnswering_EDigitalInput;
                GetSingleIOMessageForAnswering.EDigitalOutput += GetSingleIOMessageForAnswering_EDigitalOutput;
                MessageAnalyzer_.EProcessHACommando           += MessageAnalyzer__EProcessHACommando;
           }

            void ConstructorComunicationOnly( string givenname, string ipserver, string portserver )
            {
                _GivenClientName = givenname;
                _IpAdressServer = ipserver;
                _PortNumberServer = Convert.ToInt16( portserver );

                // receive servers invitation - this is a UDP packet
                try
                {
                    UDPReceiveInvitation = new UdpReceive( IPConfiguration.Port.PORT_CLIENT_INVITE );
                    UDPReceiveInvitation.EDataReceived += UDPReceiveInvitation_EDataReceived;
                }
                catch( Exception ex )
                {
                    Services.TraceMessage( InfoString.ReceiveInvitationNotPossible );
                    Console.WriteLine( ex.Message );
                }
                // establish client
                try
                {
                    TCPClient = new ClientTalktive_( _IpAdressServer,
                                                     _PortNumberServer,
                                                      InfoString.GreetingToServer +
                                                      IPConfiguration.Prefix.TCPCLIENT +
                                                      _GivenClientName +
                                                      InfoString.MyComputernameIs
                                                    );

                    TCPClient.MessageReceivedFromServer += TCPClient_MessageReceivedFromServer;
                    TCPClient.EndpointDisconnected += TCPClient_EndpointDisconnected;
                }
                catch( Exception ex )
                {
                    Services.TraceMessage( InfoString.FailedToEstablishClient );
                    Console.WriteLine( ex.Data );
                }

                MessageAnalyzer_ = new MessageAnalyzer();
                // register received ha commando eventhandler
                // server will send a commando, next fire an event to tell the world outside about the commando
                // " the world outside" will prozess the commando
                MessageAnalyzer_.EProcessHACommando += MessageAnalyzer__EProcessHACommando;

            }

            // controls one 16/16 primer
            public BasicClientComumnicator( string givenname, string ipserver, string portserver, 
                                             ref InterfaceKitDigitalOutputCollection outputs,
                                             ref Dictionary<string, int> dicoutputs,
                                             ref Dictionary<string, int> dicinputs )
            {
                ConstructorIO( givenname, ipserver, portserver,
                                             ref  outputs,
                                             ref  dicoutputs,
                                             ref  dicinputs );
            }

            // enhanced with softwareversion
            public BasicClientComumnicator( string givenname, string ipserver, string portserver,
                                             ref InterfaceKitDigitalOutputCollection outputs,
                                             ref Dictionary<string, int> dicoutputs,
                                             ref Dictionary<string, int> dicinputs,
                                             string softwareversion )
            {
                ConstructorIO( givenname, ipserver, portserver,
                                             ref  outputs,
                                             ref  dicoutputs,
                                             ref  dicinputs );
                _SoftwareVersion = softwareversion;
            }

            // without direct IO control
            public BasicClientComumnicator( string givenname, string ipserver, string portserver )
            {
                ConstructorComunicationOnly(  givenname,  ipserver,  portserver );
            }
            public BasicClientComumnicator( string givenname, string ipserver, string portserver, string softwareversion )
            {
                ConstructorComunicationOnly( givenname, ipserver, portserver );
                _SoftwareVersion = softwareversion;
            }
           
            #endregion

            #region PROPERTIES
            string        _room;
            public string Room
            {
                set
                {
                    _room = value;
                }
            }

            // Idea is that every index change will fire an event
            // this event can trigger a sending of an information string
            public int IndexInput
            {
                set
                {
                    if( GetSingleIOMessageForAnswering != null )
                    {
                        GetSingleIOMessageForAnswering.DigitalInput_IndexWithValue( value, _DigitalInputState[value] );
                    }
                }
            }

            public int IndexOutput
            {
                set
                {
                    if( GetSingleIOMessageForAnswering != null )
                    {
                        GetSingleIOMessageForAnswering.DigitalOutput_IndexWithValue( value, _DigitalOutputState[value] );
                    }
                }
            }

            bool[]        _DigitalInputState;
            bool[]        _DigitalOutputState;
            public bool[] DigitalInputs
            { 
                get
                {
                    return _DigitalInputState;
                }
                set
                {
                    _DigitalInputState = value;
                }
            }
            public bool[] DigitalOutputs 
            { 
                get
                {
                    return _DigitalOutputState;
                }
                set
                {
                    _DigitalOutputState = value;
                }
            }

            bool _Primer1IsAttached;
            public bool Primer1IsAttached
            {
                set
                {
                    _Primer1IsAttached = value;
                }
            }

            public string TransmittedSoftwareVersion
            {
                set
                {
                    _SoftwareVersion = value;
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

            #region EVENTHANDLERS

            void MessageAnalyzer__EProcessHACommando( object sender, string section, string commando )
            {
                if( EHACommando != null )
                {
                    EHACommando( this, section, commando );
                }
            }

            void GetSingleIOMessageForAnswering_EDigitalInput( int index, bool value )
            {
                if( TCPClient.Connected )
                {
                    TCPClient.WriteMessageWithHostnameAndTimestamp( 
                        MessageBuilder.BuildIOSingleEventMessage( MessageTyp.IO_SINGLE_INPUT_STATUS, _room, index, value, GeneralConstants.DigitalInput ),
                        ComunicationInfoString.ANSWERS );
                } 
           } 
           
            void GetSingleIOMessageForAnswering_EDigitalOutput( int index, bool value )
            {
                // there is no need for transmitting alive signal
                if( CommonRoomIOAssignment.indOutputIsAlive != index )
                {
                    if( TCPClient.Connected )
                    {
                        TCPClient.WriteMessageWithHostnameAndTimestamp(
                            MessageBuilder.BuildIOSingleEventMessage(MessageTyp.IO_SINGLE_OUTPUT_STATUS, _room, index, value, GeneralConstants.DigitalOutput ) 
                            ,ComunicationInfoString.ANSWERS );
                    }
                }
            }
            // received servers invitation for reconnection
            void UDPReceiveInvitation_EDataReceived( string e )
            {
                if( e == InfoString.RequestForClientConnection )
                {
                    if( TCPClient != null )
                    {
                        TCPClient.ClientAutoReconnect( ref TCPClient,
                                                       _GivenClientName,
                                                       _IpAdressServer,
                                                       IPConfiguration.Port.PORT_SERVER.ToString( ) );
                    }
                }
            }
            
            void TCPClient_EndpointDisconnected( object sender, EventArgs e )
            {
               firstReceivedDigitalOutputComando = false;
            }

            void TCPClient_MessageReceivedFromServer( string receivedmessage )
            {
                if( TCPClient.Connected )
                {
                    if( receivedmessage != "" )
                    {
                        Console.WriteLine( receivedmessage );

                        // queue contains data
                        if( TCPClient.ReceivedMessageQueue.Count > 0 )
                        {
                            // so far not used
                            string qmessage = TCPClient.ReceivedMessageQueue.Dequeue( );

                            if( MessageAnalyzer_ == null )
                            {
                                return;
                            }

                            #region processreceivedIOCommandos
                            if( ReceivedSingleIOMessage != null )
                            {
                                ReceivedSingleIOMessage = MessageAnalyzer_.AnalyzeAnyIOComando( receivedmessage );
                                if( ReceivedSingleIOMessage.IsIOMessage )
                                {
                                    Console.WriteLine( "Set digital output with Index Number " + ReceivedSingleIOMessage.DigitalOutputIndex.ToString() +
                                                   " " + ReceivedSingleIOMessage.DigitalOutputValue.ToString() );
                                    if( !_Primer1IsAttached )
                                    {
                                        return;
                                    }
                                    if( !firstReceivedDigitalOutputComando )
                                    {
                                        // save original output state   // TODO analyse for further BUGFIX!
                                        if( DigitalOutputStateBeforeProcessComandosFromServer != null )
                                        {
                                            for( int i = 0; i < DigitalOutputStateBeforeProcessComandosFromServer.Length; i++ )
                                            {
                                                DigitalOutputStateBeforeProcessComandosFromServer[i] = outputs_[i];
                                            }
                                        }
                                        firstReceivedDigitalOutputComando = true;
                                    }
                                    outputs_[ReceivedSingleIOMessage.DigitalOutputIndex] = ReceivedSingleIOMessage.DigitalOutputValue;
                                }
                            }
                            #endregion
                            // server sends a request to client which is responding
                            #region ANSWER_ASKED_IO_STATUS
                            // server asks client - "whatsup with your IO´s"  - client is responding with actual IO state
                            if( MessageAnalyzer_.AnalyzeIOStateInquiry( receivedmessage ) )
                            {
                                string response = MessageBuilder.BuildIOStateMessageSimple( _GivenClientName, _DigitalInputState, _DigitalOutputState );
                                if( response != "" )
                                {
                                    TCPClient.WriteMessageWithHostnameAndTimestamp( response,
                                                                                    ComunicationInfoString.ANSWERS );

                                    string responseInfo = MessageBuilder.BuildIOStateMessageInFullText( _GivenClientName,
                                                                                                        _DigitalInputState,
                                                                                                        _DigitalOutputState,
                                                                                                        ref _DeviceDictionaryOutputs,
                                                                                                        ref _DeviceDictionaryInputs );
                                    TCPClient.WriteMessageWithHostnameAndTimestamp( responseInfo,
                                                                                    ComunicationInfoString.ANSWERS );
                                }
                            }
                            #endregion

                            #region processreceivedSchedulerCommands
                            if( MessageAnalyzer.AnalyseSchedulerComands( receivedmessage ) )
                            {
                                string[] receivedmessageparts = receivedmessage.Split( Seperators.delimiterCharsSchedulerReserved );
                                // scheduler message contains:
                                // JobId_Commando_StartTime_StopTime_DayInformation
                                // Example 1:
                                // 1_Start_01:01:01_02:02:02_FromNow
                                // Example Complete:
                                // 0108 27042015:16h02m27s694ms 000000057_SCHEDULERJOB_SERVER_TO_Boiler_1_Start_01:01:01_02:02:02_FromNow
                                
                                // look for received job ID
                                FeedData.Device     = receivedmessageparts[MessageAnalyzer.ExpectedMessageIndex.IndDevice];
                                FeedData.JobId      = receivedmessageparts[MessageAnalyzer.ExpectedSchedulerCommandIndex.IndJobId];
                                FeedData.Command    = receivedmessageparts[MessageAnalyzer.ExpectedSchedulerCommandIndex.IndComando];
                                FeedData.Starttime  = receivedmessageparts[MessageAnalyzer.ExpectedSchedulerCommandIndex.IndStartTime];
                                FeedData.Stoptime   = receivedmessageparts[MessageAnalyzer.ExpectedSchedulerCommandIndex.IndStopTime];
                                FeedData.Days       = receivedmessageparts[MessageAnalyzer.ExpectedSchedulerCommandIndex.IndDays];
                                if( EFeedScheduler != null )
                                {
                                    EFeedScheduler( this, FeedData );
                                }
                            }
                            
                            if( MessageAnalyzer.AnalyzeSchedulerStatusInquiry( receivedmessage ) )
                            {
                                // EXAMPLE:
                                //0076 21052015:15h47m20s906ms 000000000_ASKFORSCHEDULERSTATUSJOB_Boiler_1
                                string[] receivedmessageparts = receivedmessage.Split( Seperators.delimiterCharsSchedulerReserved );

                                string job = receivedmessageparts[MessageAnalyzer.ExpectedSchedulerInquiryIndex.IndJob];
                                job += "_";
                                job += receivedmessageparts[MessageAnalyzer.ExpectedSchedulerInquiryIndex.IndJobID];
                                if( EAskSchedulerForStatus != null )
                                {
                                    EAskSchedulerForStatus( this, job ); 
                                }
                            }
                            #endregion

                            #region processreceivedHACommandos
                            MessageAnalyzer_.AnalyzeHACommando( receivedmessage );
                            #endregion

                            #region versioninformation
                            if( MessageAnalyzer_.AnalyzeVersionInquiry( receivedmessage ) )
                            {
                                TCPClient.WriteMessageWithHostnameAndTimestamp( _SoftwareVersion,
                                                    ComunicationInfoString.ANSWERS );

                            }
                            #endregion
                        }
                    }
                }
                else
                {
                    Console.WriteLine( InfoString.SorryServerIsNotConnectedWithClient );
                }
            }
            #endregion

            #region additionalmessages
            public void SendInfoToServer( string info )
            {
                if( TCPClient.Connected )
                {
                    TCPClient.WriteMessageWithHostnameAndTimestamp( info, ComunicationInfoString.ANSWERS );
                }
            }
            #endregion
        }
    }
}
