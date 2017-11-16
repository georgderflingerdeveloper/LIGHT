using System;
using System.Collections.Generic;
using Quartz;
using Quartz.Impl;
using SystemServices;

namespace Scheduler
{
    #region DECLARATION_CLASSES
    public static class SchedulerConstants
    {
        public const int StartDevice  = 1;
        public const int StopDevice   = 2;
    }
    static class QuartzApplicationConstants
    {
        public const int    LengthTime            = 8;
        public const int    LenthTimeDisplay      = 2;
        public const int    LastSecondOfOneMinute = 59;
        public const int    LastMinuteOfOneHour   = 59;
        public const int    LastHourOfOneDay      = 24;
        public const string CronPeriodic          = "-";
        public const string CronSartStop          = ",";
        public const string GroupName             = "group";
        public const string Whitespace            = " ";
        public const string CronSpecial           =  CronSartStop;
        public const string TimeComponentFormat   = "{0:00}";
        public const string TimeComponentOneDecPlace = "{0}";
        public const string ZeroTime              = "00:00:00";
        public const string ZeroPartialTime       = "00";
        public const string DefaultTriggerName    = "trigger";
    }

    static class TimeConstants
    {
        public const string Monday     = "Mon";
        public const string Tuesday    = "Tue";
        public const string Wednesday  = "Wed";
        public const string Thursday   = "Thu";
        public const string Friday     = "Fri";
        public const string Saturday   = "Sat";
        public const string Sunday     = "Sun";
        public const string Daily      = "Daily";
        public const string CronDaily  = "*";
    }

    static class QuartzApplicationMessages
    {
        public const string MessageWrongTimeFormat                   = "Given time format is wrong!";
        public const string MessageSchedulerApplicationFailure       = "Failure in scheduler Application!";
    }

    static public class SchedulerInfo
    {
        public enum Status 
        {
            WaitForStart = 0,
            Started      = 1,
            Running      = 2,
            Finished     = 3,
            Paused       = 4,
            Error        = 5,
            JobNotFound  = 6
        }
    }
    #endregion

    #region PARAMETRIZE
    class Params
    {
        public Params( ) { }

        public enum MySchedulerModes
        {
            eNotDefined,
            eOnceADay,
            eStartTimeStopTime,
            eStartTimeStopTimeWithDays,
            eStartEverySecond,
            eStartEveryMinute,
            eStartEveryMinutes,
            eStartEveryHour
        }

        MySchedulerModes _SchedulerModes;
        public MySchedulerModes SchedulerModes
        {
            get
            {
                return _SchedulerModes;
            }
        }

        public Params( MySchedulerModes SchedulerModes )
        {
            _SchedulerModes = SchedulerModes;
            _AppendTriggername = true;
        }

        public Params( MySchedulerModes SchedulerModes, string Minutes )
        {
            _SchedulerModes    = SchedulerModes;
            _EveryMinutes      = Minutes;
            _AppendTriggername = true;
        }

        public Params( string triggername, string starttime, string stoptime, string days )
        {
            _TriggerName       = triggername;
            _Starttime         = starttime;
            _Stoptime          = stoptime;
            _Days              = days;
            _AppendTriggername = false;
        }

        public Params( string starttime, string stoptime, string days )
        {
            _Starttime         = starttime;
            _Stoptime          = stoptime;
            _Days              = days;
            _AppendTriggername = true;
            _SchedulerModes    = MySchedulerModes.eStartTimeStopTimeWithDays;
        }

      //  public Params( string starttime, string stoptime )
      //  {
      //      _Starttime         = starttime;
      //      _Stoptime          = stoptime;
      //      _AppendTriggername = true;
      //      _SchedulerModes    = MySchedulerModes.eStartTimeStopTime;
      //}

        public Params( string starttime, string days )
        {
            _Starttime = starttime;
            _Days = days;
            _AppendTriggername = true;
            _SchedulerModes = MySchedulerModes.eStartTimeStopTimeWithDays;
        }

