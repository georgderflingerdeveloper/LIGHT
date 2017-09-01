using System;
using System.Collections.Generic;
using SystemServices;
using System.Data.Odbc;

namespace HomeAutomation
{
    namespace HardConfig
    {
        #region GENERAL_CONFIGS
        static class HADictionaries
        {
            public static Dictionary<string, int> DeviceDictionaryCenterdigitalOut = new Dictionary<string, int>
            {
                {HardwareDevices.Boiler,                                         CenterLivingRoomIODeviceIndices.indDigitalOutputBoiler                                     },
                {HardwareDevices.PumpCirculation,                                WaterHeatingSystemIODeviceIndices.indDigitalOutputWarmWaterCirculationPump                 },
                {HardwareDevices.PumpWarmwater,                                  CenterLivingRoomIODeviceIndices.indDigitalOutputPumpHeatingSystem                          },
                {HardwareDevices.HeaterAnteRoom,                                 AnteRoomIODeviceIndices.indDigitalOutputAnteRoomHeater                                     },
                {HardwareDevices.HeaterLivingRoomEast,                           KitchenLivingRoomIOAssignment.indFirstHeater                                               },
                {HardwareDevices.HeaterLivingRoomWest,                           KitchenLivingRoomIOAssignment.indLastHeater                                                },
                {HardwareDevices.HeaterBathRoom,                                 BathRoomIODeviceIndices.indDigitalOutputBathRoomHeater                                     },
                {HardwareDevices.HeaterSleepingRoom,                             SleepingRoomIODeviceIndices.indDigitalOutputHeater                                         },
 //TODO         {HardwareDevices.HeaterNursery,                                  SleepingRoomIODeviceIndices.indDigitalOutputHeater                                         },
                {InfoString.DeviceNotFound,                                      GeneralConstants.DeviceNotFound                                                            },
            };

            public static Dictionary<string, int> DeviceDictionaryCenterdigitalIn = new Dictionary<string, int>
            {
 // TODO        {HardwareDevices.HeaterDryerBathRoom,                            BathRoomIODeviceIndices.indDigitalOutputBathRoomHeater                                     },
                {HardwareDevices.DoorEntryAnteRoom,                              CenterLivingRoomIODeviceIndices.indDigitalInputDoorEntryAnteRoom                           },
                {InfoString.DeviceNotFound,                                      GeneralConstants.DeviceNotFound                                                            },
            };

            public static Dictionary<string, int> DeviceDictionaryAnteRoomdigitalOut = new Dictionary<string, int>
            {
                {InfoString.DeviceNotFound,                                      GeneralConstants.DeviceNotFound                                                            },
            };

            public static Dictionary<string, int> DeviceDictionaryAnteroomdigitalIn = new Dictionary<string, int>
            {
                {InfoString.DeviceNotFound,                                      GeneralConstants.DeviceNotFound                                                            },
            };


            public static string GetKeyByValue( ref Dictionary<string, int> dic, int value )
            {
                foreach( KeyValuePair<string, int> pair in dic )
                {
                    if( pair.Value == value )
                    {
                        return pair.Key;
                    }
                }
                return InfoString.DeviceNotFound;
            }

            public static Dictionary<string, string> DeviceDictionaryTranslatorForNetworkCommands = new Dictionary<string, string>
            {
                {CenterKitchenDeviceNames.Boiler,                                      HardwareDevices.Boiler                                                            },
            };

        }

        static class EscapeSequences
        {
            public const string CRLF = "\r\n";
            public const string CR   = "\r";
        }

        static class DeviceCathegory
        {
            public const string Door        = "Door";
            public const string Window      = "Window";
            public const string Mansard     = "Mansard";
            public const string Rainsensor  = "Rainsensor";
        }

        static class InfoObjectDefinitions
        {
            public const string Room                       = "ROOM";
            public const string Server                     = "SERVER";
            public const string Port                       = "PORT";
        }

        static class Seperators
        {
            public static char[] delimiterChars                   =  { ' ', ',', '.', ':', '\t', '#' };
            public static char[] delimiterCharsExtended           =  { ' ', ',', '.', ':', '\t', '#', '_' };
            public static char[] delimiterCharsSchedulerReserved  =  { ' ', '_' };
            public const string WhiteSpace   = " ";
            public const string InfoSeperator = "_";
        }

        static class GeneralConstants
        {
            public const double TimerDisabled         = 0;
            public const int    NumberOfOutputsIOCard = 16;
            public const int    NumberOfInputsIOCard  = 16;
            public const int    DeviceNotFound        = -1;
            public const string DigitalInput          = "DigitalInput:";
            public const string DigitalOutput         = "DigitalOutput:";
            // for some string operations : is intepreted as delimiter - so we don´t need this in certain cases!
            public const string DigitalInput_         = "DigitalInput";
            public const string DigitalOutput_        = "DigitalOutput";
            public const bool   ON                    = true;
            public const bool   OFF                   = false;
            public const bool   STARTAUTOMATICOFF     = false;
            public const bool   STOPAUTOMATICOFF      = false;
            public const double DURATION_COMMONTICK   = 1000.0;
			public const string SlashUsedInLinux      = "//";
			public const string BackSlash             = "\\";
        }

        static class FormatConstants
        {
            public const string TransactionNumberFormat = "{0:000000000}";
            public const string TransactionNumberFormat_ = "000000000";
        }

