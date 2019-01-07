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

namespace HomeAutomation
{
    public class LivingRoomConfig
    {
        public string IpAdressServer { get; set; }
        public string PortServer { get; set; }
        public string softwareversion { get; set; }
        public string HeatersLivingRoomAutomatic { get; set; }
    }

    class Center_kitchen_living_room_NG : CommonRoom, IIOHandlerInfo, ICenter
    {
        #region DECLARATION
        LightControlKitchen_NG Kitchen;
        LightControl_NG Outside;
        HeaterElement_NG HeatersLivingRoom;
        HeaterElement_NG HeaterAnteRoom;
        CentralControlledElements_NG FanWashRoom;
        CentralControlledElements_NG CirculationPump;
        Home_scheduler scheduler;
        SchedulerDataRecovery schedRecover;
        FeedData PrevSchedulerData = new FeedData( );
        UdpReceive UDPReceiveDataFromWebForwarder;
        UdpReceive UdpReceiveDataFromEastController;
        Timer TimerRecoverScheulder;
        Timer CommonUsedTick = new Timer( GeneralConstants.DURATION_COMMONTICK );
        bool[] _DigitalInputState;
        bool[] _DigitalOutputState;
        bool[] _InternalDigitalOutputState;
        long RemainingTime;
        PowerMeter _PowerMeter;
        int _PortNumberServer;
        bool RemoteControlLightOutsideActivated;

        public delegate void UpdateMatchedOutputs( object sender, bool[] _DigOut );
        public event UpdateMatchedOutputs EUpdateMatchedOutputs;

        public event DigitalInputChanged EDigitalInputChanged;
        public event DigitalOutputChanged EDigitalOutputChanged;

        DigitalInputEventargs _DigitalInputEventargs = new DigitalInputEventargs( );
        DigitalOutputEventargs _DigitalOutputEventargs = new DigitalOutputEventargs( );
        LivingRoomConfig _livingroomconfig;
        #endregion

        // more objects share the same eventhandler in this case
        void CommonUsedEventHandlers()
        {
            Kitchen.EUpdateOutputs_ += EShowUpdatedOutputs;
            HeatersLivingRoom.EUpdateOutputs_ += EShowUpdatedOutputs;
            HeaterAnteRoom.EUpdateOutputs_ += EShowUpdatedOutputs;
            CirculationPump.EUpdateOutputs_ += EShowUpdatedOutputs;
            Outside.EUpdateOutputs += EShowUpdatedOutputs;
            FanWashRoom.EUpdateOutputs_ += EShowUpdatedOutputs;
        }

        #region CONSTRUCTOR
        void Constructor( LivingRoomConfig livingroomconfig )
        {
            _livingroomconfig = livingroomconfig;
            _GivenClientName = InfoOperationMode.CENTER_KITCHEN_AND_LIVING_ROOM;
            _IpAdressServer = livingroomconfig.IpAdressServer;
            _PortNumberServer = Convert.ToInt16( livingroomconfig.PortServer );
            _DigitalInputState = new bool[GeneralConstants.NumberOfInputsIOCard];
            _DigitalOutputState = new bool[GeneralConstants.NumberOfOutputsIOCard];
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

            HeatersLivingRoom = new HeaterElement_NG(
                                         ParametersHeaterControl.TimeDemandForHeatersOnOff,
                                         ParametersHeaterControl.TimeDemandForHeatersAutomaticOffDisable,
                                         ParametersHeaterControlLivingRoom.TimeDemandForHeatersOnMiddle,
                                         ParametersHeaterControlLivingRoom.TimeDemandForHeatersOffMiddle,
                                         KitchenLivingRoomIOAssignment.indDigitalOutputHeaterEast,
                                         KitchenLivingRoomIOAssignment.indDigitalOutputHeaterWest )
            {
                Match = new List<int>
                                         {
                                             KitchenLivingRoomIOAssignment.indDigitalOutputHeaterEast,
                                             KitchenLivingRoomIOAssignment.indDigitalOutputHeaterWest
                                         }
            };

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


            HeatersLivingRoom.AllOn_ += HeatersLivingRoom_AllOn_;
            Kitchen.EReset += Kitchen_EReset;

            #region REGISTRATION_ONE_COMMON_EVENT_HANDLER
            CommonUsedEventHandlers( );
            #endregion

            scheduler = new Home_scheduler( );
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

            if (outputs != null)
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

                if (_livingroomconfig.HeatersLivingRoomAutomatic == "ON")
                {
                    if (device.Contains( "HeatersEastAndWestOn" ) || device.Contains( "HeatersEastAndWestAfternoonOn" ))
                    {
                        HeatersLivingRoom.Reset( );
                        outputs[KitchenLivingRoomIOAssignment.indDigitalOutputHeaterEast] = true;
                        outputs[KitchenLivingRoomIOAssignment.indDigitalOutputHeaterWest] = true;
                        Console.WriteLine( TimeUtil.GetTimestamp( ) + CenterKitchenDeviceNames.HeaterEast + " = ON" );
                        Console.WriteLine( TimeUtil.GetTimestamp( ) + CenterKitchenDeviceNames.HeaterWest + " = ON" );
                    }

                    if (device.Contains( "HeatersEastAndWestOff" ) || device.Contains( "HeatersEastAndWestAfternoonOff" ))
                    {
                        HeatersLivingRoom.Reset( );
                        outputs[KitchenLivingRoomIOAssignment.indDigitalOutputHeaterEast] = false;
                        outputs[KitchenLivingRoomIOAssignment.indDigitalOutputHeaterWest] = false;
                        Console.WriteLine( TimeUtil.GetTimestamp( ) + CenterKitchenDeviceNames.HeaterEast + " = OFF" );
                        Console.WriteLine( TimeUtil.GetTimestamp( ) + CenterKitchenDeviceNames.HeaterWest + " = OFF" );
                    }
                }
            }
        }

