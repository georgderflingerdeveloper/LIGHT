using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace BaseControlLogic
{
    static class Trigger
    {
        public const bool FALLING = false;
        public const bool RISING  = true;
    }

    class devicestepcontrol
    {
        #region DECLARATION
        uint   _numberofdevices;
        double _timeNext;
        Timer  _TimerNextDevice;
        uint _number;
        bool _value;

        public delegate void Step( uint number, bool value );
        public event Step EStep;
        #endregion


        #region CONSTRUCTOR
        public devicestepcontrol( uint numberofdevices, double timeNext )
        {
            _numberofdevices = numberofdevices;
            _timeNext        = timeNext;
            _TimerNextDevice = new Timer( timeNext );
            _TimerNextDevice.Elapsed += _TimerNextDevice_Elapsed;
        }
        #endregion

        #region PRIVATE_METHODS
        void Watcher( bool trigger )
        {
            switch( trigger )
            {
                case Trigger.RISING:
                     _TimerNextDevice.Start( );
                     break;

                case Trigger.FALLING:
                     _TimerNextDevice.Stop( );
                     _value = !_value;
                     EStep?.Invoke( _number, _value );
                     break;

                default:
                     break;
            }
        }
        #endregion

        #region PUBLIC_METHODS
        public void WatchForInputValueChange( bool trigger )
        {
            Watcher( trigger );
        }

        public void Reset()
        {
            _number = 0;
            _value = false;
            _TimerNextDevice.Stop( );
        }
        #endregion

        #region PROPERTIES
        public uint Number
        {
            get
            {
                return _number;
            }

            set
            {
                _number = value;
            }
        }

        public bool Value
        {
            get
            {
                return _value;
            }

            set
            {
                _value = value;
            }
        }
        #endregion

        #region EVENTHANDLERS
        private void _TimerNextDevice_Elapsed( object sender, ElapsedEventArgs e )
        {
            _value = true;
            if( _number < _numberofdevices )
            {
                _number++;
            }else
            {
                _number = 0; 
            }
            EStep?.Invoke( _number, _value );
        }
        #endregion
    }
}
