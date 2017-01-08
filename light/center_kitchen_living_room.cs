﻿using System;
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

namespace HomeAutomation
{
    class LightControlKitchen : LightControl
    {
        #region DECLARATIONS
        enum KitchenStep
        {
            eNext,
            eFrontLights,
            eAll,
            eNextOn,
            eSlots,
            eTurnedOff,
            eCabinetFrontLights
        }  

        const int LastIndexFrontLights = 3;
        const int NumberOfFrontLights  = 3;
        int       _index;
        int       _lastindex;
        int       _startindex;
        bool      _turnedAutoOff;
        bool      _SingleOffDone;
        bool      reset;
        int       IndexFS = 0;
        bool      _AnyExternalDeviceOn;
        bool      _AnyExternalDeviceOff;
        bool      ToggleLightGroups = false;

        KitchenStep KitchenStep_;
        KitchenStep LastKitchenStep_;
        #endregion

        #region CONSTRUCTOR

        public LightControlKitchen (double AllOnTime, double AllOutputsOffTime, double SingleOffTime, double AutomaticOffTime, int startindex, int lastindex, ref InterfaceKitDigitalOutputCollection outputs )
            : base(  AllOnTime, AllOutputsOffTime, SingleOffTime, AutomaticOffTime, startindex,  lastindex, ref  outputs )
        {
            base.AutomaticOff_          +=       LightControlKitchen_AutomaticOff_;
            base.EReset                 +=       LightControlKitchen_EReset;
            base.AllSelectedDevicesOff_ +=       LightControlKitchen_AllSelectedDevicesOff_;
            base.SingleOff_             +=       LightControlKitchen_SingleOff_;
            _startindex = startindex;
            KitchenStep_ = KitchenStep.eAll;
        }

        #endregion

        #region PROPERTIES
        public bool AnyExternalDeviceOn
        {
            set
            {
                _AnyExternalDeviceOn = value;
                if( value )
                {
                    IndexFS            = 0;
                    ToggleLightGroups  = false;
                    _SingleOffDone     = false;
                }
            }
        }

        public bool AnyExternalDeviceOff
        {
            set
            {
                _AnyExternalDeviceOff = value;
                if( value )
                {
                    ToggleLightGroups  = false;
                    IndexFS            = 0;
                    _SingleOffDone     = false;
                }
            }
        }
        #endregion

        #region EVENTHANDLERS

        void LightControlKitchen_EReset( object sender )
        {
            KitchenStep_ = KitchenStep.eAll;
            reset = true;
            _turnedAutoOff = false;
        }

        void LightControlKitchen_SingleOff_( object sender )
        {
            for( int i = _index; i <= _lastindex; i++ )
            {
                outputs_[i] = false;
            }
            _SingleOffDone = true;
            KitchenStep_ = LastKitchenStep_;
            reset = true;
        }

        void LightControlKitchen_AllSelectedDevicesOff_( object sender, int firstdevice, int lastdevice )
        {
            KitchenStep_ = LastKitchenStep_;
            reset = true;
        }

        void LightControlKitchen_AutomaticOff_ ( object sender )
        {
            KitchenStep_       = LastKitchenStep_;
            IndexFS            = 0;
            ToggleLightGroups  = false;
            _turnedAutoOff     = true;
            reset = true;
        }

        #endregion

        #region OVERWRITTEN_METHODS
        override protected void StepLight ( int startindex, ref int index, int _indexlastdevice )
        {
            if( _turnedAutoOff )
            {
                index = 0;
                _turnedAutoOff = false;
            }

            if( reset )
            {
                index = 0;
                reset = false;
                if( KitchenStep_ == KitchenStep.eNext )
                {
                    if( _lastindex > 0 )
                    {
                        index = _lastindex - 1;
                    }
                }
            }

            switch( KitchenStep_ )
            {
                   case KitchenStep.eNext:
                        if( _SingleOffDone )
                        {
                            _SingleOffDone = false;
                            return;
                        }
                        _index = startindex + index;
                        if( _index <= _indexlastdevice )
                        {
                            outputs_[_index] = true;
                            if( index > 0 )
                            {
                                outputs_[_index - 1] = false;
                            }
                            index++;
                        }
                        else
                        {
                            outputs_[_index - 1] = false;
                            index = 0;
                            KitchenStep_ = KitchenStep.eFrontLights;
                        }
                        _lastindex = index;
                        break;

                   case KitchenStep.eFrontLights:
                        int FirstFrontLight =  KitchenIOLightIndices.indFrontLight_1;
                        int LastFrontLight =   KitchenIOLightIndices.indFrontLight_3;
                        int IndexLightFrontSide = FirstFrontLight + IndexFS;

                        _lastindex = LastFrontLight;
                        _index     = FirstFrontLight;
                        if( _SingleOffDone )
                        {
                            if( IndexLightFrontSide <= LastFrontLight )
                            {
                                _SingleOffDone    = false;
                                break;
                            }
                        }

                        if( IndexLightFrontSide > LastFrontLight )
                        {
                            for( int i = FirstFrontLight; i <= LastFrontLight; i++ )
                            {
                                outputs_[i] = false;
                            }
                            IndexFS = 0;
                            if( _SingleOffDone )
                            {
                                _SingleOffDone    = false;
                                ToggleLightGroups = false;
                                KitchenStep_      = KitchenStep.eCabinetFrontLights;
                            }
                            break;
                        }
                        outputs_[IndexLightFrontSide] = true;
                        IndexFS++;
                        break;

                   case KitchenStep.eCabinetFrontLights:
                        if( _SingleOffDone )
                        {
                            _SingleOffDone = false;
                            outputs_[KitchenIOLightIndices.indFrontLight_1]                    = false;
                            outputs_[KitchenIOLightIndices.indFrontLight_2]                    = false;
                            outputs_[KitchenIOLightIndices.indFrontLight_3]                    = false;
                            outputs_[KitchenIOLightIndices.indDigitalOutputKitchenKabinet]     = false;
                            ToggleLightGroups = false;
                            KitchenStep_ = KitchenStep.eAll;
                            _lastindex = 0;
                             return;
                        }
                        if( !ToggleLightGroups )
                        {
                            outputs_[KitchenIOLightIndices.indFrontLight_1]                    = true;
                            outputs_[KitchenIOLightIndices.indFrontLight_2]                    = true;
                            outputs_[KitchenIOLightIndices.indFrontLight_3]                    = true;
                            outputs_[KitchenIOLightIndices.indDigitalOutputKitchenKabinet]     = true;
                            ToggleLightGroups = true;
                        }
                        else
                        {
                            outputs_[KitchenIOLightIndices.indFrontLight_1]                    = false;
                            outputs_[KitchenIOLightIndices.indFrontLight_2]                    = false;
                            outputs_[KitchenIOLightIndices.indFrontLight_3]                    = false;
                            outputs_[KitchenIOLightIndices.indDigitalOutputKitchenKabinet]     = false;
                            ToggleLightGroups = false; // TODO - probably this statement is no more needed-  REFACTOR
                        }
                        break;

                   case KitchenStep.eAll:
                        _index     = startindex;
                        _lastindex = _indexlastdevice;
                        if( _SingleOffDone )
                        {
                            base.TurnAllLightsOff( startindex, _indexlastdevice );
                            index = 0;
                            KitchenStep_ = KitchenStep.eSlots;
                            _SingleOffDone = false;
                            ToggleLightGroups = false;
                            break;
                        }
                        if( !ToggleLightGroups )
                        {
                            base.AllLightsOn( startindex, _indexlastdevice );
                            ToggleLightGroups = true;
                        }
                        else
                        {
                            base.TurnAllLightsOff( startindex, _indexlastdevice );
                            ToggleLightGroups = false;
                        }
                        _lastindex = _indexlastdevice;
                        break;

                   case KitchenStep.eSlots:
                        if( _SingleOffDone )
                        {
                            _SingleOffDone = false;
                            outputs_[KitchenIOLightIndices.indFumeHood] = false;
                            outputs_[KitchenIOLightIndices.indSlot]     = false;
                            KitchenStep_ = KitchenStep.eNext;
                            _lastindex = 0;
                             return;
                        }
                        if( !ToggleLightGroups )
                        {
                            outputs_[KitchenIOLightIndices.indFumeHood] = true;
                            outputs_[KitchenIOLightIndices.indSlot]     = true;
                            ToggleLightGroups = true;
                        }
                        else
                        {
                            outputs_[KitchenIOLightIndices.indFumeHood] = false;
                            outputs_[KitchenIOLightIndices.indSlot]     = false;
                            ToggleLightGroups = false;
                        }

                        _lastindex = KitchenIOLightIndices.indSlot;
                        break;

            }
            LastKitchenStep_= KitchenStep_;
        }
        #endregion
    }