        public static class InfoString
        {
            public const string InfoPhidgetException                                   =  "Phidget Exception";
            public const string InfoNoIO                                               =  "No IO primer is attached ( probably unplugged )";
            public const string AppPrefix                                              =  "Home Automation Comander: ";
            public const string AppCmdLstPrefix                                        =  "Home Automation Comado List: ";
            public const string ConfigFileName                                         =  "conf.ini";
            public const string IniSection                                             =  "SECTION";
            public const string IniSectionPhidgets                                     =  "PHIDGETS";
            public const string OperationMode                                          =  "Selected Operation Mode: ";
            public const string DeviceNotFound                                         =  "Device not found!";
            public const string FailedToEstablishClient                                =  "Failed to establish client! ";
            public const string FailedToEstablishUDPBroadcast                          =  "Failed to establish UDP invitation Broadcast! ";
            public const string FailedToEstablishUDPReceive                            =  "Failed to establisch UDP receive!";
            public const string FailedToEstablishUDPSend                               =  "Failed to establisch UDP send!";
            public const string FailedToEstablishServer                                =  "Failed to establish server! ";
            public const string ReceiveInvitationNotPossible                           =  "Receive invitation data is not possible! reason:";
            public const string GreetingToServer                                       =  "Hello Server - This is ";
            public const string MyComputernameIs                                       =  "  my computername is";
            public const string SorryServerIsNotConnectedWithClient                    =  "Sorry server is not connected with client!";
            public const string ComunicationProtocollError                             =  "Home automation comunication protocoll Error!";
            public const string SchedulerIsStartingDevice                              =  "Scheduler is Starting Device: ";
            public const string SchedulerIsStopingDevice                               =  "Scheduler is Stopping Device: ";
            public const string SchedulerIsRescheduling                                =  "Scheduler is rescheduling: ";
            public const string Asking                                                 =  "Is asking ...";
            public const string Scheduler                                              =  "Scheduler";
            public const string StatusOf                                               =  "Status of ";
            public const string Is                                                     =  "is:";
            public const string FailedToRecoverSchedulerData                           =  "Failed to recover scheduler data!";
            public const string LocationOutside                                        =  "Outside";
            public const string InfoConnectionFailed                                   =  "Sorry - connection failed - press enter for reconnect";
            public const string InfoTryToReconnect                                     =  "Try to reconnect ...";
            public const string InfoStartingTime                                       =  "Starting time: ";
            public const string InfoVersion                                            =  "Version: ";
            public const string InfoLastBuild                                          =  "Last build: ";
            public const string InfoVersioninformation                                 =  "Versioninformation: ";
            public const string InfoLoadingConfiguration                               =  "Loading configuration ...";
            public const string InfoExpectingPhidget                                   =  "Expecting Phidget ";
            public const string InfoNoConfiguredPhidgetIDused                          =  "No configured Phidget ID´s used...";
            public const string InfoLoadingConfigurationSucessfull                     =  "Loading configuration successfull";
            public const string InfoIniDidNotFindProperConfiguration                   =  "Did not find any proper configuration! - check content of ini file";
            public const string FailedToLoadConfiguration                              =  "Failed to load configuration!";
            public const string InfoTypeExit                                           =  "press (E) or (enter) ... for leave";
            public const string Exit                                                   =  "EXIT";
            public const string StartTimerForRecoverScheduler                          =  "Start timer for recover scheduler ...";
            public const string RemainingTime                                          =  "Remaining time ... ";
            public static readonly string RequestForClientConnection                   =  IPConfiguration.Prefix.TCPCLIENT + "ConnectToMe";
			public const string PressAnyKeyToTerminateApplication                      =  "Press any key to terminate application ...";
			public const string PressEnterForTerminateApplication                      =  "Press enter for terminate application";
			public const string Terminated                                             =  "Terminated";
			public const string DeviceDigitalInput                                     =  "Device digital input ";
			public const string DeviceDigialOutput                                     =  "Device digital output";
			public const string BraceOpen                                              =  "(";
			public const string BraceClose                                             =  ")";
            public const string On                                                     =  "ON";
            public const string Off                                                    =  "OFF";
		}

        static class InfoOperationMode
        {
            public const string SLEEPING_ROOM                                           =  "SLEEPINGROOM";
            public const string CENTER_KITCHEN_AND_LIVING_ROOM                          =  "CENTERKITCHENLIVINGROOM";
            public const string ANTEROOM                                                =  "ANTEROOM";
            public const string LIVING_ROOM_EAST                                        =  "LIVINGROOMEAST";
            public const string LIVING_ROOM_WEST                                        =  "LIVINGROOMWEST";
            public const string OUTSIDE                                                 =  "OUTSIDE";
            public const string IO_CTRL                                                 =  "IO_CTRL";
            public const string TEST_PWM                                                =  "TEST_PWM";
            public const string ANALOG_HEATER                                           =  "ANALOG_HEATER";
            public const string LED_CTRL                                                =  "LED_CTRL";
            public const string TCP_COMUNICATION_SERVER                                 =  "TCP_SERVER";
            public const string TCP_COMUNICATION_CLIENT                                 =  "TCP_CLIENT";
            public const string TCP_COMUNICATION_CLIENT_SINGLE_MESSAGES                 =  "TCP_CLIENT_SINGLE_MESSAGES";
            public const string TCP_COMUNICATION_CLIENT_INVITE                          =  "TCP_CLIENT_CONNECT_AFTER_INVITATION";
            public const string TCP_COMUNICATION_CLIENT_SEND_PERIODIC                   =  "TCP_CLIENT_SEND_PERIODIC";
            public const string TCP_COMUNICATION_CLIENT_INVITE_LOCALHOST                =  "TCP_CLIENT_CONNECT_AFTER_INVITATION_LOCALHOST";
            public const string TCP_COMUNICATION_CLIENT_INTERNAL                        =  "TCP_CLIENT_INTERNAL";
            public const string TCP_COMUNICATION_CLIENT_STRESSTEST                      =  "TCP_CLIENT_STRESSTEST";
            public const string TCP_COMUNICATION_SERVER_INTERNAL                        =  "TCP_SERVER_INTERNAL";
            public const string TCP_COMUNICATION_SERVER_TEST_GET_MESSAGES_FROM_CLIENTS  =  "TCP_SERVER_SHOW_CLIENT_MSG";
            public const string TCP_COMUNICATION_SERVER_SEND_PERIODIC                   =  "TCP_SERVER_SEND_PERIODIC";
            public const string TCP_COMUNICATION_SERVER_SEND_PERIODIC_LOCALHOST         =  "TCP_SERVER_SEND_PERIODIC_LOCALHOST";
            public const string TCP_COMUNICATION_SERVER_INVITE                          =  "TCP_SERVER_INVITE_CLIENTS";
        }

        public static class VirtualDevice
        {
            public const string GroupGalleryFloor                                       = "GroupGalleryFloor";
        }

		public static class HardwareDevices
        {
            public const string Boiler                                                  = "BOILER";
            public const string PumpWarmwater                                           = "PumpWarmwater";           // Zirkulationspumpe für Warmwasser ( Küche, Bad, Dusche )
            public const string PumpCirculation                                         = "PumpCirculation";         // Zirkulationspumpe Heizkreis
            public const string DoorEntryAnteRoom                                       = "DoorEntryAnteRoom";       // Eingangstür Vorhaus
            public const string HeaterLivingRoomEast                                    = "HeaterLivingRoomEast";    // Heizkörper Wohnzimmer Ost seitig
            public const string HeaterLivingRoomWest                                    = "HeaterLivingRoomWest";    // Heizkörper Wohnzimmer West seitig
            public const string HeaterAnteRoom                                          = "HeaterAnteRoom";
            public const string HeaterBathRoom                                          = "HeaterBathRoom";
            public const string HeaterDryerBathRoom                                     = "DryerBathRoom";
            public const string HeaterSleepingRoom                                      = "HeaterSleepingRoom";
            public const string HeaterNursery                                           = "HeaterNursery"; // kinderzimmer = nursery

            
            public static class Kitchen
            {
            }

            public static class LivingRoomEast
            {
            }

            public static class LivingRoomWest
            {
            }