        bool _TriggerOnceaDay;
        public Params( bool TriggerOnceaDay, string starttime, string triggerdays )
        {
            _Starttime         = starttime;
            _AppendTriggername = true;
            _TriggerOnceaDay   = TriggerOnceaDay;
            if( TriggerOnceaDay )
            {
                _SchedulerModes = MySchedulerModes.eOnceADay;
            }
        }

        public bool TriggerOnceaDay
        {
            get
            {
                return _TriggerOnceaDay;
            }
        }

        bool _AppendTriggername;
        public bool AppendTriggername
        {
            get
            {
                return _AppendTriggername;
            }
        }

        string _TriggerName;
        public  string TriggerName 
        { 
            get
            {
                return _TriggerName;
            }
            set
            {
                _TriggerName = value;
            } 
        }

        string _Starttime;
        public  string Starttime   
        {
            get
            {
                return _Starttime;
            }
            set
            {
                _Starttime = value;
            }
        }

        string _Stoptime;
        public  string Stoptime   
        {
            get
            {
                return _Stoptime;
            }
            set
            {
                _Stoptime = value;
            }
        } 

        string _Days;
        public  string Days       
        {
            get
            {
                return _Days;
            }
            set
            {
                _Days = value;
            }
        }

        string _EveryMinutes;
        public string EveryMinutes
        {
            get
            {
                return _EveryMinutes;
            }
            set
            {
                _EveryMinutes = value;
            }
        }
        decimal  _TriggerCounts;
        public decimal TriggerCounts
        {
            get
            {
                return _TriggerCounts;
            }
            set
            {
                _TriggerCounts = value;
            }
        }
    }
    #endregion

    #region CRONE_UTILS
    // converts a time string into a crone readable format
    // a brief overview of quartz crone syntax can be found under
    // http://www.quartz-scheduler.net/documentation/quartz-2.x/tutorial/crontriggers.html
    static class MyCroneConverter
    {
        static bool _partialformatSecondsOk = true;
        static bool _partialformatMinutesOk = true;
        static bool _partialformatHoursOk   = true;

        public static bool PartialFormatOk
        {
            get
            {
                return ( _partialformatSecondsOk && _partialformatMinutesOk && _partialformatHoursOk );
            }
        }

        public static string TimeToString( int hour, int minute, int second )
        {
            string hour_    = hour.ToString( QuartzApplicationConstants.ZeroPartialTime );
            string minute_  = minute.ToString( QuartzApplicationConstants.ZeroPartialTime );
            string second_  = second.ToString( QuartzApplicationConstants.ZeroPartialTime );

            return ( hour_ + ":" + minute_ + ":" + second_ );
        }

