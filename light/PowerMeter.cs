using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using System.Xml.Serialization;
using SystemServices;

namespace Equipment
{
    class PowerMeter
    {
        #region DECLARATION
        decimal                   _TotalCounts;
        decimal                   _ActualCounts;
        decimal                   _ActualCountsDay;
        decimal                   _Index;
        double                    _DataStoreIntervall;
        double                    _DataCaptureIntervall;
        const decimal             MS_PER_MINUTE = 60 * 1000;
        const decimal             MS_PER_HOUR   = 60 * 60 * 1000;
        const decimal             MS_PER_SECOND = 1000;
        bool                      _CaptureMinuteStoreHour;
        bool                      _StartCapure;
        bool                      _StartStoring;
        List<EnergyDataSet> EnergyList = new List<EnergyDataSet>();
        Timer                     TimerTick   = new Timer( Convert.ToDouble(MS_PER_SECOND) );
        EnergyDataSet             _EnergyData = new EnergyDataSet();
        public delegate void CaptureDone( object sender, DateTime e );
        public event CaptureDone ECaptureDone;
        #endregion

        #region CONSTRUCTOR

        void Constructor( double DataCaptureIntervall, double DataStoreIntervall )
        {
            if ( _CaptureMinuteStoreHour)
            {
                _DataStoreIntervall   = DataStoreIntervall * Convert.ToDouble(MS_PER_SECOND);
                _DataCaptureIntervall = DataCaptureIntervall * Convert.ToDouble( MS_PER_SECOND );
            }
            else
            {
               _DataStoreIntervall   = DataStoreIntervall;
               _DataCaptureIntervall = DataCaptureIntervall;
            }
            _ActualCounts         = 0;
            TimerTick.Elapsed += TimerTick_Elapsed;
        }


        public PowerMeter( )
        {
            _ActualCounts = 0;
            TimerTick.Elapsed += TimerTick_Elapsed;
        }

        public PowerMeter( bool start )
        {
            _ActualCounts = 0;
            TimerTick.Elapsed += TimerTick_Elapsed;
            if ( start )
            {
                StartSync( );
                StartCapture( );
                StartCyclicStoring( );
            }
        }

        public PowerMeter( double DataCaptureIntervall, double DataStoreIntervall )
        {
            Constructor( DataCaptureIntervall, DataStoreIntervall );
        }

        public PowerMeter( bool start,  double DataCaptureIntervall, double DataStoreIntervall )
        {
            Constructor( DataCaptureIntervall, DataStoreIntervall );
            if( start )
            { 
                StartSync( );
                StartCapture( );
                StartCyclicStoring( );
            }
       }

        #endregion

        #region PUBLICMETHODS
        public void PreConfigureEnergyCount( decimal TotalEnergyValue )
        {
            _TotalCounts = TotalEnergyValue * EnergyConstants.CountsPerKWH;
            _EnergyData.DisplayTotalEnergyCount = _TotalCounts;
        }

        private Object thisLock = new Object();

        // will be called every count event
        public void Tick( )
        {
            lock( thisLock )
            {
                _ActualCounts++;
                _TotalCounts++;
                _ActualCountsDay++;
            }
        }

        public void StartCapture()
        {
            _StartCapure = true;
        }

        public void StopCapture( )
        {
            _StartCapure = false;
        }

        public void StartSync()
        {
            TimerTick.Start( );
        }

        public void StopCyclicStoring()
        {
            _StartStoring = false;
        }

        public void StartCyclicStoring( )
        {
            _StartStoring = true;
        }

        public void Store( bool start )
        {
            if ( start )
            {
                Store_( EnergyList );
            }
        }

        public void Reset( )
        {
            _ActualCounts = 0;
        }
        #endregion

        #region PROPERTIES
        public EnergyDataSet EnergyData
        {
            get
            {
                return _EnergyData;
            }
        }

        public bool CaptureMinuteStoreHour
        {
            set
            {
                _CaptureMinuteStoreHour = value;
            }
        }
        #endregion 

