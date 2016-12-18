//Needed for the InterfaceKit class, phidget base classes, and the PhidgetException class
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Timers;
using Auxiliary;
using Communication.Client_;
using Communication.Server_;
using Communication.UDP;
using Filehandling;
using HardwareDevices;
using HomeAutomation.Controls;
using HomeAutomation.HardConfig;
using HomeAutomation.rooms;
using Phidgets;
using SystemServices;
using BASIC_COMPONENTS;

namespace HomeAutomation
{
    static class RaspberryPiConst
    {
        public const double WatchdogIntervall = 60000;
    }

    class MyHomeMain
    {
        #region COMMON_DECLARATIONS
        static System.Timers.Timer           SelfTestTimer                   =  new System.Timers.Timer();
        static System.Timers.Timer           CommTestTimer                   =  new System.Timers.Timer();
        static System.Timers.Timer           FeedWatchDogTimer               =  new System.Timers.Timer( RaspberryPiConst.WatchdogIntervall );
        static System.Timers.Timer           Timer_ClientInvitation          =  new System.Timers.Timer( Parameters.ClientInvitationIntervall );
        static System.Timers.Timer           Timer_SendPeriodicDataToServer  =  new System.Timers.Timer( 1000 );
        static System.Timers.Timer           Timer_SendPeriodicDataToClient  =  new System.Timers.Timer( 1000 );
        static BuildingSection               Roof;
        static Effects                       LightEffect;
        //static SleepingRoom                  MyHomeSleepingRoom;
        static SleepingRoomNG                  MyHomeSleepingRoom;
        //static Center_kitchen_living_room_NG    MyHomeKitchenLivingRoom;
        static Center_kitchen_living_room_NG    MyHomeKitchenLivingRoom;
        static AnteRoom                      MyHomeAnteRoom;
        static ServerQueue                   TCPServer;
        static ClientTalktive_               Client_;
        static UdpSend                       UDP_SendClientInvitation;
        static UdpReceive                    UDP_ReceiveClientInvitation;
        static livingroom_east               MyHomeLivingRoomEast;
        static livingroom_west               MyHomeLivingRoomWest;
        static string                        _homeAutomationCommand = "";
        static string                        AbortComand            = "";
        static string                        setting_value          = "";
        static string                        serveripadress         = "";
        static string                        serverPort             = "";
        static string                        UserDefinedClientID_   = "";
        static int[]                         PhidgetSerialNumbers;     // container of serial number when more than one card is used
        static Dictionary<string, string>    PhidgetsIds = new Dictionary<string, string>( );   // contains ID´s of phidgets provided f.e. by ini file
        static UnivPWM HeaterPWM;
        static string                        Version;  // General Version information - valid for all "rooms"
        static string                        CompleteVersion;
        #endregion

        #region helpers
        // program information last build
        private static DateTime BuildDate
        {
            get { return File.GetLastWriteTime( Assembly.GetExecutingAssembly( ).Location ); }
        }

        static void ComandInput(out string command, string title)
        {
            Console.WriteLine( title );
            command = Console.ReadLine();
        }

        static void ClientReconnect( ref ClientTalktive_ Client_, string userdefinedClientID )
        {
            if( !Client_.Connected )
            {
                Client_.Disconnect();
                Console.WriteLine( InfoString.InfoConnectionFailed );
                Console.ReadLine();
                Console.Write( InfoString.InfoTryToReconnect );
                Client_.ReConnect( serveripadress, Convert.ToInt16(serverPort), userdefinedClientID );
            }
        }

        static void ClientAutoReconnect( ref ClientTalktive_ Client_, string userdefinedClientID )
        {
            if ( !Client_.Connected )
            {
                Client_.Disconnect();
                Console.WriteLine( InfoString.InfoTryToReconnect );
                Client_.ReConnect( serveripadress, Convert.ToInt16( serverPort ), userdefinedClientID );
            }
        }

        static void ClientReconnect( ref ClientTalktive_ Client_ )
        {
            if ( !Client_.Connected )
            {
                Client_.Disconnect();
                Console.WriteLine( InfoString.InfoConnectionFailed );
                Console.ReadLine();
                Console.WriteLine( InfoString.InfoTryToReconnect );
                Client_.ReConnect( serveripadress, Convert.ToInt16( serverPort ) );
            }
        }
        #endregion