            public static class Outside
            {
            }

            public static class RoofRoom
            {
            }

            public static class Gallery
            {
                public const string FloorSpotGroup1                         = "GalleryFloorSpotGroup1";
            }

            public static class AnteRoom
            {
            }

            public static class SleepingRoom
            {
                public const string SWindowWest                   = "SleepingRoomWindowWest";              // Fenster hinten Richtung Westen
                public const string SWindowNorth_Left             = "SleepingRoomWindowNorthLeft";         // Velux Fenster links
                public const string SWindowNorth_Right            = "SleepingRoomWindowNorthRight";        // Velux Fenster rechts
                public const string SFireAlert                    = "SleepingRoomFireAlert";               // fire alert - GIRA Rauchmelder
                public const string SHeater                       = "SleepingRoomHeater";                  // Heizkörper
                public const string SMansardRightEnd              = "SleepingRoomMansardRightEnd";         // Leuchte Mansarden ganz rechts
                public const string SBarMansardWindowRight        = "SleepingRoomBarMansardWindowRight";   // Led Balken Mansarden Fenster rechter Rand 
                public const string SBarMansardWindowMiddle       = "SleepingRoomBarMansardWindowMiddle";  // Led Balken zischen linken und rechten Mansarden Fenster 
                public const string SBarMansardWindowLeft         = "SleepingRoomBarMansardWindowLeft";    // Led Balken Mansarden Fenster rechter Rand 
                public const string SLightCeiling                 = "SleepingRoomLightCeiling";            // Leuchte an der Decke
            }

            public static class WashRoom
            {
            }

            public static class BathRoom
            {
            }
        }

        static class IPConfiguration
        {
            public static class Address
            {
				public const string IP_ADRESS_LOCALHOST        = "127.0.0.1";
                public const string IP_ADRESS_BROADCAST        = "192.168.0.255";
                public const string IP_ADRESS_SLEEPINGROOM     = "192.168.0.101";
                public const string IP_ADRESS_ANTEROOM         = "192.168.0.102";
                public const string IP_ADRESS_KITCHEN_CENTER   = "192.168.0.103";
                public const string IP_ADRESS_LIVING_ROOM_EAST = "192.168.0.104";
                public const string IP_ADRESS_LIVING_ROOM_WEST = "192.168.0.105";
                public const string IP_ADRESS_SERVER_GUI       = "192.168.0.111";      
            }

            public static class Port
            {
                public const int PORT_LIGHT_CONTROL_COMMON           = 5000; 
                public const int PORT_LIGHT_CONTROL_LIVING_ROOM_EAST = 5000; 
                public const int PORT_CLIENT                         = 6000;
				public const int PORT_SERVER                         = 5000;
                public const int PORT_CLIENT_INVITE                  = 8000;
                public const int PORT_UDP_SLEEPINGROOM               = 5001;
                public const int PORT_UDP_ANTEROOM                   = 5002;
                public const int PORT_UDP_CENTER                     = 5003;
                public const int PORT_UDP_LIVINGROOM_EAST            = 5009;
                public const int PORT_UDP_LIVINGROOM_WEST            = 5010;
                public const int PORT_UDP_WEB_FORWARDER_CENTER       = 10000;
                public const int PORT_UDP_IO_ECHO                    = 12000;
            }

            public static class Prefix
            {
                public const string TCPSERVER = "SERVER_";
                public const string TCPCLIENT = "CLIENT_";
            }
        }

        // TODO - make configurable
        static class Phidget_ID
        {
             public const int ID_LED_CARD_1                             = 336230;
             public const int ID_IFKIT_SLEEPING_ROOM                    = 312651;
             public const int ID_IFKIT_ANTEROOM_1                       = 0;
             public const int ID_IFKIT_ANTEROOM_2                       = 0;
             public const int ID_IFKIT_LIVINGROOM_1                     = 0;
             public const int ID_IFKIT_LIVINGROOM_2                     = 0;
             public const int ID_IFKIT_LIVINGROOM_EAST_1                = 311883;
             public const int ID_IFKIT_LIVINGROOM_EAST_2                = 346077;
             public const int ID_IFKIT_LIVINGROOM_WEST                  = 0;
        }

        static class MESSAGE_Constants
        {
            public const int LOCATION_INDEX_MESSAGE_TRANSACTION_COUNTER = 3;
            public const int LOCATION_INDEX_PURE_MESSAGE                = 13;
        }

        public static class FileExtensions
        {
            public const string StoredDataExtension          = ".xml";
        }
        #endregion

        #region GENERAL_HELPERS
        public static class GetData
        {
            public static string ValueFromDeviceDictionary( Dictionary<uint, string> dic, uint key )
            {
                // Try to get the result in the static Dictionary
                string result;
                if( dic.TryGetValue( key, out result ) )
                {
                    return result;
                }
                else
                {
                    return "";
                }
            }
        }
        #endregion

        #region STATUS_STRINGS
        public static class DeviceStatus
        {
            public const string LIGHTS_LIVING_ROOM_WEST_ARE_OFF = "LIGHTS-LIVING-ROOM-WEST-ARE-OFF";
            public const string LIGHTS_LIVING_ROOM_WEST_ARE_ON  = "LIGHTS-LIVING-ROOM-WEST-ARE-ON";
        }
        #endregion

        #region COMAND_STRINGS
        public static class ComandoString
        {
            public const string NONE                        = "NONE";
            public const string ON                          = "ON";
            public const string OFF                         = "OFF";
            public const string EXIT                        = "EXIT";
            public const string BLINK                       = "BLINK";
            public const string TURN_ALL_LIGHTS_OFF         = "TURN-ALL-LIGHTS-OFF";
            public const string TURN_ALL_LIGHTS_ON          = "TURN-ALL-LIGHTS-ON";
            public const string TURN_ALL_KITCHEN_LIGHTS_OFF = "TURN-ALL-KITCHEN-LIGHTS-OFF";
            public const string TURN_ALL_KITCHEN_LIGHTS_ON  = "TURN-ALL-KITCHEN-LIGHTS-ON";
            public const string TURN_GALLERY_DOWN_ON        = "TURN-GALLERY-DOWN-ON";
            public const string TURN_GALLERY_DOWN_OFF       = "TURN-GALLERY-DOWN-OFF";
            public const string TURN_BOILER_ON              = "TURN-BOILER-ON";
            public const string TURN_BOILER_OFF             = "TURN-BOILER-OFF";
            public const string TURN_FRONT_LIGHTS_ON        = "TURN-FRONT-LIGHTS-ON";
            public const string TURN_FRONT_LIGHTS_OFF       = "TURN-FRONT-LIGHTS-OFF";
            public const string TURN_WINDOW_LEDGE_EAST_ON   = "TURN-WINDOW-LEDGE-EAST-ON";
            public const string TURN_WINDOW_LEDGE_EAST_OFF  = "TURN-WINDOW-LEDGE-EAST-OFF";
            public const string TURN_WINDOW_LEDGE_WEST_ON   = "TURN-WINDOW-LEDGE-WEST-ON";
            public const string TURN_WINDOW_LEDGE_WEST_OFF  = "TURN-WINDOW-LEDGE-WEST-OFF";
            public const string TURN_ALL_LIGHTS_WEST_OFF    = "TURN-ALL-LIGHTS-WEST-OFF";
            public const string TURN_ALL_LIGHTS_WEST_ON     = "TURN-ALL-LIGHTS-WEST-ON";