        string AskForSchedulerStatus( string Job )
        {
            string SystemIsAskingScheduler = TimeUtil.GetTimestamp( ) +
                                                 Seperators.WhiteSpace +
                                                 _GivenClientName +
                                                 "...." +
                                                 InfoString.Asking +
                                                 Seperators.WhiteSpace +
                                                 InfoString.Scheduler;

            Console.WriteLine( SystemIsAskingScheduler );
            SchedulerInfo.Status status = scheduler.GetJobStatus( Job );

            string StatusInformation = TimeUtil.GetTimestamp( ) +
                                           Seperators.WhiteSpace +
                                           _GivenClientName +
                                           Seperators.WhiteSpace +
                                           InfoString.StatusOf +
                                           Seperators.WhiteSpace +
                                           Job +
                                           Seperators.WhiteSpace +
                                           InfoString.Is +
                                           Seperators.WhiteSpace +
                                           status.ToString( );

            Console.WriteLine( StatusInformation );

            return InfoOperationMode.CENTER_KITCHEN_AND_LIVING_ROOM +
                                 Seperators.InfoSeperator +
                                 HomeAutomationAnswers.ANSWER_SCHEDULER_STATUS +
                                 Seperators.InfoSeperator +
                                 Job +
                                 Seperators.InfoSeperator +
                                 status.ToString( );


        }

        #endregion