        #region MAIN
        static void Main(string[] args)
        {
            try
            {
                #region APP_INIT
                Debug.Listeners.Add( new TextWriterTraceListener( Console.Out ) );
                Debug.AutoFlush = true;
                Debug.Indent();
                Console.WriteLine( InfoString.InfoStartingTime + DateTime.Now );
                Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                CompleteVersion = InfoString.InfoVersion + Version + Seperators.Spaceholder + InfoString.InfoLastBuild + BuildDate;
                Console.WriteLine( TimeUtil.GetTimestamp()           + 
                                   Seperators.Spaceholder            + 
                                   InfoString.InfoVersioninformation + 
                                   Version                           + 
                                   Seperators.Spaceholder            + 
                                   InfoString.InfoLastBuild          + 
                                   BuildDate.ToString() );
                Console.WriteLine( TimeUtil.GetTimestamp() + Seperators.Spaceholder + InfoString.InfoLoadingConfiguration );
                setting_value = INIUtility.Read( InfoString.ConfigFileName, InfoString.IniSection, InfoObjectDefinitions.Room);
                serveripadress = INIUtility.Read( InfoString.ConfigFileName, InfoString.IniSection, InfoObjectDefinitions.Server );
                serverPort = INIUtility.Read( InfoString.ConfigFileName, InfoString.IniSection, InfoObjectDefinitions.Port);
                try
                {
                    PhidgetsIds     = INIUtility.ReadAllSection( InfoString.ConfigFileName, InfoString.IniSectionPhidgets );
                    int i = 0;
                    PhidgetSerialNumbers = new int[PhidgetsIds.Count];
                    foreach( KeyValuePair<string, string> pair in PhidgetsIds )
                    {
                        Console.WriteLine( TimeUtil.GetTimestamp() + Seperators.Spaceholder + InfoString.InfoExpectingPhidget + pair.Key + Seperators.Spaceholder + pair.Value );
                        PhidgetSerialNumbers[i] = Convert.ToInt32(pair.Value);
                        i++;
                    }
                }
                catch
                {
                    Console.WriteLine( TimeUtil.GetTimestamp() + Seperators.Spaceholder + InfoString.InfoNoConfiguredPhidgetIDused );
                }

                Console.WriteLine( TimeUtil.GetTimestamp() + Seperators.Spaceholder + InfoString.InfoLoadingConfigurationSucessfull );
                #endregion

                switch( setting_value )
                {
                    #region ROOM_SELECTION
                    case InfoOperationMode.SLEEPING_ROOM:
                         Console.WriteLine( InfoString.OperationMode + InfoOperationMode.SLEEPING_ROOM );
                         break;

                    case InfoOperationMode.CENTER_KITCHEN_AND_LIVING_ROOM:
                         Console.WriteLine( InfoString.OperationMode  + InfoOperationMode.CENTER_KITCHEN_AND_LIVING_ROOM );
                         break;

                    case InfoOperationMode.ANTEROOM:
                         Console.WriteLine( InfoString.OperationMode  + InfoOperationMode.ANTEROOM );
                         break;

                    case InfoOperationMode.LIVING_ROOM_EAST:
                         Console.WriteLine( InfoString.OperationMode  + InfoOperationMode.LIVING_ROOM_EAST );
                         break;

                    case InfoOperationMode.LIVING_ROOM_WEST:
                         Console.WriteLine( InfoString.OperationMode  + InfoOperationMode.LIVING_ROOM_WEST );
                         break;
                    #endregion

                    #region SERVER_CLIENT_TESTS
                    case InfoOperationMode.TCP_COMUNICATION_SERVER:
                         Console.WriteLine( InfoString.OperationMode  + InfoOperationMode.TCP_COMUNICATION_SERVER );
                         break;

                    case InfoOperationMode.TCP_COMUNICATION_CLIENT:
                         Console.WriteLine( InfoString.OperationMode  + InfoOperationMode.TCP_COMUNICATION_CLIENT );
                         break;

                    case InfoOperationMode.TCP_COMUNICATION_SERVER_INVITE:
                         Console.WriteLine( InfoString.OperationMode + InfoOperationMode.TCP_COMUNICATION_SERVER_INVITE );
                         break;

                    case InfoOperationMode.TCP_COMUNICATION_CLIENT_INVITE:
                         Console.WriteLine( InfoString.OperationMode + InfoOperationMode.TCP_COMUNICATION_CLIENT_INVITE );
                         break;

                    case InfoOperationMode.TCP_COMUNICATION_CLIENT_SINGLE_MESSAGES:
                         Console.WriteLine( InfoString.OperationMode + InfoOperationMode.TCP_COMUNICATION_CLIENT_SINGLE_MESSAGES );
                         break;

                    case InfoOperationMode.TCP_COMUNICATION_CLIENT_INVITE_LOCALHOST:
                         Console.WriteLine( InfoString.OperationMode + InfoOperationMode.TCP_COMUNICATION_CLIENT_INVITE_LOCALHOST );
                         break;

                    case InfoOperationMode.TCP_COMUNICATION_CLIENT_SEND_PERIODIC:
                         Console.WriteLine( InfoString.OperationMode + InfoOperationMode.TCP_COMUNICATION_CLIENT_SEND_PERIODIC );
                         break;

                    case InfoOperationMode.TCP_COMUNICATION_SERVER_SEND_PERIODIC:
                         Console.WriteLine( InfoString.OperationMode + InfoOperationMode.TCP_COMUNICATION_SERVER_SEND_PERIODIC );
                         break;

                    case InfoOperationMode.TCP_COMUNICATION_SERVER_SEND_PERIODIC_LOCALHOST:
                         Console.WriteLine( InfoString.OperationMode + InfoOperationMode.TCP_COMUNICATION_SERVER_SEND_PERIODIC_LOCALHOST );
                         break;

                    case InfoOperationMode.TCP_COMUNICATION_SERVER_TEST_GET_MESSAGES_FROM_CLIENTS:
                         Console.WriteLine( InfoString.OperationMode + InfoOperationMode.TCP_COMUNICATION_SERVER_TEST_GET_MESSAGES_FROM_CLIENTS );
                         break;

                    case InfoOperationMode.TCP_COMUNICATION_SERVER_INTERNAL:
                         Console.WriteLine( InfoString.OperationMode  + InfoOperationMode.TCP_COMUNICATION_SERVER_INTERNAL );
                         break;

                    case InfoOperationMode.TCP_COMUNICATION_CLIENT_INTERNAL:
                         Console.WriteLine( InfoString.OperationMode  + InfoOperationMode.TCP_COMUNICATION_CLIENT_INTERNAL );
                         break;

                    case InfoOperationMode.TCP_COMUNICATION_CLIENT_STRESSTEST:
                         Console.WriteLine( InfoString.OperationMode  + InfoOperationMode.TCP_COMUNICATION_CLIENT_STRESSTEST );
                         break;
                    #endregion

                    #region TESTMENU
                    case InfoOperationMode.IO_CTRL:
                         Console.WriteLine( InfoString.OperationMode  + InfoOperationMode.IO_CTRL );
                         break;

                    case InfoOperationMode.TEST_PWM:
                         Console.WriteLine( InfoString.OperationMode  + InfoOperationMode.TEST_PWM );
                         break;

                    case InfoOperationMode.ANALOG_HEATER:
                         Console.WriteLine( InfoString.OperationMode + InfoOperationMode.ANALOG_HEATER );
                         break;

                    #endregion

                    default:
                         Console.WriteLine( TimeUtil.GetTimestamp() + Seperators.Spaceholder + InfoString.InfoIniDidNotFindProperConfiguration );
                         Console.ReadLine( );
                         return;
                }
            }
            catch
            {
                // fetching configuration data failed -> abort 
                Console.WriteLine( TimeUtil.GetTimestamp() + Seperators.Spaceholder + InfoString.FailedToLoadConfiguration );
                Console.ReadLine( );
            }

            _homeAutomationCommand = setting_value;
            FeedWatchDogTimer.Elapsed += FeedWatchDogTimer_Elapsed;

            switch (_homeAutomationCommand)
            {
                   #region ROOM_MODULES
                   case InfoOperationMode.SLEEPING_ROOM:
                        MyHomeSleepingRoom = new SleepingRoomNG( );
                        if( MyHomeSleepingRoom.Attached )
                        {
                            ComandInput(out AbortComand, InfoString.InfoTypeExit);
                            if (AbortComand == InfoString.Exit || AbortComand == "E" || AbortComand == "e")
                            {
                                MyHomeSleepingRoom.AllLightsOff();
                                MyHomeSleepingRoom.close();
                                break;
                            }
                        }
                        break;

                   case InfoOperationMode.CENTER_KITCHEN_AND_LIVING_ROOM:
                        MyHomeKitchenLivingRoom = new Center_kitchen_living_room_NG( serveripadress, serverPort, CompleteVersion );
                        if ( MyHomeKitchenLivingRoom.Attached )
                        {
                            ComandInput( out AbortComand, InfoString.InfoTypeExit );
                            MyHomeKitchenLivingRoom.TurnNextLightOn( );
                            if( AbortComand == InfoString.Exit || AbortComand == "E" )
                            {
                                MyHomeKitchenLivingRoom.StopAliveSignal( );
                                MyHomeKitchenLivingRoom.AllOutputsOff( );
                                MyHomeKitchenLivingRoom.close( );
                                break;
                            }
                        }
                        else
                        {
                            Console.ReadLine();
                        }
                        break;

                   case InfoOperationMode.ANTEROOM:
                        MyHomeAnteRoom = new AnteRoom( serveripadress, serverPort, CompleteVersion );
                        if( MyHomeAnteRoom.Attached )
                        {
                            ComandInput( out AbortComand, InfoString.InfoTypeExit );
                            if( AbortComand == InfoString.Exit || AbortComand == "E" )
                            {
                                MyHomeAnteRoom.StopAliveSignal( );
                                MyHomeAnteRoom.AllOutputs( false );
                                MyHomeAnteRoom.close( );
                                Environment.Exit(0);
                                break;
                            }
                        }
                        break;

                   case InfoOperationMode.LIVING_ROOM_EAST:
                        MyHomeLivingRoomEast = new livingroom_east( PhidgetSerialNumbers, serveripadress, serverPort, CompleteVersion );
                        MyHomeLivingRoomEast.SoftwareVersion = CompleteVersion;
                        ComandInput( out AbortComand, InfoString.InfoTypeExit );
                        if( AbortComand == InfoString.Exit || AbortComand == "E" )
                        {
                            MyHomeLivingRoomEast.AllCardsOutputsOff( );
                            MyHomeLivingRoomEast.Close( ); 
                            Environment.Exit( 0 );
                            break;
                        }
                        break;

                   case InfoOperationMode.LIVING_ROOM_WEST:
                        MyHomeLivingRoomWest = new livingroom_west();
                        ComandInput( out AbortComand, InfoString.InfoTypeExit );
                        if( AbortComand == InfoString.Exit || AbortComand == "E" )
                        {
                            MyHomeLivingRoomWest.AllOutputs( false );
                            MyHomeLivingRoomWest.close( );
                            Environment.Exit( 0 );
                        }
                        break;

                    case "NONE":
                        break;

                #endregion

                #region TEST_CASES
                   #region TEST_IO_CONTROL
                     case InfoOperationMode.TEST_PWM:
                           HeaterPWM = new UnivPWM( 300, 300, true );
                           HeaterPWM.PWM_ += HeaterPWM_PWM_;
                           Console.WriteLine( DateTime.Now + " PWM is INACTIVE " ); 
                           HeaterPWM.Start( 7 );
                           Console.ReadLine( );
                           break;

                     case InfoOperationMode.IO_CTRL:
                          Roof                      =  new BuildingSection();
                          LightEffect               =  new Effects( Roof );
                          SelfTestTimer.Interval    =  Parameters.SelfTestIntervallTime;
                          SelfTestTimer.Elapsed    += SelfTestTimer_Elapsed;
                          Roof.TimedOutputFeature_ += Roof_TimedOutputFeature_;
                          if ( Roof.Attached )
                          {
                               LightLoop( ComandoString.SELF_TEST );
                          }
                          break;

                     case InfoOperationMode.LED_CTRL:
                          LedControl Leds = new LedControl( );
                          Console.WriteLine( Leds.Name );
                          LedTestLoop( ref Leds );
                          Console.ReadLine( );
                          break;

                     case InfoOperationMode.ANALOG_HEATER:
                          AnalogHeaterControl Heater = new AnalogHeaterControl(  );  //277254
                          Heater.On( );
                          Console.ReadLine( );
                          Heater.Reset( );
                          break;
                     #endregion

                   #region TEST_SERVER
                     case InfoOperationMode.TCP_COMUNICATION_SERVER_INTERNAL:
                          Server TCPServer_ = new Server( );
                          do
                          {
                              ComandInput( out AbortComand, "type ...(E)XIT... for leave" );
                          } while( AbortComand != InfoString.Exit  );
                          break;

                    case InfoOperationMode.TCP_COMUNICATION_SERVER:
                         Services.RunApplicationOnce();
                         TCPServer = new ServerQueue( Convert.ToInt16( serverPort ) );
                         TCPServer.MessageReceivedFromClient += TCPServer_MessageReceivedFromClient;
                         do
                         {
                             Console.WriteLine("Message send to Client: ");
                             string MessageToClient = Console.ReadLine();
                             Console.WriteLine( "Client ID: " );
                             string ClientID = Console.ReadLine( );
                             TCPServer.SendMessageToClient( MessageToClient, ClientID );
                         } while( AbortComand.ToUpper() != InfoString.Exit );
                         break;
                   // Test
                   // When server starts, every n seconds a UDP packet is sent - this packet will itend a reconnection trial of the client -
                   // in case the client was not restarted
                   case InfoOperationMode.TCP_COMUNICATION_SERVER_INVITE:
                        Services.RunApplicationOnce();
                        TCPServer = new ServerQueue( Convert.ToInt16( serverPort ) );
                        TCPServer.ShowInternalReceivedMessageFromClient = true;
                        TCPServer.MessageReceivedFromClient += TCPServer_MessageReceivedFromClient;
                        // send a Broadcast for "invite clients" to auto connect
                        try
                        {
                            UDP_SendClientInvitation = new UdpSend( IPConfiguration.Address.IP_ADRESS_BROADCAST, IPConfiguration.Port.PORT_CLIENT_INVITE );
                            UDP_SendClientInvitation.SendString( InfoString.RequestForClientConnection );
                        }
                        catch ( Exception ex )
                        {
                            Console.WriteLine( "Send invitation data is not possible reason:" );
                            Console.WriteLine( ex.Message );
                        }

                        if ( Timer_ClientInvitation != null )
                        {
                             Timer_ClientInvitation.Elapsed += Timer_ClientInvitation_Elapsed;
                             Timer_ClientInvitation.Start();
                        }
                        Console.ReadLine();
                        do
                        {
                            Console.WriteLine( "Message send to Client (s_exit for terminate) : " );
                            string MessageToClient = Console.ReadLine();
                            if ( MessageToClient == "s_exit" )
                            {
                                 break;
                            }
                            Console.WriteLine( "Client ID: " );
                            string ClientID = Console.ReadLine();
                            TCPServer.SendMessageToClient( MessageToClient, ClientID );
                            
                        } while ( true );
                        break;

                   case InfoOperationMode.TCP_COMUNICATION_SERVER_TEST_GET_MESSAGES_FROM_CLIENTS:
                        Services.RunApplicationOnce();
                        TCPServer = new ServerQueue( Convert.ToInt16( serverPort ) );
                        TCPServer.MessageReceivedFromClient += TCPServer_MessageReceivedFromClient;
                        // send a Broadcast for "invite clients" to auto connect
                        try
                        {
                            UDP_SendClientInvitation = new UdpSend( IPConfiguration.Address.IP_ADRESS_BROADCAST, IPConfiguration.Port.PORT_CLIENT_INVITE );
                            UDP_SendClientInvitation.SendString( InfoString.RequestForClientConnection );
                        }
                        catch ( Exception ex )
                        {
                            Console.WriteLine( "Send invitation data is not possible reason:" );
                            Console.WriteLine( ex.Message );
                        }

                        if ( Timer_ClientInvitation != null )
                        {
                             Timer_ClientInvitation.Elapsed += Timer_ClientInvitation_Elapsed;
                             Timer_ClientInvitation.Start();
                        }
                        Console.ReadLine();
                        break;

                   case InfoOperationMode.TCP_COMUNICATION_SERVER_SEND_PERIODIC:
                   case InfoOperationMode.TCP_COMUNICATION_SERVER_SEND_PERIODIC_LOCALHOST:
                        Services.RunApplicationOnce();
                        TCPServer = new ServerQueue( Convert.ToInt16( serverPort ) );
                        TCPServer.MessageReceivedFromClient += TCPServer_MessageReceivedFromClient;
                        // send a Broadcast for "invite clients" to auto connect
                        try
                        {
                            if( _homeAutomationCommand == InfoOperationMode.TCP_COMUNICATION_SERVER_SEND_PERIODIC )
                            {
                                UDP_SendClientInvitation = new UdpSend( IPConfiguration.Address.IP_ADRESS_BROADCAST, IPConfiguration.Port.PORT_CLIENT_INVITE );
                            }

                            if( _homeAutomationCommand == InfoOperationMode.TCP_COMUNICATION_SERVER_SEND_PERIODIC_LOCALHOST )
                            {
                                UDP_SendClientInvitation = new UdpSend( IPConfiguration.Address.IP_ADRESS_LOCALHOST, IPConfiguration.Port.PORT_CLIENT_INVITE );
                            }

                            UDP_SendClientInvitation.SendString( InfoString.RequestForClientConnection );
                        }
                        catch( Exception ex )
                        {
                            Console.WriteLine( "Send invitation data is not possible reason:" );
                            Console.WriteLine( ex.Message );
                        }

                        if( Timer_ClientInvitation != null )
                        {
                            Timer_ClientInvitation.Elapsed += Timer_ClientInvitation_Elapsed;
                            Timer_ClientInvitation.Start( );
                        }

                        Timer_SendPeriodicDataToClient.Elapsed += Timer_SendPeriodicDataToClient_Elapsed;
                        Console.ReadLine( );
                        Timer_SendPeriodicDataToClient.Start( );
                        Console.ReadLine( );
                        Timer_SendPeriodicDataToClient.Stop( );
                        Console.ReadLine( );
                        break;
                   #endregion

                   #region TEST_CLIENT
                    case InfoOperationMode.TCP_COMUNICATION_CLIENT_INTERNAL:
                         string testclientcomand_;
                         string serverIP_ = "127.0.0.1";
                         ComandInput( out testclientcomand_, "(H) for hello" );
                         if( testclientcomand_ == "H" || testclientcomand_ == "h" )
                         {
                             Client Client_ = new Client( serverIP_, 5000, "Hello Server - This is Raspberry" );
                             string MessageToSend;
                             do
                             {
                               Console.Write( "Message: " );
                               MessageToSend = Console.ReadLine( );
                               if( Client_ != null )
                               {
                                   Client_.WriteMessage( MessageToSend );
                               }
                             } while( MessageToSend.ToUpper() != InfoString.Exit );
                             Client_.Disconnect( );
                         }
                         break;

                    case InfoOperationMode.TCP_COMUNICATION_CLIENT:
                         {
                             Console.WriteLine( "Hello - Please enter client name: " );
                             string UserDefinedClientID_ = Console.ReadLine( );
                             Client_ = new ClientTalktive_( serveripadress, Convert.ToInt16( serverPort ), "Hello Server - This is CLIENT_" + UserDefinedClientID_ + "  my hostname is" );
                             Client_.MessageReceivedFromServer += Client__MessageReceivedFromServer;
                             do
                             {
                               ClientReconnect( ref Client_, IPConfiguration.Prefix.TCPCLIENT + UserDefinedClientID_ );
                               Console.ReadLine( );
                             } while( AbortComand.ToUpper() != InfoString.Exit );
                         }
                         break;

                    case InfoOperationMode.TCP_COMUNICATION_CLIENT_SINGLE_MESSAGES:
                         {
                             Console.WriteLine( "Hello - Please enter client name: " );
                             string UserDefinedClientID_ = Console.ReadLine();
                             Client_ = new ClientTalktive_( serveripadress, Convert.ToInt16( serverPort ), "Hello Server - This is CLIENT_" + UserDefinedClientID_ + "  my hostname is" );
                             Client_.MessageReceivedFromServer += Client__MessageReceivedFromServer;
                             do
                             {
                                 Console.Write( "Message: " );
                                 string MessageToSend = Console.ReadLine();
                                 if( Client_ != null )
                                 {
                                     Client_.WriteMessageWithLengthInformation( MessageToSend );
                                 }
                             } while( AbortComand.ToUpper() != InfoString.Exit );
                         }
                         break;
                   
                    case InfoOperationMode.TCP_COMUNICATION_CLIENT_SEND_PERIODIC:
                         {
                            try
                            {
                                UDP_ReceiveClientInvitation = new UdpReceive( IPConfiguration.Port.PORT_CLIENT_INVITE );
                                UDP_ReceiveClientInvitation.EDataReceived += UDP_ReceiveClientInvitation_EDataReceived;
                            }
                            catch ( Exception ex )
                            {
                                Console.WriteLine( "Receive invitation data is not possible reason:" );
                                Console.WriteLine( ex.Message );
                            }
                            Timer_SendPeriodicDataToServer.Elapsed += Timer_SendPeriodicDataToServer_Elapsed;
                            Timer_SendPeriodicDataToServer.Start();
                            Process currentProcess = Process.GetCurrentProcess();
                            Console.WriteLine( "Hello - This is client with Prozess ID: " + currentProcess.Id );
                            UserDefinedClientID_ = currentProcess.Id.ToString();
                            try
                            {
                                Client_ = new ClientTalktive_( serveripadress, Convert.ToInt16( serverPort ), "Hello Server - This is CLIENT_" + UserDefinedClientID_ + "  my hostname is" );
                            }
                            catch( Exception ex )
                            {
                                Console.WriteLine( "Failed to establish client " );
                                Console.WriteLine( ex.Data );
                            }
                            Console.ReadLine();
                        }
                        Environment.Exit( 0 );
                        break;

                   
                    case InfoOperationMode.TCP_COMUNICATION_CLIENT_INVITE:
                    case InfoOperationMode.TCP_COMUNICATION_CLIENT_INVITE_LOCALHOST:
                         {
                             try
                             {
                                 UDP_ReceiveClientInvitation = new UdpReceive( IPConfiguration.Port.PORT_CLIENT_INVITE );
                                 UDP_ReceiveClientInvitation.EDataReceived += UDP_ReceiveClientInvitation_EDataReceived;
                             }
                             catch( Exception ex )
                             {
                                 Console.WriteLine( "Receive invitation data is not possible reason:" );
                                 Console.WriteLine( ex.Message );
                             }
                             Console.WriteLine( "Hello - Please enter client name: " );
                             UserDefinedClientID_ = Console.ReadLine();
                             if( _homeAutomationCommand == InfoOperationMode.TCP_COMUNICATION_CLIENT_INVITE )
                             {
                                 Client_ = new ClientTalktive_( serveripadress, Convert.ToInt16( serverPort ), "Hello Server - This is CLIENT_" + UserDefinedClientID_ + "  my hostname is" );
                             }

                             if( _homeAutomationCommand == InfoOperationMode.TCP_COMUNICATION_CLIENT_INVITE_LOCALHOST )
                             {
                                 Client_ = new ClientTalktive_( IPConfiguration.Address.IP_ADRESS_LOCALHOST, Convert.ToInt16( serverPort ), "Hello Server - This is CLIENT_" + UserDefinedClientID_ + "  my hostname is" );
                             }

                             Client_.MessageReceivedFromServer += Client__MessageReceivedFromServer;
                             Console.ReadLine();
                         }
                         Environment.Exit( 0 );
                         break;

                    case InfoOperationMode.TCP_COMUNICATION_CLIENT_STRESSTEST:
                         {
                             Client_ = new ClientTalktive_( serveripadress, Convert.ToInt16( serverPort ), "Hello Server - This is Raspberry" );
                             //Client_.MessageReceivedFromServer += Client__MessageReceivedFromServer;
                             CommTestTimer.Elapsed += CommTestTimer_Elapsed;
                             CommTestTimer.Interval = 50;
                             CommTestTimer.Start( );
                             if( !Client_.Connected )
                             {
                                 Console.Write( "Sorry - connection failed" );
                                 Console.ReadLine( );
                             }
                         }
                         break;
                   #endregion
                #endregion
            }

            Environment.Exit( 0 );
        }