            public const string TURN_LIGHT_OUTSIDE_ON       = "TURN-LIGHT-OUTSIDE-ON";
            public const string TURN_LIGHT_OUTSIDE_OFF      = "TURN-LIGHT-OUTSIDE-OFF";



            static Dictionary<uint, string> ComandoDictionary = new Dictionary<uint, string>
            {
                {1,                  ON                             },
                {2,                  OFF                            },
                {4,                  BLINK                          },
                {100,                EXIT                           }
            };

            public static string GetComando( uint comandokey )
            {
                return( GetData.ValueFromDeviceDictionary( ComandoDictionary, comandokey ) );
            }

            public static class Button
            {
                public const string Button_                    =  "Button";      
                public const string IsPressed                  =  "IS_PRESSED";
                public const string IsReleased                 =  "IS_RELEASED";
                public const string Udefined                   =  "UNDEFINED";
            }

            public static class Buttons
            {
                public const string MainDownRight              = "MAIN_DOWN_RIGHT";
                public const string MainDownLeft               = "MAIN_DOWN_LEFT";
                public const string MainUpLeft                 = "MAIN_UP_LEFT";
                public const string MainUpRight                = "MAIN_UP_RIGHT";
            }

            public static class Telegram
            {
                public const string Index                      = "IND";
                public const string State                      = "STA";
                public const char Seperator                    = '_';
                public const int IndexTransactionCounter       = 0;
                public const int IndexDigitalInputs            = 1;
                public const int IndexValueDigitalInputs       = 2;
            }
        }

        static class LedComandoString
        {
            public  const string  LED1_ON                       = "LED1_ON";
            public  const string  LED1_OFF                      = "LED1_OFF";
        }
        #endregion
 
        #region CENTRAL_CONTROL
        static class HardwareIOAssignment
        {
            public const int Input_ALL_ON                       = 0;
            public const int Input_TipNext                      = 1;
            public const int Output_Spot1                       = 0;
        }

        static class CommonRoomIOAssignment
        {
            public const int  indMainButton                     = 0;
            public const int  indDoor                           = 1;
            public const int  indOptionalWalk                   = 2;
            public const int  indOptionalMakeStep               = 3;
            public const int  indOutputIsAlive                  = 15;
        }

        static class CenterButtonRelayIOAssignment
        {
            public const int indDigitalInputRelaySleepingRoom    = 3;
            public const int indDigitalInputRelayWashRoom        = 4;
            public const int indDigitalInputRelayAnteRoom        = 5;
            public const int indDigitalInputRelayBathRoom        = 6;
        }

        static class ParametersRoomObserver
        {
            static readonly public double TimeDemandStartWindowObservingOnAbscence  = TimeConverter.ToMiliseconds( 5, 0 );
        }
        #endregion

        #region COMMON_LIGHT_CONTROL_PARAMETERS
        static class LightMode
        {
            public const int Walk      = 0;
            public const int MakeStep  = 1 ;
        }

        static class Parameters
        {
            public const Int32  AttachWaitTime                        = 15000;      
            public const double BlinkIntervallTime                    = 500;
            public const double SelfTestIntervallTime                 = 1000;
            public const double WalkIntervallTime                     = 100;
            public const double WalkIntervallTimeFast                 = 50;
            public const double TimeActiveateFeature                  = 3000;
            public const double TimedActivateSequence                 = 1500;
            public const double SequenceTime                          = 250;
            public const double TimeIntervallAlive                    = 500;
            public const double ClientInvitationIntervall             = 5000;
            public const double DelayTimeStartRecoverScheduler        = 10000;
            public const double MilisecondsOfSecond                   = 1000.0;
            public const bool   Unused = false;
        }

        static class ParametersLightControl
        {
            public const double TimeDemandForAllOn                     = 2500;
            public const double TimeDemandForSingleOff                 = 700;
            public const double TimeDemandForBroadcastAllOff           = 3000;
            static readonly public double TimeDemandForAutomaticOff    = TimeConverter.ToMiliseconds( 15, 0 );
        }
  
        static class ParametersLightControlSleepingRoom
        {
            static readonly public double TimeDemandForAutomaticOff    = TimeConverter.ToMiliseconds( 3, 0, 0 );
            static readonly public double TimeDemandForAllOn           = TimeConverter.ToMiliseconds( 8 );
            static readonly public double TimeDemandForAllOutputsOff   = TimeConverter.ToMiliseconds( 9 );
        }

        static class ParametersLightControlSleepingRoomNG
        {
            static readonly public double TimeDemandForAutomaticOff    = TimeConverter.ToMiliseconds( 3, 0, 0 );
            static readonly public double TimeDemandForAllOn           = TimeConverter.ToMiliseconds( 3 );
            static readonly public double TimeDemandForAllOutputsOff   = TimeConverter.ToMiliseconds( 9 );
        }
        #endregion

        #region GROUP_SLEEPING_ROOM
        static class SleepingRoomIODeviceIndices 
          {
            public const int indDigitalInputMainButton                   = 0;
            public const int indDigitalInputWindowWest                   = 2;  // Fenster hinten Richtung Westen
            public const int indDigitalInputMansardWindowNorthLeft       = 3;  // Velux Fenster links
            public const int indDigitalInputMansardWindowNorthRight      = 4;  // Velux Fenster rechts
            public const int indDigitalInputFireAlert                    = 5;  // fire alert - GIRA Rauchmelder

            public const int indDigitalOutputLightMansardRightEnd        = 0;  // Leuchte Mansarden ganz rechts
            public const int indDigitalOutputLightBarMansardWindowRight  = 1;  // Led Balken Mansarden Fenster rechter Rand 
            public const int indDigitalOutputLightBarMansardWindowMiddle = 2;  // Led Balken zischen linken und rechten Mansarden Fenster 
            public const int indDigitalOutputLightBarMansardWindowLeft   = 3;  // Led Balken Mansarden Fenster rechter Rand 
            public const int indDigitalOutputLightCeiling                = 4;  // Leuchte an der Decke
            public const int indDigitalOutputHeater                      = 5;  // Heizkörper

