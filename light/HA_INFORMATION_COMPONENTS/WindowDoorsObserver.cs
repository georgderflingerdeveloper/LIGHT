using System;
using System.Collections.Generic;
using System.Timers;
using Communication;
using Communication.HAProtocoll;
using HomeAutomation.HardConfig_Collected;
using Phidgets;
using HomeAutomation.Controls;


namespace HomeAutomation
{
    // purpose is the give information about certain window and door states
    // - are any critical windows or doors open when nobody is inside the building ?
    // direct IO control - so any signal lights can be controlled
    // events to perform different actions - like f.e. triggering alarm sounds, sending warning emails, ....
    public class WindowDoorsObserver
    {
        #region DECLARATION
        const double TimeIndicationOn  = 500;
        const double TimeIndicationOff = 1500;
        public enum ObservingStates
        {
            eWaitForObserving,
            eObserve,
            eIndicating,
            eWarning,
            eAlarm
        }

        double                              _TimeDelayObserving;
        //string                              _IOStatus;
        //bool                                _PresenceDetectorIsActive;
        ObservingStates                     _ActualState;        
        InterfaceKitDigitalOutputCollection outputs_;
        Timer                               _PresenceTimer;
        delegate void WindowIsOpen( object sender, string windowName );
        event         WindowIsOpen WindowIsOpen_;
        delegate void WindowIsClosed( object sender, string windowName );
        event         WindowIsClosed WindowIsClosed_;
        delegate void Observer( object sender, ObservingStates state );
        event         Observer EObserver;
        event         EventHandler EAllWindowsAreClosed = delegate { };
        Dictionary<string, uint> OpenWindowsDictionary = new Dictionary<string, uint>();
        string        PreviousWindowDeviceOpen;
        string        PreviousWindowDeviceClose;
        bool          AnyCriticalWindowIsOpen;
        UnivPWM       SignalGenerator = new UnivPWM( TimeIndicationOn, TimeIndicationOff ); 
        bool          InhibitFurtherCommanding = false;
        string        _GivenRoomName;

        #endregion

        #region CONSTRUCTOR
        void Constructor( double TimeDelayObserving, ref InterfaceKitDigitalOutputCollection outputs )
        {
            _TimeDelayObserving        = TimeDelayObserving;
            _ActualState               = ObservingStates.eObserve;
            outputs_                   = outputs;
            _PresenceTimer             = new Timer( TimeDelayObserving );
            _PresenceTimer.Elapsed    += _PresenceTimer_Elapsed;
            this.WindowIsOpen_        += WindowDoorsObserver_WindowIsOpen_;
            this.WindowIsClosed_      += WindowDoorsObserver_WindowIsClosed_;
            this.EObserver            += WindowDoorsObserver_EObserver;
            this.EAllWindowsAreClosed += WindowDoorsObserver_EAllWindowsAreClosed;
            SignalGenerator.PWM_      += SignalGenerator_PWM_;
        }

        public WindowDoorsObserver( double TimeDelayObserving, ref InterfaceKitDigitalOutputCollection outputs )
        {
            Constructor( TimeDelayObserving, ref outputs );
        }

        public WindowDoorsObserver( string GivenRoomName, double TimeDelayObserving, ref InterfaceKitDigitalOutputCollection outputs )
        {
            Constructor( TimeDelayObserving, ref outputs );
            _GivenRoomName = GivenRoomName;
        }

       #endregion