        #region PROPERTIES_IO_INTERFACE
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
            HeatersLivingRoom.Reset( );
            HeaterAnteRoom.Reset( );
        }

        void Reset( InputChangeEventArgs e )
        {
            if (e.Value == true)
            {
                Kitchen?.StartWaitForAllOff( );
            }
            else
            {
                Kitchen?.StopWaitForAllOff( );
            }
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

        public void StopAliveSignal()
        {
            Kitchen?.StopAliveSignal( );
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
                // first relase start one light, next relase start neighbor light, turn previous light off
                // press button longer than ( f.e. 1.. seconds ) - all lights on
                // press button longer than ( f.e. 2.. seconds ) - all lights off
                // TODO press button longer than ( f.e. 3.. seconds ) - broadcast all lights off, just default lights on
                case KitchenIOAssignment.indKitchenMainButton:
                    if (HeatersLivingRoom != null)
                    {
                        // operate light only when there is no demand of manual heater control
                        if (!HeatersLivingRoom.WasHeaterSwitched( ))  // this is not good OOP - any day try to refactor - "TELL - don´t ask"
                        {
                            Kitchen?.MakeStep( Value );
                            Kitchen?.AutomaticOff( Value );
                        }
                        else
                        {
                            Kitchen?.StopAllOnTimer( );
                            Kitchen?.ResetDeviceControl( );
                        }
                    }
                    // reset - this is a last rescue anchor in the case something went wrong ( any undiscovered bug )
                    Reset( Value );
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
            switch (index) // the index is the assigned input number
            {
                case CenterButtonRelayIOAssignment.indDigitalInputRelayWashRoom:
                    FanWashRoom?.DelayedDeviceOnFallingEdge( Value );
                    break;

                default:
                    break;
            }
        }

        void ControlHeaters( InputChangeEventArgs e )
        {
            int index = e.Index;
            bool Value = e.Value;
            ControlHeaters( index, Value );
        }

        void ControlHeaters( int index, bool Value )
        {
            switch (index) // the index is the assigned input number
            {
                case KitchenIOAssignment.indKitchenMainButton:
                    HeatersLivingRoom?.HeaterOn( Value ); // heaters can be switched on / off with the light button
                    break;
                // heater in the ante room so far is controlled by center/kitchen/living room sbc
                // reason is that the cable conneting the actuator was easier to lay 
                case CenterButtonRelayIOAssignment.indDigitalInputRelayAnteRoom:
                    HeaterAnteRoom?.HeaterOnFallingEdge( Value );
                    break;

                default:
                    break;
            }
        }

        void ControlCirculationPump( InputChangeEventArgs e )
        {
            ControlCirculationPump( e.Index, e.Value );
        }

        void ControlCirculationPump( int index, bool Value )
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

        int ToggleHeatersOnOff = 0;
        void HeatersLivingRoom_AllOn_( object sender )
        {
            if (ToggleHeatersOnOff == 0)
            {
                Kitchen.AnyExternalDeviceOn = true;
                Kitchen.AnyExternalDeviceOff = false;
            }
            ToggleHeatersOnOff++;
            if (ToggleHeatersOnOff > 1)
            {
                ToggleHeatersOnOff = 0;
                Kitchen.AnyExternalDeviceOn = false;
                Kitchen.AnyExternalDeviceOff = true;
            }
        }

        void ControlSequenceOnInputChange( int index, bool Value )
        {
            if (Kitchen != null)
            {
                Kitchen.StateDigitalOutput = base.StateDigitalOutput;
            }

            TurnNextDevice( index, Value );

            ControlHeaters( index, Value );

            ControlCirculationPump( index, Value );

            TurnFan( index, Value );

            if (_DigitalInputState == null)
            {
                return;
            }

            for (int i = 0; i < _DigitalInputState.Length; i++)
            {
                if (Attached)
                {
                    // simplification - input state is written in a bool array
                    _DigitalInputState[i] = inputs[i];
                }
            }
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

        void TurnHeaterBodyEast( bool command )
        {
            outputs[KitchenLivingRoomIOAssignment.indDigitalOutputHeaterEast] = command;
        }

        void TurLightOutside( bool command )
        {
            if (!RemoteControlLightOutsideActivated)
            {
                outputs[CenterOutsideIODevices.indDigitalOutputLightsOutside] = command;
            }
        }

        void ResetInternals()
        {
            RemoteControlLightOutsideActivated = false;
        }
        #endregion 

        #region REMOTE_CONTROLLED_UDP
        decimal receivedTransactionCounter = 0;
        decimal PreviousreceivedTransactionCounter = 0;
        const int ExpectedArrayElementsSignalTelegram = UdpTelegram.DelfaultExpectedArrayElementsSignalTelegram;
        const int ExpectedArrayElementsCommonCommand = 1;

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
                        TurnBoiler( GeneralConstants.ON );
                        break;

                    case ComandoString.TURN_BOILER_OFF:
                        TurnBoiler( GeneralConstants.OFF );
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
                        outputs[KitchenLivingRoomIOAssignment.indDigitalOutputHeaterEast] = true;
                        break;

                    case ComandoString.TURN_HEATER_BODY_EAST_OFF:
                        outputs[KitchenLivingRoomIOAssignment.indDigitalOutputHeaterEast] = false;
                        break;

                    case ComandoString.TURN_HEATER_BODY_WEST_ON:
                        outputs[KitchenLivingRoomIOAssignment.indDigitalOutputHeaterWest] = true;
                        break;

                    case ComandoString.TURN_HEATER_BODY_WEST_OFF:
                        outputs[KitchenLivingRoomIOAssignment.indDigitalOutputHeaterWest] = false;
                        break;
                }
                Console.WriteLine( TimeUtil.GetTimestamp_( ) + " Received telegramm: " + DatagrammSplitted[0] + " from " + e.Adress + " : " + e.Port );
                return;
            }

            if (DatagrammSplitted.Length != ExpectedArrayElementsSignalTelegram)
            {
                Services.TraceMessage_( "Wrong datagramm received" );
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
            int ReceivedIndex = Convert.ToInt16( DatagrammSplitted[ComandoString.Telegram.IndexDigitalInputs] );
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
            _DigitalInputEventargs.Index = e.Index;
            _DigitalInputEventargs.Value = e.Value;
            _DigitalInputEventargs.SerialNumber = base.SerialNumber;
            EDigitalInputChanged?.Invoke( sender, _DigitalInputEventargs );
        }

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

        // so far this program was not designed by using dependency injection - thats the reason using a public 
        // testbackdoor channel
        #region TESTBACKDOOR
        public void TestBackdoor_IoChange( int index, bool Value )
        {
            ControlSequenceOnInputChange( index, Value );
        }

        public bool[] ReferenceDigitalOutputState
        {
            get
            {
                return ( _InternalDigitalOutputState );
            }

            set
            {
                _InternalDigitalOutputState = value;
            }
        }

        public void TestBackdoor_UdpReceiver( string receiveddatagramm )
        {
        }

        public void TestBackdoorSchedulerControl( IJobExecutionContext context, decimal counts, string device )
        {
            ControlScheduledDevice( counts, device );
        }
        #endregion
    }
}
