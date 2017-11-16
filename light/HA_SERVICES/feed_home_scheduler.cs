using System;

namespace Scheduler
{
    // supplies scheduler with data
    [Serializable()]
    public class FeedData
    {
        public string Device     { get; set; }
        public string JobId      { get; set; }
        public string Command    { get; set; }
        public string Starttime  { get; set; }
//        public string Stoptime   { get; set; }
        public string Days       { get; set; }
    }
}