        #region COMMON_EVENT_HANDLERS
        static void Timer_SendPeriodicDataToClient_Elapsed( object sender, ElapsedEventArgs e )
        {  
            string client = "CLIENT_1";
            if( TCPServer == null )
            {
                return;
            }
            
            for( int i= 0; i < 1; i++ )
            {
                string msg = transactioncounter++.ToString( ) + " HI " + client + " this is your server";
                TCPServer.SendMessageToClient( msg, client );
                Console.WriteLine( msg );
            }
        }

        static decimal transactioncounter;
        static void Timer_SendPeriodicDataToServer_Elapsed( object sender, ElapsedEventArgs e )
        {
            string FormattedCounter;
            if ( Client_ == null )
            {
                 return;
            }
            if ( Client_.Connected )
            {
                for( int i= 0; i < 1; i++ )
                {
                    FormattedCounter = String.Format("{0:000000000}",transactioncounter++ );
                    string msg = FormattedCounter + " Hi this is client with Prozess ID: " + UserDefinedClientID_.ToString();
                    Client_.WriteMessageWithLengthInformation( msg );
                    Console.WriteLine( msg );
               }
            }
        }

        // received invitation for reconnecting the client initated by server
        static void UDP_ReceiveClientInvitation_EDataReceived( string e )
        {
            if ( e == InfoString.RequestForClientConnection )
            {
                if ( Client_ != null )
                {
                    ClientAutoReconnect( ref Client_, IPConfiguration.Prefix.TCPCLIENT + UserDefinedClientID_ );
                }
            }
        }

