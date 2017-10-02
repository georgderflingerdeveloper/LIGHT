using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using Communication.CLIENT_Communicator;
using Communication.HAProtocoll;
using Communication.UDP;
using HAHardware;
using HomeAutomation.HardConfig;
using HomeAutomation.rooms;
using Phidgets;
using Phidgets.Events;
using Scheduler;
using SystemServices;
using Equipment;
using HA_COMPONENTS;
using BASIC_COMPONENTS;
using HomeControl.BASIC_COMPONENTS.Interfaces;
using ROOMS.CENTER.INTERFACE;


namespace HomeAutomation
{

	class Center_kitchen_living_room_NG : CommonRoom, IIOHandlerInfo, ICenter 
    {
        #region DECLARATION
        LightControlKitchen_NG               Kitchen;
        LightControl_NG                      Outside;
        HeaterElement_NG                     HeatersLivingRoom;
        HeaterElement_NG                     HeaterAnteRoom;
        CentralControlledElements_NG         FanWashRoom;
        CentralControlledElements_NG         CirculationPump;
        BasicClientComumnicator              BasicClientCommunicator_;
        home_scheduler                       scheduler;
        SchedulerDataRecovery                schedRecover;
        FeedData                             PrevSchedulerData = new FeedData();
        UdpReceive                           UDPReceive_;
        Timer                                TimerRecoverScheulder;
        Timer                                CommonUsedTick =  new Timer( GeneralConstants.DURATION_COMMONTICK );
        bool[]                               _DigitalInputState;
        bool[]                               _DigitalOutputState;
        bool[]                               _InternalDigitalOutputState;
        bool                                 _Test;
        long                                 RemainingTime;
        PowerMeter                           _PowerMeter;
		int                                  _PortNumberServer;

        public delegate void UpdateMatchedOutputs( object sender, bool[] _DigOut );
        public event UpdateMatchedOutputs EUpdateMatchedOutputs;

		public event DigitalInputChanged  EDigitalInputChanged;
		public event DigitalOutputChanged EDigitalOutputChanged;

		DigitalInputEventargs             _DigitalInputEventargs   = new DigitalInputEventargs();
		DigitalOutputEventargs            _DigitalOutputEventargs  = new DigitalOutputEventargs();
		#endregion

        // more objects share the same eventhandler in this case
        void CommonUsedEventHandlers( )
        {
            Kitchen.EUpdateOutputs_           += EShowUpdatedOutputs;
            HeatersLivingRoom.EUpdateOutputs_ += EShowUpdatedOutputs;
            HeaterAnteRoom.EUpdateOutputs_    += EShowUpdatedOutputs;
            CirculationPump.EUpdateOutputs_   += EShowUpdatedOutputs;
            Outside.EUpdateOutputs            += EShowUpdatedOutputs;
            FanWashRoom.EUpdateOutputs_       += EShowUpdatedOutputs;
        }

        #region CONSTRUCTOR
        void Constructor( string IpAdressServer, string PortServer, string softwareversion )
        {
            _GivenClientName            = InfoOperationMode.CENTER_KITCHEN_AND_LIVING_ROOM;
            _IpAdressServer             = IpAdressServer;
            _PortNumberServer           = Convert.ToInt16( PortServer );
            _DigitalInputState          = new bool[GeneralConstants.NumberOfInputsIOCard];
            _DigitalOutputState         = new bool[GeneralConstants.NumberOfOutputsIOCard];
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
                                         );
                
            Outside.Match = new List<int> { CenterOutsideIODevices.indDigitalOutputLightsOutside };

            Kitchen.IsPrimaryIOCardAttached = base._PrimaryIOCardIsAttached;

            HeatersLivingRoom = new HeaterElement_NG(
                                         ParametersHeaterControl.TimeDemandForHeatersOnOff,
                                         ParametersHeaterControl.TimeDemandForHeatersAutomaticOffHalfDay,
                                         ParametersHeaterControlLivingRoom.TimeDemandForHeatersOnMiddle,
                                         ParametersHeaterControlLivingRoom.TimeDemandForHeatersOffMiddle,
                                         KitchenLivingRoomIOAssignment.indFirstHeater,
                                         KitchenLivingRoomIOAssignment.indLastHeater );

