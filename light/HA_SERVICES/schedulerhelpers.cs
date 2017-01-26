using System;
using System.Collections.Generic;
using Communication.HAProtocoll;


namespace light_visu_classic
{
    static public class SComand
    {
        public const string Start        = "Start";
        public const string Stop         = "Stop";
        public const string Idle         = "Idle";
        public const string FromNow      = "FromNow";
        public const string DefaultTime  = "00:00:00";
    }

    public class DayTime
    {
        public int    Number                { get; set; }
        public bool   IsStopTime            { get; set; }
        public int    Hour                  { get; set; }
        public int    Minute                { get; set; }
        public int    Second                { get; set; }
        public string WhichComandedTime     { get; set; }
        public string Time                  { get; set; }
        public string Days                  { get; set; }
        public string Comand                { get; set; }  // used for starting / stopping / idling scheduler
        public string SchedulerJobID        { get; set; }  // increasing counter each time pair
    }

    // more comfortable using static member
    public class SchedulerControl
    {
        public static List<DayTime> Day_Time;
    }

    // used for apply as eventargument
    public class SchedulerControl_
    {
        List<DayTime> _Day_Time;
        public List<DayTime> Day_Time
        {
            get
            {
                return ( _Day_Time );
            }
            set
            {
                _Day_Time = value;
            }
        }

        // TODO - COMPLETE!
        public string PrepareComandString( int JobId, string comando )
        {
            string comandstring = "";

            if( JobId > 0 )
            {
                int StartIndexSchedulerData = ( JobId * 2 ) - 2; // f.e. index 0 ist STARTTIME, index 1 ist STOPTIME and so on ...
                if( _Day_Time != null && _Day_Time.Count > 0 )
                {
                    if( (StartIndexSchedulerData + 1) < _Day_Time.Count )
                    {
                        comandstring += JobId.ToString();
                        comandstring += MessageBuilder.Seperator;
                        comandstring += comando;                                        // START / STOP
                        comandstring += MessageBuilder.Seperator;
                        comandstring += _Day_Time[StartIndexSchedulerData].Time;        // STARTTIME
                        comandstring += MessageBuilder.Seperator;
                        comandstring += _Day_Time[StartIndexSchedulerData + 1].Time;    // STOPTIME
                        comandstring += MessageBuilder.Seperator;
                        if( !String.IsNullOrWhiteSpace( _Day_Time[StartIndexSchedulerData + 1].Days ) )
                        {
                            comandstring += _Day_Time[StartIndexSchedulerData + 1].Days;    // EXECUTING DAYS
                        }
                        else
                        {
                            comandstring += SComand.FromNow;
                        }
                    }
                }
            }

            return ( comandstring );
        }
    }
}