        static void Timer_ClientInvitation_Elapsed( object sender, ElapsedEventArgs e )
        {
            UDP_SendClientInvitation.SendString( InfoString.RequestForClientConnection );
        }

        static void HeaterPWM_PWM_ ( object sender, UnivPWM.ePWMStatus pwmstatus )
        {
            switch( pwmstatus )
            {
                case UnivPWM.ePWMStatus.eIsOn:
                     Console.WriteLine( DateTime.Now + " PWM is ON " + HeaterPWM.OnCounter.ToString() ); 
                     
                     break;
                case UnivPWM.ePWMStatus.eIsOff:
                     Console.WriteLine( DateTime.Now + " PWM is OFF "  + HeaterPWM.OnCounter.ToString( ) ); 
                     break;
            }
        }

        static void FeedWatchDogTimer_Elapsed ( object sender, ElapsedEventArgs e )
        {
            Console.WriteLine( "Feed Watchdog " + DateTime.Now.ToString() );
        }

        static void CommTestTimer_Elapsed ( object sender, ElapsedEventArgs e )
        {
            // Client_.WriteMessageWithHostnameAndTimestamp( "TEST.................HALLLOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOO" );
        }

        static void Client__MessageReceivedFromServer ( string receivedmessage )
        {
            if( Client_.Connected )
            {
                if( receivedmessage != "" )
                {
                    Console.WriteLine(receivedmessage);
                    if( Client_.ReceivedMessageQueue.Count > 0 )
                    {
                        Client_.ReceivedMessageQueue.Dequeue( );
                    }
                }
            }
            else
            {
                Console.WriteLine( "Sorry - connection failed" );
                Console.ReadLine( );
            }
       }

