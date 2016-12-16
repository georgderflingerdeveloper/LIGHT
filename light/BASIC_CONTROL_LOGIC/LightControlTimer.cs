using System;
using System.Collections.Generic;
using System.Timers;
using HomeAutomation.HardConfig;


namespace BASIC_CONTROL_LOGIC
{
    // this timer class is also used for heater control - that can sometimes be confusing - TODO refactor or try to make it better understandable
    class LightControlTimer_
    {
        #region DECLARATIONS
        Timer AutomaticTurnSelectedOff;                                     // used for a Light Grpoup - f.e. 1-4,   3-7, ..... aso.... 
        Timer DelayTurnAllOn                            = new Timer();      // idea ist to push f.e a button for a certain time, after that all lights go on
        Timer SingleTurnOff                             = new Timer();                 // "single" group of devices can be turned off manually
        List<Timer> SelectedAutomaticOffList            = new List<Timer>();           // turn a single selected device automatic off
        Dictionary<int,int> IndexDic                    = new Dictionary<int, int>();  // contains digital output index used for timer elapsed event handler
        int PrevSelectionIndex                          = 0;
        int IndTimList                                  = 0;
        bool InitSingleAutomaticTimerDone               = false;

        public delegate void  AllOn( object sender );
        public event          AllOn AllOn_;
        public delegate void  SingleOff( object sender );     // "single" group of devices can be turned off manually
        public event          SingleOff SingleOff_;
        public delegate void  AutomaticOff( object sender );  // selected devices turn outomatic off with optional preselecting
        public event          AutomaticOff AutomaticOff_;
        public delegate void  SingleDelayedIndexedAutomaticOff( object sender, int index );   // one device can be turned off 
        public event          SingleDelayedIndexedAutomaticOff ESingleDelayedIndexedOff_;
        #endregion

        #region CONSTRUCTOR
        public LightControlTimer_( ) { }

        public LightControlTimer_( double AllOnTime, double SingleOffTime )
        {
            DelayTurnAllOn.Elapsed          += DelayAllOn_Elapsed;
            SingleTurnOff.Elapsed           += SingleLightOff_Elapsed;
            DelayTurnAllOn.Interval          = AllOnTime;
            SingleTurnOff.Interval           = SingleOffTime;
        }

        public LightControlTimer_( double AllOnTime, double SingleOffTime, double AutomaticOffTime )
        {
            AutomaticTurnSelectedOff = new Timer( );
            DelayTurnAllOn.Elapsed           += DelayAllOn_Elapsed;
            SingleTurnOff.Elapsed            += SingleLightOff_Elapsed;
            AutomaticTurnSelectedOff.Elapsed += AutomaticTurnSelectedOff_Elapsed;

            if( AllOnTime > 0 )
            {
                DelayTurnAllOn.Interval = AllOnTime;
            }

            if( SingleOffTime > 0 )
            {
                SingleTurnOff.Interval = SingleOffTime;
            }

            if( AutomaticOffTime > 0 )
            {
                AutomaticTurnSelectedOff.Interval = AutomaticOffTime;
            }
        }
        #endregion

        #region PUBLIC_METHODS
        public void ReconfigAutomaticOffTimer( double time )
        {
            if( time > 0 )
            {
                AutomaticTurnSelectedOff.Stop( );
                AutomaticTurnSelectedOff.Interval = time;
                AutomaticTurnSelectedOff.Start( );
            }
        }

        public void StartAllOnTimer( )
        {
            DelayTurnAllOn.Start( );
        }

        public void StopAllOnTimer( )
        {
            DelayTurnAllOn.Stop( );
        }

        public void StartAutomaticOfftimer( )
        {
            AutomaticTurnSelectedOff.Start( );
        }

        public void StopAutomaticOfftimer( )
        {
            AutomaticTurnSelectedOff.Stop( );
        }

        public void RestartAutomaticOfftimer( )
        {
            AutomaticTurnSelectedOff.Stop( );
            AutomaticTurnSelectedOff.Start( );
        }

        public void StartAllTimers( )
        {
            DelayTurnAllOn.Start( );
            SingleTurnOff.Start( );
        }

        public void StopAllTimers( )
        {
            DelayTurnAllOn.Stop( );
            SingleTurnOff.Stop( );
            AutomaticTurnSelectedOff.Stop( );
        }

        public void StartSingleAutomaticOffTimer( int index, double timeOff )
        {
            int listindex = 0;
            if( index < 0 )
            {
                throw new Exception( EXCEPTIONMessages.IndexMustNotBeNegative );
            }
            // every change there is a new entry in the list and the index dictionary
            if( ( index != PrevSelectionIndex ) || !InitSingleAutomaticTimerDone )
            {
                SelectedAutomaticOffList.Add( new Timer( timeOff ) );
                // from the list, store the index into a dictionary
                IndexDic.Add( index, IndTimList );
                IndTimList++;
                PrevSelectionIndex = index;
                InitSingleAutomaticTimerDone = true;
            }

            if( (IndTimList <= SelectedAutomaticOffList.Count) && (IndTimList > 0) )
            {
                // now get the proper index for the list out of the dictionary
                IndexDic.TryGetValue( index, out listindex );

                if( listindex >= 0 && listindex < SelectedAutomaticOffList.Count )
                {
                    SelectedAutomaticOffList[listindex].Stop( );
                    SelectedAutomaticOffList[listindex].Elapsed += ( sender, args ) => LightControlTimer__Elapsed( sender, index );
                    SelectedAutomaticOffList[listindex].Interval = timeOff;
                    SelectedAutomaticOffList[listindex].Enabled = true;
                    SelectedAutomaticOffList[listindex].Start( );
                }
            }
        }

        public void StopSingleAutomaticOffTimer( int Index )
        {
            if( Index < 0 )
            {
                throw new Exception( EXCEPTIONMessages.IndexMustNotBeNegative );
            }
            if( Index < SelectedAutomaticOffList.Count )
            {
                SelectedAutomaticOffList[Index]?.Stop( );
            }
        }

        void LightControlTimer__Elapsed( object sender, int ioindex )
        {
            int listindex = 0;
            ESingleDelayedIndexedOff_?.Invoke( sender, ioindex );
            IndexDic.TryGetValue( ioindex, out listindex );
            if( ( listindex >= 0 ) && ( listindex < SelectedAutomaticOffList.Count ) )
            {
                SelectedAutomaticOffList[listindex].Stop( );
            }
        }
        #endregion

        #region PRIVATEMETHODS
        void StartSingleLightOffTimer( )
        {
            SingleTurnOff.Start( );
        }

        void StopSingleLightOffTimer( )
        {
            SingleTurnOff.Stop( );
        }
        #endregion

        #region EVENTHANDLERS
        void SingleLightOff_Elapsed( object sender, ElapsedEventArgs e )
        {
            SingleTurnOff.Stop( );
            SingleOff_?.Invoke( this );
        }

        void DelayAllOn_Elapsed( object sender, ElapsedEventArgs e )
        {
            DelayTurnAllOn.Stop( );
            AllOn_?.Invoke( this );
        }

        void AutomaticTurnSelectedOff_Elapsed( object sender, ElapsedEventArgs e )
        {
             AutomaticOff_?.Invoke( this );
        }
        #endregion
    }
}
