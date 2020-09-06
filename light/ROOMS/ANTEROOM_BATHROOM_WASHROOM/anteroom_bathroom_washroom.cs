using System;
using Phidgets;
using Phidgets.Events;
using HomeAutomation.HardConfig_Collected;
using HomeAutomation.rooms;
using Scheduler;
using HomeAutomationProtocoll;

namespace HomeAutomation
{
    #region COMMON_CLASSES
    static class AnteRoomID
    {
        public enum EID
        {
            eAnteRoom   = 0,
            eWashRoom   = 1,
            eBathRoom   = 2,
            eRoofNorth  = 3
        };
    }
    #endregion

    #region LIGHTCONTROL
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    class LightControlAnteRoom : LightControl
    {
          #region DECLARATION
          bool      _turnedAutoOff;
          int       _startindex;
          const int LastIndexAnteRoomLights      = 1;
          const int NumberOfAnteRoomLightGroups  = 2;
          int       _lastindex;
          bool      turnedDeviceGroupManuallyOff = false;
          bool      resetOnAutomaticOff;

          enum AnteRoomStep
          {
              eAnteRoomLightsOnly,
              eRoofFloorLightsOnly,
              eAnteFrontAndBackLight,
              eAnteBackLight,
              eAnteAllLights,
              eAnteNightLight,  
          }

          AnteRoomStep AnteRoomStep_;
          AnteRoomStep LastAnteRoomStep_;
          #endregion

          #region CONSTRUCTOR
          public LightControlAnteRoom( double AllOnTime, double SingleOffTime, double AutomaticOffTime, int startindex, int lastindex, ref InterfaceKitDigitalOutputCollection outputs ) :
                 base( AllOnTime,  SingleOffTime, AutomaticOffTime, startindex,  lastindex, ref outputs )
          {
              Constructor( startindex );
          }

          public LightControlAnteRoom( double AllOnTime,
                                       double SingleOffTime,
                                       double AutomaticOffTime,
                                       double AllFinalOffTime,
                                       int startindex, int lastindex, ref InterfaceKitDigitalOutputCollection outputs ) :
              base( AllOnTime, AllFinalOffTime, SingleOffTime, AutomaticOffTime, startindex, lastindex, ref outputs )
          {
              Constructor( startindex );
          }
          #endregion

          #region PRIVATEMETHODS
          void Constructor( int index )
          {
              base.AutomaticOff_          += LightControlAnteRoom_AutomaticOff_;
              base.AllSelectedDevicesOff_ += LightControlAnteRoom_AllSelectedDevicesOff_;
              _startindex = index;
              AnteRoomStep_ = AnteRoomStep.eAnteRoomLightsOnly;
          }
          #endregion

          #region EVENTHANDLERS
          // when pushing button for a certain time                                                                     
          void LightControlAnteRoom_AllSelectedDevicesOff_( object sender, int firstdevice, int lastdevice )
          {
              if( firstdevice == AnteRoomIOLightIndices.indFirstLight && lastdevice == AnteRoomIOLightIndices.indLastLight )
              {
                  turnedDeviceGroupManuallyOff = true;
                  AnteRoomStep_ = LastAnteRoomStep_;
              }
          }

          // auto off when f.e presence detector reports no movement after a certain time
          void LightControlAnteRoom_AutomaticOff_( object sender )
          {
              AnteRoomStep_ = LastAnteRoomStep_;
              _turnedAutoOff = true;
              resetOnAutomaticOff = true;
              // info for toggling seleced "permanent device"
              for( int i = _startindex; i <= _lastindex; i++ )
              {
                   if( base.SelectedPermanentOnDevice[i] && outputs_[i] == true )
                   {
                       break;
                   }
              }
          }
          #endregion

          #region OVERWRITTENMETHODS
          override protected void StepLight( int startindex, ref int index, int _indexlastdevice )
          {
              if( _turnedAutoOff )
              {
                  index = 0;
                  _turnedAutoOff = false;
              }

              // preferred by a connected hardware button
              if( turnedDeviceGroupManuallyOff )
              {
                  index = 0;
                  turnedDeviceGroupManuallyOff = false;
              }

              switch( AnteRoomStep_ )
              {
                  case AnteRoomStep.eAnteRoomLightsOnly:
                       if( index > 0 )
                       {
                           index = 0;
                           outputs_[AnteRoomIOLightIndexNaming.AnteRoomMainLight] = false;
                           outputs_[AnteRoomIOLightIndexNaming.AnteRoomBackSide]  = false;
                           AnteRoomStep_ =  AnteRoomStep.eAnteNightLight;
                           break;
                       }
                       outputs_[AnteRoomIOLightIndexNaming.AnteRoomMainLight] = true;
                       outputs_[AnteRoomIOLightIndexNaming.AnteRoomBackSide]  = true;
                       index++;
                       break;

                  case AnteRoomStep.eAnteNightLight:
                       if( resetOnAutomaticOff )
                       {
                           index = 0;
                           AnteRoomStep_ =  AnteRoomStep.eAnteAllLights;
                           break;
                       }

                       if( index > 0 )
                       {
                           index = 0;
                           outputs_[AnteRoomIOLightIndexNaming.AnteRoomNightLight]  = false;
                           AnteRoomStep_ =  AnteRoomStep.eAnteAllLights;
                           break;
                       }
                       outputs_[AnteRoomIOLightIndexNaming.AnteRoomNightLight]  = true;
                       index++;
                       break;


                  case AnteRoomStep.eAnteAllLights:
                       if( resetOnAutomaticOff )
                       {
                           index = 0;
                           break;
                       }
                       if( index > 0 )
                       {
                           index = 0;
                           outputs_[AnteRoomIOLightIndexNaming.AnteRoomNightLight]                        = false;
                           outputs_[AnteRoomIOLightIndexNaming.AnteRoomMainLight]                         = false;
                           outputs_[AnteRoomIOLightIndexNaming.AnteRoomBackSide]                          = false;
                           outputs_[AnteRoomIOLightIndexNaming.AnteRoomRoofBackSideFloorSpotGroupMiddle1] = false;
                           outputs_[AnteRoomIOLightIndexNaming.AnteRoomRoofBackSideFloorSpotGroupMiddle2] = false;
                           outputs_[AnteRoomIOLightIndexNaming.AnteRoomNightLight]                        = false;
                           outputs_[AnteRoomIOLightIndexNaming.AnteRoomNightLight]                        = false;
                           AnteRoomStep_ =  AnteRoomStep.eRoofFloorLightsOnly;
                           break;
                       }
                           outputs_[AnteRoomIOLightIndexNaming.AnteRoomNightLight]                        = true;
                           outputs_[AnteRoomIOLightIndexNaming.AnteRoomMainLight]                         = true;
                           outputs_[AnteRoomIOLightIndexNaming.AnteRoomBackSide]                          = true;
                           outputs_[AnteRoomIOLightIndexNaming.AnteRoomRoofBackSideFloorSpotGroupMiddle1] = true;
                           outputs_[AnteRoomIOLightIndexNaming.AnteRoomRoofBackSideFloorSpotGroupMiddle2] = true;
                           outputs_[AnteRoomIOLightIndexNaming.AnteRoomNightLight]                        = true;
                           outputs_[AnteRoomIOLightIndexNaming.AnteRoomNightLight]                        = true;
                           index++;
                      break;

                  case AnteRoomStep.eRoofFloorLightsOnly:
                       if( resetOnAutomaticOff )
                       {
                           index=0;
                           break;
                       }

                       if( index > 0 )
                       {
                           index = 0;
                           outputs_[AnteRoomIOLightIndexNaming.AnteRoomRoofBackSideFloorSpotGroupMiddle1]  = false;
                           outputs_[AnteRoomIOLightIndexNaming.AnteRoomRoofBackSideFloorSpotGroupMiddle2]  = false;
                           AnteRoomStep_ =  AnteRoomStep.eAnteRoomLightsOnly;
                           break;
                       }
                           outputs_[AnteRoomIOLightIndexNaming.AnteRoomRoofBackSideFloorSpotGroupMiddle1]  = true;
                           outputs_[AnteRoomIOLightIndexNaming.AnteRoomRoofBackSideFloorSpotGroupMiddle2]  = true;
                       index++;
                       break;
              }
              LastAnteRoomStep_ = AnteRoomStep_;
              _lastindex = index;
              resetOnAutomaticOff = false;
          }
          #endregion

          #region PUBLICMETHODS
          public void SwitchDeviceViaPresenceDetector( InputChangeEventArgs e, double delayTimeOff, int deviceindex )
          {
            bool enable = false;

            if( turnedDeviceGroupManuallyOff )
            {
                enable = true;
            }

            if( e.Value == false )
            {
                //convention - f.e all off is done - activate control via presence detector, further control can be dactivated via GUI
                base.TurnOnWithDelayedOffSingleLight( e, enable, delayTimeOff, deviceindex );
            }
          }
          #endregion
    }