    class LightControlKitchenNG : LightControlNG
    {
        #region DECLARATIONS
        public enum KitchenStep
        {
            eNext,
            eFrontLights,
            eAll,
            eNextOn,
            eSlots,
            eTurnedOff,
            eCabinetFrontLights
        }

        const int LastIndexFrontLights = 3;
        const int NumberOfFrontLights  = 3;
        int       _index;
        int       _lastindex;
        int       _startindex;
        bool      _turnedAutoOff;
        bool      _SingleOffDone;
        bool      reset;
        int       IndexFS = 0;
        bool      _AnyExternalDeviceOn;
        bool      _AnyExternalDeviceOff;
        bool      ToggleLightGroups = false;
        bool      ToggleLightWindowBoardEastDown;

        KitchenStep KitchenStep_;
        KitchenStep LastKitchenStep_;
        KitchenStep NextKitchenStep_;

        List<int> Match_ = new List<int>() { KitchenIOLightIndices.indDigitalOutputKitchenKabinet,
                                             KitchenIOLightIndices.indDigitalOutputWindowBoardEastDown,
                                             KitchenIOLightIndices.indFirstKitchen,
                                             KitchenIOLightIndices.indFrontLight_1,
                                             KitchenIOLightIndices.indFrontLight_2,
                                             KitchenIOLightIndices.indFrontLight_3,
                                             KitchenIOLightIndices.indFumeHood,
                                             KitchenIOLightIndices.indSlot
                                           };
        public  delegate void UpdateOutputs_( object sender, bool[] _DigOut, List<int> match  );
        public event          UpdateOutputs_ EUpdateOutputs_;

        #endregion

        #region CONSTRUCTOR

        public LightControlKitchenNG( double AllOnTime, 
                                      double AllOutputsOffTime, 
                                      double SingleOffTime, 
                                      double AutomaticOffTime, 
                                      int    startindex, 
                                      int    lastindex, 
                                      ref    InterfaceKitDigitalOutputCollection outputs )
            : base( AllOnTime, AllOutputsOffTime, SingleOffTime, AutomaticOffTime, startindex, lastindex, ref  outputs )
        {
            base.AutomaticOff_          += LightControlKitchen_AutomaticOff_;
            base.EReset                 += LightControlKitchen_EReset;
            base.AllSelectedDevicesOff_ += LightControlKitchen_AllSelectedDevicesOff_;
            base.SingleOff_             += LightControlKitchen_SingleOff_;
            base.EUpdateOutputs         += LightControlKitchenNG_EUpdateOutputs;
            _startindex = startindex;
            KitchenStep_ = KitchenStep.eAll;
        }
        #endregion

        #region PROPERTIES
        public bool AnyExternalDeviceOn
        {
            set
            {
                _AnyExternalDeviceOn = value;
                if( value )
                {
                    IndexFS = 0;
                    ToggleLightGroups = false;
                    _SingleOffDone = false;
                }
            }
        }

        public bool AnyExternalDeviceOff
        {
            set
            {
                _AnyExternalDeviceOff = value;
                if( value )
                {
                    ToggleLightGroups = false;
                    IndexFS = 0;
                    _SingleOffDone = false;
                }
            }
        }

        public KitchenStep ActualKitchenLightStep
        {
            get
            {
                return ( KitchenStep_ );
            }
        }
        #endregion

        #region EVENTHANDLERS
        void LightControlKitchenNG_EUpdateOutputs( object sender, bool[] _DigOut )
        {
            if( EUpdateOutputs_ != null )
            {
                EUpdateOutputs_( this, _DigOut, Match_ );
            }
        }

        void LightControlKitchen_EReset( object sender )
        {
            KitchenStep_   = KitchenStep.eAll;
            reset          = true;
            _turnedAutoOff = false;
        }

        void LightControlKitchen_SingleOff_( object sender )
        {
            for( int i = _index; i <= _lastindex; i++ )
            {
                base._DigitalOutput[i] = false;
            }
            _SingleOffDone = true;
            KitchenStep_   = NextKitchenStep_;
            reset          = true;
            DoUpDateIO( base._DigitalOutput );
        }

        void LightControlKitchen_AllSelectedDevicesOff_( object sender, int firstdevice, int lastdevice )
        {
            KitchenStep_ = LastKitchenStep_;
            reset        = true;
        }

        void LightControlKitchen_AutomaticOff_( object sender )
        {
            KitchenStep_      = LastKitchenStep_;
            IndexFS           = 0;
            ToggleLightGroups = false;
            _turnedAutoOff    = true;
            reset             = true;
        }

        #endregion