            HeatersLivingRoom.Match = new List<int>
                                         { 
                                             KitchenLivingRoomIOAssignment.indFirstHeater,
                                             KitchenLivingRoomIOAssignment.indLastHeater 
                                         };

            HeaterAnteRoom = new HeaterElement_NG(
                                         ParametersHeaterControl.TimeDemandForHeatersOnOff,
                                         ParametersHeaterControl.TimeDemandForHeatersAutomaticOffBig,
                                         ParametersHeaterControlLivingRoom.TimeDemandForHeatersOnMiddle,
                                         ParametersHeaterControlLivingRoom.TimeDemandForHeatersOffMiddle,
                                         AnteRoomIODeviceIndices.indDigitalOutputAnteRoomHeater,
                                         AnteRoomIODeviceIndices.indDigitalOutputAnteRoomHeater );
                
            HeaterAnteRoom.Match = new List<int> { AnteRoomIODeviceIndices.indDigitalOutputAnteRoomHeater };

            FanWashRoom = new CentralControlledElements_NG(
                                     ParametersWashRoomControl.TimeDemandForFanOn,
                                     ParametersWashRoomControl.TimeDemandForFanAutomaticOff,
                                     WashRoomIODeviceIndices.indDigitalOutputWashRoomFan );

            FanWashRoom.Match = new List<int> { WashRoomIODeviceIndices.indDigitalOutputWashRoomFan };

            CirculationPump = new CentralControlledElements_NG(
                                         ParametersWaterHeatingSystem.TimeDemandForWarmCirculationPumpAutomaticOff,
                                         WaterHeatingSystemIODeviceIndices.indDigitalOutputWarmWaterCirculationPump );

            CirculationPump.Match = new List<int> { WaterHeatingSystemIODeviceIndices.indDigitalOutputWarmWaterCirculationPump };


            HeatersLivingRoom.AllOn_ += HeatersLivingRoom_AllOn_;
            Kitchen.EReset           += Kitchen_EReset;

            #region REGISTRATION_ONE_COMMON_EVENT_HANDLER
            CommonUsedEventHandlers( );
            #endregion

            if ( _Test )
            {
                return;
            }

            scheduler             = new home_scheduler();
            TimerRecoverScheulder = new Timer(Parameters.DelayTimeStartRecoverScheduler);

            CommonUsedTick.Elapsed            += CommonUsedTick_Elapsed;

            base.Attach                       += Center_kitchen_living_room_Attach;
            base.Detach                       += Center_kitchen_living_room_Detach;

            BasicClientCommunicator_ = new BasicClientComumnicator( _GivenClientName,
                                                                    _IpAdressServer,
                                                                     PortServer,
                                                                     ref base.outputs, // control directly digital outputs of primer - server can control this outputs
                                                                     ref HADictionaries.DeviceDictionaryCenterdigitalOut,
                                                                     ref HADictionaries.DeviceDictionaryCenterdigitalIn, softwareversion );
            // establish client
            BasicClientCommunicator_.Room                     = _GivenClientName;
            BasicClientCommunicator_.EFeedScheduler          += BasicClientCommunicator__EFeedScheduler;
            BasicClientCommunicator_.EAskSchedulerForStatus  += BasicClientCommunicator__EAskSchedulerForStatus;
            scheduler.EvTriggered                            += Scheduler_EvTriggered;

            BasicClientCommunicator_.Primer1IsAttached = Attached;

            if( !Attached )
            {
                BasicClientCommunicator_.SendInfoToServer( InfoString.InfoNoIO );
            }

            try
            {
                UDPReceive_ = new UdpReceive( IPConfiguration.Port.PORT_UDP_WEB_FORWARDER_CENTER );
                UDPReceive_.EDataReceived += UDPReceive__EDataReceived;
            }
            catch( Exception ex )
            {
                Console.WriteLine( TimeUtil.GetTimestamp() + Seperators.WhiteSpace + ex.Message );
                Services.TraceMessage_( InfoString.FailedToEstablishUDPReceive );
            }

