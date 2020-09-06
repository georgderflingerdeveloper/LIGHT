using System.Timers;


namespace BASIC_CONTROL_LOGIC
{
    class ToggleButtonController : System.IDisposable
    {
        const uint     INITIAL_COUNT_VALUE          = 1;
        const uint     DEFAULT_REQUIRED_COUNT_VALUE = 2;
        const uint     DEFAULT_TIME_WINDOW          = 500;
        uint           _ToggleCounter;
        uint           _countsrequired;
        bool           _toggleflag;
        double         _timewindow;
        Timer          _OperateWithinWindowTimer;
        public delegate void Toggle_( object sender, bool value );
        public event         Toggle_ EToggle_;

        public ToggleButtonController( )
        {
            _countsrequired = DEFAULT_REQUIRED_COUNT_VALUE;
            _timewindow     = DEFAULT_TIME_WINDOW;
            Constructor( );
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public ToggleButtonController( uint countsrequired, double timewindow )
        {
            _countsrequired = countsrequired;
            _timewindow = timewindow;
            Constructor( );
        }

        #region PROPERTIES
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public uint Countsrequired
        {
            get
            {
                return _countsrequired;
            }

            set
            {
                _countsrequired = value;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public double Timewindow
        {
            get
            {
                return _timewindow;
            }

            set
            {
                _timewindow = value;
            }
        }
        #endregion

        #region PRIVATE_METHODS
        void Constructor( )
        {
            _OperateWithinWindowTimer = new Timer( _timewindow );
            _OperateWithinWindowTimer.Elapsed += _OperateWithinWindowTimer_Elapsed;
            _ToggleCounter = INITIAL_COUNT_VALUE;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        void DeviceToggleOnCounts_( uint countsrequired, double timewindow )
        {
            if( timewindow > 0.0 )
            {
                _OperateWithinWindowTimer.Interval = timewindow;
                _OperateWithinWindowTimer?.Start( );
            }
            else
            {
                return;
            }

            if( 0 == ( _ToggleCounter % countsrequired ) )
            {
                _ToggleCounter = INITIAL_COUNT_VALUE;
                _OperateWithinWindowTimer?.Stop( );

                if( !_toggleflag )
                {
                    EToggle_?.Invoke( this, true );
                }
                else
                {
                    EToggle_?.Invoke( this, false );
                }

                _toggleflag = !_toggleflag;
                return;
            }
            _ToggleCounter++;
        }
        #endregion

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public void DeviceToggleOnCounts( )
        {
            DeviceToggleOnCounts_( _countsrequired, _timewindow );
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public void DeviceToggleOnCounts( uint countsrequired, double timewindow )
        {
            DeviceToggleOnCounts_( countsrequired, timewindow );
        }

        #region EVENTHANDLERS
        private void _OperateWithinWindowTimer_Elapsed( object sender, ElapsedEventArgs e )
        {
            _ToggleCounter = INITIAL_COUNT_VALUE;
            _OperateWithinWindowTimer?.Stop( );
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "_OperateWithinWindowTimer")]
        public void Dispose()
        {
            throw new System.NotImplementedException();
        }
        #endregion
    }
}