        #region OVERWRITTEN_METHODS
        override protected void StepLight( int startindex, ref int index, int _indexlastdevice )
        {
            if( _turnedAutoOff )
            {
                index = 0;
                _turnedAutoOff = false;
            }

            if( reset )
            {
                index = 0;
                reset = false;
                if( KitchenStep_ == KitchenStep.eNext )
                {
                    if( _lastindex > 0 )
                    {
                        index = _lastindex - 1;
                    }
                }
            }

            if( LastKitchenStep_ != KitchenStep_ )
            {
                _SingleOffDone             = false;
                ToggleLightGroups          = false;
                index                      = 0;
            }

            switch( KitchenStep_ )
            {
               case KitchenStep.eNext:
                    if( ToggleLightWindowBoardEastDown )
                    {
                        KitchenStep_ = KitchenStep.eFrontLights;
                        ToggleLightWindowBoardEastDown = false;
                        base._DigitalOutput[KitchenIOLightIndices.indDigitalOutputWindowBoardEastDown] = false;
                        break;
                    }
                    if( _SingleOffDone )
                    {
                        _SingleOffDone = false;
                        return;
                    }
                    _index = startindex + index;
                    if( _index <= _indexlastdevice )
                    {
                        base._DigitalOutput[_index] = true;
                        if( index > 0 )
                        {
                            base._DigitalOutput[_index - 1] = false;
                        }
                        index++;
                    }
                    else
                    {
                        base._DigitalOutput[_index - 1] = false;
                        index = 0;
                        base._DigitalOutput[KitchenIOLightIndices.indDigitalOutputWindowBoardEastDown] = true;
                        ToggleLightWindowBoardEastDown = true;
                    }
                    _lastindex = index;
                    break;

               case KitchenStep.eFrontLights:
                    NextKitchenStep_        = KitchenStep.eCabinetFrontLights;
                    int FirstFrontLight     = KitchenIOLightIndices.indFrontLight_1;
                    int LastFrontLight      = KitchenIOLightIndices.indFrontLight_3;
                    int IndexLightFrontSide = FirstFrontLight + IndexFS;

                    _lastindex = LastFrontLight;
                    _index     = FirstFrontLight;
                    if( _SingleOffDone )
                    {
                        if( IndexLightFrontSide <= LastFrontLight )
                        {
                            _SingleOffDone = false;
                            break;
                        }
                    }

                    if( IndexLightFrontSide > LastFrontLight )
                    {
                        for( int i = FirstFrontLight; i <= LastFrontLight; i++ )
                        {
                            base._DigitalOutput[i] = false;
                        }
                        IndexFS = 0;
                        if( _SingleOffDone )
                        {
                            _SingleOffDone    = false;
                            ToggleLightGroups = false;
                        }
                        break;
                    }
                    base._DigitalOutput[IndexLightFrontSide] = true;
                    IndexFS++;
                    break;

               case KitchenStep.eCabinetFrontLights:
                    NextKitchenStep_ =  KitchenStep.eAll;
                    if( _SingleOffDone )
                    {
                        _SingleOffDone = false;
                        base._DigitalOutput[KitchenIOLightIndices.indFrontLight_1] = false;
                        base._DigitalOutput[KitchenIOLightIndices.indFrontLight_2] = false;
                        base._DigitalOutput[KitchenIOLightIndices.indFrontLight_3] = false;
                        base._DigitalOutput[KitchenIOLightIndices.indDigitalOutputKitchenKabinet] = false;
                        ToggleLightGroups = false;
                        _lastindex = 0;
                        return;
                    }
                    if( !ToggleLightGroups )
                    {
                        base._DigitalOutput[KitchenIOLightIndices.indFrontLight_1] = true;
                        base._DigitalOutput[KitchenIOLightIndices.indFrontLight_2] = true;
                        base._DigitalOutput[KitchenIOLightIndices.indFrontLight_3] = true;
                        base._DigitalOutput[KitchenIOLightIndices.indDigitalOutputKitchenKabinet] = true;
                        ToggleLightGroups = true;
                    }
                    else
                    {
                        base._DigitalOutput[KitchenIOLightIndices.indFrontLight_1] = false;
                        base._DigitalOutput[KitchenIOLightIndices.indFrontLight_2] = false;
                        base._DigitalOutput[KitchenIOLightIndices.indFrontLight_3] = false;
                        base._DigitalOutput[KitchenIOLightIndices.indDigitalOutputKitchenKabinet] = false;
                        ToggleLightGroups = false; 
                    }
                    break;

               case KitchenStep.eAll:
                    NextKitchenStep_ = KitchenStep.eSlots;
                    _index = startindex;
                    _lastindex = _indexlastdevice;
                    if( _SingleOffDone )
                    {
                        base.TurnAllLightsOff( startindex, _indexlastdevice );
                        index = 0;
                        _SingleOffDone    = false;
                        ToggleLightGroups = false;
                        break;
                    }
                    if( !ToggleLightGroups )
                    {
                        base.AllLightsOn( startindex, _indexlastdevice );
                        ToggleLightGroups = true;
                    }
                    else
                    {
                        base.TurnAllLightsOff( startindex, _indexlastdevice );
                        ToggleLightGroups = false;
                    }
                    _lastindex = _indexlastdevice;
                    break;

               case KitchenStep.eSlots:
                    NextKitchenStep_ = KitchenStep.eNext;
                    if( _SingleOffDone )
                    {
                        _SingleOffDone = false;
                        base._DigitalOutput[KitchenIOLightIndices.indFumeHood]      = false;
                        base._DigitalOutput[KitchenIOLightIndices.indSlot]          = false;
                        _lastindex = 0;
                        return;
                    }
                    if( !ToggleLightGroups )
                    {
                        base._DigitalOutput[KitchenIOLightIndices.indFumeHood]       = true;
                        base._DigitalOutput[KitchenIOLightIndices.indSlot]           = true;
                        ToggleLightGroups = true;
                    }
                    else
                    {
                        base._DigitalOutput[KitchenIOLightIndices.indFumeHood]       = false;
                        base._DigitalOutput[KitchenIOLightIndices.indSlot]           = false;
                        ToggleLightGroups = false;
                    }
                    _lastindex = KitchenIOLightIndices.indSlot;
                    break;
            }
            LastKitchenStep_ = KitchenStep_;
            DoUpDateIO( base._DigitalOutput );
        }
        #endregion
    }

    // new - with strict separation business logic IO handler
    class LightControlKitchen_NG : LightControl_NG
    {
        #region DECLARATIONS
        public enum KitchenStep
        {
            eNext,
            eFrontLights,
            eAll,
            eNextOn,
            eSlots,
            eTurnedOff,
            eCabinetFrontLights
        }

        const int LastIndexFrontLights = 3;
        const int NumberOfFrontLights  = 3;
        int       _index;
        int       _lastindex;
        int       _startindex;
        bool      _turnedAutoOff;
        bool      _SingleOffDone;
        bool      reset;
        int       IndexFS = 0;
        bool      _AnyExternalDeviceOn;
        bool      _AnyExternalDeviceOff;
        bool      ToggleLightGroups = false;
        bool      ToggleLightWindowBoardEastDown;

        KitchenStep KitchenStep_;
        KitchenStep LastKitchenStep_;
        KitchenStep NextKitchenStep_;

        List<int> Match_; // this list keeps information about which items can acess digital output

        public delegate void  UpdateOutputs_( object sender, bool[] _DigOut, List<int> match );
        public event          UpdateOutputs_ EUpdateOutputs_;

        #endregion

        #region CONSTRUCTOR

        public LightControlKitchen_NG( double AllOnTime,
                                       double AllOutputsOffTime,
                                       double SingleOffTime,
                                       double AutomaticOffTime,
                                       int startindex,
                                       int lastindex
                                      )
            : base( AllOnTime, AllOutputsOffTime, SingleOffTime, AutomaticOffTime, startindex, lastindex )
        {
            base.AutomaticOff_          += LightControlKitchen_AutomaticOff_;
            base.EReset                 += LightControlKitchen_EReset;
            base.AllSelectedDevicesOff_ += LightControlKitchen_AllSelectedDevicesOff_;
            base.SingleOff_             += LightControlKitchen_SingleOff_;
            _startindex = startindex;
            KitchenStep_ = KitchenStep.eAll;

            // this list keeps information about which items can acess digital output
            // only booleans with this configured index can write to an digital output
            Match_ = new List<int>() { KitchenIOLightIndices.indDigitalOutputKitchenKabinet,
                                       KitchenIOLightIndices.indDigitalOutputWindowBoardEastDown,
                                       KitchenIOLightIndices.indFirstKitchen,
                                       KitchenIOLightIndices.indFrontLight_1,
                                       KitchenIOLightIndices.indFrontLight_2,
                                       KitchenIOLightIndices.indFrontLight_3,
                                       KitchenIOLightIndices.indFumeHood,
                                       KitchenIOLightIndices.indSlot
                                     };
        }
        #endregion

        #region PROPERTIES
        public bool AnyExternalDeviceOn
        {
            set
            {
                _AnyExternalDeviceOn = value;
                if( value )
                {
                    IndexFS = 0;
                    ToggleLightGroups = false;
                    _SingleOffDone = false;
                }
            }
        }

        public bool AnyExternalDeviceOff
        {
            set
            {
                _AnyExternalDeviceOff = value;
                if( value )
                {
                    ToggleLightGroups = false;
                    IndexFS = 0;
                    _SingleOffDone = false;
                }
            }
        }

        public KitchenStep ActualKitchenLightStep
        {
            get
            {
                return ( KitchenStep_ );
            }
        }
        #endregion

        #region EVENTHANDLERS

        void LightControlKitchen_EReset( object sender )
        {
            KitchenStep_ = KitchenStep.eAll;
            reset = true;
            _turnedAutoOff = false;
        }

        void LightControlKitchen_SingleOff_( object sender )
        {
            for( int i = _index; i <= _lastindex; i++ )
            {
                base._DigitalOutput[i] = false;
            }
            _SingleOffDone = true;
            KitchenStep_ = NextKitchenStep_;
            reset = true;
            DoUpDateIO( base._DigitalOutput );
        }

