using System.Timers;

namespace BASIC_COMPONENTS
{
    public interface ITimer
    {
        void Start( );
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Stop")]
        void Stop( );
        event ElapsedEventHandler Elapsed;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly")]
    public class Timer_ : ITimer, System.IDisposable
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "timer")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1816:CallGCSuppressFinalizeCorrectly")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly")]
        public void Dispose()
        {
            throw new System.NotImplementedException();
        }
    }
}