            static Dictionary<uint, string> InputDeviceDictionary = new Dictionary<uint, string>
            {
                 { indDigitalInputWindowWest,                        SleepingRoomDeviceNames.WindowWest                  },
                 { indDigitalInputMansardWindowNorthLeft,            SleepingRoomDeviceNames.MansardWindowNorthLeft      },
                 { indDigitalInputMansardWindowNorthRight,           SleepingRoomDeviceNames.MansardWindowNorthRight     },
                 { indDigitalInputFireAlert,                         SleepingRoomDeviceNames.FireAlert                   },
            };

            static Dictionary<uint, string> OutputDeviceDictionary = new Dictionary<uint, string>
            {
                 { indDigitalOutputHeater,                           SleepingRoomDeviceNames.Heater                      },
                 { indDigitalOutputLightMansardRightEnd,             SleepingRoomDeviceNames.LightMansardRightEnd        },
                 { indDigitalOutputLightBarMansardWindowRight,       SleepingRoomDeviceNames.LightBarMansardWindowRight  },
                 { indDigitalOutputLightBarMansardWindowMiddle,      SleepingRoomDeviceNames.LightBarMansardWindowMiddle },
                 { indDigitalOutputLightBarMansardWindowLeft,        SleepingRoomDeviceNames.LightBarMansardWindowLeft   },
                 { indDigitalOutputLightCeiling,                     SleepingRoomDeviceNames.LightCeiling                },
            };

            public static string GetInputDeviceName( uint key )
            {
                return ( GetData.ValueFromDeviceDictionary( InputDeviceDictionary, key ) );
            }
            public static string GetOutputDeviceName( uint key )
            {
                return ( GetData.ValueFromDeviceDictionary( OutputDeviceDictionary, key ) );
            }
          }

         static class SleepingRoomDeviceNames
         {
             public  const string Prefix                          = InfoOperationMode.SLEEPING_ROOM + Seperators.InfoSeperator;
             public  const string WindowWest                      = Prefix + "WindowWest";
             public  const string MansardWindowNorthLeft          = Prefix + "MansardWindowNorthLeft";
             public  const string MansardWindowNorthRight         = Prefix + "MansardWindowNorthRight";
             public  const string FireAlert                       = Prefix + "FireAlert";
             public  const string Heater                          = Prefix + "Heater";
             public  const string LightMansardRightEnd            = Prefix + "LightMansardRightEnd";
             public  const string LightBarMansardWindowRight      = Prefix + "LightBarMansardWindowRight";
             public  const string LightBarMansardWindowMiddle     = Prefix + "LightBarMansardWindowMiddle";
             public  const string LightBarMansardWindowLeft       = Prefix + "LightBarMansardWindowLeft";
             public  const string LightCeiling                    = Prefix + "LightCeiling";
         }

         static class SleepingIOLightIndices
         {
            public const int indSleepingRoomFirstLight     = 0;
            public const int indSleepingRoomSecondLight    = 1;
            public const int indSleepingRoomThirdLight     = 2;
            public const int indSleepingRoomFourthLight    = 3;
            public const int indSleepingRoomLastLight      = 4;
         }

         static class ParametersHeaterControlSleepingRoom
         {
            static readonly public double TimeDemandForHeatersAutomaticOff           = TimeConverter.ToMiliseconds( 10, 0, 0 );
         }

         static class ParametersLightControlLivingRoomWest
         {
            public const double TimeDemandForAllOnWest                     = 1200;
            static readonly public double TimeDemandForAutomaticOffWest    = TimeConverter.ToMiliseconds( 3, 0, 0);
         }
        #endregion

        #region HEATING_SYSTEM_COMMON
        static class ParametersHeaterControl
        {
            static readonly public double TimeDemandForHeatersOnOff                  = TimeConverter.ToMiliseconds( 1.2 );
            static readonly public double TimeDemandForHeatersOnOffLonger            = TimeConverter.ToMiliseconds( 1.7 );
            static readonly public double TimeDemandForHeatersFinalOff               = TimeConverter.ToMiliseconds( 7 );
            static readonly public double TimeDemandForHeatersOnSleepingRoomBig      = TimeConverter.ToMiliseconds( 30, 0 );
            static readonly public double TimeDemandForHeatersOnSleepingRoomMiddle   = TimeConverter.ToMiliseconds( 15, 0 );
            static readonly public double TimeDemandForHeatersOnSleepingRoomSmall    = TimeConverter.ToMiliseconds( 10, 0 );
            static readonly public double TimeDemandForHeatersOffSleepingRoomBig     = TimeConverter.ToMiliseconds( 20, 0 );
            static readonly public double TimeDemandForHeatersOffSleepingRoomMiddle  = TimeConverter.ToMiliseconds( 10, 0 );
            static readonly public double TimeDemandForHeatersOffSleepingRoomSmall   = TimeConverter.ToMiliseconds( 5, 0 );
            static readonly public double TimeDemandForHeatersAutomaticOff           = TimeConverter.ToMiliseconds( 25, 0 );
            static readonly public double TimeDemandForHeatersAutomaticOffBig        = TimeConverter.ToMiliseconds( 3, 0, 0 );
            static readonly public double TimeDemandForHeatersAutomaticOffSuperBig   = TimeConverter.ToMiliseconds( 6, 0, 0 );
            static readonly public double TimeDemandForHeatersAutomaticOffHalfDay    = TimeConverter.ToMiliseconds( 12, 0, 0 );
            static readonly public double TimeDemandForHeatersAutomaticOffMiddle     = TimeConverter.ToMiliseconds( 1, 30, 0 );
            static readonly public double TimeDemandForHeatersAutomaticOffSmall      = TimeConverter.ToMiliseconds( 30, 0 );
            static readonly public double TimeDemandShowHeaterActive                 = TimeConverter.ToMiliseconds( 0.3 );
            static readonly public double TimeDemandPauseShowHeaterActive            = TimeConverter.ToMiliseconds( 0.2 );
            static readonly public double TimeDemandForItensityTimer                 = 1200;
            static readonly public double TimeDemandForPermanentOnWindow             = 1500;
            static readonly public int    MaxIntensitySteps                          = 3;

            static readonly public double ShowOff                                    = TimeConverter.ToMiliseconds( 0.2 );
            static readonly public double ShowOn                                     = TimeConverter.ToMiliseconds( 0.1 );

            static Dictionary<uint, double> HeaterOnDic = new Dictionary<uint, double>
            {
            {0,                  TimeDemandForHeatersOnSleepingRoomSmall                },
            {1,                  TimeDemandForHeatersOnSleepingRoomMiddle               },
            {2,                  TimeDemandForHeatersOnSleepingRoomBig                  },
            };