        void LightControlKitchen_AllSelectedDevicesOff_( object sender, int firstdevice, int lastdevice )
        {
            KitchenStep_ = LastKitchenStep_;
            reset = true;
        }

        void LightControlKitchen_AutomaticOff_( object sender )
        {
            KitchenStep_      = LastKitchenStep_;
            IndexFS           = 0;
            ToggleLightGroups = false;
            _turnedAutoOff    = true;
            reset             = true;
        }

        #endregion

        #region PRIVATEMETHODS
        new void DoUpDateIO( bool[] _DigOut )
        {
            if( EUpdateOutputs_ != null )
            {
                EUpdateOutputs_( this, _DigOut, Match_ );
            }
        }
        #endregion

        #region OVERWRITTEN_METHODS

        override protected void StepLight( int startindex, ref int index, int _indexlastdevice )
        {
            if( _turnedAutoOff )
            {
                index = 0;
                _turnedAutoOff = false;
            }

            if( reset )
            {
                index = 0;
                reset = false;
                if( KitchenStep_ == KitchenStep.eNext )
                {
                    if( _lastindex > 0 )
                    {
                        index = _lastindex - 1;
                    }
                }
            }

            if( LastKitchenStep_ != KitchenStep_ )
            {
                _SingleOffDone = false;
                ToggleLightGroups = false;
                index = 0;
            }

            switch( KitchenStep_ )
            {
                case KitchenStep.eNext:
                    if( ToggleLightWindowBoardEastDown )
                    {
                        KitchenStep_ = KitchenStep.eFrontLights;
                        ToggleLightWindowBoardEastDown = false;
                        base._DigitalOutput[KitchenIOLightIndices.indDigitalOutputWindowBoardEastDown] = false;
                        break;
                    }
                    if( _SingleOffDone )
                    {
                        _SingleOffDone = false;
                        return;
                    }
                    _index = startindex + index;
                    if( _index <= _indexlastdevice )
                    {
                        base._DigitalOutput[_index] = true;
                        if( index > 0 )
                        {
                            base._DigitalOutput[_index - 1] = false;
                        }
                        index++;
                    }
                    else
                    {
                        base._DigitalOutput[_index - 1] = false;
                        index = 0;
                        base._DigitalOutput[KitchenIOLightIndices.indDigitalOutputWindowBoardEastDown] = true;
                        ToggleLightWindowBoardEastDown = true;
                    }
                    _lastindex = index;
                    break;

                case KitchenStep.eFrontLights:
                    NextKitchenStep_ = KitchenStep.eCabinetFrontLights;
                    int FirstFrontLight     = KitchenIOLightIndices.indFrontLight_1;
                    int LastFrontLight      = KitchenIOLightIndices.indFrontLight_3;
                    int IndexLightFrontSide = FirstFrontLight + IndexFS;

                    _lastindex = LastFrontLight;
                    _index = FirstFrontLight;
                    if( _SingleOffDone )
                    {
                        if( IndexLightFrontSide <= LastFrontLight )
                        {
                            _SingleOffDone = false;
                            break;
                        }
                    }

                    if( IndexLightFrontSide > LastFrontLight )
                    {
                        for( int i = FirstFrontLight; i <= LastFrontLight; i++ )
                        {
                            base._DigitalOutput[i] = false;
                        }
                        IndexFS = 0;
                        if( _SingleOffDone )
                        {
                            _SingleOffDone = false;
                            ToggleLightGroups = false;
                        }
                        break;
                    }
                    base._DigitalOutput[IndexLightFrontSide] = true;
                    IndexFS++;
                    break;

                case KitchenStep.eCabinetFrontLights:
                    NextKitchenStep_ = KitchenStep.eAll;
                    if( _SingleOffDone )
                    {
                        _SingleOffDone = false;
                        base._DigitalOutput[KitchenIOLightIndices.indFrontLight_1] = false;
                        base._DigitalOutput[KitchenIOLightIndices.indFrontLight_2] = false;
                        base._DigitalOutput[KitchenIOLightIndices.indFrontLight_3] = false;
                        base._DigitalOutput[KitchenIOLightIndices.indDigitalOutputKitchenKabinet] = false;
                        ToggleLightGroups = false;
                        _lastindex = 0;
                        return;
                    }
                    if( !ToggleLightGroups )
                    {
                        base._DigitalOutput[KitchenIOLightIndices.indFrontLight_1] = true;
                        base._DigitalOutput[KitchenIOLightIndices.indFrontLight_2] = true;
                        base._DigitalOutput[KitchenIOLightIndices.indFrontLight_3] = true;
                        base._DigitalOutput[KitchenIOLightIndices.indDigitalOutputKitchenKabinet] = true;
                        ToggleLightGroups = true;
                    }
                    else
                    {
                        base._DigitalOutput[KitchenIOLightIndices.indFrontLight_1] = false;
                        base._DigitalOutput[KitchenIOLightIndices.indFrontLight_2] = false;
                        base._DigitalOutput[KitchenIOLightIndices.indFrontLight_3] = false;
                        base._DigitalOutput[KitchenIOLightIndices.indDigitalOutputKitchenKabinet] = false;
                        ToggleLightGroups = false;
                    }
                    break;

                case KitchenStep.eAll:
                    NextKitchenStep_ = KitchenStep.eSlots;
                    _index = startindex;
                    _lastindex = _indexlastdevice;
                    if( _SingleOffDone )
                    {
                        base.TurnAllLightsOff( startindex, _indexlastdevice );
                        index = 0;
                        _SingleOffDone = false;
                        ToggleLightGroups = false;
                        break;
                    }
                    if( !ToggleLightGroups )
                    {
                        base.AllLightsOn( startindex, _indexlastdevice );
                        ToggleLightGroups = true;
                    }
                    else
                    {
                        base.TurnAllLightsOff( startindex, _indexlastdevice );
                        ToggleLightGroups = false;
                    }
                    _lastindex = _indexlastdevice;
                    break;

                case KitchenStep.eSlots:
                    NextKitchenStep_ = KitchenStep.eNext;
                    if( _SingleOffDone )
                    {
                        _SingleOffDone = false;
                        base._DigitalOutput[KitchenIOLightIndices.indFumeHood] = false;
                        base._DigitalOutput[KitchenIOLightIndices.indSlot] = false;
                        _lastindex = 0;
                        return;
                    }
                    if( !ToggleLightGroups )
                    {
                        base._DigitalOutput[KitchenIOLightIndices.indFumeHood] = true;
                        base._DigitalOutput[KitchenIOLightIndices.indSlot] = true;
                        ToggleLightGroups = true;
                    }
                    else
                    {
                        base._DigitalOutput[KitchenIOLightIndices.indFumeHood] = false;
                        base._DigitalOutput[KitchenIOLightIndices.indSlot] = false;
                        ToggleLightGroups = false;
                    }
                    _lastindex = KitchenIOLightIndices.indSlot;
                    break;
            }
            LastKitchenStep_ = KitchenStep_;
            DoUpDateIO( base._DigitalOutput );
        }
        #endregion
    }

    class LightControlOutside : LightControl
    {
        #region CONSTRUCTOR
        public LightControlOutside( double AllOnTime, double SingleOffTime, ref InterfaceKitDigitalOutputCollection outputs )
            : base( AllOnTime, SingleOffTime, ref  outputs )
        {
        }
        #endregion
    }

    class Center_kitchen_living_room : CommonRoom, IDigitalIO
    {
        #region DECLARATION
        LightControlKitchen                  Kitchen;
        LightControlOutside                  Outside;
        HeaterElement                        HeatersLivingRoom;
        HeaterElement                        HeaterAnteRoom;
        CentralControlledElements            FanWashRoom;
        CentralControlledElements            CirculationPump;
        BasicClientComumnicator              BasicClientCommunicator_;
        bool[]                               _DigitalInputState;
        bool[]                               _DigitalOutputState;
        home_scheduler                       scheduler         = new home_scheduler( );
        SchedulerDataRecovery                schedRecover;
        FeedData                             PrevSchedulerData = new FeedData( );
        UdpReceive                           UDPReceive_;
        Timer                                TimerRecoverScheulder;
        #endregion