        static void TCPServer_MessageReceivedFromClient ( string receivedmessage )
        {
            //Console.WriteLine( receivedmessage );
            lock ( TCPServer.MessageQueue )
            {
                string message = TCPServer.MessageQueue.Dequeue();
                Console.WriteLine( "OUT OF QUEUE: " + message );
            }
        }
        #endregion

        #endregion  // MAIN

        #region FEATURES
        static void LedTestLoop( ref LedControl _Leds )
        {
            _Leds.Voltage = LED.LEDVoltage.VOLTAGE_5_0V;
            const int LastFragment = 4;
            const double MaxBrightness = 100;

            do
            {
                ComandInput( out _homeAutomationCommand, "LED_x_ON / LED_x_OFF / LED_1_BR_50.0 (LED_x_BR_y) ");
                // for test only
                //switch ( _homeAutomationCommand )
                //{
                //    case "LED_1_ON":
                //         _Leds.leds2[1].Brightness = 100;
                //         break;
                //    case "LED_1_OFF":
                //         _Leds.leds2[1].Brightness = 0;
                //         break;
                //}

                // Int32 Index = 
                Int32 Index=0;
                double Brightness = 0;
                uint count = 0;

                string [] Comando = _homeAutomationCommand.Split( new Char[] { '_' } );
                foreach ( string StingFragment in Comando )
                {
                   if ( StingFragment.Trim( ) != "" )
                    {
                        switch ( count )
                        {
                            case 0:
                                 if ( StingFragment != "LED" )
                                      break;
                                 else
                                 {
                                      count++;
                                      continue;
                                 }
                            // LED INDEX
                            case 1:
                                 if ( Int32.TryParse( StingFragment, out Index ) )
                                 {
                                      count++;
                                      continue;
                                 }
                                 else
                                      break;
                            // LED CONTROL
                            case 2:
                                 if ( StingFragment == "ON" )
                                 {
                                     Brightness = 100;
                                     count = LastFragment;
                                     continue;
                                 }
                                 if ( StingFragment == "OFF" )
                                 {
                                     Brightness = 0;
                                     count = LastFragment;
                                     continue;
                                 }
                                 if ( StingFragment != "BR" )
                                      break;
                                 else
                                 {
                                     count++;
                                     continue;
                                 }
                            // ON DEMAND BRIGHTNESS
                            case 3:
                                 if ( double.TryParse( StingFragment, out Brightness ) )
                                 {
                                     count++;
                                     continue;
                                 }
                                 else
                                      break;
                       }
                    }
                }

                // string parsing failed - default value for brightness
                if ( count < LastFragment )
                {
                     Brightness = 0;
                }

                if( Brightness <= MaxBrightness )
                {
                    _Leds.leds2[Index].Brightness = Brightness;
                }
                else
                {
                    _Leds.leds2[Index].Brightness = MaxBrightness;
                }


            } while ( true );
        }