            static Dictionary<uint, double> HeaterOffDic = new Dictionary<uint, double>
            {
            {0,                  TimeDemandForHeatersOffSleepingRoomSmall                },
            {1,                  TimeDemandForHeatersOffSleepingRoomMiddle               },
            {2,                  TimeDemandForHeatersOffSleepingRoomBig                  },
            };

            static double GetHeaterTime( ref Dictionary<uint, double> Dic, uint key )
            {
                // Try to get the result in the static Dictionary
                double result;
                if( Dic.TryGetValue( key, out result ) )
                {
                    return result;
                }
                else
                {
                    return 0;
                }
            }

            public static double GetHeaterOnTime( uint index )
            {
                return GetHeaterTime( ref HeaterOnDic, index );
            }

            public static double GetHeaterOffTime( uint index )
            {
                return GetHeaterTime( ref HeaterOffDic, index );
            }
        }
        #endregion

        #region GROUP_KITCHEN_OUTSIDE_LIVING_ROOM_KIDS_ROOM
        static class KitchenIOAssignment
        {
            public const int indKitchenMainButton                              = 0;
            public const int indKitchenPresenceDetector                        = 7;
        }

        static class CenterLivingRoomIODeviceIndices
        {
            public const int indDigitalOutputBoiler                            = 10;
            public const int indDigitalOutputPumpHeatingSystem                 = 8;
            public const int indDigitalInputDoorEntryAnteRoom                  = 8;
            public const int indDigitalInputPowerMeter                         = 10;
        }

        static class CenterOutsideIODevices
        {
            public const int indDigitalOutputLightsOutside                     = 15;
            public const int indDigitalInputSecondaryLightControlOutside       = 9;
            public const int indDigitalInputRainSensor                         = 10;

            static Dictionary<uint, string> InputDeviceDictionary = new Dictionary<uint, string>
            {
                 { indDigitalInputRainSensor,                 CenterOutsideDeviceNames.Rainsensor                 },
            };

            static Dictionary<uint, string> OutputDeviceDictionary = new Dictionary<uint, string>
            {
            };

            public static string GetInputDeviceName( uint key )
            {
                return ( GetData.ValueFromDeviceDictionary( InputDeviceDictionary, key ) );
            }
            public static string GetOutputDeviceName( uint key )
            {
                return ( GetData.ValueFromDeviceDictionary( OutputDeviceDictionary, key ) );
            }
        }

        static class CenterOutsideDeviceNames
        {
            public const string Prefix                             = InfoOperationMode.CENTER_KITCHEN_AND_LIVING_ROOM + Seperators.InfoSeperator;
            public const string Rainsensor                         = Prefix + "Rainsensor";
        }

		static class CenterKitchenDeviceNames
		{
			public const string Prefix                             = InfoOperationMode.CENTER_KITCHEN_AND_LIVING_ROOM + Seperators.WhiteSpace;
			public const string LightSightCabinet                  = Prefix + "LED Balken Seitenschrank mit Elektroverteilung";
			public const string MainButton                         = Prefix + "Haupt Taster";
			public const string PresenceDetector                   = Prefix + "Bewegungsmelder neben E-Verteiler";
			public const string ButtonSleepingRoom                 = Prefix + "Taster Schlafzimmer"; 
			public const string ButtonAnteRoom                     = Prefix + "Taster Vorhaus";
			public const string ButtonBathRoom                     = Prefix + "Taster Badezimmer";
			public const string ButtonWashRoom                     = Prefix + "Taster WC";
			public const string FrontLight1                        = Prefix + "Lichtbalken Küche 1 ( ganz rechts und links )";
			public const string FrontLight2                        = Prefix + "Lichtbalken Küche 2 ( von rechts )";
			public const string FrontLight3                        = Prefix + "Lichtbalken Küche 3 ( von rechts )";
			public const string FumeHood                           = Prefix + "Lichtbalken am Dunstabzug";
			public const string Slot                               = Prefix + "Lichtbalken im Zwischenraum";
			public const string KitchenCabinet                     = Prefix + "Lichtbalken über Küchenschrank";
			public const string WindowBoardEastDown                = Prefix + "Zierlicht Fensterbalken Ost";
			public const string Boiler                             = Prefix + "Warmwasser Boiler";
			public const string CirculationPump                    = Prefix + "Zirkulationspumpe für Warmwasser";
			public const string HeaterEast                         = Prefix + "Thermostatkopf Heizung Ost";
			public const string HeaterWest                         = Prefix + "Thermostatkopf Heizung West";
            public const string FanWashRoom                        = Prefix + "Lüfter WC";
        }

        static class ParametersLightControlCenterOutside
        {
            static readonly public double TimeDemandForAutomaticOff = TimeConverter.ToMiliseconds( 1, 0, 0 );
            static readonly public double TimeDemandForAllOn        = TimeConverter.ToMiliseconds( 5 );
        }

        static class DeviceNames
        {
            static public Dictionary<uint, string> InputDeviceDictionary { get; private set; }
            static public Dictionary<uint, string> OutputDeviceDictionary { get; private set; }

            static public string GetInputDeviceName(int key)
            {
                return (GetData.ValueFromDeviceDictionary( InputDeviceDictionary, Convert.ToUInt32( key ) ));
            }

            static public string GetOutputDeviceName(int key)
            {
                return (GetData.ValueFromDeviceDictionary( OutputDeviceDictionary, Convert.ToUInt32( key ) ));
            }
        }

        static class KitchenCenterIoDevices 
        {
            public const int indDigitalOutputFirstKitchen        = 0;
            public const int indDigitalOutputFrontLight_1        = 1;
            public const int indDigitalOutputFrontLight_2        = 2;
            public const int indDigitalOutputFrontLight_3        = 3;
            public const int indDigitalOutputFumeHood            = 4;
            public const int indDigitalOutputSlot                = 5;
            public const int indDigitalOutputKitchenKabinet      = 6;
            public const int indDigitalOutputWindowBoardEastDown = 9;
            static readonly public int  indLastKitchen           = indDigitalOutputKitchenKabinet;

			static Dictionary<uint, string> CenterInputDeviceDictionary = new Dictionary<uint, string>
			{
				{ KitchenIOAssignment.indKitchenMainButton,                                   CenterKitchenDeviceNames.MainButton         },
				{ KitchenIOAssignment.indKitchenPresenceDetector,                             CenterKitchenDeviceNames.PresenceDetector   },
				{ CenterButtonRelayIOAssignment.indDigitalInputRelayAnteRoom,                 CenterKitchenDeviceNames.ButtonAnteRoom     },
				{ CenterButtonRelayIOAssignment.indDigitalInputRelayBathRoom,                 CenterKitchenDeviceNames.ButtonBathRoom     },
				{ CenterButtonRelayIOAssignment.indDigitalInputRelaySleepingRoom,             CenterKitchenDeviceNames.ButtonSleepingRoom },
				{ CenterButtonRelayIOAssignment.indDigitalInputRelayWashRoom,                 CenterKitchenDeviceNames.ButtonWashRoom     },
			};

