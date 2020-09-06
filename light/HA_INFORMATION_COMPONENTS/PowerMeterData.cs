using System;
using SystemServices;
namespace Equipment
{
    public static class Message
    {
        public const string DataStoringFailed                 = "Data storing failed!";
        public const string DataReadingFailed                 = "Data reading failed!";
        public const string DataDeletingFailed                = "Data deleting failed!";
    }
    static class EnergyConstants
    {
        public const int    CountsPerKWH               = 1000;
        public const int    DefaultDataStoreIntervall  = 3600;
        public const int    DefaultDatCaptureIntervall = 600;
        public const string DefaultDirectory           = @"/HA/";
        public const string DefaultFileName            = "energy";
        public const string FileTyp                    = ".xml";
    }

    [Serializable]
    public class EnergyDataSet
    {
        public DateTime TimeStamp                     { get; set; }
        public decimal Index                          { get; set; }
        public decimal DisplayTotalEnergy             { get; set; }
        public decimal DisplayTotalEnergyCount        { get; set; }
        public decimal DisplayTotalEnergyCountDay     { get; set; }
        public decimal DisplayActualEnergy            { get; set; }
        public decimal DisplayActualEnergyCount       { get; set; }
    }

    [Serializable]
    static class PowerConfigurationDataSet
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static double DataStoreIntervall   { get; set; }   = TimeConverter.ToMiliseconds( EnergyConstants.DefaultDataStoreIntervall );       // seconds
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static double DataCaptureIntervall { get; set; }   = TimeConverter.ToMiliseconds( EnergyConstants.DefaultDatCaptureIntervall );      // seconds
    }
}