        static void LightLoop ( string initcomand )
        {
            int init=0;
            do
            {
                if ( init++ < 2 )
                {
                    _homeAutomationCommand = initcomand;
                }
                else
                {
                    ComandInput( out _homeAutomationCommand, InfoString.AppCmdLstPrefix + ComandoString.COMAND_INFOTEXT );
                }

                if ( _homeAutomationCommand != ComandoString.BLINK )
                {
                    LightEffect.StopWalk( );
                    Roof.StopBlink( );
                    SelfTestTimer.Stop( );
                    Roof.AllLightsOn = false;
                }

                switch ( _homeAutomationCommand )
                {
                    case ComandoString.ON:
                         Roof.ButtonLightBarOn = true;
                         Roof.InhibitButtonComands = false;
                         Roof.AllLightsOn = true;
                         continue;
                    case ComandoString.OFF:
                         Roof.ButtonLightBarOn = false;
                         Roof.InhibitButtonComands = false;
                         Roof.AllLightsOn = false;
                         _toggleTimedOutput = false;
                         continue;
                    case ComandoString.BLINK:
                         Roof.InhibitButtonComands = true;
                         Roof.StartBlink( Parameters.BlinkIntervallTime );
                         continue;
                    case ComandoString.EFFECT_WALK:
                         Roof.InhibitButtonComands = true;
                         LightEffect.StartWalk( );
                         continue;
                    case ComandoString.SELF_TEST:
                         selftestStep = 0;
                         SelfTestTimer.Start( );
                         continue;
                    case ComandoString.SELF_TEST_OFF:
                         SelfTestTimer.Stop( );
                         continue;
                    case ComandoString.EXIT:
                         Roof.AllLightsOn = false;
                         Roof.close( );
                         Roof = null;
                         System.Threading.Thread.Sleep( 10 );  // avoid console crash - ask why
                         return;
                }
            } while ( true );

        }