        #region PUBLIC_METHODS
        // listen for IO datagrams sent by different stations or create on owner 
        public void ListenForChanges( string receivedDatagramm )   // IOStatus must contain ROOM_InputINDEX_InputValue
        {
            const int PositionValue   = 1;
            string[]  inputStatusParts;
            string    Room;
            uint      Index;
            bool      Value;
            string    device = "";

            // muting can be commanded 
            if( receivedDatagramm.Contains( ObserverComandos.WindowObserverMute ) )
            {
                EObserver( this, ObservingStates.eWaitForObserving );
                InhibitFurtherCommanding = true;
                return;
            }

            if( receivedDatagramm.Contains( ObserverComandos.WindowObserverUnMute ) )
            {
                EObserver( this, ObservingStates.eObserve );
                InhibitFurtherCommanding = false;
                return;
            }

            if( InhibitFurtherCommanding )
            {
                return;
            }

            // TODO muting remains until timer elapsed
            inputStatusParts = receivedDatagramm.Split( Seperators.delimiterCharsExtended );
            Room     = inputStatusParts[ObserverIndices.IndRoom];
            Index    = Convert.ToUInt32( MessageAnalyzer.GetMessagePartAfterKeyWord( receivedDatagramm, Room ) );
            Value    = Convert.ToBoolean( MessageAnalyzer.GetMessagePartAfterKeyWord( receivedDatagramm, Room, PositionValue ) );

            switch( Room )
            {
                case InfoOperationMode.SLEEPING_ROOM:
                     device = SleepingRoomIODeviceIndices.GetInputDeviceName( Index );
                     break;
                case InfoOperationMode.OUTSIDE:
                     device = CenterOutsideIODevices.GetInputDeviceName( Index );
                     break;
            }

            if( device.Contains( DeviceCathegory.Window ) )
            {
                if( Value == true )
                {
                    if( WindowIsOpen_ != null )
                    {
                        WindowIsOpen_( this, device );
                    }
                    if( PreviousWindowDeviceOpen != device )
                    {
                        if( OpenWindowsDictionary != null )
                        {
                            OpenWindowsDictionary.Add( device, Index );   // add entry all the time any new window is opened
                        }
                        // Mansardwindow is open - this is critical!
                        if( device.Contains( DeviceCathegory.Mansard )  )
                        {
                            AnyCriticalWindowIsOpen = true;
                        }
                        PreviousWindowDeviceOpen = device;
                    }
                }
                else
                {
                    if( WindowIsClosed_ != null )
                    {
                        WindowIsClosed_( this, device );
                    }
                    if( PreviousWindowDeviceClose != device )
                    {
                        if( (OpenWindowsDictionary != null) && (OpenWindowsDictionary.ContainsKey( device )) && (OpenWindowsDictionary.Count > 0) )
                        {
                            OpenWindowsDictionary.Remove( device );  // remove entry all the time any new window is closed as long as all windows are closed
                        }
                        PreviousWindowDeviceClose = device;
                    }
                    if( OpenWindowsDictionary.Count == 0 )
                    {
                        if( EAllWindowsAreClosed != null )
                        {
                            EAllWindowsAreClosed( this, EventArgs.Empty );
                        }
                    }
                    // no more "critical windows" are open
                    if( OpenWindowsDictionary.ContainsKey( DeviceCathegory.Mansard ) == false )
                    {
                        AnyCriticalWindowIsOpen = false;
                    }
                }
            }

            // message that rainsensor changed its state
            if( device.Contains( DeviceCathegory.Rainsensor ) )
            {
                if( EObserver != null )
                {
                    if( AnyCriticalWindowIsOpen )
                    {
                        if( Value ) // Rainsensor reports TRUE
                        {
                            EObserver( this, ObservingStates.eAlarm );
                        }
                        else
                        {
                            EObserver( this, ObservingStates.eObserve );
                        }
                    }
                    else
                    {
                        EObserver( this, ObservingStates.eObserve );
                    }
                }
            }
        }
        #endregion

        #region PRIVATE_METHODS
        void _PresenceTimer_Elapsed( object sender, ElapsedEventArgs e )
        {
        }

        private void ObservingStateMachine( string usercomando, ObservingStates state )
        {
                switch( state )
                {
                    case ObservingStates.eWaitForObserving:
                         if( SignalGenerator != null )
                         {
                             SignalGenerator.Stop();
                         }
                         break;

                    case ObservingStates.eObserve:
                         if( SignalGenerator != null )
                         {
                             SignalGenerator.Stop();
                         }
                         break;

                    case ObservingStates.eIndicating:
                         if( SignalGenerator != null )
                         {
                             SignalGenerator.Start();
                         }
                         break;

                    case ObservingStates.eWarning:
                         break;

                    case ObservingStates.eAlarm:
                         break;

                    default:
                         break;
                }
                _ActualState = state;
        }
        #endregion

        #region EVENTS

        #region WINDOW
        void WindowDoorsObserver_WindowIsClosed_( object sender, string windowName )
        {
        }

        void WindowDoorsObserver_WindowIsOpen_( object sender, string windowName )
        {
            switch( windowName )
            {
                case SleepingRoomDeviceNames.MansardWindowNorthLeft:
                case SleepingRoomDeviceNames.MansardWindowNorthRight:
                     if( EObserver != null )
                     {
                         EObserver( this, ObservingStates.eIndicating );
                     }
                     break;
                default:
                     break;
            }
        }

        void WindowDoorsObserver_EObserver( object sender, WindowDoorsObserver.ObservingStates state )
        {
             ObservingStateMachine( FormatString.Empty, state );
        }

        void WindowDoorsObserver_EAllWindowsAreClosed( object sender, EventArgs e )
        {
            AnyCriticalWindowIsOpen = false;
            EObserver( this, ObservingStates.eObserve );
        }
        #endregion

        void SignalGenerator_PWM_( object sender, UnivPWM.ePWMStatus pwmstatus )
        {
            switch( pwmstatus )
            {
                case UnivPWM.ePWMStatus.eInactive:
                     if( outputs_ != null )
                     {
                        outputs_[AnteRoomIOLightIndexNaming.AnteRoomNightLight] = false;
                     }
                     break;

                case UnivPWM.ePWMStatus.eIsOn:
                     if( outputs_ != null )
                     {
                         outputs_[AnteRoomIOLightIndexNaming.AnteRoomNightLight] = true;
                     }
                     break;

                case UnivPWM.ePWMStatus.eIsOff:
                     if( outputs_ != null )
                     {
                         outputs_[AnteRoomIOLightIndexNaming.AnteRoomNightLight] = false;
                     }
                     break;
            }
        }
        #endregion
 
        #region PROPERTIES
        public ObservingStates ActualState
        {
            get
            {
                return ( _ActualState );
            }
        }
        #endregion
    }
}
