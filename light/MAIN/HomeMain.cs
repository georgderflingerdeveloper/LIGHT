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
 
    class MyHomeMain
    {
        #region COMMON_DECLARATIONS
        static Timer                            SelfTestTimer                   =  new Timer();
        static Timer                            CommTestTimer                   =  new Timer();
        static Timer                            Timer_ClientInvitation          =  new Timer( Parameters.ClientInvitationIntervall );
        static Timer                            Timer_SendPeriodicDataToServer  =  new Timer( 1000 );
        static Timer                            Timer_SendPeriodicDataToClient  =  new Timer( 1000 );
        static SleepingRoomNG                   MyHomeSleepingRoom;
        static Center_kitchen_living_room_NG    MyHomeKitchenLivingRoom;
        static AnteRoom                         MyHomeAnteRoom;
        static ServerQueue                      TCPServer;
        static ClientTalktive_                  Client_;
        static UdpSend                          UDP_SendClientInvitation;
        static UdpReceive                       UDP_ReceiveClientInvitation;
        static livingroom_east                  MyHomeLivingRoomEast;
        static livingroom_west                  MyHomeLivingRoomWest;
        static string                           _homeAutomationCommand = "";
        static string                           AbortComand            = "";
        static string                           setting_value          = "";
        static string                           serveripadress         = "";
        static string                           serverPort             = "";
        static string                           UserDefinedClientID_   = "";
        static int[]                            PhidgetSerialNumbers;     // container of serial number when more than one card is used
        static Dictionary<string, string>       PhidgetsIds = new Dictionary<string, string>( );   // contains ID´s of phidgets provided f.e. by ini file
        static UnivPWM                          HeaterPWM;
        static string                           Version;  // General Version information - valid for all "rooms"
        static string                           CompleteVersion;
        #endregion

        #region helpers
        // program information last build
        private static DateTime BuildDate
        {
            get { return File.GetLastWriteTime( Assembly.GetExecutingAssembly( ).Location ); }
        }

        static bool IsProgrammAborted( string command )
        {
 			return ( (command == InfoString.Exit || command == "E") ? true : false);
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

		static void WaitUntilKeyPressed( )
		{
			Console.WriteLine( "Press any key to terminate application ...");
			Console.ReadKey(true);
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

            switch (_homeAutomationCommand)
            {
                   #region ROOM_MODULES
                   case InfoOperationMode.SLEEPING_ROOM:
                        MyHomeSleepingRoom = new SleepingRoomNG( );
                        if( MyHomeSleepingRoom.Attached )
                        {
                            ComandInput(out AbortComand, InfoString.InfoTypeExit);
                            if ( IsProgrammAborted( AbortComand ) )
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
                            MyHomeKitchenLivingRoom.TurnNextLightOn( );
						    WaitUntilKeyPressed();
                            MyHomeKitchenLivingRoom.StopAliveSignal( );
                            MyHomeKitchenLivingRoom.AllOutputsOff( );
                            MyHomeKitchenLivingRoom.close( );
                        }
                        else
                        {
						    WaitUntilKeyPressed();
                        }
                        break;

                   case InfoOperationMode.ANTEROOM:
                        MyHomeAnteRoom = new AnteRoom( serveripadress, serverPort, CompleteVersion );
                        if( MyHomeAnteRoom.Attached )
                        {
                            ComandInput( out AbortComand, InfoString.InfoTypeExit );
                            if( IsProgrammAborted( AbortComand ) )
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
                        if ( IsProgrammAborted(AbortComand) )
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
                        if ( IsProgrammAborted(AbortComand) )
                        {
                            MyHomeLivingRoomWest.AllOutputs( false );
                            MyHomeLivingRoomWest.close( );
                            Environment.Exit( 0 );
                        }
                        break;

                   case "NONE":
                        break;

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
            lock ( TCPServer.MessageQueue )
            {
                string message = TCPServer.MessageQueue.Dequeue();
                Console.WriteLine( "OUT OF QUEUE: " + message );
            }
        }
        #endregion

        #endregion  // MAIN
    }
}
