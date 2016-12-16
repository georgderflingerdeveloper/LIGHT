using System.Timers;

namespace BASIC_COMPONENTS
{
    public interface ITimer
    {
        void Start( );
        void Stop( );
        event ElapsedEventHandler Elapsed;
    }

    public class Timer_ : ITimer
    {
        private Timer timer = new Timer();

        public Timer_( double interval )
        {
            timer.Interval = interval;
        }

        public void Start( )
        {
            timer.Start( );
        }

        public void Stop( )
        {
            timer.Stop( );
        }

        public event ElapsedEventHandler Elapsed
        {
            add    { timer.Elapsed += value; }
            remove { timer.Elapsed -= value; }
        }
    }
}