            // after power fail, certain important scheduler data is recovered
            schedRecover                   = new SchedulerDataRecovery( Directory.GetCurrentDirectory() );
            schedRecover.ERecover         += schedRecover_ERecover;
            schedRecover.ERecovered       += schedRecover_ERecovered;
            TimerRecoverScheulder.Elapsed += TimerRecoverScheulder_Elapsed;
            TimerRecoverScheulder.Start( );
            CommonUsedTick.Start( );
            Console.WriteLine( TimeUtil.GetTimestamp() + Seperators.WhiteSpace + InfoString.StartTimerForRecoverScheduler );
            RemainingTime = Convert.ToUInt32( (Parameters.DelayTimeStartRecoverScheduler / Parameters.MilisecondsOfSecond)  );

            _PowerMeter = new PowerMeter( true, PowermeterConstants.DefaultCaptureIntervallTime, PowermeterConstants.DefaultStoreTime );
        }

        public Center_kitchen_living_room_NG( string IpAdressServer, string PortServer, string softwareversion )
            : base()
        {
            Constructor(  IpAdressServer,  PortServer,  softwareversion );
        }

        public Center_kitchen_living_room_NG( string IpAdressServer, string PortServer, string softwareversion, bool test )
            : base(true)
        {
           _Test = test;
           Constructor( IpAdressServer, PortServer, softwareversion );
        }
        #endregion

        #region SCHEDULER
        // configure, start, stop scheduler
        void BasicClientCommunicator__EFeedScheduler( object sender, FeedData e )
        {
            SchedulerApplication.Worker( sender, e, ref scheduler );
        }

        void schedRecover_ERecovered( object sender, EventArgs e )
        {
            SchedulerApplication.DataRecovered = true;
        }

        // scheduler starts with recovered data
        void schedRecover_ERecover( FeedData e )
        {
            string Job = e.Device + Seperators.InfoSeperator + e.JobId.ToString();
            SchedulerApplication.Worker( this, e, ref scheduler );
            Console.WriteLine( TimeUtil.GetTimestamp()  + Seperators.WhiteSpace + "Recover scheduler after booting " );
            Console.WriteLine( TimeUtil.GetTimestamp( ) + Seperators.WhiteSpace + "Device:                   " + e.Device );
            Console.WriteLine( TimeUtil.GetTimestamp()  + Seperators.WhiteSpace + "Start time:               " + e.Starttime );
            Console.WriteLine( TimeUtil.GetTimestamp()  + Seperators.WhiteSpace + "Stop time :               " + e.Stoptime );
            Console.WriteLine( TimeUtil.GetTimestamp()  + Seperators.WhiteSpace + "Configured days:          " + e.Days );
            Console.WriteLine( TimeUtil.GetTimestamp( ) + Seperators.WhiteSpace + "Job:                      " + Job );
            Console.WriteLine( TimeUtil.GetTimestamp()  + Seperators.WhiteSpace + "Current scheduler status: " + scheduler.GetJobStatus( Job ) );
        }

        void Scheduler_EvTriggered( string time, Quartz.IJobExecutionContext context, decimal counts )
        {
            SchedulerApplication.WriteStatus( time, context, counts );

            if ( !Attached )
            {
                return;
            }

            ControlScheduledDevice( context, counts, context.JobDetail.Key.Name );
        }