        static void LightLoop ( )
        {
            do
            {
                ComandInput( out _homeAutomationCommand, InfoString.AppCmdLstPrefix + ComandoString.COMAND_INFOTEXT );

                if ( _homeAutomationCommand != ComandoString.BLINK )
                {
                     LightEffect.StopWalk( );
                     Roof.StopBlink( );
                     SelfTestTimer.Stop( );
                     Roof.AllLightsOn = false;
                }

                switch ( _homeAutomationCommand )
                {
                    case ComandoString.ON:
                         Roof.ButtonLightBarOn = true;
                         Roof.InhibitButtonComands = false;
                         Roof.AllLightsOn = true;
                         continue;
                    case ComandoString.OFF:
                         Roof.ButtonLightBarOn = false;
                         Roof.InhibitButtonComands = false;
                         Roof.AllLightsOn = false;
                         _toggleTimedOutput = false;
                         continue;
                    case ComandoString.BLINK:
                         Roof.InhibitButtonComands = true;
                         Roof.StartBlink( Parameters.BlinkIntervallTime );
                         continue;
                    case ComandoString.EFFECT_WALK:
                         Roof.InhibitButtonComands = true;
                         LightEffect.StartWalk( );
                         continue;
                    case ComandoString.SELF_TEST:
                         selftestStep = 0;
                         SelfTestTimer.Start( );
                         continue;
                    case ComandoString.SELF_TEST_OFF:
                         SelfTestTimer.Stop( );
                         continue;
                    case ComandoString.EXIT:
                         Roof.AllLightsOn = false;
                         Roof.close( );
                         Roof = null;
                         System.Threading.Thread.Sleep( 10 );  // avoid console crash - ask why
                         return;
                }
            } while ( true );

        }