        #region CONSTRUCTOR
        public Center_kitchen_living_room( string IpAdressServer, string PortServer, string softwareversion )
            : base()
        {
            {
                _GivenClientName         = InfoOperationMode.CENTER_KITCHEN_AND_LIVING_ROOM;
                _IpAdressServer          = IpAdressServer;
                _PortNumberServer        = Convert.ToInt16( PortServer );
                _DigitalInputState       = new bool[GeneralConstants.NumberOfInputsIOCard];
                _DigitalOutputState      = new bool[GeneralConstants.NumberOfOutputsIOCard];
                TimerRecoverScheulder    = new Timer( Parameters.DelayTimeStartRecoverScheduler );

                // IO primer is attached
                if( base.Attached )
                {
                    Kitchen          = new LightControlKitchen(
                                            ParametersLightControlKitchen.TimeDemandForAllOn,
                                            ParametersLightControlKitchen.TimeDemandForAllOutputsOff,
                                            ParametersLightControl.TimeDemandForSingleOff,
                                            ParametersLightControlKitchen.TimeDemandForAutomaticOffKitchen,
                                            KitchenIOLightIndices.indFirstKitchen,
                                            KitchenIOLightIndices.indLastKitchen,
                                            ref base.outputs );     // control digital outputs of primer

                    Outside          = new LightControlOutside(
                                            ParametersLightControlCenterOutside.TimeDemandForAllOn,
                                            ParametersLightControlCenterOutside.TimeDemandForAutomaticOff,
                                            ref base.outputs );     // control digital outputs of primer

                    Kitchen.IsPrimaryIOCardAttached = base._PrimaryIOCardIsAttached;

                    HeatersLivingRoom = new HeaterElement( 
                                             ParametersHeaterControl.TimeDemandForHeatersOnOff,
                                             ParametersHeaterControl.TimeDemandForHeatersAutomaticOffHalfDay,
                                             ParametersHeaterControlLivingRoom.TimeDemandForHeatersOnMiddle,
                                             ParametersHeaterControlLivingRoom.TimeDemandForHeatersOffMiddle,
                                             KitchenLivingRoomIOAssignment.indFirstHeater,
                                             KitchenLivingRoomIOAssignment.indLastHeater,
                                             ref base.outputs );     // control directly digital outputs of primer

                    HeaterAnteRoom    = new HeaterElement( 
                                             ParametersHeaterControl.TimeDemandForHeatersOnOff,
                                             ParametersHeaterControl.TimeDemandForHeatersAutomaticOffBig,
                                             ParametersHeaterControlLivingRoom.TimeDemandForHeatersOnMiddle,
                                             ParametersHeaterControlLivingRoom.TimeDemandForHeatersOffMiddle,
                                             AnteRoomIODeviceIndices.indDigitalOutputAnteRoomHeater,
                                             AnteRoomIODeviceIndices.indDigitalOutputAnteRoomHeater,
                                             ref base.outputs );    // control directly digital outputs of primer

                    FanWashRoom       = new CentralControlledElements(
                                             ParametersWashRoomControl.TimeDemandForFanOn,
                                             ParametersWashRoomControl.TimeDemandForFanAutomaticOff,
                                             WashRoomIODeviceIndices.indDigitalOutputWashRoomFan,
                                             ref base.outputs );   // control directly digital outputs of primer

                    CirculationPump   = new CentralControlledElements( 
                                             ParametersWaterHeatingSystem.TimeDemandForWarmCirculationPumpAutomaticOff,
                                             WaterHeatingSystemIODeviceIndices.indDigitalOutputWarmWaterCirculationPump,
                                             ref base.outputs ); // control directly digital outputs of primer

                    HeatersLivingRoom.AllOn_ += HeatersLivingRoom_AllOn_;
                    Kitchen.EReset           += Kitchen_EReset;
                    Kitchen.StartAliveSignal();
                }

                base.Attach += Center_kitchen_living_room_Attach;
                base.Detach += Center_kitchen_living_room_Detach;

                BasicClientCommunicator_ = new BasicClientComumnicator( _GivenClientName, 
                                                                        _IpAdressServer, 
                                                                         PortServer,
                                                                         ref base.outputs, // control directly digital outputs of primer - server can control this outputs
                                                                         ref HADictionaries.DeviceDictionaryCenterdigitalOut,
                                                                         ref HADictionaries.DeviceDictionaryCenterdigitalIn, softwareversion );
                // establish client
                BasicClientCommunicator_.Room                    = _GivenClientName;
                BasicClientCommunicator_.EFeedScheduler         += BasicClientCommunicator__EFeedScheduler;
                BasicClientCommunicator_.EAskSchedulerForStatus += BasicClientCommunicator__EAskSchedulerForStatus;
                scheduler.EvTriggered                           += scheduler_EvTriggered;
                
                BasicClientCommunicator_.Primer1IsAttached = Attached;

                if( !Attached )
                {
                    BasicClientCommunicator_.SendInfoToServer( InfoString.InfoNoIO );
                }

                try
                {
                    UDPReceive_ = new UdpReceive( IPConfiguration.Port.PORT_LIGHT_CONTROL_COMMON );
                    UDPReceive_.EDataReceived += UDPReceive__EDataReceived;
                }
                catch( Exception ex )
                {
                    Console.WriteLine( TimeUtil.GetTimestamp() + Seperators.Spaceholder + ex.Message );
                    Services.TraceMessage_( InfoString.FailedToEstablishUDPReceive ); 
                }

                // after power fail, certain important scheduler data is recovered
                schedRecover                   = new SchedulerDataRecovery( Directory.GetCurrentDirectory() );
                schedRecover.ERecover         += schedRecover_ERecover;
                schedRecover.ERecovered       += schedRecover_ERecovered;
                TimerRecoverScheulder.Elapsed += TimerRecoverScheulder_Elapsed;
                TimerRecoverScheulder.Start();
                Console.WriteLine( TimeUtil.GetTimestamp() + Seperators.Spaceholder + InfoString.StartTimerForRecoverScheduler );
            }
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
            SchedulerApplication.Worker( this, e, ref scheduler );
            Console.WriteLine( TimeUtil.GetTimestamp( ) + Seperators.Spaceholder + "Recover scheduler after booting " );
            Console.WriteLine( TimeUtil.GetTimestamp( ) + Seperators.Spaceholder + "Start time: " + e.Starttime );
            Console.WriteLine( TimeUtil.GetTimestamp( ) + Seperators.Spaceholder + "Stop time : " + e.Stoptime );
            Console.WriteLine( TimeUtil.GetTimestamp( ) + Seperators.Spaceholder + "Configured days: "  + e.Days );
            Console.WriteLine( TimeUtil.GetTimestamp( ) + Seperators.Spaceholder + "Current scheduler status: " 
                + scheduler.GetJobStatus( e.Device + Seperators.InfoSeperator + e.JobId ).ToString() );
        }
        
        void ControlScheduledDevice( Quartz.IJobExecutionContext context, decimal counts, string device )
        {
             int index = 0 ;
             if( context.JobDetail.Key.Name.Contains( device ) )
             {
                 HADictionaries.DeviceDictionaryCenterdigitalOut.TryGetValue( device, out index );
                 if( base.outputs != null )
                 {
                     if( index >= 0 && index < GeneralConstants.NumberOfOutputsIOCard )
                     {
                         if( counts % 2 == 0 )
                         {
                             base.outputs[index] = GeneralConstants.OFF; 
                         }
                         else
                         {
                             base.outputs[index] = GeneralConstants.ON;
                         }
                     }
                 }
             }
        }