        void ControlScheduledDevice( Quartz.IJobExecutionContext context, decimal counts, string device )
        {
            string[] DeviceParts;
            DeviceParts = device.Split( '_' );
            HADictionaries.DeviceDictionaryCenterdigitalOut.TryGetValue( DeviceParts[0], out int index );
            bool SchedulerIsStartingAnyAction = (counts % 2 != 0) ? true : false;

            if( device.Contains( nameof( CenterKitchenDeviceNames.FumeHood ) ) )
            {
               if( SchedulerIsStartingAnyAction ) 
               {
                   Kitchen.ActualKitchenStep = LightControlKitchen_NG.KitchenStep.eSlots;
               }
               else 
               {
                   Kitchen.ActualKitchenStep = LightControlKitchen_NG.KitchenStep.eFrontLights;
               }
            }

            if( device.Contains( nameof( HardConfig.HardwareDevices.Boiler ) ) )
            {
                if( base.outputs != null )
                {
                    if( index >= 0 && index < GeneralConstants.NumberOfOutputsIOCard )
                    {
                        if( SchedulerIsStartingAnyAction )
                        {
                            base.outputs[index] = GeneralConstants.ON;
                        }
                        else
                        {
                            base.outputs[index] = GeneralConstants.OFF;
                        }
                    }
                }
            }
        }

        string AskForSchedulerStatus( string Job )
        {
            string SystemIsAskingScheduler = TimeUtil.GetTimestamp()                +
                                                 Seperators.WhiteSpace              +
                                                 _GivenClientName                   +
                                                 "...."                             +
                                                 InfoString.Asking                  +
                                                 Seperators.WhiteSpace              +
                                                 InfoString.Scheduler;

            Console.WriteLine( SystemIsAskingScheduler );
            SchedulerInfo.Status status = scheduler.GetJobStatus( Job );

            string StatusInformation = TimeUtil.GetTimestamp()                      +
                                           Seperators.WhiteSpace                    +
                                           _GivenClientName                         +
                                           Seperators.WhiteSpace                    +
                                           InfoString.StatusOf                      +
                                           Seperators.WhiteSpace                    +
                                           Job                                      +
                                           Seperators.WhiteSpace                    +
                                           InfoString.Is                            +
                                           Seperators.WhiteSpace                    +
                                           status.ToString();

            Console.WriteLine( StatusInformation );

            return    InfoOperationMode.CENTER_KITCHEN_AND_LIVING_ROOM   +
                                 Seperators.InfoSeperator                           +
                                 HomeAutomationAnswers.ANSWER_SCHEDULER_STATUS      +
                                 Seperators.InfoSeperator                           +
                                 Job                                                +
                                 Seperators.InfoSeperator                           +
                                 status.ToString();


        }

        void BasicClientCommunicator__EAskSchedulerForStatus( object sender, string Job )
        {
            string Answer = AskForSchedulerStatus( Job );
            BasicClientCommunicator_.SendInfoToServer( Answer );
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
            if( Kitchen != null )
            {
                Kitchen.IsPrimaryIOCardAttached = base._PrimaryIOCardIsAttached;
                BasicClientCommunicator_.SendInfoToServer( InfoString.InfoNoIO + e.Device.ID.ToString() );
            }
         }

        void Center_kitchen_living_room_Attach( object sender, AttachEventArgs e )
        {
            if( Kitchen != null )
            {
                Kitchen.IsPrimaryIOCardAttached = base._PrimaryIOCardIsAttached;
            }
        }
        #endregion

        #region CONTROLLOGIC
        void Kitchen_EReset( object sender )
        {
            HeatersLivingRoom.Reset();
            HeaterAnteRoom.Reset();
        }

        void Reset( InputChangeEventArgs e )
        {
            if( e.Value == true )
            {
                Kitchen?.StartWaitForAllOff();
            }
            else
            {
                Kitchen?.StopWaitForAllOff();
            }
        }

        void Reset( bool command )
        {
            if( command == true )
            {
                Kitchen?.StartWaitForAllOff();
            }
            else
            {
                Kitchen?.StopWaitForAllOff();
            }
        }

        public void StopAliveSignal()
        {
            Kitchen?.StopAliveSignal();
        }

        protected override void TurnNextLightOn_( InputChangeEventArgs e )
        {
            int index  = e.Index;
            bool Value = e.Value;
            TurnNextDevice( index, Value );
        }

