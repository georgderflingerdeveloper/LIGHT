using Phidgets;
using System.Timers;
using HomeAutomation.HardConfig;

namespace Auxiliary
{
    class Effects
    {
        InterfaceKit _iocard;
        Timer EffectTimer;
        public Effects ( InterfaceKit iocard )
        {
            _iocard = iocard;
            if( _iocard == null )
                throw new System.ArgumentNullException( "Parameter cannot be null", "_iocard" );
            if( !_iocard.Attached )
                throw new System.ArgumentException( " IO card not attached ", "_iocard.Attached" );
            EffectTimer = new Timer( Parameters.WalkIntervallTime );
            EffectTimer.Elapsed += EffectTimer_Elapsed;
        }

        int ind = 0;
        void EffectTimer_Elapsed ( object sender, ElapsedEventArgs e )
        {
            if( ind > 0 )
            {
                _iocard.outputs[ind - 1] = false;
            }
            if( ind >= _iocard.outputs.Count )
            {
                ind = 0;
            }
            _iocard.outputs[ind++] = true;
        }

        public void StartWalk ( )
        {
            EffectTimer.Start( );
        }

        public void StartWalk ( double timenextdevice )
        {
            EffectTimer.Interval = timenextdevice;
            EffectTimer.Start( );
        }

        public void StopWalk ( )
        {
            EffectTimer.Interval = Parameters.WalkIntervallTime;
            EffectTimer.Stop( );
        }
    }
}