    class LightControlAnteRoomNG : LightControlNG
    {
        #region DECLARATION  
        const int LastIndexAnteRoomLights      = 1;
        const int NumberOfAnteRoomLightGroups  = 2;
        bool      _turnedAutoOff;
        int       _startindex;
        int       _lastindex;
        bool      _nextStepActivated;
        bool      turnedDeviceGroupManuallyOff = false;
        bool      resetOnAutomaticOff;
        bool      DisableNightLightViaPresenceDetector;

        public enum AnteRoomStep
        {
            eAnteRoomLightsOnly,
            eRoofFloorLightsOnly,
            eAnteAllLights,
            eAnteNightLight,
            eFloorLight,
            eBackLight
        }

        AnteRoomStep AnteRoomStep_;
        AnteRoomStep LastAnteRoomStep_;
        #endregion

        #region CONSTRUCTOR
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public LightControlAnteRoomNG( double AllOnTime,
                                       double SingleOffTime, 
                                       double AutomaticOffTime, 
                                       int startindex, 
                                       int lastindex, 
                                       ref InterfaceKitDigitalOutputCollection outputs ) 
                                       :   base( AllOnTime, SingleOffTime, AutomaticOffTime, startindex, lastindex, ref outputs )
        {
            Constructor( startindex );
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public LightControlAnteRoomNG( double AllOnTime,
                                       double SingleOffTime,
                                       double AutomaticOffTime,
                                       double AllFinalOffTime,
                                       int startindex, 
                                       int lastindex, 
                                       ref InterfaceKitDigitalOutputCollection outputs )
                                       :   base( AllOnTime, AllFinalOffTime, SingleOffTime, AutomaticOffTime, startindex, lastindex, ref outputs )
        {
            Constructor( startindex );
        }



        public LightControlAnteRoomNG( double AllOnTime,
                                       double AllOutputsOffTime, 
                                       double SingleOffTime,
                                       double AutomaticOffTime,
                                       double AllFinalOffTime,
                                       int startindex,
                                       int lastindex,
                                       ref InterfaceKitDigitalOutputCollection outputs )
            : base( AllOnTime, AllOutputsOffTime, SingleOffTime, AutomaticOffTime, AllFinalOffTime, startindex, lastindex, ref outputs )
        {
            Constructor( startindex );
        }

        
        #endregion

        #region PRIVATEMETHODS
        void Constructor( int index )
        {
            base.AutomaticOff_          += LightControlAnteRoom_AutomaticOff_;
            base.AllSelectedDevicesOff_ += LightControlAnteRoom_AllSelectedDevicesOff_;
            base.SingleOff_             += LightControlAnteRoomNG_SingleOff_;
            base.AllOn_                 += LightControlAnteRoomNG_AllOn_;
            _startindex = index;
            AnteRoomStep_ = AnteRoomStep.eFloorLight;
        }


        void SwitchDeviceViaPresenceDetector( bool command, double delayTimeOff, int deviceindex )
        {
            bool enable = true;

            if( DisableNightLightViaPresenceDetector )
            {
                enable = false;
            }

            if( command == false )
            {
                //convention - f.e all off is done - activate control via presence detector, further control can be dactivated via GUI
                base.TurnOnWithDelayedOffSingleLight( command, enable, delayTimeOff, deviceindex );
            }
        }

        #endregion

        #region EVENTHANDLERS
        private void LightControlAnteRoomNG_AllOn_( object sender )
        {
            _nextStepActivated = false;
        }

        private void LightControlAnteRoomNG_SingleOff_( object sender )
        {
            _LightControlSingleOffDone = false;
            _nextStepActivated = true;
        }

        // when pushing button for a certain time                                                                     
        void LightControlAnteRoom_AllSelectedDevicesOff_( object sender, int firstdevice, int lastdevice )
        {
            if( (firstdevice == AnteRoomIOLightIndices.indFirstLight) && (lastdevice == AnteRoomIOLightIndices.indLastLight) )
            {
                turnedDeviceGroupManuallyOff = true;
                AnteRoomStep_ = LastAnteRoomStep_;
            }
        }

        // auto off when f.e presence detector reports no movement after a certain time
        void LightControlAnteRoom_AutomaticOff_( object sender )
        {
            AnteRoomStep_       = LastAnteRoomStep_;
            _turnedAutoOff      = true;
            resetOnAutomaticOff = true;
            // info for toggling seleced "permanent device"
            for( int i = _startindex; i <= _lastindex; i++ )
            {
                if( base.SelectedPermanentOnDevice[i] && outputs_[i] == true )
                {
                    break;
                }
            }
        }
        #endregion

        #region PROTECTED_METHODS_OVERRIDE
        override protected void StepLight( int startindex, ref int index, int _indexlastdevice )
        {
            DisableNightLightViaPresenceDetector = false;
            if( _turnedAutoOff )
            {
                index = 0;
                _turnedAutoOff = false;
            }

            // preferred by a connected hardware button
            if( turnedDeviceGroupManuallyOff )
            {
                index = 0;
                turnedDeviceGroupManuallyOff = false;
            }

            switch( AnteRoomStep_ )
            {
                case AnteRoomStep.eFloorLight:
                     if( _nextStepActivated )
                     {
                         base._DigitalOutput[AnteRoomIOLightIndexNaming.AnteRoomMainLight] = false;
                         AnteRoomStep_ = AnteRoomStep.eBackLight;
                         index = 0;
                         _nextStepActivated = false;
                         break;
                     }
                     if( index > 0 )
                     {
                         index = 0;
                         base._DigitalOutput[AnteRoomIOLightIndexNaming.AnteRoomMainLight] = false;
                         break;
                     }
                     DisableNightLightViaPresenceDetector = true;
                     base._DigitalOutput[AnteRoomIOLightIndexNaming.AnteRoomMainLight] = true;
                     index++;
                     break;
  
                case AnteRoomStep.eBackLight:
                     if( _nextStepActivated )
                     {
                         AnteRoomStep_ = AnteRoomStep.eAnteRoomLightsOnly;
                         _nextStepActivated = false;
                         index = 0;
                         break;
                     }
                     if( index > 0 )
                     {
                         index = 0;
                         base._DigitalOutput[AnteRoomIOLightIndexNaming.AnteRoomBackSide]  = false;
                         break;
                     }
                     DisableNightLightViaPresenceDetector = true;
                     base._DigitalOutput[AnteRoomIOLightIndexNaming.AnteRoomBackSide]  = true;
                     index++;
                     break;

                case AnteRoomStep.eAnteRoomLightsOnly:
                     if( _nextStepActivated )
                     {
                        base._DigitalOutput[AnteRoomIOLightIndexNaming.AnteRoomMainLight] = false;
                        base._DigitalOutput[AnteRoomIOLightIndexNaming.AnteRoomBackSide]  = false;
                        AnteRoomStep_ = AnteRoomStep.eAnteNightLight;
                        _nextStepActivated = false;
                        break;
                     }
                     if( index > 0 )
                     {
                         index = 0;
                         base._DigitalOutput[AnteRoomIOLightIndexNaming.AnteRoomMainLight] = false;
                         base._DigitalOutput[AnteRoomIOLightIndexNaming.AnteRoomBackSide]  = false;
                         break;
                     }
                     DisableNightLightViaPresenceDetector = true;
                     base._DigitalOutput[AnteRoomIOLightIndexNaming.AnteRoomMainLight] = true;
                     base._DigitalOutput[AnteRoomIOLightIndexNaming.AnteRoomBackSide]  = true;
                     index++;
                     break;

                case AnteRoomStep.eAnteNightLight:
                     if( _nextStepActivated )
                     {
                         base._DigitalOutput[AnteRoomIOLightIndexNaming.AnteRoomNightLight] = false;
                         AnteRoomStep_ = AnteRoomStep.eAnteAllLights;
                         DisableNightLightViaPresenceDetector = false;
                         _nextStepActivated = false;
                         index = 0;
                         break;
                     }

                     if( resetOnAutomaticOff )
                     {
                         index = 0;
                         AnteRoomStep_ =  AnteRoomStep.eAnteAllLights;
                         break;
                     }

                     if( index > 0 )
                     {
                         index = 0;
                         base._DigitalOutput[AnteRoomIOLightIndexNaming.AnteRoomNightLight]  = false;
                         break;
                     }
                     base._DigitalOutput[AnteRoomIOLightIndexNaming.AnteRoomNightLight]  = true;
                     DisableNightLightViaPresenceDetector = true;
                     index++;
                     break;


               case AnteRoomStep.eAnteAllLights:
                    if( _nextStepActivated )
                    {
                        base._DigitalOutput[AnteRoomIOLightIndexNaming.AnteRoomNightLight] = false;
                        base._DigitalOutput[AnteRoomIOLightIndexNaming.AnteRoomMainLight] = false;
                        base._DigitalOutput[AnteRoomIOLightIndexNaming.AnteRoomBackSide] = false;
                        base._DigitalOutput[AnteRoomIOLightIndexNaming.AnteRoomRoofBackSideFloorSpotGroupMiddle1] = false;
                        base._DigitalOutput[AnteRoomIOLightIndexNaming.AnteRoomRoofBackSideFloorSpotGroupMiddle2] = false;
                        AnteRoomStep_ = AnteRoomStep.eRoofFloorLightsOnly;
                        _nextStepActivated = false;
                        index = 0;
                        break;
                    }


                    if( resetOnAutomaticOff )
                    {
                        index = 0;
                        break;
                    }
                    if( index > 0 )
                    {
                        index = 0;
                        base._DigitalOutput[AnteRoomIOLightIndexNaming.AnteRoomNightLight]                        = false;
                        base._DigitalOutput[AnteRoomIOLightIndexNaming.AnteRoomMainLight]                         = false;
                        base._DigitalOutput[AnteRoomIOLightIndexNaming.AnteRoomBackSide]                          = false;
                        base._DigitalOutput[AnteRoomIOLightIndexNaming.AnteRoomRoofBackSideFloorSpotGroupMiddle1] = false;
                        base._DigitalOutput[AnteRoomIOLightIndexNaming.AnteRoomRoofBackSideFloorSpotGroupMiddle2] = false;
                        break;
                    }
                    base._DigitalOutput[AnteRoomIOLightIndexNaming.AnteRoomNightLight]                        = true;
                    base._DigitalOutput[AnteRoomIOLightIndexNaming.AnteRoomMainLight]                         = true;
                    base._DigitalOutput[AnteRoomIOLightIndexNaming.AnteRoomBackSide]                          = true;
                    base._DigitalOutput[AnteRoomIOLightIndexNaming.AnteRoomRoofBackSideFloorSpotGroupMiddle1] = true;
                    base._DigitalOutput[AnteRoomIOLightIndexNaming.AnteRoomRoofBackSideFloorSpotGroupMiddle2] = true;
                    DisableNightLightViaPresenceDetector = true;
                    index++;
                    break;

               case AnteRoomStep.eRoofFloorLightsOnly:
                    if( resetOnAutomaticOff )
                    {
                        index=0;
                        break;
                    }

                    if( index > 0 )
                    {
                        index = 0;
                        base._DigitalOutput[AnteRoomIOLightIndexNaming.AnteRoomRoofBackSideFloorSpotGroupMiddle1]  = false;
                        base._DigitalOutput[AnteRoomIOLightIndexNaming.AnteRoomRoofBackSideFloorSpotGroupMiddle2]  = false;
                        AnteRoomStep_ =  AnteRoomStep.eFloorLight;
                        break;
                    }
                    base._DigitalOutput[AnteRoomIOLightIndexNaming.AnteRoomRoofBackSideFloorSpotGroupMiddle1]  = true;
                    base._DigitalOutput[AnteRoomIOLightIndexNaming.AnteRoomRoofBackSideFloorSpotGroupMiddle2]  = true;
                    index++;
                    break;
            }
            LastAnteRoomStep_ = AnteRoomStep_;
            _lastindex = index;
            resetOnAutomaticOff = false;

            DoUpDateIOUntilLastIndex( base._DigitalOutput, _indexlastdevice );
        }
        #endregion

        #region PUBLICMETHODS
        public void SwitchDeviceViaPresenceDetector( InputChangeEventArgs e, double delayTimeOff, int deviceindex )
        {
            SwitchDeviceViaPresenceDetector( e.Value, delayTimeOff, deviceindex );
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public void ResetLightComander()
        {
            base.TurnAllLightsOff( AnteRoomIOLightIndexNaming.AnteRoomMainLight, AnteRoomIOLightIndexNaming.AnteRoomNightLight );
            base.ResetLightControl();
            LastAnteRoomStep_ = AnteRoomStep_ = AnteRoomStep.eAnteRoomLightsOnly;
        }
        #endregion

        #region PROPERTIES
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public AnteRoomStep ActualAnteRoomStep
        {
            get
            {
                return ( AnteRoomStep_ );
            }
        }
        #endregion
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    class LightControlBathRoom : LightControl
    {
        #region DECLARATION
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        int _startindex;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        int _lastindex;
        bool turnedDeviceGroupManuallyOff = false;
        bool turnedAutoOff = false;

        enum BathRoomStep
        {
            eMiddleLight,
            eRGBBar
        }

        BathRoomStep BathRoomStep_;
        BathRoomStep LastBathRoomStep_;
        #endregion

        #region CONSTRUCTOR
        public LightControlBathRoom( double AllOnTime, double SingleOffTime, double AutomaticOffTime, int startindex, int lastindex, ref InterfaceKitDigitalOutputCollection outputs ) :
                base( AllOnTime,  SingleOffTime, AutomaticOffTime, startindex,  lastindex, ref outputs )
        {
            _startindex = startindex;
            _lastindex = lastindex;
            BathRoomStep_ = BathRoomStep.eMiddleLight;
            base.AllSelectedDevicesOff_ +=LightControlBathRoom_AllSelectedDevicesOff_;
            base.AutomaticOff_ += LightControlBathRoom_AutomaticOff_;
        }
        #endregion

        #region EVENTHANDLERS
        void LightControlBathRoom_AutomaticOff_( object sender )
        {
            BathRoomStep_ = LastBathRoomStep_;
            turnedAutoOff = true;
        }

        void LightControlBathRoom_AllSelectedDevicesOff_( object sender, int firstdevice, int lastdevice )
        {
            if( firstdevice == BathRoomIOLightIndices.indFirstBathRoom && lastdevice == BathRoomIOLightIndices.indLastBathRoom )
            {
                BathRoomStep_ = LastBathRoomStep_;
                turnedDeviceGroupManuallyOff = true;
            }
        }
        #endregion

        #region OVERWRITTENMETHODS
        override protected void StepLight( int startindex, ref int index, int _indexlastdevice )
        {
            if( turnedAutoOff )
            {
                index = 0;
                turnedAutoOff = false;
            }

            // preferred by a connected hardware button
            if( turnedDeviceGroupManuallyOff )
            {
                index = 0;
                turnedDeviceGroupManuallyOff = false;
            }

            switch( BathRoomStep_ )
            {
                case BathRoomStep.eMiddleLight:
                     if( index > 0 )
                     {
                         index = 0;
                         outputs_[BathRoomIOLightIndexNaming.MiddleLight]  = false;
                         BathRoomStep_ = BathRoomStep.eRGBBar;
                         break;
                     }
                     outputs_[BathRoomIOLightIndexNaming.MiddleLight]  = true;
                     index++;
                     break;

                case BathRoomStep.eRGBBar:
                     if( index > 0 )
                     {
                         index = 0;
                         outputs_[BathRoomIOLightIndexNaming.RBG_PanelOverBath]  = false;
                         BathRoomStep_ = BathRoomStep.eMiddleLight;
                         break;
                     }
                     outputs_[BathRoomIOLightIndexNaming.RBG_PanelOverBath]  = true;
                     index++;
                     break;
            }
            LastBathRoomStep_ = BathRoomStep_;
        }
        #endregion
    }
    #endregion

    #region ANTEROOM_CENTRAL_CONTROL
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    class AnteRoom : CommonRoom
    {
        #region DECLARATIONES
        LightControlAnteRoomNG               LightAnteRoom;
        LightControl                         LightWashRoom;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        LightControl LightBathRoom;
        HeaterElement                        HeatersBathRoom;

        bool[]                               _DigitalInputState;
        bool[]                               _DigitalOutputState;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        Home_scheduler scheduler         = new Home_scheduler();
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        FeedData PrevSchedulerData = new FeedData();
        #endregion

        #region CONSTRUCTOR 
        void InitRooms()
        {

            LightAnteRoom = new LightControlAnteRoomNG(   ParametersLightControlAnteRoom.TimeDemandForAllOn,
                                                          ParametersLightControlAnteRoom.TimeDemandAllOutputsOff,
                                                          ParametersLightControl.TimeDemandForSingleOff,
                                                          ParametersLightControlAnteRoom.TimeDemandForAutomaticOffAnteRoom,
                                                          ParametersLightControlAnteRoom.TimeDemandForAllOffFinal,
                                                          AnteRoomIOLightIndices.indFirstLight,
                                                          AnteRoomIOLightIndices.indLastLight,
                                                          ref base.outputs );

            LightWashRoom  = new           LightControl( ParametersLightControl.TimeDemandForAllOn,
                                                         ParametersLightControl.TimeDemandForSingleOff,
                                                         ParametersLightControlWashRoom.TimeDemandForAutomaticOffWashRoom,
                                                         AnteRoomIOLightIndices.indFirstWashroom,
                                                         AnteRoomIOLightIndices.indFirstWashroom,
                                                         ref base.outputs );

            //LightBathRoom = new LightControlBathRoom( ParametersLightControl.TimeDemandForAllOn,
            //                                              ParametersLightControl.TimeDemandForSingleOff,
            //                                              ParametersLightControlBathRoom.TimeDemandForAutomaticOffBath,
            //                                              BathRoomIOLightIndices.indFirstBathRoom,
            //                                              BathRoomIOLightIndices.indLastBathRoom,
            //                                              ref base.outputs );

            //HeatersBathRoom = new HeaterElement( ParametersHeaterControl.TimeDemandForHeatersOnOff,
            //                                              ParametersHeaterControlBathRoom.TimeDemandForHeaterAutomaticOff,
            //                                              BathRoomIODeviceIndices.indDigitalOutputBathRoomFirstHeater,
            //                                              BathRoomIODeviceIndices.indDigitalOutputBathRoomLastHeater,
            //                                              ref base.outputs );

            //LightAnteRoom.IsPrimaryIOCardAttached = base.Attached;
            //LightAnteRoom.StartAliveSignal( );
        }

        #region ANTEROOM_WITHOUT_COMMUNICATION
        public AnteRoom ( ) : base( )
        {
            if( base.Attached )
            {
                InitRooms();
            }
        }
        #endregion

        #region ANTEROOM_WITH_COMMUNICATION
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Convert.ToInt16(System.String)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "softwareversion")]
        public AnteRoom( string IpAdressServer, string PortServer, string softwareversion )
            : base()
        {
            _GivenClientName    = InfoOperationMode.ANTEROOM;
            _IpAdressServer     = IpAdressServer;
            _PortNumberServer   = Convert.ToInt16( PortServer );
            _DigitalInputState  = new bool[GeneralConstants.NumberOfInputsIOCard];
            _DigitalOutputState = new bool[GeneralConstants.NumberOfOutputsIOCard];

            if( base.Attached )
            {
                InitRooms();
            }

            #region INITIATE_COMMUNICATION
                //BasicClientCommunicator_ = new BasicClientComumnicator( _GivenClientName,
                //                                                        _IpAdressServer,
                //                                                         PortServer,
                //                                                         ref base.outputs, // control directly digital outputs of primer - server can control this outputs
                //                                                         ref HADictionaries.DeviceDictionaryAnteRoomdigitalOut,                        
                //                                                         ref HADictionaries.DeviceDictionaryAnteroomdigitalIn, softwareversion );      
                //// establish client
                //BasicClientCommunicator_.Room = _GivenClientName;
                //BasicClientCommunicator_.EFeedScheduler += BasicClientCommunicator__EFeedScheduler;
                //scheduler.EvTriggered += Scheduler_EvTriggered;

                //BasicClientCommunicator_.Primer1IsAttached = Attached;

                //if( !Attached )
                //{
                //    BasicClientCommunicator_.SendInfoToServer( InfoString.InfoNoIO );
                //}

                //try
                //{
                //    UDPReceive_ = new UdpReceive( IPConfiguration.Port.PORT_LIGHT_CONTROL_COMMON );
                //    UDPReceive_.EDataReceived += UDPReceive__EDataReceived;
                //}
                //catch( Exception ex )
                //{
                //    Console.WriteLine( TimeUtil.GetTimestamp() + " " + ex.Message );
                //    Services.TraceMessage_( InfoString.FailedToEstablishUDPReceive );
                //}
            #endregion
        }
        #endregion
        #endregion

        #region SCHEDULER
 

        #endregion

        #region PROPERTIES_IO_INTERFACE
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string GivenClientName
        {
            get
            {
                return _GivenClientName;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        string _IpAdressServer;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string IpAdressServer
        {
            set
            {
                _IpAdressServer = value;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        string _PortServer;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        int _PortNumberServer;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Convert.ToInt16(System.String)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string PortServer
        {
            set
            {
                _PortServer = value;
                _PortNumberServer = Convert.ToInt16( value );
            }
        }
        #endregion

        #region PUBLIC_METHODS
        public void StopAliveSignal()
        {
            LightAnteRoom.StopAliveSignal();
        }
        #endregion

        #region PROTECTED_METHODS
        protected override void TurnNextLightOn_ ( InputChangeEventArgs e )
        {
            {

                switch( e.Index ) // the index is the assigned input number
                {
                        // first relase start one light, next relase start neighbor light, turn previous light off
                        // press button longer than ( f.e. 1.. seconds ) - all lights on
                        // press button longer than ( f.e. 2.. seconds ) - all lights off
                        // TODO press button longer than ( f.e. 3.. seconds ) - broadcast all lights off, just default lights on
                        case AnteRoomIOAssignment.indAnteRoomMainButton:
                             LightAnteRoom?.MakeStep( e );
                             LightAnteRoom?.FinalAutomaticOff( e );
                             break;

                        case AnteRoomIOAssignment.indWashRoomMainButton:
                             LightWashRoom?.MakeStep( e );
                             LightWashRoom?.AutomaticOff( e );
                             break;

                        case AnteRoomIOAssignment.indBathRoomMainButton:
                             // operate light only when there is no demand of manual heater control
                             //if( !HeatersBathRoom.WasHeaterSwitched( ) )
                             //{
                             //    LightBathRoom.MakeStep( e );
                             //}
                             //else
                             //{
                             //    LightBathRoom.StopAllOnTimer( );
                             //    LightBathRoom.ResetLightControl( );
                             //}
                             //LightBathRoom.AutomaticOff( e );
                             break;

                        case AnteRoomIOAssignment.indAnteRoomPresenceDetector:
                             LightAnteRoom.AutomaticOffSelect( e,
                                                                 AnteRoomIOLightIndexNaming.AnteRoomMainLight,
                                                                 AnteRoomIOLightIndexNaming.AnteRoomBackSide );
                             LightAnteRoom.SwitchDeviceViaPresenceDetector( e, ParametersLightControlAnteRoom.TimeDemandAutomaticOffViaPresenceDetector,
                                                                    AnteRoomIOLightIndexNaming.AnteRoomNightLight );
                             break;

                        default:
                             break;
                }
            }
        }
        #endregion

        #region PRIVATE_METHODS
        void HeaterControl( InputChangeEventArgs e )
        {
            switch( e.Index ) // the index is the assigned input number
            {
                case AnteRoomIOAssignment.indBathRoomMainButton:
                     HeatersBathRoom?.HeaterOn( e );
                     break;
            }
        }
        #endregion

        #region IOEVENTHANDLERS
        protected override void BuildingSection_InputChange ( object sender, InputChangeEventArgs e )
        {
            CheckFireAlert( e );

            CheckOpenWindow( e );

            TurnNextLightOn_( e );

            HeaterControl( e );
        }
        #endregion

        #region REMOTE_CONTROLLED_UDP
        decimal receivedTransactionCounter         = 0;
        decimal PreviousreceivedTransactionCounter = 0;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Convert.ToDecimal(System.String)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Convert.ToInt16(System.String)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Convert.ToBoolean(System.String)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        void UDPReceive__EDataReceived( string e )
        {
            string[] DatagrammSplitted = e.Split( ComandoString.Telegram.Seperator );

            receivedTransactionCounter = Convert.ToDecimal( DatagrammSplitted[ComandoString.Telegram.IndexTransactionCounter] );

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
            int ReceivedIndex = Convert.ToInt16( DatagrammSplitted[ComandoString.Telegram.IndexDigitalInputs] );
            // received acutal fired value of digital input
            bool ReceivedValue = Convert.ToBoolean( DatagrammSplitted[ComandoString.Telegram.IndexValueDigitalInputs] );

            switch( ReceivedIndex )
            {
                case LivingRoomWestIOAssignment.LivWestDigInputs.indDigitalInputButtonMainUpLeft:
                    // turn light ON / OFF when releasing the button 
                    if( ReceivedValue == false )
                    {
                        //Outside.AutomaticOff( ReceivedValue );
                        //Outside.ToggleSingleLight( CenterOutsideIODevices.indDigitalOutputLightsOutside );
                    }
                    break;
            }

        }
        #endregion
    }
    #endregion
}