			static Dictionary<uint, string> CenterOutputDeviceDictionary = new Dictionary<uint, string>
			{
				{ indDigitalOutputFirstKitchen,                                               CenterKitchenDeviceNames.LightSightCabinet  },
				{ indDigitalOutputFrontLight_1,                                               CenterKitchenDeviceNames.FrontLight1        },
				{ indDigitalOutputFrontLight_2,                                               CenterKitchenDeviceNames.FrontLight2        },
				{ indDigitalOutputFrontLight_3,                                               CenterKitchenDeviceNames.FrontLight3        },
				{ indDigitalOutputFumeHood,                                                   CenterKitchenDeviceNames.FumeHood           },
				{ indDigitalOutputSlot,                                                       CenterKitchenDeviceNames.Slot               },
				{ indDigitalOutputKitchenKabinet,                                             CenterKitchenDeviceNames.KitchenCabinet     },
				{ indDigitalOutputWindowBoardEastDown,                                        CenterKitchenDeviceNames.KitchenCabinet     },
				{ CenterLivingRoomIODeviceIndices.indDigitalOutputBoiler,                     CenterKitchenDeviceNames.Boiler             },
				{ WaterHeatingSystemIODeviceIndices.indDigitalOutputWarmWaterCirculationPump, CenterKitchenDeviceNames.CirculationPump    },
				{ KitchenLivingRoomIOAssignment.indFirstHeater,                               CenterKitchenDeviceNames.HeaterEast         },
				{ KitchenLivingRoomIOAssignment.indLastHeater,                                CenterKitchenDeviceNames.HeaterWest         },
                { WashRoomIODeviceIndices.indDigitalOutputWashRoomFan,                        CenterKitchenDeviceNames.FanWashRoom        }
			};

            public static string GetInputDeviceName(int key)
            {
                return (GetData.ValueFromDeviceDictionary( CenterInputDeviceDictionary, Convert.ToUInt32( key ) ));
            }
            public static string GetOutputDeviceName(int key)
            {
                return (GetData.ValueFromDeviceDictionary( CenterOutputDeviceDictionary, Convert.ToUInt32( key ) ));
            }
        }

        static class KitchenLivingRoomIOAssignment
        {
            public const int indFirstHeater                      = 13;
            public const int indLastHeater                       = 14;
        }

        static class ParametersHeaterControlLivingRoom
        {
            static readonly public double TimeDemandForHeatersOnBig      = TimeConverter.ToMiliseconds( 30, 0 );
            static readonly public double TimeDemandForHeatersOnMiddle   = TimeConverter.ToMiliseconds( 15, 0 );
            static readonly public double TimeDemandForHeatersOnSmall    = TimeConverter.ToMiliseconds( 10, 0 );
            static readonly public double TimeDemandForHeatersOffBig     = TimeConverter.ToMiliseconds( 15, 0 );
            static readonly public double TimeDemandForHeatersOffMiddle  = TimeConverter.ToMiliseconds( 10, 0 );
            static readonly public double TimeDemandForHeatersOffSmall   = TimeConverter.ToMiliseconds( 5, 0 );
            static readonly public double TimeDemandForHeatersOnDefrost  = TimeConverter.ToMiliseconds( 30 );
            static readonly public double TimeDemandForHeatersOffDefrost = TimeConverter.ToMiliseconds( 45 );
        }

        static class ParametersWaterHeatingSystem
        {
            static readonly public double TimeDemandForWarmCirculationPumpAutomaticOff       = TimeConverter.ToMiliseconds( 5, 0 );
        }

        static class WaterHeatingSystemIODeviceIndices
        {
            public const int indDigitalOutputWarmWaterCirculationPump = 11;
        }

        static class ParametersHeatingSystem
        {
            static readonly public double TimeDemandForHeaterCirculationPumpAutomaticOff        = TimeConverter.ToMiliseconds( 6, 0, 0 );
        }

        static class ParametersLightControlKitchen
        {
            static readonly public double TimeDemandForAutomaticOffKitchen                      = TimeConverter.ToMiliseconds( 20, 5 );
            static readonly public double TimeDemandForAllOn                                    = TimeConverter.ToMiliseconds( 15 );
            static readonly public double TimeDemandForAllOutputsOff                            = TimeConverter.ToMiliseconds( 9 );
        }

        static class KidsRoomIODeviceIndices
        {
            public const int indDigitalOutputHeater      = 15;
        }

        static class KidsRoomConfiguration
        {
            public const int PushPullCountsTurnHeaterOnOff = 2;
            public const double TimeWindowTurnHeatersOnOff = 500;
        }
        #endregion

        #region GROUP_LIVINGROOM_EAST
        static class LivingRoomEastHardwareAssignment
        {
            public const int indInterfaceCard_1 = 0;
            public const int indInterfaceCard_2 = 1;
        }

        static class LivingRoomEastIOAssignment
        {
            public static class LivDigInputs
            {
            }
        }

        static class ParametersLightControlEASTSide
        {
            static readonly public double TimeDemandForAutomaticOffEastSide                     = TimeConverter.ToMiliseconds( 1, 0, 0 );
            static readonly public double TimeDemandForAllOn                                    = TimeConverter.ToMiliseconds( 2.5 );
        }

        static class EastDeviceNames
        {
            public const string Prefix = InfoOperationMode.LIVING_ROOM_EAST + Seperators.WhiteSpace;
            public const string TestButton = Prefix + "Test Knopf";
            public const string DoorSwitch = Prefix + "Tür Schalter Eingang Haupt (rechts)";
        }

        static class EastSideIOAssignment
        {
            public const int indTestButton                           = 0;
            public const int indDigitalOutput_SpotFrontSide_1_4      = 0;
            public const int indDigitalOutput_SpotFrontSide_5_8      = 1;
            public const int indDigitalOutput_SpotBackSide_1_3       = 2;
            public const int indSpotBackSide4_8                      = 3;
            public const int indLightsTriangleGalleryBack            = 4;
            public const int indDoorEntry_Window_Right               = 5;
            public const int indWindowBesideDoorRight                = 6;
            public const int indWindowLEDEastUpside                  = 7;
            public const int indWindowLimitSwitchWestUpside          = 2;
            public const int indSpotGalleryFloor_1_18                = 0;
            public const int indSpotGalleryFloor_2_4                 = 1;
            public const int indSpotGalleryFloor_5_6                 = 3;
            public const int indSpotGalleryFloor_7                   = 4;
            public const int indSpotGalleryFloor_8_10                = 5;
            public const int indSpotGalleryFloor_11_12               = 6;
            public const int indSpotGalleryFloor_13                  = 7;
            public const int indSpotGalleryFloor_14_15               = 8;
            public const int indSpotGalleryFloor_16                  = 9;
            public const int indSpotGalleryFloor_17_19_20_21         = 10;
            public const int indBarGallery1_4                        = 11;
            public const int indDigitalInput_PresenceDetector        = 5;
            public const int indDigitalInput_MainDoorWingRight       = 4;
            public const int indDigitalInput_DoorSwitchMainRight     = 3;