        void scheduler_EvTriggered( string time, Quartz.IJobExecutionContext context, decimal counts )
        {
             SchedulerApplication.WriteStatus( time, context, counts );

             if( !Attached )
             {
                 return;
             }

             ControlScheduledDevice( context, counts, HomeAutomation.HardConfig.HardwareDevices.Boiler ); 
        }

        void BasicClientCommunicator__EAskSchedulerForStatus( object sender, string Job )
        {
            if( scheduler != null )
            {
                string SystemIsAskingScheduler = TimeUtil.GetTimestamp() + Seperators.Spaceholder + _GivenClientName + "...." + InfoString.Asking + Seperators.Spaceholder + InfoString.Scheduler;
                Console.WriteLine( SystemIsAskingScheduler );
                SchedulerInfo.Status status = scheduler.GetJobStatus( Job );
                string StatusInformation = TimeUtil.GetTimestamp()        + 
                                           Seperators.Spaceholder         + 
                                           _GivenClientName               + 
                                           Seperators.Spaceholder         + 
                                           InfoString.StatusOf            + 
                                           Seperators.Spaceholder         + 
                                           Job                            + 
                                           Seperators.Spaceholder         + 
                                           InfoString.Is                  + 
                                           Seperators.Spaceholder         + 
                                           status.ToString();
                Console.WriteLine( StatusInformation );
                string Answer =  InfoOperationMode.CENTER_KITCHEN_AND_LIVING_ROOM       + 
                                 Seperators.InfoSeperator                               +
                                 HomeAutomationAnswers.ANSWER_SCHEDULER_STATUS          + 
                                 Seperators.InfoSeperator                               + 
                                 Job                                                    + 
                                 Seperators.InfoSeperator                               + 
                                 status.ToString();
                BasicClientCommunicator_.SendInfoToServer( Answer );
            }
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

        string _PortServer;
        int _PortNumberServer;
        public string PortServer
        {
            set
            {
                _PortServer = value;
                _PortNumberServer = Convert.ToInt16( value );
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
            HeatersLivingRoom.Reset( );
            HeaterAnteRoom.Reset( );
        }

        void Reset( InputChangeEventArgs e )
        {
            if( e.Value == true )
            {
                Kitchen.StartWaitForAllOff( );
            }
            else
            {
                Kitchen.StopWaitForAllOff( );
            }
        }

        void Reset( bool command )
        {
            if( command == true )
            {
                Kitchen.StartWaitForAllOff();
            }
            else
            {
                Kitchen.StopWaitForAllOff();
            }
        }

        public void StopAliveSignal()
        {
            Kitchen.StopAliveSignal();
        }

        protected override void TurnNextLightOn_( InputChangeEventArgs e )
        {
            if( Kitchen != null )
            {
                switch( e.Index ) // the index is the assigned input number
                {
                    // first relase start one light, next relase start neighbor light, turn previous light off
                    // press button longer than ( f.e. 1.. seconds ) - all lights on
                    // press button longer than ( f.e. 2.. seconds ) - all lights off
                    // TODO press button longer than ( f.e. 3.. seconds ) - broadcast all lights off, just default lights on
                    case KitchenIOAssignment.indKitchenMainButton:
                         if( HeatersLivingRoom != null )
                         {
                             // operate light only when there is no demand of manual heater control
                             if( !HeatersLivingRoom.WasHeaterSwitched( ) )
                             {
                                 Kitchen.MakeStep( e );
                             }
                             else
                             {
                                 Kitchen.StopAllOnTimer( );
                                 Kitchen.ResetLightControl( );
                             }
                         }
                         // reset - this is a last rescue anchor in the case something went wrong ( any undiscovered bug )
                         Reset( e );
                         break;

                    case KitchenIOAssignment.indKitchenPresenceDetector:
                         Kitchen.AutomaticOff( e );
                         break;

                    default:
                         break;
                }
            }

            if( FanWashRoom != null )
            {
                switch( e.Index ) // the index is the assigned input number
                {
                    case CenterButtonRelayIOAssignment.indDigitalInputRelayWashRoom:
                         FanWashRoom.DelayedDeviceOnFallingEdge( e );
                         break;

                    default:
                         break;
                }
            }
        }

        void ControlHeaters( InputChangeEventArgs e )
        {
            controlheaters_( e.Index, e.Value );
        }

        void controlheaters_( int index, bool value )
        {
            if( Kitchen != null )
            {
                switch( index ) // the index is the assigned input number
                {
                    case KitchenIOAssignment.indKitchenMainButton:
                         HeatersLivingRoom.HeaterOn( value ); // heaters can be switched on / off with the light button
                         break;

                    default:
                        break;
                }

                if( HeaterAnteRoom != null )
                {
                    switch( index )
                    {
                        // heater in the ante room so far is controlled by center/kitchen/living room sbc
                        // reason is that the cable conneting the actuator was easier to lay 
                        case CenterButtonRelayIOAssignment.indDigitalInputRelayAnteRoom:
                            HeaterAnteRoom.HeaterOnFallingEdge( value );
                            break;

                    }
                }
            }

        }

        void ControlCirculationPump( InputChangeEventArgs e )
        {
            // Warmwater Circulation pump ( for shower, pipe and bathroom )control
            if( CirculationPump != null )
            {
                switch (e.Index) // the index is the assigned input number
                {
                    case KitchenIOAssignment.indKitchenPresenceDetector:
                    case KitchenIOAssignment.indKitchenMainButton:
                    case AnteRoomIOAssignment.indBathRoomMainButton:
                         CirculationPump.DeviceOnFallingEdgeAutomaticOff( e );
                         break;

                    default:
                        break;
                }
            }
        }

        int ToggleHeatersOnOff = 0;
        void HeatersLivingRoom_AllOn_( object sender )
        {
            if( ToggleHeatersOnOff == 0 )
            {
                Kitchen.AnyExternalDeviceOn = true;
                Kitchen.AnyExternalDeviceOff = false;
            }
            ToggleHeatersOnOff++;
            if( ToggleHeatersOnOff > 1 )
            {
                ToggleHeatersOnOff = 0;
                Kitchen.AnyExternalDeviceOn = false;
                Kitchen.AnyExternalDeviceOff = true;
            }
        }
        #endregion

        #region REMOTE_CONTROLLED_UDP
        decimal receivedTransactionCounter         = 0;
        decimal PreviousreceivedTransactionCounter = 0;
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
                         Outside.AutomaticOff( ReceivedValue );
                         Outside.ToggleSingleLight( CenterOutsideIODevices.indDigitalOutputLightsOutside );
                     }
                     break;
            }
        }
        #endregion

        #region IOEVENTHANDLERS
        // EVENT HANDLER DIGITAL INPUTS
        protected override void BuildingSection_InputChange( object sender, InputChangeEventArgs e )
        {
            CheckFireAlert(e);

            CheckOpenWindow(e);

            if( Kitchen != null )
            {
                Kitchen.StateDigitalOutput = base.StateDigitalOutput;
            }

            TurnNextLightOn_(e);

            ControlHeaters( e );

            ControlCirculationPump( e );

            if( _DigitalInputState == null )
            {
                return;
            }
            for( int i = 0; i < _DigitalInputState.Length; i++ )
            {
                // simplification - input state is written in a bool array
                _DigitalInputState[i] = base.inputs[i];
            }
            if( BasicClientCommunicator_ != null )
            {
                BasicClientCommunicator_.DigitalInputs = _DigitalInputState;
                BasicClientCommunicator_.IndexInput    = e.Index;
            }

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
        }
        #endregion

