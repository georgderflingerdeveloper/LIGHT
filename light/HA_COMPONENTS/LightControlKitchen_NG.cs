using System.Collections.Generic;
using HomeAutomation.HardConfig;

namespace HA_COMPONENTS
{
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

        public delegate void UpdateOutputs_( object sender, bool[] _DigOut, List<int> match );
        public event         UpdateOutputs_ EUpdateOutputs_;

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
            base.EUpdateOutputs         += LightControlKitchen_NG_EUpdateOutputs;
            _startindex  = startindex;
            KitchenStep_ = KitchenStep.eAll;

            // this list keeps information about which items can acess digital output
            // only booleans with this configured index can write to an digital output
            Match_ = new List<int>( ) { KitchenIOLightIndices.indDigitalOutputKitchenKabinet,
                                        KitchenIOLightIndices.indDigitalOutputWindowBoardEastDown,
                                        KitchenIOLightIndices.indFirstKitchen,
                                        KitchenIOLightIndices.indFrontLight_1,
                                        KitchenIOLightIndices.indFrontLight_2,
                                        KitchenIOLightIndices.indFrontLight_3,
                                        KitchenIOLightIndices.indFumeHood,
                                        KitchenIOLightIndices.indSlot
                                      };
            base.Match = Match_;
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
            KitchenStep_ = LastKitchenStep_;
            IndexFS = 0;
            ToggleLightGroups = false;
            _turnedAutoOff = true;
            reset = true;
        }
        #endregion

        #region PRIVATEMETHODS
        new void DoUpDateIO( bool[] _DigOut )
        {
            EUpdateOutputs_?.Invoke( this, _DigOut, Match_ );
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

        #region EVENTHANDLERS
        private void LightControlKitchen_NG_EUpdateOutputs( object sender, bool[] _DigOut, List<int> match )
        {
            EUpdateOutputs_?.Invoke( this, _DigOut, match );
        }
        #endregion
    }
}