        static bool _toggleTimedOutput=false;
 
        static void Roof_TimedOutputFeature_ ( ElapsedEventArgs e )
        {
            if ( !_toggleTimedOutput )
            {
                 Console.WriteLine( InfoString.AppPrefix + "Self Test active" );
                 SelfTestTimer.Start( );
            }
            else
            {
                SelfTestTimer.Stop( );
                LightEffect.StopWalk( );
                Roof.AllLightsOn = false;
                Roof.InhibitButtonComands = false;
                _homeAutomationCommand = ComandoString.OFF;
                Console.WriteLine( InfoString.AppPrefix + "Self Test OFF" );
            }
            _toggleTimedOutput = !_toggleTimedOutput;
        }

        static void SelfTest ( )
        {
            switch ( selftestStep )
            {
               case 0:
                    Roof.StopBlink( );
                    Roof.InhibitButtonComands = false;
                    Roof.AllLightsOn = true;
                    selftestStep = 10;
                    break;
               case 10:
                    Roof.AllLightsOn = false;
                    selftestStep = 1;
                    break;
               case 1:
                    Roof.InhibitButtonComands = true;
                    LightEffect.StartWalk( );
                    selftestStep = 2;
                    break;
               case 2:
                    LightEffect.StartWalk( Parameters.WalkIntervallTimeFast );
                    selftestStep = 3;
                    break;
               case 3:
                    LightEffect.StartWalk( 20 );
                    selftestStep = 4;
                    break;
               case 4:
                    LightEffect.StopWalk( );
                    Roof.StartBlink( 200 );
                    selftestStep = 0;
                    break;
            }

        }
  
        static int selftestStep = 0;
 
        static void SelfTestTimer_Elapsed ( object sender, ElapsedEventArgs e )
        {
               SelfTest( );
        }
        #endregion
    }
}
