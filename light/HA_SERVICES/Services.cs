﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.Threading;


namespace SystemServices
{
    // pool of common usefull methods
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    class Services
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Int32.ToString")]
        public static void TraceMessage( string message,
                                 [System.Runtime.CompilerServices.CallerMemberName] string memberName    = "",
                                 [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath  = "",
                                 [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0 )
        {
            System.Diagnostics.Trace.WriteLine( "message: " + message );
            System.Diagnostics.Trace.WriteLine( "member name: " + memberName );
            System.Diagnostics.Trace.WriteLine( "source file path: " + sourceFilePath );
            System.Diagnostics.Trace.WriteLine( "source line number: " + sourceLineNumber );
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Int32.ToString")]
        public static void TraceMessage_( string message,
                                 [System.Runtime.CompilerServices.CallerMemberName] string memberName    = "",
                                 [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath  = "",
                                 [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0 )
        {
            System.Diagnostics.Trace.WriteLine( TimeUtil.GetTimestamp()  + " " + "message: " + message );
            System.Diagnostics.Trace.WriteLine( TimeUtil.GetTimestamp( ) + " " + "member name: " + memberName );
            System.Diagnostics.Trace.WriteLine( TimeUtil.GetTimestamp( ) + " " + "source file path: " + sourceFilePath );
            System.Diagnostics.Trace.WriteLine( TimeUtil.GetTimestamp( ) + " " + "source line number: " + sourceLineNumber );
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "pobjMutex")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "System.Console.WriteLine(System.String)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Objekte verwerfen, bevor Bereich verloren geht")]
        public static void RunApplicationOnce( )
        {
            // This will be set to true if this process can own the mutex. 
            bool pobjIOwnMutex = false;

            // Try and get ownership of a mutex who's name is known. 
            Mutex pobjMutex = new Mutex( true, "1a04779c-60ab-40cc-bd19-28f72fbeb2bf", out pobjIOwnMutex );

            // If the mutex is owned by this process, then run the application. 
            if( !pobjIOwnMutex )
            {
                Console.WriteLine( "Application already running ..." );
                Console.ReadLine();
                Environment.Exit( 0 );
            }
        }

        // program information last build
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static DateTime BuildDate
        {
            get { return File.GetLastWriteTime( Assembly.GetExecutingAssembly().Location ); }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static int IPStringToInt( string ipAddress )
        {
            IPAddress address = IPAddress.Parse( ipAddress );
            byte[] asBytes = address.GetAddressBytes();

            if( asBytes.Length != 4 )
            {
                throw new ArgumentException( "IP Address must be an IPv4 address" );
            }

            return BitConverter.ToInt32( asBytes, 0 );
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Convert.ToInt64(System.String)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static string FakeIpForComparison( string ip )
        {
            string fakeip = "";
            string FormattedPart ="";
            string[] IpParts = ip.Split( '.' );

            foreach( string part in IpParts )
            {
                Int64 numpart = Convert.ToInt64( part );
                FormattedPart = String.Format( "{0:000}", numpart );
                fakeip += FormattedPart;
            }
            return ( fakeip );
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    class IPUtil
    {
        /// <summary>
        /// Resolves the host name from the given IP address.
        /// </summary>
        /// <param name="IPAddress">The ip address for getting the host name.</param>
        /// <returns>The host name if exists else "no entry"</returns>
        /// 
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public static String GetHostNameByIp( String IPAddress )
        {
            IPHostEntry Host = null;
            try
            {
                if (System.Net.IPAddress.TryParse( IPAddress, out IPAddress IP ))
                {
                    Host = System.Net.Dns.GetHostEntry( IP );
                }
            }
            catch // swallaw exception ...
            {
                return ( "No entry"  );
            }
            return( Host == null  ? "No entry" : Host.HostName );
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static bool IsValidIp( string addr )
        {
            IPAddress ip;
            bool valid = !string.IsNullOrEmpty( addr ) && IPAddress.TryParse( addr, out ip );
            return valid;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static bool IsMAC( string MAC )
        {
            return Regex.IsMatch( MAC, @"((([a-f]|[0-9]|[A-F]){2}\:){5}([a-f]|[0-9]|[A-F]){2}\b)|((([a-f]|[0-9]|[A-F]){2}\-){5}([a-f]|[0-9]|[A-F]){2}\b)" );
        }

        // f.e. checks wether 123.168.10.x ason... is correct
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static bool IsIPRange( string IpRange )
        {
            bool res = false;
            string range = IpRange;

            if( range.Contains( 'x' ) )
            {
                string newRange = range.Replace( 'x', '0' );
                res = IsValidIp( newRange );
            }

            return ( res );
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    class TimeUtil
    {
        public const string FORMAT_DAY_MONTH_HOUR_MINUTE_SECOND = "{0:00}";
        public const string FORMAT_MS                           = "{0:000}";

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Int32.ToString")]
        public static string GetTimestamp( )
        {
            string timestamp =   String.Format( FORMAT_DAY_MONTH_HOUR_MINUTE_SECOND, DateTime.Now.Day )     + 
                                 String.Format( FORMAT_DAY_MONTH_HOUR_MINUTE_SECOND, DateTime.Now.Month ) +
                                 DateTime.Now.Year.ToString( )   +
                                 ":" + 
                                 String.Format( FORMAT_DAY_MONTH_HOUR_MINUTE_SECOND, DateTime.Now.Hour ) + 
                                 "h" +
                                 String.Format( FORMAT_DAY_MONTH_HOUR_MINUTE_SECOND, DateTime.Now.Minute ) +
                                 "m" +
                                 String.Format( FORMAT_DAY_MONTH_HOUR_MINUTE_SECOND, DateTime.Now.Second ) +
                                 "s" +
                                 String.Format( FORMAT_MS, DateTime.Now.Millisecond )+
                                 "ms";
            return ( timestamp );
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Int32.ToString")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static string GetDate()
        {
            string timestamp =   String.Format( FORMAT_DAY_MONTH_HOUR_MINUTE_SECOND, DateTime.Now.Day ) +
                                 String.Format( FORMAT_DAY_MONTH_HOUR_MINUTE_SECOND, DateTime.Now.Month ) +
                                 DateTime.Now.Year.ToString();
            return ( timestamp );
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Int32.ToString")]
        public static string GetDate_( )
        {
            string timestamp =   String.Format( FORMAT_DAY_MONTH_HOUR_MINUTE_SECOND, DateTime.Now.Day )   + "_" +
                                 String.Format( FORMAT_DAY_MONTH_HOUR_MINUTE_SECOND, DateTime.Now.Month ) + "_" +
                                 DateTime.Now.Year.ToString();
            return ( timestamp );
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static string GetTimeHmSms()
        {
            string timestamp =   String.Format( FORMAT_DAY_MONTH_HOUR_MINUTE_SECOND, DateTime.Now.Hour ) +
                                 "h" +
                                 String.Format( FORMAT_DAY_MONTH_HOUR_MINUTE_SECOND, DateTime.Now.Minute ) +
                                 "m" +
                                 String.Format( FORMAT_DAY_MONTH_HOUR_MINUTE_SECOND, DateTime.Now.Second ) +
                                 "s" +
                                 String.Format( FORMAT_MS, DateTime.Now.Millisecond ) +
                                 "ms";
            return ( timestamp );
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static string GetTimeHmS()
        {
            string timestamp =   String.Format( FORMAT_DAY_MONTH_HOUR_MINUTE_SECOND, DateTime.Now.Hour ) +
                                 "h" +
                                 String.Format( FORMAT_DAY_MONTH_HOUR_MINUTE_SECOND, DateTime.Now.Minute ) +
                                 "m" +
                                 String.Format( FORMAT_DAY_MONTH_HOUR_MINUTE_SECOND, DateTime.Now.Second ) +
                                 "s";
            return ( timestamp );
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static string GetTimeHm()
        {
            string timestamp =   String.Format( FORMAT_DAY_MONTH_HOUR_MINUTE_SECOND, DateTime.Now.Hour ) +
                                 "h" +
                                 String.Format( FORMAT_DAY_MONTH_HOUR_MINUTE_SECOND, DateTime.Now.Minute ) +
                                 "m";
            return ( timestamp );
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static string GetTimeH()
        {
            string timestamp =   String.Format( FORMAT_DAY_MONTH_HOUR_MINUTE_SECOND, DateTime.Now.Hour ) +
                                 "h";
            return ( timestamp );
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Int32.ToString")]
        public static string GetTimestamp_( )
        {
            string timestamp =   String.Format( FORMAT_DAY_MONTH_HOUR_MINUTE_SECOND, DateTime.Now.Day )     +
                                 String.Format( FORMAT_DAY_MONTH_HOUR_MINUTE_SECOND, DateTime.Now.Month ) +
                                 DateTime.Now.Year.ToString( )   +
                                 "_" +
                                 String.Format( FORMAT_DAY_MONTH_HOUR_MINUTE_SECOND, DateTime.Now.Hour ) +
                                 "h" +
                                 String.Format( FORMAT_DAY_MONTH_HOUR_MINUTE_SECOND, DateTime.Now.Minute ) +
                                 "m" +
                                 String.Format( FORMAT_DAY_MONTH_HOUR_MINUTE_SECOND, DateTime.Now.Second ) +
                                 "s" +
                                 String.Format( FORMAT_MS, DateTime.Now.Millisecond )+
                                 "ms";
            return ( timestamp );
        }
    }

    public static class Month
    {
        /// <summary>
        /// Gets the first day of the month.
        /// </summary>
        /// <param name="givenDate">The given date.</param>
        /// <returns>the first day of the month</returns>
        public static DateTime GetFirstDayOfMonth( DateTime givenDate )
        {
            return new DateTime( givenDate.Year, givenDate.Month, 1 );
        }

        /// <summary>
        /// Gets the last day of month.
        /// </summary>
        /// <param name="givenDate">The given date.</param>
        /// <returns>the last day of the month</returns>
        public static DateTime GetTheLastDayOfMonth( DateTime givenDate )
        {
            return GetFirstDayOfMonth( givenDate ).AddMonths( 1 ).Subtract( new TimeSpan( 1, 0, 0, 0, 0 ) );
        }
    }

    static class TimeConverter
    {
        static public double ToMiliseconds( double seconds )
        {
            return ( seconds * 1000 );
        }

        static public double ToMiliseconds( double minutes, double seconds )
        {
            return ( ( minutes * 60 + seconds ) * 1000 );
        }

        static public double ToMiliseconds( double hours, double minutes, double seconds )
        {
            return ( ( hours * 3600 + minutes * 60 + seconds ) * 1000 );
        }
    }
    // program information last build
    static class BuildInformation
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static DateTime BuildDate
        {
            get { return File.GetLastWriteTime( Assembly.GetExecutingAssembly().Location ); }
        }
    }

    static class Memory
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static bool CompareStreams( MemoryStream ms1, MemoryStream ms2 )
        {
            if( ms1.Length != ms2.Length )
            {
                return false;
            }
            ms1.Position = 0;
            ms2.Position = 0;

            var msArray1 = ms1.ToArray( );
            var msArray2 = ms2.ToArray( );

            bool result = msArray1.SequenceEqual( msArray2 );
            return result;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Objekte verwerfen, bevor Bereich verloren geht")]
        public static bool CompareObjects( object o1, object o2 )
        {
           BinaryFormatter fmt1 = new BinaryFormatter( );
           MemoryStream ms1 = new MemoryStream( );
            // Original serialisieren:
           fmt1.Serialize( ms1, o1 );
           // Position des Streams auf den Anfang zurücksetzen:
           ms1.Position = 0;

           BinaryFormatter fmt2 = new BinaryFormatter( );
           MemoryStream ms2 = new MemoryStream( );
           // Original serialisieren:
           fmt2.Serialize( ms2, o2 );
           // Position des Streams auf den Anfang zurücksetzen:
           ms2.Position = 0;

           return( CompareStreams( ms1, ms2 ) );
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static bool CompareBoolArryas( bool[] a, bool[] b )
        {
            if( a.Length != b.Length )
            {
                return false;
            }

            for( int i = 0; i < a.Length; i++ )
            {
                if( a[i] != b[i] )
                {
                    return false;
                }
            }

            return true;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static bool CompareArryas( object[] a, object[] b )
        {
            if( a.Length != b.Length )
            {
                return false;
            }

            for( int i = 0; i < a.Length; i++ )
            {
                if( a[i] != b[i] )
                {
                    return false;
                }
            }

            return true;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static bool CompareArryas( byte[] a, byte[] b )
        {
            if( a.Length != b.Length )
            {
                return false;
            }

            for( int i = 0; i < a.Length; i++ )
            {
                if( a[i] != b[i] )
                {
                    return false;
                }
            }
            return true;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static byte[] Combine( params byte[][] arrays )
        {
            byte[] rv = new byte[arrays.Sum( a => a.Length )];
            int offset = 0;
            foreach( byte[] array in arrays )
            {
                System.Buffer.BlockCopy( array, 0, rv, offset, array.Length );
                offset += array.Length;
            }
            return rv;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Objekte verwerfen, bevor Bereich verloren geht")]
        public static MemoryStream SerializeToStream( object objectType )
        {
            MemoryStream stream = new MemoryStream();
            IFormatter formatter = new BinaryFormatter();
            formatter.Serialize( stream, objectType );
            return stream;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static object DeserializeFromStream( MemoryStream stream )
        {
            IFormatter formatter = new BinaryFormatter();
            stream.Seek( 0, SeekOrigin.Begin );
            object objectType = formatter.Deserialize( stream );
            return objectType;
        } 
    }
}
