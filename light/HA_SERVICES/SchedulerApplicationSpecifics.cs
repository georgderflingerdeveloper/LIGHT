using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Xml.Serialization;
using SystemServices;
using HomeAutomation.HardConfig_Collected;

#region ApplicationSpecificUsing
using light_visu_classic;
#endregion

namespace Scheduler
{
    static class SchedulerApplication
    {
        static string Device;
        static string JobId_;
        static bool JobIsPaused = false;
        static string PreviousJobName = "";
        public delegate void JobDataChange( FeedData data );
        public static event JobDataChange EAnyJobChange;
        static bool _DataRecovered;

        public static bool DataRecovered
        {
            set
            {
                _DataRecovered = value;
            }
        }

        static public void Worker( object sender, FeedData e, ref FeedData PrevSchedulerData, ref Home_scheduler scheduler )
        {
            string JobName = e.Device + "_" + e.JobId;
            Device = e.Device;
            JobId_ = e.JobId;

            if (
                ( PrevSchedulerData.Command != e.Command ) ||
                ( PrevSchedulerData.Days != e.Days ) ||
                ( PrevSchedulerData.Device != e.Device ) ||
                ( PrevSchedulerData.JobId != e.JobId ) ||
                ( PrevSchedulerData.Starttime != e.Starttime ) ||
                ( PrevSchedulerData.Stoptime != e.Stoptime )
              )
            {
                // prevent unecessary saving of the same contens
                if (_DataRecovered)
                {
                    EAnyJobChange?.Invoke( e );
                }
            }

            // we got a new job ID
            if (PrevSchedulerData.JobId != e.JobId)
            {
                if (e.Days == SComand.FromNow)
                {
                    scheduler.NewJob( JobName, new Params( e.Starttime, e.Stoptime ) );
                }
                else
                {
                    scheduler.NewJob( JobName, new Params( e.Starttime, e.Stoptime, e.Days ) );
                }
                scheduler.StartJob( );
            }
            else
            {
                // any time setting changed - reschedule
                if (PrevSchedulerData.Starttime != e.Starttime || PrevSchedulerData.Stoptime != e.Stoptime)
                {
                    scheduler.RemoveJob( JobName );
                    if (e.Days == SComand.FromNow)
                    {
                        scheduler.NewJob( JobName, new Params( e.Starttime, e.Stoptime ) );
                    }
                    else
                    {
                        scheduler.NewJob( JobName, new Params( e.Starttime, e.Stoptime, e.Days ) );
                    }
                    scheduler.StartJob( );
                    if (PreviousJobName == JobName)
                    {
                        Console.WriteLine( TimeUtil.GetTimestamp( ) + Seperators.WhiteSpace + InfoString.SchedulerIsRescheduling + PrevSchedulerData.Starttime + "=>" + e.Starttime );
                        Console.WriteLine( TimeUtil.GetTimestamp( ) + Seperators.WhiteSpace + InfoString.SchedulerIsRescheduling + PrevSchedulerData.Stoptime + "=>" + e.Stoptime );
                    }
                    else
                    {
                        PreviousJobName = JobName;
                    }
                }
            }

            // STOP means pause a job
            if (e.Command == SComand.Stop)
            {
                scheduler.PauseJob( JobName );
                JobIsPaused = true;
            }

            // starts again
            if (e.Command == SComand.Start)
            {
                if (JobIsPaused)
                {
                    scheduler.StartPausedJob( JobName );
                    JobIsPaused = false;
                }
            }

            // CREATE a deep copy of feed object!
            using (MemoryStream ms = new MemoryStream( ))
            {
                BinaryFormatter fmt = new BinaryFormatter( );

                // Original serialisieren:
                fmt.Serialize( ms, e );

                // Position des Streams auf den Anfang zurücksetzen:
                ms.Position = 0;

                // Kopie erstellen:
                PrevSchedulerData = fmt.Deserialize( ms ) as FeedData;
                return;
            }
        }

        static FeedData PrevData = new FeedData( );
        static public void Worker( object sender, FeedData e, ref Home_scheduler scheduler )
        {
            string JobName = e.Device + "_" + e.JobId;
            Device = e.Device;
            JobId_ = e.JobId;

            if (
                ( PrevData.Command != e.Command ) ||
                ( PrevData.Days != e.Days ) ||
                ( PrevData.Device != e.Device ) ||
                ( PrevData.JobId != e.JobId ) ||
                ( PrevData.Starttime != e.Starttime ) ||
                ( PrevData.Stoptime != e.Stoptime )
              )
            {
                // prevent unecessary saving of the same contens
                if (_DataRecovered)
                {
                    EAnyJobChange?.Invoke( e );
                }
            }

            // we got a new job ID
            if (PrevData.JobId != e.JobId)
            {
                if (e.Days == SComand.FromNow)
                {
                    scheduler.NewJob( JobName, new Params( e.Starttime, e.Stoptime ) );
                }
                else
                {
                    scheduler.NewJob( JobName, new Params( e.Starttime, e.Stoptime, e.Days ) );
                }
                scheduler.StartJob( );
                PrevData.JobId = e.JobId;
            }
            else
            {
                // any time setting changed - reschedule
                if (PrevData.Starttime != e.Starttime || PrevData.Stoptime != e.Stoptime)
                {
                    scheduler.RemoveJob( JobName );
                    if (e.Days == SComand.FromNow)
                    {
                        scheduler.NewJob( JobName, new Params( e.Starttime, e.Stoptime ) );
                    }
                    else
                    {
                        scheduler.NewJob( JobName, new Params( e.Starttime, e.Stoptime, e.Days ) );
                    }
                    scheduler.StartJob( );
                    if (PreviousJobName == JobName)
                    {
                        Console.WriteLine( TimeUtil.GetTimestamp( ) + Seperators.WhiteSpace + InfoString.SchedulerIsRescheduling + PrevData.Starttime + "=>" + e.Starttime );
                        Console.WriteLine( TimeUtil.GetTimestamp( ) + Seperators.WhiteSpace + InfoString.SchedulerIsRescheduling + PrevData.Stoptime + "=>" + e.Stoptime );
                    }
                    else
                    {
                        PreviousJobName = JobName;
                    }
                }
            }

            // STOP means pause a job
            if (e.Command == SComand.Stop)
            {
                scheduler.PauseJob( JobName );
                JobIsPaused = true;
            }

            // starts again
            if (e.Command == SComand.Start)
            {
                if (JobIsPaused)
                {
                    scheduler.StartPausedJob( JobName );
                    JobIsPaused = false;
                }
            }

            // CREATE a deep copy of feed object!
            using (MemoryStream ms = new MemoryStream( ))
            {
                BinaryFormatter fmt = new BinaryFormatter( );

                // Original serialisieren:
                fmt.Serialize( ms, e );

                // Position des Streams auf den Anfang zurücksetzen:
                ms.Position = 0;

                // Kopie erstellen:
                PrevData = fmt.Deserialize( ms ) as FeedData;
                return;
            }
        }