        static bool IsTimeRangeOk( string time, int whichTime )
        {
            if (int.TryParse( time, out int time_ ))
            {
                if (time_ >= 0)
                {
                    if (time_ <= whichTime)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        // STARTTIME
        // 04:30:10 
        // 01234567

        // STOPTIME
        // 04:30:20 
        // 01234567

        // ==>>

        //example  startime 10:00:15, stoptime 10:22:30 => 15,30
        public static string GetSecondIntervall( string StartTime, string StopTime )
        {
            _partialformatSecondsOk = true;
            string PartOfStartTimeSecond = StartTime.Substring( 6, QuartzApplicationConstants.LenthTimeDisplay );
            string PartOfStopTimeSecond  = StopTime.Substring( 6, QuartzApplicationConstants.LenthTimeDisplay );
            if( IsTimeRangeOk( PartOfStartTimeSecond, QuartzApplicationConstants.LastSecondOfOneMinute ) &&
                IsTimeRangeOk( PartOfStopTimeSecond, QuartzApplicationConstants.LastSecondOfOneMinute ) )
            {
                return ( PartOfStartTimeSecond + QuartzApplicationConstants.CronSpecial + PartOfStopTimeSecond );
            }
            else
            {
                _partialformatSecondsOk = false;
                return ( QuartzApplicationConstants.ZeroPartialTime );
            }
        }

        //example  startime 10:10:15, stoptime 10:20:30 => 10,20
        public static string GetMinuteIntervall( string StartTime, string StopTime )
        {
            _partialformatMinutesOk = true;
            string PartOfStartTimeMinute =  StartTime.Substring( 3, QuartzApplicationConstants.LenthTimeDisplay );
            string PartOfStopTimeMinute  =  StopTime.Substring( 3, QuartzApplicationConstants.LenthTimeDisplay );
            if( IsTimeRangeOk( PartOfStartTimeMinute, QuartzApplicationConstants.LastSecondOfOneMinute ) &&
                IsTimeRangeOk( PartOfStopTimeMinute, QuartzApplicationConstants.LastSecondOfOneMinute ) )
            {
                return ( PartOfStartTimeMinute + QuartzApplicationConstants.CronSpecial + PartOfStopTimeMinute );
            }
            else
            {
                _partialformatMinutesOk = false;
                return ( QuartzApplicationConstants.ZeroPartialTime );
            }
        }

        //example  startime 10:10:15, stoptime 14:20:30 => 10,14
        public static string GetHourIntervall( string StartTime, string StopTime )
        {
            _partialformatHoursOk = true;
            string PartOfStartHour =  StartTime.Substring( 0, QuartzApplicationConstants.LenthTimeDisplay );
            string PartOfStopHour  =  StopTime.Substring( 0, QuartzApplicationConstants.LenthTimeDisplay );

            if( IsTimeRangeOk( PartOfStartHour, QuartzApplicationConstants.LastHourOfOneDay ) &&
                IsTimeRangeOk( PartOfStopHour, QuartzApplicationConstants.LastHourOfOneDay ) )
            {
                return ( PartOfStartHour + QuartzApplicationConstants.CronSpecial + PartOfStopHour );
            }
            else
            {
                _partialformatHoursOk = false;
                return ( QuartzApplicationConstants.ZeroPartialTime );
            }
        }

        //attention - this does not work for all situationes
        //example  startime 10:10:15, stoptime 14:20:30 => 15,30 10,20, 10,14 * * ?
        public static string GetTimeString( string StartTime, string StopTime )
        {
            string cronstring ="";
            try
            {
                if( !String.IsNullOrEmpty( StartTime ) && !String.IsNullOrEmpty( StopTime ) )
                {
                    if( ( StartTime.Length == QuartzApplicationConstants.LengthTime ) ||
                        ( StopTime.Length == QuartzApplicationConstants.LengthTime ) )
                    {

                        cronstring = GetSecondIntervall( StartTime, StopTime ) + QuartzApplicationConstants.Whitespace +
                                           GetMinuteIntervall( StartTime, StopTime ) + QuartzApplicationConstants.Whitespace +
                                           GetHourIntervall( StartTime, StopTime ) + QuartzApplicationConstants.Whitespace +
                                           "*" + QuartzApplicationConstants.Whitespace +
                                           "*" + QuartzApplicationConstants.Whitespace +
                                           "?";
                        if( PartialFormatOk )
                        {
                            return ( cronstring );
                        }
                    }
                }
            }
            catch( Exception ex )
            {
                Services.TraceMessage( TimeUtil.GetTimestamp( ) +  ex.Message );
                Console.WriteLine( TimeUtil.GetTimestamp( ) + ex.Data );
            }
            Services.TraceMessage_( QuartzApplicationMessages.MessageWrongTimeFormat );
            return ( QuartzApplicationConstants.ZeroTime );
       }

        public static string GetTimeString( string StartTime, string StopTime, string days )
        {
             string cronstring ="";
             try
             {
                 if( ( StartTime.Length == QuartzApplicationConstants.LengthTime ) ||
                     ( StopTime.Length == QuartzApplicationConstants.LengthTime ) )
                 {

                     cronstring = GetSecondIntervall( StartTime, StopTime )         + QuartzApplicationConstants.Whitespace +
                                  GetMinuteIntervall( StartTime, StopTime )         + QuartzApplicationConstants.Whitespace +
                                  GetHourIntervall( StartTime, StopTime )           + QuartzApplicationConstants.Whitespace +
                                  "?"                                               + QuartzApplicationConstants.Whitespace +
                                  "*"                                               + QuartzApplicationConstants.Whitespace +
                                  days;
                     if( PartialFormatOk )
                     {
                         return ( cronstring );
                     }
                 }
             }
             catch( Exception ex )
             {
                 Services.TraceMessage( TimeUtil.GetTimestamp() +  ex.Message );
                 Console.WriteLine( TimeUtil.GetTimestamp( ) + ex.Data );
             }
             Services.TraceMessage( QuartzApplicationMessages.MessageWrongTimeFormat );
             return ( QuartzApplicationConstants.ZeroTime );
        }

        const int ExpectedTimeElements = 3;

        static bool IsSecondFormatValid( string Seconds )
        {
            uint ConvertedSeconds = Convert.ToUInt16( Seconds );
            if( ConvertedSeconds <= 59 && ConvertedSeconds >= 0 )
            {
                return true;
            }
            return false;
        }

        static bool IsHourFormatValid( string Seconds )
        {
            uint ConvertedSeconds = Convert.ToUInt16( Seconds );
            if (ConvertedSeconds <= 24 && ConvertedSeconds >= 0)
            {
                return true;
            }
            return false;
        }

        static bool IsMinuteFormatValid( string Minutes )
        {
            return ( IsSecondFormatValid(Minutes) );
        }

        public static string GetPointOfTime( string PointOfTime, string days )
        {
            string cronePointOfTime;
            string[] TimeComponents = PointOfTime.Split( ':' );

            string CroneSecond = String.Format( QuartzApplicationConstants.TimeComponentOneDecPlace, Convert.ToInt16(TimeComponents[2]) );
            string CroneMinute = TimeComponents[1];
            string CroneHour   = TimeComponents[0];

            if (
                IsSecondFormatValid( CroneSecond ) && 
                IsMinuteFormatValid( CroneMinute ) &&
                IsHourFormatValid( CroneHour )
               )
            {
                cronePointOfTime = CroneSecond + QuartzApplicationConstants.Whitespace +
                                   CroneMinute + QuartzApplicationConstants.Whitespace +
                                   CroneHour   + QuartzApplicationConstants.Whitespace +
                                           "?" + QuartzApplicationConstants.Whitespace +
                                           "*" + QuartzApplicationConstants.Whitespace +
                                         days;
            }
            else
            {
                cronePointOfTime = QuartzApplicationMessages.MessageWrongTimeFormat;
            }
            return cronePointOfTime;
        }

        //an expression to create a trigger that simply fires every second
        public static string GetStringEverySecond()
        {
            string cronstring = "0/1"  + QuartzApplicationConstants.Whitespace +
                                "*"    + QuartzApplicationConstants.Whitespace +
                                "*"    + QuartzApplicationConstants.Whitespace +
                                "*"    + QuartzApplicationConstants.Whitespace +
                                "*"    + QuartzApplicationConstants.Whitespace +
                                "?";


            return ( cronstring );
        }

        public static string GetStringEveryMinute( )
        {
            string cronstring = "0"      + QuartzApplicationConstants.Whitespace +
                                "0/1"    + QuartzApplicationConstants.Whitespace +
                                "*"      + QuartzApplicationConstants.Whitespace +
                                "*"      + QuartzApplicationConstants.Whitespace +
                                "*"      + QuartzApplicationConstants.Whitespace +
                                "?";


            return ( cronstring );
        }

        public static string GetStringEveryMinute( string minute )
        {
            string cronstring = "0"               + QuartzApplicationConstants.Whitespace +
                                "0"               +
                                "/"               +
                                minute            + QuartzApplicationConstants.Whitespace +
                                "*"               + QuartzApplicationConstants.Whitespace +
                                "*"               + QuartzApplicationConstants.Whitespace +
                                "*"               + QuartzApplicationConstants.Whitespace +
                                "?";
            return ( cronstring );
        }

        public static string GetStringEveryHour( )
        {
            string cronstring = "*"      + QuartzApplicationConstants.Whitespace +
                                "*"      + QuartzApplicationConstants.Whitespace +
                                "0/1"    + QuartzApplicationConstants.Whitespace +
                                "*"      + QuartzApplicationConstants.Whitespace +
                                "*"      + QuartzApplicationConstants.Whitespace +
                                "?";


            return ( cronstring );
        }

    }
    #endregion

    #region SCHEDULER_INTERFACE
    class ScheduledJobs
    {
        public string       JobName           { get; set; }
        public Params       TriggerParameters { get; set; }
        public bool         IsStarted         { get; set; }
    }

    interface IScheduleParams
    {
        List<ScheduledJobs> JobItemsParameters { get; set; }
    }

    class MySchedulerException : Exception
    {
        public MySchedulerException( string message )
            : base( QuartzApplicationMessages.MessageSchedulerApplicationFailure )
        {
            Console.WriteLine( base.Message + "-" + message );
        }
    }
    #endregion

    #region HOME_SCHEDULER
    class Home_scheduler : IScheduleParams
    {
        #region DECLARATIONES
        ISchedulerFactory        schedFact;
        IScheduler               sched;
        List<IJobDetail>         JobList                     = new List<IJobDetail>( );
        List<ITrigger>           TriggerList                 = new List<ITrigger>( );
        List<ScheduledJobs>      JobItemsParametersInternal  = new List<ScheduledJobs>( );
        List<ScheduledJobs>      JobItemsParameters_;
        public delegate void     _Triggered( string time, IJobExecutionContext context, decimal counts );
        public event             _Triggered EvTriggered;
        public delegate void     JobStatus( string job, SchedulerInfo.Status state );
        public event             JobStatus  EJobStatus;
        #endregion

        #region CONSTRUCTOR
        public Home_scheduler( )
        {
            // construct a scheduler factory
            schedFact = new StdSchedulerFactory( );
            ScheduleJob.EvScheduler += SJob_EvScheduler;

            // get a scheduler
            sched = schedFact.GetScheduler( );
            sched.Start( );
            sched.PauseAll( );
        }

        public Home_scheduler( string name )
        {
            var properties = new System.Collections.Specialized.NameValueCollection
            {
                ["quartz.scheduler.instanceName"] = name
            };

            // construct a scheduler factory
            schedFact = new StdSchedulerFactory( properties );
            ScheduleJob.EvScheduler += SJob_EvScheduler;

            // get a scheduler
            sched = schedFact.GetScheduler(  );
            sched.Start( );
            sched.PauseAll( );
        }

        public Home_scheduler( ref List<ScheduledJobs> _JobItemsParameters )
        {
            JobItemsParameters_ = _JobItemsParameters;

            schedFact = new StdSchedulerFactory( );
  
            sched = schedFact.GetScheduler( );
            sched.Start( );

                // initialisation of scheduler
                int jobindex     = 0;
                if( _JobItemsParameters != null )
                {
                    try
                    {
                        foreach( var jobs in _JobItemsParameters )
                        {
                            CreateJobWithTrigger( ref _JobItemsParameters, jobindex );
                            jobindex++;
                        }

                        ScheduleJob.EvScheduler += SJob_EvScheduler;

                        int i = 0;
                        foreach( var Elements in JobList )
                        {
                            sched.ScheduleJob( JobList[i], TriggerList[i]);
                            i++;
                        }

                        // pause all until started
                        sched.PauseAll();
                    }
                    catch( Exception ex )
                    {
                        Services.TraceMessage( TimeUtil.GetTimestamp( ) +  ex.Message );
                        Console.WriteLine( TimeUtil.GetTimestamp( ) + ex.Data );
                    }
               }
        }
        #endregion

        #region PROPERTIES
        public List<ScheduledJobs> JobItemsParameters
        {
            get => ( JobItemsParameters_ );
            set => JobItemsParameters_ = value;
        }
        #endregion

        #region PUBLIC_METHODS
        public void StartPausedJob( string jobname )
        {
            if( JobItemsParameters_ == null || sched == null )
            {
                return;
            }
            int jobnameindex = 0;
            foreach( var jobnames in JobItemsParameters_ )
            {
                if( JobItemsParameters_[jobnameindex].JobName == jobname )
                {
                    sched.ResumeJob( JobList[jobnameindex].Key );
                }
                jobnameindex++;
            }
        }

        public void PauseJob( string jobname )
        {
            if( JobItemsParameters_ == null || sched == null )
            {
                return;
            }
            int jobnameindex = 0;
            foreach( var jobnames in JobItemsParameters_ )
            {
                if( JobItemsParameters_[jobnameindex].JobName == jobname )
                {
                    sched.PauseJob( JobList[jobnameindex].Key );
                }
                jobnameindex++;
            }
        }

        public void StartJob()
        {
            if( sched != null )
            {
                try
                {
                    sched.Start();
                    int jobitemindex = 0;
                    if( JobItemsParametersInternal != null && JobItemsParametersInternal.Count > 0 )
                    {
                        foreach( var parameters in JobItemsParametersInternal )
                        {
                            JobItemsParametersInternal[jobitemindex].IsStarted = true;
                            jobitemindex++;
                        }
                    }
                }
                catch( Exception ex )
                {
                    Services.TraceMessage_( ex.Message );
                }
            }
        }
 
        public void ShutdownJob( bool waitForFinish )
        {
            if( sched != null )
            {
                sched.Shutdown( waitForFinish );
            }
        }

        public void NewJob( string jobname, Params parameters )
        {
            try
            {

                ScheduledJobs SingleJob = new ScheduledJobs
                {
                    JobName = jobname,
                    TriggerParameters = parameters
                };
                if ( SingleJob.TriggerParameters.AppendTriggername )
                {
                    SingleJob.TriggerParameters.TriggerName = jobname + "_" + QuartzApplicationConstants.DefaultTriggerName;
                }
                JobItemsParameters_         = JobItemsParametersInternal;
                JobItemsParameters_.Add( SingleJob );
                int nextJobIndex = JobItemsParameters_.Count - 1;
                CreateJobWithTrigger( ref JobItemsParameters_, nextJobIndex );
                if( (nextJobIndex < JobList.Count) && (nextJobIndex < TriggerList.Count) )
                {
                    sched.ScheduleJob( JobList[nextJobIndex], TriggerList[nextJobIndex] );    // TODO - BUGFIX when creating multiple jobs!
                    ScheduleJob.JobItemsParameters [nextJobIndex].TriggerParameters.TriggerCounts = 0; 
               }
            }
            catch( Exception ex )
            {
                Services.TraceMessage_( ex.Message );
                Console.WriteLine( TimeUtil.GetTimestamp( ) + ex.Data );
            }
        }

        public void RemoveJob( string jobname )
        {
            int jobnameindex = 0;
            try
            {
                foreach( var jobnames in JobItemsParameters_ )
                {
                    if( JobItemsParameters_[jobnameindex].JobName == jobname )
                    {
                        sched.UnscheduleJob( TriggerList[jobnameindex].Key );
                        TriggerList.RemoveAt( jobnameindex );
                        JobList.RemoveAt( jobnameindex );
                        JobItemsParameters_.RemoveAt( jobnameindex );
                        break;
                    }
                    jobnameindex++;
                }
            }
            catch( Exception ex )
            {
                Services.TraceMessage( TimeUtil.GetTimestamp( ) +  ex.Message );
                Console.WriteLine( TimeUtil.GetTimestamp( ) + ex.Data );
            }
        }

        // TODO - so far rescheduling does not work correctly - reason not found - further investigation necessary
        public void ReScheduleJob( string jobname, string starttime, string stoptime, string days )
        {
            string CroneTime = MyCroneConverter.GetTimeString( starttime, stoptime, days );
            string triggername = jobname + QuartzApplicationConstants.DefaultTriggerName;
            ITrigger newtrigger =  TriggerBuilder.Create()
                                                         .WithIdentity( triggername, QuartzApplicationConstants.GroupName )
                                                         .WithCronSchedule( CroneTime )
                                                         .ForJob( jobname, QuartzApplicationConstants.GroupName )
                                                         .Build(); 
            int i = 0;
            try
            {
                foreach( var jobs in JobList )
                {
                    if( TriggerList[i].JobKey.Name == jobname )
                    {
                        sched.RescheduleJob( TriggerList[i].Key, newtrigger);
                        break;
                    }
                    i++;
                }
            }
            catch( Exception ex )
            {
                Services.TraceMessage( TimeUtil.GetTimestamp( ) +  ex.Message );
                Console.WriteLine( TimeUtil.GetTimestamp( ) + ex.Data );
            }
        }

        public void StartAllPausedJobs( )
        {
            sched.ResumeAll( );
        }

        public SchedulerInfo.Status GetJobStatus( string jobname )
        {
           int foundtriggerindex       = 0;
           SchedulerInfo.Status status = SchedulerInfo.Status.WaitForStart;
           TriggerState TriggerState_  = TriggerState.None;
           var CurrentExecutingJobs    = sched.GetCurrentlyExecutingJobs();
           string JobNameAndTimestamp  = TimeUtil.GetTimestamp() + " " + jobname;

           #region CURRENTLY_EXECUTING_JOB
           // any job name exists says that the requested job is running
           foreach( var jobs in CurrentExecutingJobs )
           {
             if( jobname == jobs.JobDetail.Key.Name )
             {
                 TriggerState_ = sched.GetTriggerState( jobs.Trigger.Key );
                 switch( TriggerState_ )
                 {
                     case TriggerState.Normal:
                          status = SchedulerInfo.Status.Running;
                          break;
                     default:
                          status = SchedulerInfo.Status.Error;
                          break;
                 }
                    EJobStatus?.Invoke( JobNameAndTimestamp, status );
                    return ( status );
             }
           }
           #endregion

           foreach( var trigger in TriggerList )
           {
               if( trigger.Key.Name == jobname + "_" + QuartzApplicationConstants.DefaultTriggerName )
               {
                   if( JobItemsParametersInternal != null && JobItemsParametersInternal.Count > 0 )
                   {
                       if( foundtriggerindex < JobItemsParametersInternal.Count )
                       {
                           if( !JobItemsParametersInternal[foundtriggerindex].IsStarted )
                           {
                               status = SchedulerInfo.Status.WaitForStart;
                               EJobStatus?.Invoke( JobNameAndTimestamp, status );
                               return ( status );
                           }
                       }
                   }
                   break;
               }
               foundtriggerindex++;
               if( foundtriggerindex >= TriggerList.Count )
               {
                   status = SchedulerInfo.Status.JobNotFound;
                   EJobStatus?.Invoke( JobNameAndTimestamp, status );
                   return ( status );
               }
           }

           if( TriggerList.Count > 0 )
           {
               TriggerState_ = sched.GetTriggerState( TriggerList[foundtriggerindex].Key );
           }
           else
           {
               status = SchedulerInfo.Status.JobNotFound;
               EJobStatus?.Invoke( JobNameAndTimestamp, status );
               return ( status );
           }

           switch( TriggerState_ )
           {
               case TriggerState.Normal:
                    status = SchedulerInfo.Status.Started;
                    break;
               case TriggerState.Paused:
                    status = SchedulerInfo.Status.Paused;
                    break;
               case TriggerState.Complete:
                    status = SchedulerInfo.Status.Finished;
                    break;
               default:
                   status = SchedulerInfo.Status.Error;
                   break;
           }

           EJobStatus?.Invoke( JobNameAndTimestamp, status );
           return ( status );
        }
        #endregion

        #region PRIVATE_METHODS
        void CreateJobWithTrigger( ref List<ScheduledJobs> _JobItemsParameters, int jobindex )
        {
            if( _JobItemsParameters != null )
            {
                    JobList.Add( JobBuilder.Create<ScheduleJob>( )
                                                     .WithIdentity( _JobItemsParameters[jobindex].JobName, QuartzApplicationConstants.GroupName )
                                                     .Build( ) );
                    ScheduleJob.JobItemsParameters = _JobItemsParameters;

                    string CroneTime = "";
                    string CroneStartTime = "";

                // assembly of crone string
                switch ( _JobItemsParameters[jobindex].TriggerParameters.SchedulerModes )
                    {
                            case Params.MySchedulerModes.eStartTimeStopTimeWithDays:
                                 CroneStartTime = MyCroneConverter.GetPointOfTime( _JobItemsParameters[jobindex].TriggerParameters.Starttime,
                                                                                   _JobItemsParameters[jobindex].TriggerParameters.Days );
                                 break;

                            case Params.MySchedulerModes.eStartTimeStopTime:
                                 // TODO - modify Croneconverter for one single event ....
                                 CroneTime = MyCroneConverter.GetTimeString( _JobItemsParameters[jobindex].TriggerParameters.Starttime,
                                                                             _JobItemsParameters[jobindex].TriggerParameters.Stoptime );
                                 break;

                           case Params.MySchedulerModes.eStartEverySecond:
                                CroneTime = MyCroneConverter.GetStringEverySecond( );
                                break;

                           case Params.MySchedulerModes.eStartEveryMinute:
                                CroneTime = MyCroneConverter.GetStringEveryMinute( );
                                break;

                           case Params.MySchedulerModes.eStartEveryMinutes:
                                CroneTime = MyCroneConverter.GetStringEveryMinute( _JobItemsParameters[jobindex].TriggerParameters.EveryMinutes );
                                break;

                           case Params.MySchedulerModes.eStartEveryHour:
                                CroneTime = MyCroneConverter.GetStringEveryHour( );
                                break;
                    default:
                                break;
                    }
                    if( !MyCroneConverter.PartialFormatOk )
                    {
                        throw new MySchedulerException( QuartzApplicationMessages.MessageWrongTimeFormat );
                    }
                                TriggerList.Add( TriggerBuilder.Create( )
                                                                           .WithIdentity( _JobItemsParameters[jobindex].TriggerParameters.TriggerName
                                                                                          , QuartzApplicationConstants.GroupName )
                                                                           .WithCronSchedule( CroneStartTime )
                                                                           .ForJob( _JobItemsParameters[jobindex].JobName, QuartzApplicationConstants.GroupName )
                                                                           .Build( ) );
            }
        }

        void SJob_EvScheduler( string time, IJobExecutionContext context, decimal counts )
        {
            EvTriggered?.Invoke( time, context, counts );
        }

        class ScheduleJob : IJob
        {
            public delegate void  _ScheduleJob( string time, IJobExecutionContext context, decimal counts );
            public  static  event _ScheduleJob EvScheduler;
            static List<ScheduledJobs>  _JobItemsParameters;
            int jobindex_;

            public static List<ScheduledJobs> JobItemsParameters
            {
                set
                {
                    _JobItemsParameters = value;
                }

                get
                {
                    return( _JobItemsParameters ); 
                }
            }

            public ScheduleJob()
            {
            }

            public ScheduleJob( ref List<ScheduledJobs> JobItemsParameters )
            {
                _JobItemsParameters = JobItemsParameters;
            }

            public void Execute( IJobExecutionContext context )
            {
                if( EvScheduler != null )
                {
                    decimal _counts = 0;
                    if( _JobItemsParameters != null )
                    {
                        int jobindex = 0;
                        foreach( var jobs in _JobItemsParameters )
                        {
                            if( context.JobDetail.Key.Name == _JobItemsParameters[jobindex].JobName )
                            {
                                _counts = ++_JobItemsParameters[jobindex].TriggerParameters.TriggerCounts;
                                jobindex_ = jobindex;
                                break;
                            }
                            jobindex++;
                        }
                    }
                    EvScheduler( TimeUtil.GetTimestamp(), context, _counts );
                }
            }
        }
        #endregion
    }
    #endregion
}
