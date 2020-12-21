using HomeAutomation.HardConfig_Collected;
using System.Collections.Generic;

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

        int _index;
        int _lastindex;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        int _startindex;
        bool _turnedAutoOff;
        bool _SingleOffDone;
        bool reset;
        int IndexFS = 0;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        bool _AnyExternalDeviceOn;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        bool _AnyExternalDeviceOff;       
        bool ToggleLightGroups = false;
        bool ToggleLightWindowBoardEastDown;

        KitchenStep ActualKitchenStep_;
        KitchenStep LastKitchenStep_;
        KitchenStep NextKitchenStep_;

        List<int> Match_; // this list keeps information about which items can acess digital output

        public delegate void UpdateOutputs_( object sender, bool[] _DigOut, List<int> match );
        public event UpdateOutputs_ EUpdateOutputs_;

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
            AutomaticOff_ += LightControlKitchen_AutomaticOff_;
            EReset += LightControlKitchen_EReset;
            AllSelectedDevicesOff_ += LightControlKitchen_AllSelectedDevicesOff_;
            SingleOff_ += LightControlKitchen_SingleOff_;
            EUpdateOutputs += LightControlKitchen_NG_EUpdateOutputs;

            _startindex = startindex;
            ActualKitchenStep_ = KitchenStep.eAll;

            // this list keeps information about which items can acess digital output
            // only booleans with this configured index can write to an digital output
            Match_ = new List<int>( ) { KitchenCenterIoDevices.indDigitalOutputKitchenKabinet,
                                        KitchenCenterIoDevices.indDigitalOutputWindowBoardEastDown,
                                        KitchenCenterIoDevices.indDigitalOutputFirstKitchen,
                                        KitchenCenterIoDevices.indDigitalOutputFrontLight_1,
                                        KitchenCenterIoDevices.indDigitalOutputFrontLight_2,
                                        KitchenCenterIoDevices.indDigitalOutputFrontLight_3,
                                        KitchenCenterIoDevices.indDigitalOutputFumeHood,
                                        KitchenCenterIoDevices.indDigitalOutputSlot,
                                        CenterOutsideIODevices.indDigitalOutputLightsOutside
                                      };
            Match = Match_;
        }
        #endregion

        #region PROPERTIES
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool AnyExternalDeviceOn
        {
            set
            {
                _AnyExternalDeviceOn = value;

                if (value)
                {
                    IndexFS = 0;
                    ToggleLightGroups = false;
                    _SingleOffDone = false;
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool AnyExternalDeviceOff
        {
            set
            {
                _AnyExternalDeviceOff = value;

                if (value)
                {
                    ToggleLightGroups = false;
                    IndexFS = 0;
                    _SingleOffDone = false;
                }
            }
        }



        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public KitchenStep ActualKitchenStep { get => ActualKitchenStep_; set => ActualKitchenStep_ = value; }
        #endregion

        #region EVENTHANDLERS
        void LightControlKitchen_EReset( object sender )
        {
            ActualKitchenStep_ = KitchenStep.eAll;
            reset = true;
            _turnedAutoOff = false;
        }

        void LightControlKitchen_SingleOff_( object sender )
        {
            for (int i = _index; i <= _lastindex; i++)
            {
                _DigitalOutput[i] = false;
            }
            _SingleOffDone = true;
            ActualKitchenStep_ = NextKitchenStep_;
            reset = true;
            DoUpDateIO( _DigitalOutput );
        }

        void LightControlKitchen_AllSelectedDevicesOff_( object sender, int firstdevice, int lastdevice )
        {
            ActualKitchenStep_ = LastKitchenStep_;
            reset = true;
        }

        void LightControlKitchen_AutomaticOff_( object sender )
        {
            ActualKitchenStep_ = LastKitchenStep_;
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        override protected void StepLight( int startindex, ref int index, int _indexlastdevice )
        {
            if (_turnedAutoOff)
            {
                index = 0;
                _turnedAutoOff = false;
            }

            if (reset)
            {
                index = 0;
                reset = false;
                if (ActualKitchenStep_ == KitchenStep.eNext)
                {
                    if (_lastindex > 0)
                    {
                        index = _lastindex - 1;
                    }
                }
            }

            if (LastKitchenStep_ != ActualKitchenStep_)
            {
                _SingleOffDone = false;
                ToggleLightGroups = false;
                index = 0;
            }

            switch (ActualKitchenStep_)
            {
                case KitchenStep.eNext:
                    if (ToggleLightWindowBoardEastDown)
                    {
                        ActualKitchenStep_ = KitchenStep.eFrontLights;
                        ToggleLightWindowBoardEastDown = false;
                        _DigitalOutput[KitchenCenterIoDevices.indDigitalOutputWindowBoardEastDown] = false;
                        break;
                    }

                    if (_SingleOffDone)
                    {
                        _SingleOffDone = false;
                    }

                    _index = startindex + index;

                    if (_index <= _indexlastdevice)
                    {
                        _DigitalOutput[_index] = true;
                        if (index > 0)
                        {
                            _DigitalOutput[_index - 1] = false;
                        }
                        index++;
                    }
                    else
                    {
                        _DigitalOutput[_index - 1] = false;
                        index = 0;
                        _DigitalOutput[KitchenCenterIoDevices.indDigitalOutputWindowBoardEastDown] = true;
                        ToggleLightWindowBoardEastDown = true;
                    }

                    _lastindex = index;
                    break;

                case KitchenStep.eFrontLights:
                    NextKitchenStep_ = KitchenStep.eCabinetFrontLights;
                    int FirstFrontLight = KitchenCenterIoDevices.indDigitalOutputFrontLight_1;
                    int LastFrontLight = KitchenCenterIoDevices.indDigitalOutputFrontLight_3;
                    int IndexLightFrontSide = FirstFrontLight + IndexFS;

                    _lastindex = LastFrontLight;
                    _index = FirstFrontLight;

                    if (_SingleOffDone)
                    {
                        if (IndexLightFrontSide <= LastFrontLight)
                        {
                            _SingleOffDone = false;
                            break;
                        }
                    }

                    if (IndexLightFrontSide > LastFrontLight)
                    {
                        for (int i = FirstFrontLight; i <= LastFrontLight; i++)
                        {
                            _DigitalOutput[i] = false;
                        }
                        IndexFS = 0;
                        if (_SingleOffDone)
                        {
                            _SingleOffDone = false;
                            ToggleLightGroups = false;
                        }
                        break;
                    }
                    _DigitalOutput[IndexLightFrontSide] = true;
                    IndexFS++;
                    break;

                case KitchenStep.eCabinetFrontLights:
                    NextKitchenStep_ = KitchenStep.eAll;

                    if (_SingleOffDone)
                    {
                        _SingleOffDone = false;
                        _DigitalOutput[KitchenCenterIoDevices.indDigitalOutputFrontLight_1] = false;
                        _DigitalOutput[KitchenCenterIoDevices.indDigitalOutputFrontLight_2] = false;
                        _DigitalOutput[KitchenCenterIoDevices.indDigitalOutputFrontLight_3] = false;
                        _DigitalOutput[KitchenCenterIoDevices.indDigitalOutputKitchenKabinet] = false;
                        ToggleLightGroups = false;
                        _lastindex = 0;
                        return;
                    }

                    if (!ToggleLightGroups)
                    {
                        _DigitalOutput[KitchenCenterIoDevices.indDigitalOutputFrontLight_1] = true;
                        _DigitalOutput[KitchenCenterIoDevices.indDigitalOutputFrontLight_2] = true;
                        _DigitalOutput[KitchenCenterIoDevices.indDigitalOutputFrontLight_3] = true;
                        _DigitalOutput[KitchenCenterIoDevices.indDigitalOutputKitchenKabinet] = true;
                        ToggleLightGroups = true;
                    }
                    else
                    {
                        _DigitalOutput[KitchenCenterIoDevices.indDigitalOutputFrontLight_1] = false;
                        _DigitalOutput[KitchenCenterIoDevices.indDigitalOutputFrontLight_2] = false;
                        _DigitalOutput[KitchenCenterIoDevices.indDigitalOutputFrontLight_3] = false;
                        _DigitalOutput[KitchenCenterIoDevices.indDigitalOutputKitchenKabinet] = false;
                        ToggleLightGroups = false;
                    }
                    break;

                case KitchenStep.eAll:
                    NextKitchenStep_ = KitchenStep.eSlots;
                    _index = startindex;
                    _lastindex = _indexlastdevice;

                    if (_SingleOffDone)
                    {
                        TurnAllLightsOff( startindex, _indexlastdevice );
                        index = 0;
                        _SingleOffDone = false;
                        ToggleLightGroups = false;
                        break;
                    }
                    if (!ToggleLightGroups)
                    {
                        AllLightsOn( startindex, _indexlastdevice );
                        ToggleLightGroups = true;
                    }
                    else
                    {
                        TurnAllLightsOff( startindex, _indexlastdevice );
                        ToggleLightGroups = false;
                    }

                    _lastindex = _indexlastdevice;
                    break;

                case KitchenStep.eSlots:
                    NextKitchenStep_ = KitchenStep.eNext;
                    if (_SingleOffDone)
                    {
                        _SingleOffDone = false;
                        _DigitalOutput[KitchenCenterIoDevices.indDigitalOutputFumeHood] = false;
                        _DigitalOutput[KitchenCenterIoDevices.indDigitalOutputSlot] = false;
                        _lastindex = 0;
                        return;
                    }
                    if (!ToggleLightGroups)
                    {
                        _DigitalOutput[KitchenCenterIoDevices.indDigitalOutputFumeHood] = true;
                        _DigitalOutput[KitchenCenterIoDevices.indDigitalOutputSlot] = true;
                        ToggleLightGroups = true;
                    }
                    else
                    {
                        _DigitalOutput[KitchenCenterIoDevices.indDigitalOutputFumeHood] = false;
                        _DigitalOutput[KitchenCenterIoDevices.indDigitalOutputSlot] = false;
                        ToggleLightGroups = false;
                    }
                    _lastindex = KitchenCenterIoDevices.indDigitalOutputSlot;
                    break;
            }
            LastKitchenStep_ = ActualKitchenStep_;
            DoUpDateIO( _DigitalOutput );
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