        #region EVENTHANDLERS
        void TimerRecoverScheulder_Elapsed( object sender, ElapsedEventArgs e )
        {
             schedRecover.RecoverScheduler( Directory.GetCurrentDirectory( ), HomeAutomation.HardConfig.HardwareDevices.Boiler );
             TimerRecoverScheulder.Stop();
        }
        #endregion
    }

    class Center_kitchen_living_room_NG : CommonRoom, IDigitalIO
    {
        #region DECLARATION
        LightControlKitchen_NG               Kitchen;
        LightControl_NG                      Outside;
        HeaterElement_NG                     HeatersLivingRoom;
        HeaterElement_NG                     HeaterAnteRoom;
        HeaterElement_NG                     HeaterKidsRoom;
        CentralControlledElements_NG         FanWashRoom;
        CentralControlledElements_NG         CirculationPump;
        BasicClientComumnicator              BasicClientCommunicator_;
        bool[]                               _DigitalInputState;
        bool[]                               _DigitalOutputState;
        bool[]                               _InternalDigitalOutputState;
        bool                                 _Test;
        home_scheduler                       scheduler         = new home_scheduler();
        SchedulerDataRecovery                schedRecover;
        FeedData                             PrevSchedulerData = new FeedData();
        UdpReceive                           UDPReceive_;
        Timer                                TimerRecoverScheulder;
        Timer                                CommonUsedTick =  new Timer( GeneralConstants.DURATION_COMMONTICK );
        long                                 RemainingTime;
        PowerMeter                           HomePowerMeter;

        public delegate void UpdateMatchedOutputs( object sender, bool[] _DigOut );
        public event UpdateMatchedOutputs EUpdateMatchedOutputs;

        #endregion

        #region CONSTRUCTOR
        void Constructor( string IpAdressServer, string PortServer, string softwareversion )
        {
            _GivenClientName            = InfoOperationMode.CENTER_KITCHEN_AND_LIVING_ROOM;
            _IpAdressServer             = IpAdressServer;
            _PortNumberServer           = Convert.ToInt16( PortServer );
            _DigitalInputState          = new bool[GeneralConstants.NumberOfInputsIOCard];
            _DigitalOutputState         = new bool[GeneralConstants.NumberOfOutputsIOCard];
            _InternalDigitalOutputState = new bool[GeneralConstants.NumberOfOutputsIOCard];
            TimerRecoverScheulder       = new Timer( Parameters.DelayTimeStartRecoverScheduler );

                Kitchen = new LightControlKitchen_NG(
                                        ParametersLightControlKitchen.TimeDemandForAllOn,
                                        ParametersLightControlKitchen.TimeDemandForAllOutputsOff,
                                        ParametersLightControl.TimeDemandForSingleOff,
                                        ParametersLightControlKitchen.TimeDemandForAutomaticOffKitchen,
                                        KitchenIOLightIndices.indFirstKitchen,
                                        KitchenIOLightIndices.indLastKitchen );

                Outside = new LightControl_NG(
                                        ParametersLightControlCenterOutside.TimeDemandForAllOn,
                                        ParametersLightControlCenterOutside.TimeDemandForAutomaticOff,
                                        CenterOutsideIODevices.indDigitalOutputLightsOutside );
                
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

                HeaterKidsRoom = new HeaterElement_NG(
                                         ParametersHeaterControl.TimeDemandForHeatersOnOffLonger,
                                         ParametersHeaterControl.TimeDemandForHeatersAutomaticOffBig,
                                         ParametersHeaterControlLivingRoom.TimeDemandForHeatersOnMiddle,
                                         ParametersHeaterControlLivingRoom.TimeDemandForHeatersOffMiddle,
                                         KidsRoomIODeviceIndices.indDigitalOutputHeater,
                                         KidsRoomIODeviceIndices.indDigitalOutputHeater );
                HeaterKidsRoom.Match = new List<int> { KidsRoomIODeviceIndices.indDigitalOutputHeater };
                HeaterKidsRoom.ConfigOnOffCount( KidsRoomConfiguration.PushPullCountsTurnHeaterOnOff, KidsRoomConfiguration.TimeWindowTurnHeatersOnOff );

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

            #region REGISTRATION_FOR_ONECOMMONHANDLER
            Kitchen.EUpdateOutputs_           += EShowUpdatedOutputs;
            HeatersLivingRoom.EUpdateOutputs_ += EShowUpdatedOutputs;
            HeaterAnteRoom.EUpdateOutputs_    += EShowUpdatedOutputs;
            HeaterKidsRoom.EUpdateOutputs_    += EShowUpdatedOutputs;
            FanWashRoom.EUpdateOutputs_       += EShowUpdatedOutputs;
            CirculationPump.EUpdateOutputs_   += EShowUpdatedOutputs;
            #endregion

            CommonUsedTick.Elapsed            += CommonUsedTick_Elapsed;

            base.Attach += Center_kitchen_living_room_Attach;
            base.Detach += Center_kitchen_living_room_Detach;

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
            scheduler.EvTriggered                            += scheduler_EvTriggered;

            BasicClientCommunicator_.Primer1IsAttached = Attached;

            if( !Attached )
            {
                BasicClientCommunicator_.SendInfoToServer( InfoString.InfoNoIO );
            }

            try
            {
                UDPReceive_ = new UdpReceive( IPConfiguration.Port.PORT_LIGHT_CONTROL_COMMON );
                UDPReceive_.EDataReceived += UDPReceive__EDataReceived;
            }
            catch( Exception ex )
            {
                Console.WriteLine( TimeUtil.GetTimestamp() + Seperators.Spaceholder + ex.Message );
                Services.TraceMessage_( InfoString.FailedToEstablishUDPReceive );
            }

            // after power fail, certain important scheduler data is recovered
            schedRecover = new SchedulerDataRecovery( Directory.GetCurrentDirectory() );
            schedRecover.ERecover         += schedRecover_ERecover;
            schedRecover.ERecovered       += schedRecover_ERecovered;
            TimerRecoverScheulder.Elapsed += TimerRecoverScheulder_Elapsed;
            TimerRecoverScheulder.Start( );
            CommonUsedTick.Start( );
            Console.WriteLine( TimeUtil.GetTimestamp() + Seperators.Spaceholder + InfoString.StartTimerForRecoverScheduler );
            RemainingTime = Convert.ToUInt32( (Parameters.DelayTimeStartRecoverScheduler / Parameters.MilisecondsOfSecond)  );

            //HomePowerMeter = new PowerMeter( true, PowermeterConstants.DefaultCaptureIntervallTime, PowermeterConstants.DefaultStoreTime );

        }

        public Center_kitchen_living_room_NG( string IpAdressServer, string PortServer, string softwareversion )
            : base()
        {
            Constructor(  IpAdressServer,  PortServer,  softwareversion );
        }

        public Center_kitchen_living_room_NG( string IpAdressServer, string PortServer, string softwareversion, bool test )
            : base()
        {
            Constructor( IpAdressServer, PortServer, softwareversion );
            _Test = test;
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
            SchedulerApplication.Worker( this, e, ref scheduler );
            Console.WriteLine( TimeUtil.GetTimestamp() + Seperators.Spaceholder + "Recover scheduler after booting " );
            Console.WriteLine( TimeUtil.GetTimestamp() + Seperators.Spaceholder + "Start time: " + e.Starttime );
            Console.WriteLine( TimeUtil.GetTimestamp() + Seperators.Spaceholder + "Stop time : " + e.Stoptime );
            Console.WriteLine( TimeUtil.GetTimestamp() + Seperators.Spaceholder + "Configured days: " + e.Days );
            Console.WriteLine( TimeUtil.GetTimestamp() + Seperators.Spaceholder + "Current scheduler status: "
                + scheduler.GetJobStatus( e.Device + Seperators.InfoSeperator + e.JobId ).ToString() );
        }

        void ControlScheduledDevice( Quartz.IJobExecutionContext context, decimal counts, string device )
        {
            int index = 0;
            if( context.JobDetail.Key.Name.Contains( device ) )
            {
                HADictionaries.DeviceDictionaryCenterdigitalOut.TryGetValue( device, out index );
                if( base.outputs != null )
                {
                    if( index >= 0 && index < GeneralConstants.NumberOfOutputsIOCard )
                    {
                        if( counts % 2 == 0 )
                        {
                            base.outputs[index] = GeneralConstants.OFF;
                        }
                        else
                        {
                            base.outputs[index] = GeneralConstants.ON;
                        }
                    }
                }
            }
        }

        void scheduler_EvTriggered( string time, Quartz.IJobExecutionContext context, decimal counts )
        {
            SchedulerApplication.WriteStatus( time, context, counts );

            if( !Attached )
            {
                return;
            }

            ControlScheduledDevice( context, counts, HomeAutomation.HardConfig.HardwareDevices.Boiler );
        }

        void BasicClientCommunicator__EAskSchedulerForStatus( object sender, string Job )
        {
            if( scheduler != null )
            {
                string SystemIsAskingScheduler = TimeUtil.GetTimestamp() + Seperators.Spaceholder + _GivenClientName + "...." + InfoString.Asking + Seperators.Spaceholder + InfoString.Scheduler;
                Console.WriteLine( SystemIsAskingScheduler );
                SchedulerInfo.Status status = scheduler.GetJobStatus( Job );
                string StatusInformation = TimeUtil.GetTimestamp() +
                                           Seperators.Spaceholder  +
                                           _GivenClientName        +
                                           Seperators.Spaceholder  +
                                           InfoString.StatusOf     +
                                           Seperators.Spaceholder  +
                                           Job                     +
                                           Seperators.Spaceholder  +
                                           InfoString.Is           +
                                           Seperators.Spaceholder  +
                                           status.ToString();
                Console.WriteLine( StatusInformation );
                string Answer =  InfoOperationMode.CENTER_KITCHEN_AND_LIVING_ROOM   +
                                 Seperators.InfoSeperator                           +
                                 HomeAutomationAnswers.ANSWER_SCHEDULER_STATUS      +
                                 Seperators.InfoSeperator                           +
                                 Job                                                +
                                 Seperators.InfoSeperator                           +
                                 status.ToString();
                BasicClientCommunicator_.SendInfoToServer( Answer );
            }
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

        string _PortServer;
        int _PortNumberServer;
        public string PortServer
        {
            set
            {
                _PortServer = value;
                _PortNumberServer = Convert.ToInt16( value );
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
                Kitchen.StartWaitForAllOff();
            }
            else
            {
                Kitchen.StopWaitForAllOff();
            }
        }

        void Reset( bool command )
        {
            if( command == true )
            {
                Kitchen.StartWaitForAllOff();
            }
            else
            {
                Kitchen.StopWaitForAllOff();
            }
        }

        public void StopAliveSignal()
        {
            Kitchen.StopAliveSignal();
        }

        protected override void TurnNextLightOn_( InputChangeEventArgs e )
        {
            int index  = e.Index;
            bool Value = e.Value;
            TurnNextLightOn_( index, Value );
        }

        void TurnNextLightOn_( int index,  bool Value  )
        {
            if( Kitchen != null )
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
                                Kitchen.MakeStep( Value );
                            }
                            else
                            {
                                Kitchen.StopAllOnTimer();
                                Kitchen.ResetLightControl();
                            }
                        }
                        // reset - this is a last rescue anchor in the case something went wrong ( any undiscovered bug )
                        Reset( Value );
                        break;

                   case KitchenIOAssignment.indKitchenPresenceDetector:
                        Kitchen.AutomaticOff( Value );
                        break;

                    default:
                        break;
                }
            }

            if( FanWashRoom != null )
            {
                switch( index ) // the index is the assigned input number
                {
                   case CenterButtonRelayIOAssignment.indDigitalInputRelayWashRoom:
                        FanWashRoom.DelayedDeviceOnFallingEdge( Value );
                        break;

                    default:
                        break;
                }
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
                        if( Value )
                        {
                            HeaterKidsRoom?.TurnHeaterOnOffWithCounts( );
                        }
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
            if( CirculationPump != null )
            {
                switch( index ) // the index is the assigned input number
                {
                   case KitchenIOAssignment.indKitchenPresenceDetector:
                   case KitchenIOAssignment.indKitchenMainButton:
                   case AnteRoomIOAssignment.indBathRoomMainButton:
                        CirculationPump.DeviceOnFallingEdgeAutomaticOff( Value );
                        break;

                    default:
                        break;
                }
            }
        }

        int ToggleHeatersOnOff = 0;
        void HeatersLivingRoom_AllOn_( object sender )
        {
            if( ToggleHeatersOnOff == 0 )
            {
                Kitchen.AnyExternalDeviceOn = true;
                Kitchen.AnyExternalDeviceOff = false;
            }
            ToggleHeatersOnOff++;
            if( ToggleHeatersOnOff > 1 )
            {
                ToggleHeatersOnOff = 0;
                Kitchen.AnyExternalDeviceOn = false;
                Kitchen.AnyExternalDeviceOff = true;
            }
        }

        void ControlSequenceOnInputChange( int index, bool Value )
        {
            if( Kitchen != null )
            {
                Kitchen.StateDigitalOutput = base.StateDigitalOutput;
            }

            TurnNextLightOn_( index, Value );

            ControlHeaters( index, Value );

            ControlCirculationPump( index, Value );

            if( _DigitalInputState == null )
            {
                return;
            }
            for( int i = 0; i < _DigitalInputState.Length; i++ )
            {
                if( base.Attached )
                {
                    // simplification - input state is written in a bool array
                    _DigitalInputState[i] = base.inputs[i];
                }
            }
            if( BasicClientCommunicator_ != null )
            {
                BasicClientCommunicator_.DigitalInputs = _DigitalInputState;
                BasicClientCommunicator_.IndexInput = index;
            }
        }
        #endregion

        #region REMOTE_CONTROLLED_UDP
        decimal receivedTransactionCounter         = 0;
        decimal PreviousreceivedTransactionCounter = 0;
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
                        Outside.AutomaticOff( ReceivedValue );
                        Outside.ToggleSingleLight( CenterOutsideIODevices.indDigitalOutputLightsOutside );
                    }
                    break;
            }
        }
        #endregion

        #region IOEVENTHANDLERS
        // EVENT HANDLER DIGITAL INPUTS
        protected override void BuildingSection_InputChange( object sender, InputChangeEventArgs e )
        {
            ControlSequenceOnInputChange( e.Index, e.Value );
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
        }
        #endregion

        #region EVENTHANDLERS
        void EShowUpdatedOutputs( object sender, bool[] _DigOut, List<int> Match)
        {
            for( int i = 0; i < _DigOut.Length; i++ )
            {
                if( Match.Contains( i ) ) // only matching index within the defined list are written into the array
                {
                   _InternalDigitalOutputState[i] = _DigOut[i];
                   if( !_Test )
                   {
                        if( base.Attached )
                        {
                            // DIGITAL OUTPUT MAPPING
                            base.outputs[i] = _DigOut[i]; // only one location where real output is written!
                        }
                   }
                }
            }

            if( EUpdateMatchedOutputs != null )
            {
                EUpdateMatchedOutputs( this, _DigOut );
            }
        }

        void TimerRecoverScheulder_Elapsed( object sender, ElapsedEventArgs e )
        {
            schedRecover.RecoverScheduler( Directory.GetCurrentDirectory(), HomeAutomation.HardConfig.HardwareDevices.Boiler );
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

        #region TESTBACKDOOR
        public void TestIoChange( int index,  bool Value  )
        {
            ControlSequenceOnInputChange( index, Value );
        }

        public bool[] ReferenceDigitalOutputState
        {
            get
            {
                return ( _InternalDigitalOutputState );
            }
        }
        #endregion
    }

}