        #region PRIVATEMETHODS
        static decimal CalcEnergy( decimal Counts )
        {
            return ( Convert.ToDecimal( Counts /  Convert.ToDecimal( EnergyConstants.CountsPerKWH ) ) );
        }

        void Store_( List<EnergyDataSet> data )
        {
            try
            {
                Directory.GetCurrentDirectory( );
                if( !Directory.Exists( EnergyConstants.DefaultDirectory ) )
                {
                    Directory.CreateDirectory( EnergyConstants.DefaultDirectory );
                }
                XmlSerializer ser = new XmlSerializer( typeof( List<EnergyDataSet> ) );
                string FileName = EnergyConstants.DefaultFileName + "_" + TimeUtil.GetDate_() + EnergyConstants.FileTyp;
                string DirectoryWithFileName = EnergyConstants.DefaultDirectory + "\\" + FileName;
                FileStream str = new FileStream( DirectoryWithFileName, FileMode.Create );
                ser.Serialize( str, data );
                str.Close( );
            }
            catch( Exception ex )
            {
                Services.TraceMessage_( ex.Message, Message.DataStoringFailed );
            }
        }

        void TimedStartCapture( )
        {
            int EveryNSecondCapture = Convert.ToInt32( _DataCaptureIntervall ) / Convert.ToInt32( MS_PER_SECOND );
            if ( ( (DateTime.Now.Second % EveryNSecondCapture ) == 0 ) )
            {
                Capture( _StartCapure );
            }
        }

        void TimedStartStoring( )
        {
            int EveryNSecondStore = Convert.ToInt32( _DataStoreIntervall ) / Convert.ToInt32( MS_PER_SECOND );
            if ( ( (DateTime.Now.Second % EveryNSecondStore ) == 0 ) )
            {
                Store( _StartStoring );
            }
        }

        // only start at the begining of a minute
        void TimedStartCaptureMinute( )
        {
            if ( ( DateTime.Now.Minute % Convert.ToInt32( _DataCaptureIntervall ) ) == 0 )
            {
                Capture( _StartCapure );
            }
        }

        // only start at the begining of a hour
        void TimedStartStoringHour( )
        {
            // store data every hour
            if ( ( DateTime.Now.Hour % Convert.ToInt32( _DataStoreIntervall ) ) == 0 )
            {
                Store( _StartStoring );
            }
            // one day passed
            if( ( DateTime.Now.Hour % 24 ) == 0 )
            {
                EnergyList.Clear( );
                _ActualCountsDay = 0;
            }
        }

        void Capture( bool start )
        {
            lock ( thisLock )
            {
                if ( start )
                {
                    _EnergyData = new EnergyDataSet( );
                    _EnergyData.Index                        = _Index++;
                    _EnergyData.DisplayTotalEnergyCountDay   = _ActualCountsDay;
                    _EnergyData.TimeStamp                    = DateTime.Now;
                    _EnergyData.DisplayActualEnergyCount     = _ActualCounts;
                    _EnergyData.DisplayActualEnergy          = CalcEnergy( _ActualCounts );
                    _EnergyData.DisplayTotalEnergyCount     += _TotalCounts;
                    _EnergyData.DisplayTotalEnergy           = CalcEnergy( _TotalCounts );

                    if ( _EnergyData != null )
                    {
                        EnergyList.Add( _EnergyData );
                    }
                    _ActualCounts = 0;

                    _EnergyData = null;

                    ECaptureDone?.Invoke( this, DateTime.Now );
                }
            }
        }

        void ClearList()
        {
            if( EnergyList?.Count > 0 )
            {
                EnergyList.Clear( );
            }
        }

        #endregion

        #region EVENTHANDLERS
        private void TimerTick_Elapsed( object sender, ElapsedEventArgs e )
        {
            TimerTick.Start( );

            if( _CaptureMinuteStoreHour )
            {

            }
            else
            {
                TimedStartCapture( );
                TimedStartStoring( );
            }

        }
        #endregion
    }
}
