using System.Timers;

namespace HomeAutomation
{
    namespace Controls
    {
        class UnivPWM
        {
            #region DECLARATION
            double          pwmtimeon_, pwmtimeoff_;
            Timer           PWMTimerOn;
            Timer           PWMTimerOff;
            decimal         _OnCounter;
            decimal         _presetcounts;
            bool            _stopwithIsOn;
            ePWMStatus      PwmStatus;
            public enum ePWMStatus
            {
                eIsOn,
                eIsOff,
                eInactive
            };
            public delegate void PWM ( object sender, ePWMStatus pwmstatus );
            public event         PWM PWM_;
            #endregion

            #region PROPERTIES
            public decimal OnCounter
            {
                get
                {
                    return _OnCounter;
                }
            }

            public double PwmTimeOn
            {
                set
                {
                    if( PWMTimerOn != null )
                    {
                        if( value > 0 )
                        {
                            PWMTimerOn.Interval = value;
                        }
                    }
                }
            }

            public double PwmTimeOff
            {
                set
                {
                    if( PWMTimerOff != null )
                    {
                        if( value > 0 )
                        {
                            PWMTimerOff.Interval = value;
                        }
                    }
                }
            }
            #endregion

            #region CONSTRUCTOR
            public UnivPWM ( double timeon, double timeoff )      // starts with high signal
            {
                Init( timeon, timeoff );
                PwmStatus = ePWMStatus.eInactive;
            }
            public UnivPWM( double timeon, double timeoff, bool stopwithIsOn )   // starts with low signal
            {
                Init( timeon, timeoff );
                PwmStatus = ePWMStatus.eInactive;
                _stopwithIsOn = stopwithIsOn;
            }
            #endregion

            #region PUBLIC_METHODS
            public void Start( )
            {
                start( );
            }

            public void Start( decimal presetcounts ) // starts a predefined number of on / off counts
            {
                start( );
                _OnCounter = 0;
                _presetcounts = presetcounts;
            }

            public void Stop( )
            {
                pwmstop( );
                _OnCounter = 0;
            }

            public void Restart( )
            {
                stoptimers( );
                Start( );
            }
            #endregion

            #region PRIVATE_METHODS
            void Init( double timeon, double timeoff )
            {
                pwmtimeon_  = timeon;
                pwmtimeoff_ = timeoff;
                PWMTimerOn  = new Timer( timeon );
                PWMTimerOff = new Timer( timeoff );
                PWMTimerOff.Elapsed += PWMTimerOff_Elapsed;
                PWMTimerOn.Elapsed  += PWMTimerOn_Elapsed;
                _OnCounter = 0;
            }

            void start( )
            {
                PWMTimerOn.Start( );
                PwmStatus = ePWMStatus.eIsOn;
                PWM_?.Invoke(this, PwmStatus);
                _OnCounter = 0;
            }

            void StopOnReachedCounts( decimal presetcounts )
            {
                 if( _OnCounter > presetcounts-1 )
                 {
                     pwmstop( );
                     _OnCounter = 0;
                 }
            }

            void pwmstop( )
            {
                PwmStatus = ePWMStatus.eInactive;
                PWM_?.Invoke(this, PwmStatus);
                stoptimers( );
            }

            void stoptimers( )
            {
                 PWMTimerOn.Stop( );
                 PWMTimerOff.Stop( );
            }
            #endregion

            #region EVENT_HANDLERS
            void PWMTimerOn_Elapsed ( object sender, ElapsedEventArgs e )
            {
                PWMTimerOff.Start( );
                PWMTimerOn.Stop( );
                PwmStatus = ePWMStatus.eIsOff;
                if( _stopwithIsOn )
                {
                    if( _presetcounts > 0 )
                    {
                        StopOnReachedCounts( _presetcounts );
                    }
                }
                PWM_?.Invoke(this, PwmStatus);
            }

            void PWMTimerOff_Elapsed ( object sender, ElapsedEventArgs e )
            {
                PWMTimerOn.Start( );
                PWMTimerOff.Stop( );
                
                PwmStatus = ePWMStatus.eIsOn;
                if( _presetcounts > 0 )
                {
                    StopOnReachedCounts( _presetcounts );
                }
                _OnCounter++;
                PWM_?.Invoke(this, PwmStatus);
            }
            #endregion
        }
    }
}