            public static uint SerialCard1;

            static Dictionary<uint, string> EastInputDeviceDictionary = new Dictionary<uint, string>
            {
                { indTestButton                       + SerialCard1,             EastDeviceNames.TestButton         },
                { indDigitalInput_DoorSwitchMainRight + SerialCard1,             EastDeviceNames.DoorSwitch        },
            };

            public static string GetInputDeviceName(int key)
            {
                return (GetData.ValueFromDeviceDictionary( EastInputDeviceDictionary, Convert.ToUInt32( key ) ));
            }

        }
        #endregion

        #region GROUP_LIVINGROOM_WEST
        static class LivingRoomWestIOAssignment
        {
            public static class LivWestDigInputs
            {
                public const int indDigitalInputButtonMainDownRight      = 0;
                public const int indDigitalInputButtonMainDownLeft       = 1;
                public const int indDigitalInputButtonMainUpLeft         = 2;
                public const int indDigitalInputButtonMainUpRight        = 3;
                public const int indDigitalInputPresenceDetector         = 5;
            }

            public static class LivWestDigOutputs
            {
                public const int indDigitalOutLightWindowDoorEntryLeft  = 0;
                public const int indDigitalOutLightKitchenDown_1        = 1;
                public const int indDigitalOutLightKitchenDown_2        = 2;
                public const int indDigitalOutLightWindowBoardLeft      = 4;
                public const int indDigitalOutLightWindowBoardRight     = 5;
                public const int indDigitalOutLightWall                 = 7;
            }

            public const int indFirstLight                      = 0;
            public const int indLastLight                       = LivWestDigOutputs.indDigitalOutLightWall;
        }
        #endregion

        #region GROUP_ANTEROOM_WASHROOM_BATHROOM
            #region BATH_ROOM
            static class BathRoomIODeviceIndices
            {
                public const int indDigitalOutputBathRoomFirstHeater = 12;
                public const int indDigitalOutputBathRoomLastHeater  = 12;
                public const int indDigitalOutputBathRoomHeater      = 12;
                //public const int indDigitalOutputBathRoomHeater      = ??;
            }           

            static class ParametersHeaterControlBathRoom
            {
                static readonly public double TimeDemandForHeaterAutomaticOff = new TimeSpan( 2, 0, 0, 0, 0 ).TotalMilliseconds;
            } 
      
            static class ParametersLightControlBathRoom
            {
                static readonly public double TimeDemandForAutomaticOffBath = TimeConverter.ToMiliseconds( 1, 0, 0 );
            }

            static class BathRoomIOLightIndices
            {
                public const int indFirstBathRoom = 7;
                public const int indLastBathRoom  = 11;
            }

            static class BathRoomIOLightIndexNaming
            {
                public const int MiddleLight       = BathRoomIOLightIndices.indFirstBathRoom;
                public const int RBG_PanelOverBath = BathRoomIOLightIndices.indLastBathRoom;
            }                
            #endregion

            #region WASH_ROOM
            static class ParametersWashRoomControl
            {
                static readonly public double TimeDemandForFanOn                                    = TimeConverter.ToMiliseconds( 2 );
                static readonly public double TimeDemandForFanAutomaticOff                          = TimeConverter.ToMiliseconds( 15, 0 );
            }
            static class ParametersLightControlWashRoom
            {
                static readonly public double TimeDemandForAutomaticOffWashRoom = TimeConverter.ToMiliseconds( 20, 0 );
            }
            static class WashRoomIODeviceIndices
            {
                public const int indDigitalOutputWashRoomLight       = 5;
                public const int indDigitalOutputWashRoomFan         = 12;
            }
        
            #endregion

            #region ANTE_ROOM
            static class AnteRoomIOAssignment
            {
                public const int indAnteRoomMainButton               = 0;
                public const int indWashRoomMainButton               = 1;
                public const int indBathRoomMainButton               = 2;
                public const int indAnteRoomPresenceDetector         = 3;
            }

            static class ParametersLightControlAnteRoom
            {
                static readonly public double TimeDemandAllOutputsOff                           = TimeConverter.ToMiliseconds( 10 );
                static readonly public double TimeDemandForAutomaticOffAnteRoom                 = TimeConverter.ToMiliseconds( 5 );
                static readonly public double TimeDemandForAllOn                                = TimeConverter.ToMiliseconds( 3.5 );
                static readonly public double TimeDemandAutomaticOffViaPresenceDetector         = TimeConverter.ToMiliseconds( 1, 0 );
                static readonly public double TimeDemandForAllOffFinal                          = TimeConverter.ToMiliseconds( 3, 0, 0 );

            }
            static class AnteRoomIODeviceIndices
            {
                public const int indDigitalOutputAnteRoomHeater      = 12;
            }
            static class AnteRoomIOLightIndices
            {
                public const int indFirstLight    = 0;
                public const int indLastLight     = 4;
                public const int indFirstWashroom = 5;
                public const int indLastWashroom  = 6;
            }
            static class AnteRoomIOLightIndexNaming
            {
                public const int AnteRoomMainLight                         = 0;
                public const int AnteRoomBackSide                          = 1;
                public const int AnteRoomRoofBackSideFloorSpotGroupMiddle1 = 2;
                public const int AnteRoomRoofBackSideFloorSpotGroupMiddle2 = 3;
                public const int AnteRoomNightLight                        = 4;
            }            
            #endregion
        #endregion

        #region OBSERVER_SERVICES
            static class ObserverComandos
            {
                public const string WindowObserverPrefix = "CmdObserver_";
                public const string WindowObserverMute   = WindowObserverPrefix                    + "Mute";
                public const string WindowObserverUnMute = WindowObserverPrefix                    + "UnMute";
            }

            static class ObserverConstants
            {
                static readonly public double TimeDemandForObserverWarning    = TimeConverter.ToMiliseconds( 0, 2, 0 );
            }

            static class ObserverIndices
            {
                public static uint IndRoom = 0;
            }

        #endregion

        #region COMMON_EXCEPTION_MESSAGES
        static class EXCEPTIONMessages
        {
           public const string IndexMustNotBeNegative = "Index must not be negative!"; 
        }
        #endregion

        #region POWERMETER_CONSTANTS
        static class PowermeterConstants
        {
            public const double DefaultCaptureIntervallTime = 60;
            public const double DefaultStoreTime = 3600;
        }
        #endregion
    }
}