        static int AskForStartOrStop( decimal counts )
        {
            return ( ( counts % 2 ) == 0 ? SchedulerConstants.StopDevice : SchedulerConstants.StartDevice );
        }

        static public void WriteStatus( string time, Quartz.IJobExecutionContext context, decimal counts )
        {
            if (AskForStartOrStop( counts ) == SchedulerConstants.StartDevice)
            {
                Console.WriteLine( InfoString.SchedulerIsStartingDevice + time + " " + context.JobDetail.Key.Name + " " + context.Trigger.Key.Name + " " + counts.ToString( ) );
            }

            if (AskForStartOrStop( counts ) == SchedulerConstants.StopDevice)
            {
                Console.WriteLine( InfoString.SchedulerIsStopingDevice + time + " " + context.JobDetail.Key.Name + " " + context.Trigger.Key.Name + " " + counts.ToString( ) );
            }
        }
    }

    class SchedulerDataRecovery
    {
        List<FeedData> SchedFeedData;
        string _directory = "";

        public delegate void RecoverJobs( FeedData e );
        public event RecoverJobs ERecover;
        public event EventHandler ERecovered = delegate { };
        event EventHandler EHelper = delegate { };
        string PrevJobID;
        public SchedulerDataRecovery( string directory )
        {
            SchedFeedData = new List<FeedData>( );
            _directory = directory;
            SchedulerApplication.EAnyJobChange += SchedulerApplication_EAnyJobChange;
        }

        void SchedulerApplication_EAnyJobChange( FeedData e )
        {
            DetectDataChange( e );
        }

        public void RecoverScheduler( string directory, string device )
        {
            List<FeedData> RecoveredData = Recover( directory, device );
            if (RecoveredData != null)
            {
                foreach (var elements in RecoveredData)
                {
                    ERecover?.Invoke( elements );
                }
                ERecovered?.Invoke( null, EventArgs.Empty );
            }
        }

        void DetectDataChange( FeedData feed )
        {
            bool GotNewJob = false;
            if (PrevJobID != feed.JobId)
            {
                GotNewJob = true;
                PrevJobID = feed.JobId;
            }
            Store( feed, GotNewJob );
        }

        private List<FeedData> Recover( string directory, string device )
        {
            try
            {
                XmlSerializer ser = new XmlSerializer( typeof( List<FeedData> ) );
                string currentDir = Environment.CurrentDirectory;
                StreamReader sr = new StreamReader( @directory + GeneralConstants.SlashUsedInLinux + device + FileExtensions.StoredDataExtension );
                List<FeedData> FeedData_ = ( List<FeedData> ) ser.Deserialize( sr );
                sr.Close( );
                SchedFeedData?.Clear( );
                return FeedData_;
            }
            catch (Exception ex)
            {
                Console.WriteLine( TimeUtil.GetTimestamp( ) + " " + InfoString.FailedToRecoverSchedulerData );
                Services.TraceMessage_( ex.Message );
                ERecovered?.Invoke( null, EventArgs.Empty ); // fake data recovered to allow new saving
                return null;
            }
        }

        public void Store( FeedData data, bool GotNewJob )
        {
            try
            {
                if (SchedFeedData != null)
                {
                    if (GotNewJob)
                    {
                        SchedFeedData.Add( data );
                    }
                    else
                    {
                        int index = Convert.ToInt32( data.JobId ) - 1;
                        if (( index >= 0 ) && ( index < SchedFeedData.Count ))
                        {
                            SchedFeedData[index] = data;
                        }
                    }
                    XmlSerializer ser = new XmlSerializer( typeof( List<FeedData> ) );
                    string currentDir = Environment.CurrentDirectory;
                    FileStream str = new FileStream( @currentDir + GeneralConstants.SlashUsedInLinux + data.Device + FileExtensions.StoredDataExtension, FileMode.Create );
                    ser.Serialize( str, SchedFeedData );
                    str.Close( );
                }
            }
            catch (Exception ex)
            {
                Services.TraceMessage_( ex.Message, "Data save error" );
            }
        }
    }
}
