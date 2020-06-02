using Filehandling;
using HomeAutomation.HardConfig_Collected;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using SystemServices;

namespace HomeAutomation
{
    class MyHomeMain
    {
        #region COMMON_DECLARATIONS
        static SleepingRoomNG                   MyHomeSleepingRoom;
        static Center_kitchen_living_room_NG    MyHomeKitchenLivingRoom;
        static AnteRoom                         MyHomeAnteRoom;
        static Livingroom_east                  MyHomeLivingRoomEast;
        static livingroom_west                  MyHomeLivingRoomWest;
        static string                           _homeAutomationCommand = "";
        static string                           AbortComand            = "";
        static string                           setting_value          = "";
        static string                           serveripadress         = "";
        static string                           serverPort             = "";
        static string AutoOnHeaterLivingRoom = "";
        static int[]                            PhidgetSerialNumbers;     // container of serial number when more than one card is used
        static Dictionary<string, string>       PhidgetsIds = new Dictionary<string, string>( );   // contains ID´s of phidgets provided f.e. by ini file
        static string                           Version;  // General Version information - valid for all "rooms"
        static string                           CompleteVersion;
		static bool                             _EnableConsoleIoOutput = true;
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

  
		static void WaitUntilKeyPressed( )
		{
			Console.WriteLine( InfoString.PressEnterForTerminateApplication );
			while(Console.ReadKey(true).Key != ConsoleKey.Enter) 
			{ Thread.Sleep(1000); };
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
                Version         = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                CompleteVersion = InfoString.InfoVersion + Version   + Seperators.WhiteSpace + InfoString.InfoLastBuild + BuildDate;
                Console.WriteLine( TimeUtil.GetTimestamp()           + 
                                   Seperators.WhiteSpace             + 
                                   InfoString.InfoVersioninformation + 
                                   Version                           + 
                                   Seperators.WhiteSpace             + 
                                   InfoString.InfoLastBuild          + 
                                   BuildDate.ToString() );
                Console.WriteLine( TimeUtil.GetTimestamp() + Seperators.WhiteSpace + InfoString.InfoLoadingConfiguration );
                setting_value  = INIUtility.Read( InfoString.ConfigFileName, InfoString.IniSection, InfoObjectDefinitions.Room);
                serveripadress = INIUtility.Read( InfoString.ConfigFileName, InfoString.IniSection, InfoObjectDefinitions.Server );
                serverPort     = INIUtility.Read( InfoString.ConfigFileName, InfoString.IniSection, InfoObjectDefinitions.Port);
                AutoOnHeaterLivingRoom = INIUtility.Read( InfoString.ConfigFileName, InfoString.IniSection, InfoObjectDefinitions.HeatersLivingRoomAutomaticOnOff );
                try
                {
                    PhidgetsIds     = INIUtility.ReadAllSection( InfoString.ConfigFileName, InfoString.IniSectionPhidgets );
                    int i = 0;
                    PhidgetSerialNumbers = new int[PhidgetsIds.Count];
                    foreach( KeyValuePair<string, string> pair in PhidgetsIds )
                    {
                        Console.WriteLine( TimeUtil.GetTimestamp() + Seperators.WhiteSpace + InfoString.InfoExpectingPhidget + pair.Key + Seperators.WhiteSpace + pair.Value );
                        PhidgetSerialNumbers[i] = Convert.ToInt32(pair.Value);
                        i++;
                    }
                }
                catch (Exception ex)
                {
					Services.TraceMessage_( ex.Message.ToString() );
                    Console.WriteLine( TimeUtil.GetTimestamp() + Seperators.WhiteSpace + InfoString.InfoNoConfiguredPhidgetIDused );
                }

                Console.WriteLine( TimeUtil.GetTimestamp() + Seperators.WhiteSpace + InfoString.InfoLoadingConfigurationSucessfull );
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
                         Console.WriteLine( TimeUtil.GetTimestamp() + Seperators.WhiteSpace + InfoString.InfoIniDidNotFindProperConfiguration );
						 break;
                }
            }
			catch( Exception ex )
            {
 				Services.TraceMessage_( ex.Message.ToString() );
                Console.WriteLine( TimeUtil.GetTimestamp() + Seperators.WhiteSpace + InfoString.FailedToLoadConfiguration );
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
                        WaitUntilKeyPressed();
                        break;

                   case InfoOperationMode.CENTER_KITCHEN_AND_LIVING_ROOM:
                       MyHomeKitchenLivingRoom = new Center_kitchen_living_room_NG
                       ( 
                         new LivingRoomConfig( )
                             {
                               IpAdressServer             = serveripadress,
                               PortServer                 = serverPort,
                               softwareversion            = CompleteVersion,
                               HeatersLivingRoomAutomatic = AutoOnHeaterLivingRoom
                             } 
                       );  
                       if ( MyHomeKitchenLivingRoom.Attached )
                       {
						    MyHomeKitchenLivingRoom.DigitalInputChanged  += RoomsIoHandling_EDigitalInputChanged;
						    MyHomeKitchenLivingRoom.EDigitalOutputChanged += RoomIoHandling_EDigitalOutputChanged;
                            MyHomeKitchenLivingRoom.ActivateDeviceControl( );
						    WaitUntilKeyPressed();
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
                                break;
                            }
                        }
                        break;

