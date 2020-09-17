using BASIC_COMPONENTS;
using Communication.HAProtocoll;
using Communication.UDP;
using Equipment;
using HA_COMPONENTS;
using HomeAutomation.HardConfig_Collected;
using HomeAutomation.rooms;
using HomeControl.BASIC_COMPONENTS.Interfaces;
using Phidgets.Events;
using Quartz;
using ROOMS.CENTER.INTERFACE;
using Scheduler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using SystemServices;
using HomeAutomationProtocoll;
using System.Runtime.CompilerServices;

namespace HomeAutomation
{
    public class LivingRoomConfig
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Ip")]
        public string IpAdressServer { get; set; }
        public string PortServer { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "softwareversion")]
        public string softwareversion { get; set; }
        public string HeatersLivingRoomAutomatic { get; set; }
    }

    class Center_kitchen_living_room_NG : CommonRoom, IIOHandlerInfo, ICenter
    {
        #region DECLARATION
        LightControlKitchen_NG       Kitchen;
        LightControl_NG              Outside;
        HeaterElement_NG             HeaterAnteRoom;
        CentralControlledElements_NG FanWashRoom;
        CentralControlledElements_NG CirculationPump;
        CentralControlledElements_NG PowerPlugs230VWest;
        Home_scheduler               scheduler;
        SchedulerDataRecovery        schedRecover;
        UdpReceive UDPReceiveDataFromWebForwarder;
        UdpReceive UdpReceiveDataFromEastController;
        Timer TimerRecoverScheulder;
        Timer CommonUsedTick = new Timer( GeneralConstants.DURATION_COMMONTICK );
        bool[] _DigitalInputState;
        bool[] _DigitalOutputState;
        bool[] _InternalDigitalOutputState;
        long RemainingTime;
        PowerMeter _PowerMeter;
        bool RemoteControlLightOutsideActivated;
        bool AutoControlPowerPlug;
        bool AutoControlBoiler;

        public delegate void UpdateMatchedOutputs( object sender, bool[] _DigOut );
        public event         UpdateMatchedOutputs EUpdateMatchedOutputs;

        public event DigitalInputChanged  DigitalInputChanged;
        public event DigitalOutputChanged EDigitalOutputChanged;

        DigitalInputEventargs  _DigitalInputEventargs  = new DigitalInputEventargs( );
        DigitalOutputEventargs _DigitalOutputEventargs = new DigitalOutputEventargs( );
        #endregion

        // more objects share the same eventhandler in this case
        void CommonUsedEventHandlers()
        {
            Kitchen.EUpdateOutputs_           += EShowUpdatedOutputs;
            HeaterAnteRoom.EUpdateOutputs_    += EShowUpdatedOutputs;
            CirculationPump.EUpdateOutputs    += EShowUpdatedOutputs;
            Outside.EUpdateOutputs            += EShowUpdatedOutputs;
            FanWashRoom.EUpdateOutputs        += EShowUpdatedOutputs;
            PowerPlugs230VWest.EUpdateOutputs += EShowUpdatedOutputs;
        }

        #region CONSTRUCTOR
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Convert.ToInt16(System.String)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "System.Console.WriteLine(System.String)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "SystemServices.Services.TraceMessage_(System.String,System.String,System.String,System.Int32)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Objekte verwerfen, bevor Bereich verloren geht")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        void Constructor( LivingRoomConfig livingroomconfig )
        {
            AutoControlPowerPlug = true;
            AutoControlBoiler    = true;
            _GivenClientName     = InfoOperationMode.CENTER_KITCHEN_AND_LIVING_ROOM;
            _IpAdressServer      = livingroomconfig.IpAdressServer;
            _DigitalInputState   = new bool[GeneralConstants.NumberOfInputsIOCard];
            _DigitalOutputState  = new bool[GeneralConstants.NumberOfOutputsIOCard];
            _InternalDigitalOutputState = new bool[GeneralConstants.NumberOfOutputsIOCard];

            Kitchen = new LightControlKitchen_NG(
                                                  ParametersLightControlKitchen.TimeDemandForAllOn,
                                                  ParametersLightControlKitchen.TimeDemandForAllOutputsOff,
                                                  ParametersLightControl.TimeDemandForSingleOff,
                                                  ParametersLightControlKitchen.TimeDemandForAutomaticOffKitchen,
                                                  KitchenCenterIoDevices.indDigitalOutputFirstKitchen,
                                                  KitchenCenterIoDevices.indLastKitchen
                                                );

            Outside = new LightControl_NG( ParametersLightControlCenterOutside.TimeDemandForAllOn,
                                           ParametersLightControlCenterOutside.TimeDemandForAutomaticOff,
                                           CenterOutsideIODevices.indDigitalOutputLightsOutside
                                         )
            {
                Match = new List<int> { CenterOutsideIODevices.indDigitalOutputLightsOutside }
            };

            Kitchen.IsPrimaryIOCardAttached = _PrimaryIOCardIsAttached;


            HeaterAnteRoom = new HeaterElement_NG(
                                         ParametersHeaterControl.TimeDemandForHeatersOnOff,
                                         ParametersHeaterControl.TimeDemandForHeatersAutomaticOff,
                                         ParametersHeaterControlLivingRoom.TimeDemandForHeatersOnMiddle,
                                         ParametersHeaterControlLivingRoom.TimeDemandForHeatersOffMiddle,
                                         AnteRoomIODeviceIndices.indDigitalOutputAnteRoomHeater,
                                         AnteRoomIODeviceIndices.indDigitalOutputAnteRoomHeater )
            {
                Match = new List<int> { AnteRoomIODeviceIndices.indDigitalOutputAnteRoomHeater }
            };

            FanWashRoom = new CentralControlledElements_NG(
                                     ParametersWashRoomControl.TimeDemandForFanOn,
                                     ParametersWashRoomControl.TimeDemandForFanAutomaticOff,
                                     WashRoomIODeviceIndices.indDigitalOutputWashRoomFan )
            {
                Match = new List<int> { WashRoomIODeviceIndices.indDigitalOutputWashRoomFan }
            };

            CirculationPump = new CentralControlledElements_NG(
                                         ParametersWaterHeatingSystem.TimeDemandForWarmCirculationPumpAutomaticOff,
                                         WaterHeatingSystemIODeviceIndices.indDigitalOutputWarmWaterCirculationPump )
            {
                Match = new List<int> { WaterHeatingSystemIODeviceIndices.indDigitalOutputWarmWaterCirculationPump }
            };

            PowerPlugs230VWest = new CentralControlledElements_NG(
                             ParametersPower.TimeDemandForPowerPlug230V,
                             KitchenLivingRoomIOAssignment.indDigitalOutputPowerPlugsWest230V)
            {
                Match = new List<int> { KitchenLivingRoomIOAssignment.indDigitalOutputPowerPlugsWest230V }
            };


            Kitchen.EReset += Kitchen_EReset;

            #region REGISTRATION_ONE_COMMON_EVENT_HANDLER
            CommonUsedEventHandlers( );
            #endregion

            scheduler = new Home_scheduler( );
            scheduler.EvTriggered += SchedulerTriggered;
            TimerRecoverScheulder = new Timer( Parameters.DelayTimeStartRecoverScheduler );

            CommonUsedTick.Elapsed += CommonUsedTick_Elapsed;

            base.Attach += Center_kitchen_living_room_Attach;
            base.Detach += Center_kitchen_living_room_Detach;

            try
            {
                UDPReceiveDataFromWebForwarder = new UdpReceive( IPConfiguration.Port.PORT_UDP_WEB_FORWARDER_CENTER );
                UDPReceiveDataFromWebForwarder.DatagrammReceived += UdpDataReceived;
                UdpReceiveDataFromEastController = new UdpReceive( IPConfiguration.Port.PORT_LIGHT_CONTROL_LIVING_ROOM_EAST );
                UdpReceiveDataFromEastController.DatagrammReceived += UdpDataReceived;
            }
            catch (Exception ex)
            {
                Console.WriteLine( TimeUtil.GetTimestamp( ) + Seperators.WhiteSpace + ex.Message );
                Services.TraceMessage_( InfoString.FailedToEstablishUDPReceive );
            }

            // after power fail, certain important scheduler data is recovered
            schedRecover = new SchedulerDataRecovery( Directory.GetCurrentDirectory( ) );
            schedRecover.ERecover += RecoverScheduler;
            schedRecover.ERecovered += SchedRecover_ERecovered;
            TimerRecoverScheulder.Elapsed += TimerRecoverScheulder_Elapsed;
            TimerRecoverScheulder.Start( );
            CommonUsedTick.Start( );
            Console.WriteLine( TimeUtil.GetTimestamp( ) + Seperators.WhiteSpace + InfoString.StartTimerForRecoverScheduler );
            RemainingTime = Convert.ToUInt32( ( Parameters.DelayTimeStartRecoverScheduler / Parameters.MilisecondsOfSecond ) );

            _PowerMeter = new PowerMeter( true, PowermeterConstants.DefaultCaptureIntervallTime, PowermeterConstants.DefaultStoreTime );
        }

        public Center_kitchen_living_room_NG( LivingRoomConfig ConfigLivingRoom )
            : base( )
        {
            Constructor( ConfigLivingRoom );
        }
        #endregion

        #region SCHEDULER
 
        void SchedRecover_ERecovered( object sender, EventArgs e )
        {
            SchedulerApplication.DataRecovered = true;
        }

        // scheduler starts with recovered data
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "System.Console.WriteLine(System.String)")]
        void RecoverScheduler( FeedData e )
        {
            string Job = e.Device + Seperators.InfoSeperator + e.JobId.ToString( );
            SchedulerApplication.Worker( this, e, ref scheduler );
            Console.WriteLine( TimeUtil.GetTimestamp( ) + Seperators.WhiteSpace + "Recover scheduler after booting " );
            Console.WriteLine( TimeUtil.GetTimestamp( ) + Seperators.WhiteSpace + "Device:                   " + e.Device );
            Console.WriteLine( TimeUtil.GetTimestamp( ) + Seperators.WhiteSpace + "Fire Time:                " + e.Starttime );
            Console.WriteLine( TimeUtil.GetTimestamp( ) + Seperators.WhiteSpace + "Configured days:          " + e.Days );
            Console.WriteLine( TimeUtil.GetTimestamp( ) + Seperators.WhiteSpace + "Job:                      " + Job );
            Console.WriteLine( TimeUtil.GetTimestamp( ) + Seperators.WhiteSpace + "Current scheduler status: " + scheduler.GetJobStatus( Job ) );
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "counts")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "System.Console.WriteLine(System.String)")]
        void ControlScheduledDevice( decimal counts, string device )
        {
            string[] DeviceParts;
            DeviceParts = device.Split( '_' );
            HADictionaries.DeviceDictionaryCenterdigitalOut.TryGetValue( DeviceParts[0], out int index );

            if (device.Contains( "FumeHoodOn" ))
            {
                Kitchen.ActualKitchenStep = LightControlKitchen_NG.KitchenStep.eSlots;
            }

            if (device.Contains( "FumeHoodOff" ))
            {
                Kitchen.ActualKitchenStep = LightControlKitchen_NG.KitchenStep.eFrontLights;
            }

            if (outputs != null && AutoControlBoiler)
            {
                if (device.Contains( "BoilerOn" ))
                {
                    outputs[CenterLivingRoomIODeviceIndices.indDigitalOutputBoiler] = GeneralConstants.ON;
                    Console.WriteLine( TimeUtil.GetTimestamp( ) + CenterKitchenDeviceNames.Boiler + " =  ON"  );
                }

                if (device.Contains( "BoilerOff" ))
                {
                    outputs[CenterLivingRoomIODeviceIndices.indDigitalOutputBoiler] = GeneralConstants.OFF;
                    Console.WriteLine( TimeUtil.GetTimestamp( ) + CenterKitchenDeviceNames.Boiler + " = OFF"  );
                }
            }
        }

 
        void SchedulerTriggered(string time, IJobExecutionContext context, decimal counts)
        {
            SchedulerApplication.WriteStatus(time, context, counts);

            if (!Attached)
            {
                return;
            }

            ControlScheduledDevice(counts, context.JobDetail.Key.Name);
        }

        #endregion

        #region IPCONFIGURATION
        string _GivenClientName;
        string _IpAdressServer;

        #endregion

        #region IO_CARD_OBSERVATION
        void Center_kitchen_living_room_Detach( object sender, DetachEventArgs e )
        {
            if (Kitchen != null)
            {
                Kitchen.IsPrimaryIOCardAttached = _PrimaryIOCardIsAttached;
            }
        }

        void Center_kitchen_living_room_Attach( object sender, AttachEventArgs e )
        {
            if (Kitchen != null)
            {
                Kitchen.IsPrimaryIOCardAttached = _PrimaryIOCardIsAttached;
            }
        }
        #endregion

        #region CONTROLLOGIC
        void Kitchen_EReset( object sender )
        {
            HeaterAnteRoom.Reset( );
        }

        void Reset( bool command )
        {
            if (command == true)
            {
                Kitchen?.StartWaitForAllOff( );
            }
            else
            {
                Kitchen?.StopWaitForAllOff( );
            }
        }


        protected override void TurnNextLightOn_( InputChangeEventArgs e )
        {
            int index = e.Index;
            bool Value = e.Value;
            TurnNextDevice( index, Value );
        }

        void TurnNextDevice( int index, bool Value )
        {
            switch (index) // the index is the assigned input number
            {
                case KitchenIOAssignment.indKitchenMainButton:
                    Kitchen?.MakeStep( Value );
                    Kitchen?.AutomaticOff( Value );
                    // reset - this is a last rescue anchor in the case something went wrong ( any undiscovered bug )
                    Reset( Value );
                    break;

                case CenterButtonRelayIOAssignment.indDigitalInputRelayAnteRoom:
                    HeaterAnteRoom?.HeaterOnFallingEdge(Value);
                    break;

                case KitchenIOAssignment.indKitchenPresenceDetector:
                    Kitchen?.AutomaticOff( Value );
                    break;

                default:
                    break;
            }
        }

        void TurnFan( int index, bool Value )
        {
            switch (index) 
            {
                case CenterButtonRelayIOAssignment.indDigitalInputRelayWashRoom:
                    FanWashRoom?.DelayedDeviceOnFallingEdge( Value );
                    break;

                default:
                    break;
            }
        }


        void TimedDevice( int index, bool Value )
        {
            // Warmwater Circulation pump ( for shower, pipe and bathroom )control
            switch (index) // the index is the assigned input number
            {
                case KitchenIOAssignment.indKitchenPresenceDetector:
                case KitchenIOAssignment.indKitchenMainButton:
                case AnteRoomIOAssignment.indBathRoomMainButton:
                    CirculationPump?.DeviceOnFallingEdgeAutomaticOff( Value );
                    break;

                default:
                    break;
            }
        }

        void ControlSequenceOnInputChange( int index, bool Value )
        {
 
            TurnNextDevice( index, Value );

            TimedDevice( index, Value );

            TurnFan( index, Value );

         }
        #endregion

        #region SWITCH_DEVICE_GROUPS
        void TurnBoiler( bool commando )
        {
            outputs[CenterLivingRoomIODeviceIndices.indDigitalOutputBoiler] = commando;
        }

        void TurnFrontLights( bool commando )
        {
            outputs[KitchenCenterIoDevices.indDigitalOutputFrontLight_1] = commando;
            outputs[KitchenCenterIoDevices.indDigitalOutputFrontLight_2] = commando;
            outputs[KitchenCenterIoDevices.indDigitalOutputFrontLight_3] = commando;
        }

        void TurnKitchenLights( bool commando )
        {
            outputs[KitchenCenterIoDevices.indDigitalOutputFirstKitchen] = commando;
            outputs[KitchenCenterIoDevices.indDigitalOutputFrontLight_1] = commando;
            outputs[KitchenCenterIoDevices.indDigitalOutputFrontLight_2] = commando;
            outputs[KitchenCenterIoDevices.indDigitalOutputFrontLight_3] = commando;
            outputs[KitchenCenterIoDevices.indDigitalOutputFumeHood] = commando;
            outputs[KitchenCenterIoDevices.indDigitalOutputSlot] = commando;
            outputs[KitchenCenterIoDevices.indDigitalOutputKitchenKabinet] = commando;
        }

        void TurnWindowLedgeEast( bool command )
        {
            outputs[KitchenCenterIoDevices.indDigitalOutputWindowBoardEastDown] = command;
        }

        void TurLightOutside( bool command )
        {
            if (!RemoteControlLightOutsideActivated)
            {
                outputs[CenterOutsideIODevices.indDigitalOutputLightsOutside] = command;
            }
        }

        #endregion 

        #region REMOTE_CONTROLLED_UDP
        decimal receivedTransactionCounter = 0;
        decimal PreviousreceivedTransactionCounter = 0;
        const int ExpectedArrayElementsSignalTelegram = UdpTelegram.DelfaultExpectedArrayElementsSignalTelegram;
        const int ExpectedArrayElementsCommonCommand = 1;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Convert.ToInt16(System.String)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Convert.ToDecimal(System.String)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Convert.ToBoolean(System.String)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1505:AvoidUnmaintainableCode")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "SystemServices.Services.TraceMessage_(System.String,System.String,System.String,System.Int32)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "System.Console.WriteLine(System.String)")]
        void UdpDataReceived( object sender, ReceivedEventargs e )
        {
            string[] DatagrammSplitted = e.Payload.Split( ComandoString.Telegram.Seperator );

            if (DatagrammSplitted.Length == ExpectedArrayElementsCommonCommand)
            {
                switch (DatagrammSplitted[0])
                {
                    case ComandoString.TURN_ALL_LIGHTS_ON:
                    case ComandoString.TURN_ALL_KITCHEN_LIGHTS_ON:
                        TurnKitchenLights( GeneralConstants.ON );
                        Kitchen?.AutomaticOff( true );
                        break;

                    case ComandoString.TURN_ALL_LIGHTS_OFF:
                    case ComandoString.TURN_ALL_KITCHEN_LIGHTS_OFF:
                        TurnKitchenLights( GeneralConstants.OFF );
                        RemoteControlLightOutsideActivated = false;
                        break;

                    case ComandoString.TURN_BOILER_ON:
                        AutoControlBoiler = false;
                        TurnBoiler( GeneralConstants.ON );
                        break;

                    case ComandoString.TURN_BOILER_OFF:
                        AutoControlBoiler = false;
                        TurnBoiler( GeneralConstants.OFF );
                        break;

                    case ComandoString.ACTIVATE_BOILER_AUTO:
                        AutoControlBoiler = true;
                        break;

                    case ComandoString.TURN_FRONT_LIGHTS_OFF:
                        TurnFrontLights( GeneralConstants.OFF );
                        break;

                    case ComandoString.TURN_FRONT_LIGHTS_ON:
                        TurnFrontLights( GeneralConstants.ON );
                        break;

                    case ComandoString.TURN_KITCHEN_LIGHTS_CABINET_ON:
                        outputs[KitchenCenterIoDevices.indDigitalOutputKitchenKabinet] = true;
                        break;

                    case ComandoString.TURN_KITCHEN_LIGHTS_CABINET_OFF:
                        outputs[KitchenCenterIoDevices.indDigitalOutputKitchenKabinet] = false;
                        break;

                    case ComandoString.TURN_KITCHEN_LIGHT_FUMEHOOD_ON:
                        outputs[KitchenCenterIoDevices.indDigitalOutputFumeHood] = true;
                        break;

                    case ComandoString.TURN_KITCHEN_LIGHT_FUMEHOOD_OFF:
                        outputs[KitchenCenterIoDevices.indDigitalOutputFumeHood] = false;
                        break;

                    case ComandoString.TURN_KITCHEN_LIGHT_OVER_CABINET_RIGHT_ON:
                        outputs[KitchenCenterIoDevices.indDigitalOutputFirstKitchen] = true;
                        break;

                    case ComandoString.TURN_KITCHEN_LIGHT_OVER_CABINET_RIGHT_OFF:
                        outputs[KitchenCenterIoDevices.indDigitalOutputFirstKitchen] = false;
                        break;

                    case ComandoString.TURN_KITCHEN_LIGHT_SLOT_ON:
                        outputs[KitchenCenterIoDevices.indDigitalOutputSlot] = true;
                        break;

                    case ComandoString.TURN_KITCHEN_LIGHT_SLOT_OFF:
                        outputs[KitchenCenterIoDevices.indDigitalOutputSlot] = false;
                        break;

                    case ComandoString.TURN_KITCHEN_FRONT_LIGHT_1_ON:
                        outputs[KitchenCenterIoDevices.indDigitalOutputFrontLight_1] = true;
                        break;

                    case ComandoString.TURN_KITCHEN_FRONT_LIGHT_2_ON:
                        outputs[KitchenCenterIoDevices.indDigitalOutputFrontLight_2] = true;
                        break;

                    case ComandoString.TURN_KITCHEN_FRONT_LIGHT_3_ON:
                        outputs[KitchenCenterIoDevices.indDigitalOutputFrontLight_3] = true;
                        break;

                    case ComandoString.TURN_KITCHEN_FRONT_LIGHT_1_OFF:
                        outputs[KitchenCenterIoDevices.indDigitalOutputFrontLight_1] = false;
                        break;

                    case ComandoString.TURN_KITCHEN_FRONT_LIGHT_2_OFF:
                        outputs[KitchenCenterIoDevices.indDigitalOutputFrontLight_2] = false;
                        break;

                    case ComandoString.TURN_KITCHEN_FRONT_LIGHT_3_OFF:
                        outputs[KitchenCenterIoDevices.indDigitalOutputFrontLight_3] = false;
                        break;

                    case ComandoString.TURN_LIGHT_OUTSIDE_ON:
                        Outside.TurnAllDevices( GeneralConstants.ON );
                        RemoteControlLightOutsideActivated = true;
                        break;

                    case ComandoString.TURN_LIGHT_OUTSIDE_OFF:
                        Outside.TurnAllDevices( GeneralConstants.OFF );
                        RemoteControlLightOutsideActivated = false;
                        break;

                    case ComandoString.TURN_LIGHT_OUTSIDE_BY_OPEN_DOOR_CONTACT_ON:
                        TurLightOutside( GeneralConstants.ON );
                        break;

                    case ComandoString.TURN_LIGHT_OUTSIDE_BY_OPEN_DOOR_CONTACT_OFF:
                        TurLightOutside( GeneralConstants.OFF );
                        break;

                    case ComandoString.TURN_WINDOW_LEDGE_EAST_ON:
                        TurnWindowLedgeEast( GeneralConstants.ON );
                        break;

                    case ComandoString.TURN_WINDOW_LEDGE_EAST_OFF:
                        TurnWindowLedgeEast( GeneralConstants.OFF );
                        break;

                    case ComandoString.TURN_HEATER_BODY_EAST_ON:
                        outputs[KitchenLivingRoomIOAssignment.indDigitalOutputPowerPlugsWest230V] = true;
                        break;

                    case ComandoString.TURN_HEATER_BODY_EAST_OFF:
                        outputs[KitchenLivingRoomIOAssignment.indDigitalOutputPowerPlugsWest230V] = false;
                        break;

                    case ComandoString.TURN_HEATER_BODY_WEST_ON:
                        outputs[KitchenLivingRoomIOAssignment.indDigitalOutputHeaterWest] = true;
                        break;

                    case ComandoString.TURN_HEATER_BODY_WEST_OFF:
                        outputs[KitchenLivingRoomIOAssignment.indDigitalOutputHeaterWest] = false;
                        break;

                    case ComandoString.POWER_PLUG_INFRA_RED_ON:
                        AutoControlPowerPlug = false;
                        outputs[KitchenLivingRoomIOAssignment.indDigitalOutputPowerPlugsWest230V] = true;
                        break;

                    case ComandoString.POWER_PLUG_INFRA_RED_OFF:
                        AutoControlPowerPlug = false;
                        outputs[KitchenLivingRoomIOAssignment.indDigitalOutputPowerPlugsWest230V] = false;
                        break;

                    case ComandoString.ACTIVATE_POWER_PLUG_INFRA_RED_AUTO:
                        AutoControlPowerPlug = true;
                        break;

 
                    case ComandoString.PRESENCE_DETECTOR_EAST_1_ON:
                    case ComandoString.PRESENCE_DETECTOR_WEST_ON:
                    case ComandoString.PRESENCE_DETECTOR_EAST_KITCHEN_ON:
                        if( AutoControlPowerPlug )
                        {
                            PowerPlugs230VWest?.DelayedDeviceOnRisingEdge(true);
                        }
                        break;
                }
                Console.WriteLine( TimeUtil.GetTimestamp_( ) + " Received telegramm: " + DatagrammSplitted[0] + " from " + e.Adress + " : " + e.Port );
                return;
            }

            if (DatagrammSplitted.Length != ExpectedArrayElementsSignalTelegram)
            {
                Services.TraceMessage_("Wrong datagramm received");
                return;
            }

            string transactioncounter = DatagrammSplitted[ComandoString.Telegram.IndexTransactionCounter];

            receivedTransactionCounter = Convert.ToDecimal( transactioncounter );

            // basic check wether counter counts up
            if (receivedTransactionCounter > PreviousreceivedTransactionCounter)
            {
                PreviousreceivedTransactionCounter = receivedTransactionCounter;
            }
            else
            {
                return;
            }

            // received actual fired digital input as index
            int ReceivedIndex = Convert.ToInt16(DatagrammSplitted[ComandoString.Telegram.IndexDigitalInputs]);
            // received acutal fired value of digital input
            bool ReceivedValue = Convert.ToBoolean( DatagrammSplitted[ComandoString.Telegram.IndexValueDigitalInputs] );

            switch (ReceivedIndex)
            {
                case LivingRoomWestIOAssignment.LivWestDigInputs.indDigitalInputButtonMainUpLeft:
                    // turn light ON / OFF when releasing the button 
                    if (ReceivedValue == false)
                    {
                        Outside.ToggleSingleDevice( CenterOutsideIODevices.indDigitalOutputLightsOutside );
                        Outside.AutomaticOff( ReceivedValue );
                    }
                    break;
            }
        }
        #endregion

        #region IOEVENTHANDLERS
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Argumente von öffentlichen Methoden validieren", MessageId = "1")]
        protected override void BuildingSection_InputChange( object sender, InputChangeEventArgs e )
        {
            ControlSequenceOnInputChange( e.Index, e.Value );

            if (e.Index == CenterLivingRoomIODeviceIndices.indDigitalInputPowerMeter)
            {
                if (e.Value)
                {
                    _PowerMeter?.Tick( );
                }
            }

            switch( e.Index )
            {
                case KitchenIOAssignment.indKitchenMainButton:
                case KitchenIOAssignment.indKitchenPresenceDetector:
                    PowerPlugs230VWest.DeviceOnFallingEdgeAutomaticOff(e.Value);
                    break;
            }

   
            _DigitalInputEventargs.Index = e.Index;
            _DigitalInputEventargs.Value = e.Value;
            _DigitalInputEventargs.SerialNumber = base.SerialNumber;
            DigitalInputChanged?.Invoke( sender, _DigitalInputEventargs );
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Argumente von öffentlichen Methoden validieren", MessageId = "1")]
        protected override void BuildingSection_OutputChange( object sender, OutputChangeEventArgs e )
        {
            if (_DigitalOutputState == null)
            {
                return;
            }
            for (int i = 0; i < _DigitalOutputState.Length; i++)
            {
                _DigitalOutputState[i] = base.outputs[i];
            }
            _DigitalOutputEventargs.Index = e.Index;
            _DigitalOutputEventargs.Value = e.Value;
            _DigitalOutputEventargs.SerialNumber = SerialNumber;
            EDigitalOutputChanged?.Invoke( sender, _DigitalOutputEventargs );
        }
        #endregion

        #region EVENTHANDLERS
        void EShowUpdatedOutputs( object sender, bool[] _DigOut, List<int> Match )
        {
            for (int i = 0; i < _DigOut.Length; i++)
            {
                if (Match.Contains( i )) // only matching index within the defined list are written into the array
                {
                    _InternalDigitalOutputState[i] = _DigOut[i];
                    if (Attached)
                    {
                        // DIGITAL OUTPUT MAPPING
                        outputs[i] = _DigOut[i];
                    }
                }
            }
            EUpdateMatchedOutputs?.Invoke( this, _DigOut );
        }

        void TimerRecoverScheulder_Elapsed( object sender, ElapsedEventArgs e )
        {
            schedRecover.RecoverScheduler( Directory.GetCurrentDirectory( ), HardConfig_Collected.HardwareDevices.Devices );
            TimerRecoverScheulder.Stop( );
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Int64.ToString(System.String)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "System.Console.Write(System.String)")]
        private void CommonUsedTick_Elapsed( object sender, ElapsedEventArgs e )
        {
            string RemainingTimeInfo;
            string TimeLeft = ( --RemainingTime ).ToString( "000" );

            if (RemainingTime > 0)
            {
                RemainingTimeInfo = InfoString.RemainingTime + TimeLeft + EscapeSequences.CR;
                Console.Write( RemainingTimeInfo );
            }
            else
            {
                CommonUsedTick.Stop( );
            }
        }
        #endregion

        #region INTERFACE_IMPLEMENTATION
        public void ResetDeviceController()
        {
            Outside.ResetDeviceControl( );
            PreviousreceivedTransactionCounter = 0;
        }
        #endregion
 
    }
}