		void TurnNextDevice( int index,  bool Value  )
        {
                switch( index ) // the index is the assigned input number
                {
                    // first relase start one light, next relase start neighbor light, turn previous light off
                    // press button longer than ( f.e. 1.. seconds ) - all lights on
                    // press button longer than ( f.e. 2.. seconds ) - all lights off
                    // TODO press button longer than ( f.e. 3.. seconds ) - broadcast all lights off, just default lights on
                   case KitchenIOAssignment.indKitchenMainButton:
                        if( HeatersLivingRoom != null )
                        {
                            // operate light only when there is no demand of manual heater control
                            if( !HeatersLivingRoom.WasHeaterSwitched() )  // this is not good OOP - any day try to refactor - "TELL - don´t ask"
                            {
                                Kitchen?.MakeStep( Value );
                                Kitchen?.AutomaticOff( Value );
                            }
                            else
                            {
                                Kitchen?.StopAllOnTimer();
                                Kitchen?.ResetDeviceControl();
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
            int index  = e.Index;
            bool Value = e.Value;
            ControlHeaters( index, Value );
        }

        void ControlHeaters( int index, bool Value )
        {
                switch( index ) // the index is the assigned input number
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
            switch( index ) // the index is the assigned input number
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
            if( ToggleHeatersOnOff == 0 )
            {
                Kitchen.AnyExternalDeviceOn  = true;
                Kitchen.AnyExternalDeviceOff = false;
            }
            ToggleHeatersOnOff++;
            if( ToggleHeatersOnOff > 1 )
            {
                ToggleHeatersOnOff = 0;
                Kitchen.AnyExternalDeviceOn  = false;
                Kitchen.AnyExternalDeviceOff = true;
            }
        }

        void ControlSequenceOnInputChange( int index, bool Value )
        {
            if( Kitchen != null )
            {
                Kitchen.StateDigitalOutput = base.StateDigitalOutput;
            }

            TurnNextDevice( index, Value );

            ControlHeaters( index, Value );

            ControlCirculationPump( index, Value );

            TurnFan( index, Value );

            if ( _DigitalInputState == null )
            {
                return;
            }

            for( int i = 0; i < _DigitalInputState.Length; i++ )
            {
                if( Attached )
                {
                    // simplification - input state is written in a bool array
                    _DigitalInputState[i] = inputs[i];
                }
            }

            if( BasicClientCommunicator_ != null )
            {
                BasicClientCommunicator_.DigitalInputs = _DigitalInputState;
                BasicClientCommunicator_.IndexInput    = index;
            }
        }
        #endregion

        #region SWITCH_DEVICE_GROUPS
        void TurnBoiler( bool commando )
        {
            base.outputs[CenterLivingRoomIODeviceIndices.indDigitalOutputBoiler] = commando;
        }

        void TurnFrontLights( bool commando )
        {
            base.outputs[KitchenCenterIoDevices.indDigitalOutputFrontLight_1] = commando; 
            base.outputs[KitchenCenterIoDevices.indDigitalOutputFrontLight_2] = commando; 
            base.outputs[KitchenCenterIoDevices.indDigitalOutputFrontLight_3] = commando; 
        }

        void TurnKitchenLights( bool commando )
        {
            base.outputs[KitchenCenterIoDevices.indDigitalOutputFirstKitchen]   = commando;
            base.outputs[KitchenCenterIoDevices.indDigitalOutputFrontLight_1]   = commando;
            base.outputs[KitchenCenterIoDevices.indDigitalOutputFrontLight_2]   = commando;
            base.outputs[KitchenCenterIoDevices.indDigitalOutputFrontLight_3]   = commando;
            base.outputs[KitchenCenterIoDevices.indDigitalOutputFumeHood]       = commando;
            base.outputs[KitchenCenterIoDevices.indDigitalOutputSlot]           = commando;
            base.outputs[KitchenCenterIoDevices.indDigitalOutputKitchenKabinet] = commando;
        }

        void TurnWindowLedgeEast( bool command )
        {
            base.outputs[KitchenCenterIoDevices.indDigitalOutputWindowBoardEastDown] = command;
        }
        #endregion 

        #region REMOTE_CONTROLLED_UDP
        decimal receivedTransactionCounter            = 0;
        decimal PreviousreceivedTransactionCounter    = 0;
		const int ExpectedArrayElementsSignalTelegram = UdpTelegram.DelfaultExpectedArrayElementsSignalTelegram;
        const int ExpectedArrayElementsCommonCommand  = 1;

        void UDPReceive__EDataReceived( string e )
        {
            string[] DatagrammSplitted = e.Split( ComandoString.Telegram.Seperator );

            if( DatagrammSplitted.Length == ExpectedArrayElementsCommonCommand )
            {
                switch( DatagrammSplitted[0] )
                {
                    case ComandoString.TURN_ALL_LIGHTS_ON:
                    case ComandoString.TURN_ALL_KITCHEN_LIGHTS_ON:
                         TurnKitchenLights( GeneralConstants.ON );
                         Kitchen?.AutomaticOff( true );
                         break;

                    case ComandoString.TURN_ALL_LIGHTS_OFF:
                    case ComandoString.TURN_ALL_KITCHEN_LIGHTS_OFF:
                         TurnKitchenLights( GeneralConstants.OFF );
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
                         base.outputs[KitchenCenterIoDevices.indDigitalOutputKitchenKabinet] = true;
                         break;

                    case ComandoString.TURN_KITCHEN_LIGHTS_CABINET_OFF:
                         base.outputs[KitchenCenterIoDevices.indDigitalOutputKitchenKabinet] = false;
                         break;

                    case ComandoString.TURN_KITCHEN_LIGHT_FUMEHOOD_ON:
                         base.outputs[KitchenCenterIoDevices.indDigitalOutputFumeHood] = true;
                         break;

                    case ComandoString.TURN_KITCHEN_LIGHT_FUMEHOOD_OFF:
                         base.outputs[KitchenCenterIoDevices.indDigitalOutputFumeHood] = false;
                         break;

                    case ComandoString.TURN_KITCHEN_LIGHT_OVER_CABINET_RIGHT_ON:
                         base.outputs[KitchenCenterIoDevices.indDigitalOutputFirstKitchen] = true;
                         break;

                    case ComandoString.TURN_KITCHEN_LIGHT_OVER_CABINET_RIGHT_OFF:
                         base.outputs[KitchenCenterIoDevices.indDigitalOutputFirstKitchen] = false;
                         break;

                    case ComandoString.TURN_KITCHEN_LIGHT_SLOT_ON:
                         base.outputs[KitchenCenterIoDevices.indDigitalOutputSlot] = true;
                         break;

                    case ComandoString.TURN_KITCHEN_LIGHT_SLOT_OFF:
                         base.outputs[KitchenCenterIoDevices.indDigitalOutputSlot] = false;
                         break;

                    case ComandoString.TURN_KITCHEN_FRONT_LIGHT_1_ON:
                         base.outputs[KitchenCenterIoDevices.indDigitalOutputFrontLight_1] = true;
                         break;

                    case ComandoString.TURN_KITCHEN_FRONT_LIGHT_2_ON:
                         base.outputs[KitchenCenterIoDevices.indDigitalOutputFrontLight_2] = true;
                         break;

                    case ComandoString.TURN_KITCHEN_FRONT_LIGHT_3_ON:
                         base.outputs[KitchenCenterIoDevices.indDigitalOutputFrontLight_3] = true;
                         break;

                    case ComandoString.TURN_KITCHEN_FRONT_LIGHT_1_OFF:
                         base.outputs[KitchenCenterIoDevices.indDigitalOutputFrontLight_1] = false;
                         break;

                    case ComandoString.TURN_KITCHEN_FRONT_LIGHT_2_OFF:
                         base.outputs[KitchenCenterIoDevices.indDigitalOutputFrontLight_2] = false;
                         break;

                    case ComandoString.TURN_KITCHEN_FRONT_LIGHT_3_OFF:
                         base.outputs[KitchenCenterIoDevices.indDigitalOutputFrontLight_3] = false;
                         break;

                    case ComandoString.TURN_LIGHT_OUTSIDE_ON:
                         Outside.TurnAllDevices( GeneralConstants.ON );
                         break;

                    case ComandoString.TURN_LIGHT_OUTSIDE_OFF:
                         Outside.TurnAllDevices( GeneralConstants.OFF );
                         break;

                    case ComandoString.TURN_WINDOW_LEDGE_EAST_ON:
                         TurnWindowLedgeEast( GeneralConstants.ON );
                         break;

                    case ComandoString.TURN_WINDOW_LEDGE_EAST_OFF:
                         TurnWindowLedgeEast( GeneralConstants.OFF );
                         break;

                }
                return;
            }

            if( DatagrammSplitted.Length != ExpectedArrayElementsSignalTelegram )
            {
                Services.TraceMessage_("Wrong datagramm received");
                return;
            }

            string transactioncounter = DatagrammSplitted[ComandoString.Telegram.IndexTransactionCounter];

            receivedTransactionCounter = Convert.ToDecimal( transactioncounter );

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
            int ReceivedIndex = Convert.ToInt16(DatagrammSplitted[ComandoString.Telegram.IndexDigitalInputs]);
            // received acutal fired value of digital input
            bool ReceivedValue = Convert.ToBoolean(DatagrammSplitted[ComandoString.Telegram.IndexValueDigitalInputs]);

            switch( ReceivedIndex )
            {
                case LivingRoomWestIOAssignment.LivWestDigInputs.indDigitalInputButtonMainUpLeft:
                     // turn light ON / OFF when releasing the button 
                     if( ReceivedValue == false )
                     {
                         Outside.ToggleSingleDevice(CenterOutsideIODevices.indDigitalOutputLightsOutside);
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

            if( e.Index == CenterLivingRoomIODeviceIndices.indDigitalInputPowerMeter )
            {
                if( e.Value )
                {
                    _PowerMeter?.Tick( );
                }
            }
			_DigitalInputEventargs.Index        = e.Index;
			_DigitalInputEventargs.Value        = e.Value;
			_DigitalInputEventargs.SerialNumber = base.SerialNumber;
			EDigitalInputChanged?.Invoke( sender, _DigitalInputEventargs);
        }

        protected override void BuildingSection_OutputChange( object sender, OutputChangeEventArgs e )
        {
            if( _DigitalOutputState == null )
            {
                return;
            }
            for( int i = 0; i < _DigitalOutputState.Length; i++ )
            {
                _DigitalOutputState[i] = base.outputs[i];
            }
            if( BasicClientCommunicator_ != null )
            {
                BasicClientCommunicator_.DigitalOutputs = _DigitalOutputState;
                BasicClientCommunicator_.IndexOutput    = e.Index;
            }
			_DigitalOutputEventargs.Index        = e.Index;
			_DigitalOutputEventargs.Value        = e.Value;
			_DigitalOutputEventargs.SerialNumber = SerialNumber;
			EDigitalOutputChanged?.Invoke( sender, _DigitalOutputEventargs );
        }
        #endregion

        #region EVENTHANDLERS
        void EShowUpdatedOutputs( object sender, bool[] _DigOut, List<int> Match)
        {
            for ( int i = 0; i < _DigOut.Length; i++ )
            {
                if( Match.Contains( i ) ) // only matching index within the defined list are written into the array
                {
                    _InternalDigitalOutputState[i] = _DigOut[i];
                    if( Attached )
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
            schedRecover.RecoverScheduler( Directory.GetCurrentDirectory(), HardConfig.HardwareDevices.Devices );
            TimerRecoverScheulder.Stop();
        }
        
        private void CommonUsedTick_Elapsed( object sender, ElapsedEventArgs e )
        {
            string RemainingTimeInfo;
            string TimeLeft = (--RemainingTime).ToString( "000" );

            if( RemainingTime > 0 )
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
            Outside.ResetDeviceControl();
            PreviousreceivedTransactionCounter = 0;
        }
        #endregion

        // so far this program was not designed by using dependency injection - thats the reason using a public 
        // testbackdoor channel
        #region TESTBACKDOOR
        public void TestBackdoor_IoChange( int index,  bool Value  )
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
			UDPReceive__EDataReceived( receiveddatagramm );
		}
        #endregion
    }
}