                   case InfoOperationMode.LIVING_ROOM_EAST:
                        MyHomeLivingRoomEast = new Livingroom_east( PhidgetSerialNumbers, serveripadress, serverPort, CompleteVersion )
                        {
                           SoftwareVersion = CompleteVersion
                        };
                        MyHomeLivingRoomEast.EDigitalInputChanged += RoomsIoHandling_EDigitalInputChanged;
                        WaitUntilKeyPressed( );
                        MyHomeLivingRoomEast.AllCardsOutputsOff( );
                        MyHomeLivingRoomEast.Close( ); 
                        break;

                   case InfoOperationMode.LIVING_ROOM_WEST:
                        MyHomeLivingRoomWest = new livingroom_west();
                        ComandInput( out AbortComand, InfoString.InfoTypeExit );
                        if ( IsProgrammAborted(AbortComand) )
                        {
                            MyHomeLivingRoomWest.AllOutputs( false );
                            MyHomeLivingRoomWest.close( );
                        }
                        break;

                   case "NONE":
                        break;

                #endregion
            }

			Console.WriteLine( TimeUtil.GetTimestamp()                               + 
			                   Seperators.WhiteSpace                                 + 
			                   Assembly.GetExecutingAssembly().GetName().FullName    +
			                   Seperators.WhiteSpace                                 +
			                   InfoString.Terminated);
			
            Environment.Exit( 0 );
        }

        #region COMMON_EVENT_HANDLERS

		static void RoomsIoHandling_EDigitalInputChanged( object sender, BASIC_COMPONENTS.DigitalInputEventargs e )
		{
			if( !_EnableConsoleIoOutput )
			{
				return;
			}

			if( sender is Center_kitchen_living_room_NG )
			{
                // no need for output
                if ( e.Index == CenterLivingRoomIODeviceIndices.indDigitalInputPowerMeter )
                {
                    return;
                }

                Console.WriteLine( TimeUtil.GetTimestamp_()                                     + 
				                   Seperators.WhiteSpace                                        + 
				                   InfoString.DeviceDigitalInput                                +
				                   Seperators.WhiteSpace                                        + 
				                   InfoString.BraceOpen                                         +
				                   e.Index.ToString()                                           +
				                   InfoString.BraceClose                                        +
				                   Seperators.WhiteSpace                                        + 
				                   KitchenCenterIoDevices.GetInputDeviceName(e.Index)           + 
				                   Seperators.WhiteSpace                                        + 
				                   InfoString.Is                                                + 
				                   Seperators.WhiteSpace                                        + 
				                   e.Value.ToString() );
 			}
            if( sender is Livingroom_east )
            {
                Console.WriteLine( TimeUtil.GetTimestamp_( )                                    +
                                   Seperators.WhiteSpace                                        +
                                   InfoString.DeviceDigitalInput                                +
                                   Seperators.WhiteSpace                                        +
                                   InfoString.BraceOpen                                         +
                                   e.Index.ToString( )                                          +
                                   InfoString.BraceClose                                        +
                                   Seperators.WhiteSpace                                        +
                                   EastSideIOAssignment.GetInputDeviceName( e.Index )           +
                                   Seperators.WhiteSpace                                        +
                                   InfoString.Is                                                +
                                   Seperators.WhiteSpace                                        +
                                   e.Value.ToString( ) );

            }
        }

		static void RoomIoHandling_EDigitalOutputChanged(object sender, BASIC_COMPONENTS.DigitalOutputEventargs e)
		{
			if( !_EnableConsoleIoOutput )
			{
				return;
			}
			
			if( sender is Center_kitchen_living_room_NG )
			{

				Console.WriteLine( TimeUtil.GetTimestamp_()                                     + 
				                   Seperators.WhiteSpace                                        + 
				                   InfoString.DeviceDigialOutput                                +
				                   Seperators.WhiteSpace                                        + 
				                   InfoString.BraceOpen                                         +
				                   e.Index.ToString()                                           +
				                   InfoString.BraceClose                                        +
				                   Seperators.WhiteSpace                                        + 
				                   KitchenCenterIoDevices.GetOutputDeviceName(e.Index)          + 
				                   Seperators.WhiteSpace                                        + 
				                   InfoString.Is                                                + 
				                   Seperators.WhiteSpace                                        + 
				                   e.Value.ToString() );

                string DeviceName = KitchenCenterIoDevices.GetOutputDeviceName(e.Index);
            }
		}
 
       #endregion

        #endregion  // MAIN
    }
}
