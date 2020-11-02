using HomeAutomation.HardConfig_Collected;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using SystemServices;

namespace Communication
{
    public static class ComunicationConstants
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "EXPECTED")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "LENGTH")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "MANDATORY")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "STREAM")]
        public const int MANDATORY_EXPECTED_STREAM_LENGTH       = 4; // expected 4 Bytes Length information
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "ELEMENTS")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "MAX")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "QUEUE")]
        public const int MAX_QUEUE_ELEMENTS                     = 20;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "DEFAULT")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "PORT")]
        public const int DEFAULT_PORT                           = 5000;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "BUFFER")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "DEFAULT")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SIZE")]
        public const int DEFAULT_BUFFER_SIZE                    = 4096;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "FORMAT")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "LENGTH")]
        public const string FORMAT_LENGTH                       = "{0:0000}";
    }

    static class FormatString
    {
        public const string Empty                = "";
        public const string SpaceHolder1         = " ";
        public const string MessageEndTerminator =  "<*#*>";
    }

    static class ComunicationInfoString
    {
        public const string MESSAGE_ID          = "MESSAGE_ID: ";
        public const string SAYS                = " says: ";
        public const string ANSWERS             = " answers: ";
        public const string RECONNECTED         = "Server was reconnected with ";
        public const string QUEUE_IS_FULL       = "Queue is full! - last received message is not inserted";
        public const string CLIENT_MESSAGE_ID   = "CLIENT_MESSAGE_ID: ";
        public const string SERVER_REPLAYS      = " SERVER replys : ";
        public const string CLIENT_NAME_PREFIX  = "CLIENT_";
    }

    public static class QueueHandling
    {
        // put all received messages in a queue
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "SystemServices.Services.TraceMessage_(System.String,System.String,System.String,System.Int32)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "0#")]
        public static void Queue_MessageReceived( ref Queue<string> _msgqueu, string receivedmessage )
        {
            if ( _msgqueu == null )
            {
                return;
            }
            lock ( _msgqueu )
            {
                if ( _msgqueu.Count <= ComunicationConstants.MAX_QUEUE_ELEMENTS )
                {
                    _msgqueu.Enqueue( receivedmessage );
                }
                // not enough messages fetched from queue
                else
                {
                    Services.TraceMessage_( ComunicationInfoString.QUEUE_IS_FULL );
                }
            }
        }
    }

    public static class MyNetwork
    {
        static IPGlobalProperties  _ipProperties;
        static TcpConnectionInformation[] tcpConnections;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public delegate void TcpStates_( TcpState state );
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
        public static event TcpStates_ ETcpStates_;

        // TODO - testing on real system - figure out why it does not work with local IP ( 127.0.0.1 ) correctly - endpoint could not be found
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "tcpClient")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "0#")]
        static public void GetConnectionInformation( ref TcpClient tcpClient, int port )
        {
             _ipProperties  = IPGlobalProperties.GetIPGlobalProperties();
             tcpConnections = _ipProperties.GetActiveTcpConnections();
             foreach( TcpConnectionInformation info in tcpConnections )
             {
                 if( info.RemoteEndPoint.Port == port )
                 {
                     if( ETcpStates_ != null )
                     {
                         ETcpStates_( info.State );
                     }
                 }
             }
        }
    }

    public static class ComunicationReadLoop
    {
        // used for raising en event when reading data is stopped - f.e. disconnecting a server or client
        public static event EventHandler StopReadEvent = delegate { };
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
        public static event EventHandler EndpointDisconnected_ = delegate { };

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public delegate void EMsgReceived( string msg );
        public static event EMsgReceived MessageReceived;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public delegate void EProtocollError( string msg );
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
        public static event EProtocollError EProtocollError_;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "TCP")]
        public static void RegisterTCPStateEventHandler()
        {
            MyNetwork.ETcpStates_ += MyNetwork_ETcpStates_;
        }

        static void MyNetwork_ETcpStates_( TcpState state )
        {
              switch( state )
              {
                  case System.Net.NetworkInformation.TcpState.Closed:
                       EndpointDisconnected_( null, EventArgs.Empty );
                       break;
              }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1307:SpecifyStringComparison", MessageId = "System.String.IndexOf(System.String)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Int32.ToString")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "SystemServices.Services.TraceMessage_(System.String,System.String,System.String,System.Int32)")]
        static void VerifyReceivedLengthInformationWithMessageLength( ref StringBuilder FullMessagString, ref int ReceivedMessageLenghtInformation, ref string[] stringSeparators )
        {
            string stringseperator_      =  stringSeparators[0];
            int VerifiedLenthInformation =  FullMessagString.ToString().IndexOf( stringseperator_ ) + stringseperator_.Length;

            if( ReceivedMessageLenghtInformation == FullMessagString.ToString().Length )
            {
                if( !FullMessagString.ToString().Contains( stringseperator_ ) )
                {
                    string exInfoString = "Received message does not contain terminator: "
                                        + stringseperator_ + " as expected in application protocoll!";
                    Services.TraceMessage_( exInfoString );
                    if( EProtocollError_ != null )
                    {
                        EProtocollError_( exInfoString );
                    }
                    throw new Exception( exInfoString );
                }
            }

            // we got valid lenght information,
            // but this number does not match with the message length up 
            // to the message terminator
            if( VerifiedLenthInformation != ReceivedMessageLenghtInformation )
            {
                string exInfoString = "Received message length information: " + ReceivedMessageLenghtInformation.ToString()
                                     + " does not match with real message length taken from buffer: " + VerifiedLenthInformation.ToString() + " up to the terminator string: "
                                     + stringseperator_;
                Services.TraceMessage_( exInfoString );
                if( EProtocollError_ != null )
                {
                    EProtocollError_( exInfoString );
                }
                throw new Exception( exInfoString );
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Convert.ToInt16(System.String)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "SystemServices.Services.TraceMessage(System.String,System.String,System.String,System.Int32)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1820:TestForEmptyStringsUsingStringLength")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        static void HandleMessages( ref          StringBuilder FullMessagString,
                                    ref int      ReceivedMessageLenghtInformation,
                                    ref string[] stringSeparators, 
                                    ref string   tail, 
                                    ref bool     stopread 
                                  )
        {

            try
            {
                string[] Messages =  FullMessagString.ToString( ).Split( stringSeparators, StringSplitOptions.None );
                // one complete message to verify
                if( Messages.Length <= 2 )
                {
                    VerifyReceivedLengthInformationWithMessageLength( ref  FullMessagString, ref  ReceivedMessageLenghtInformation, ref  stringSeparators );
                }
                string receivedMessage = "";
                // we got proper messages
                for( int i = 0; i < Messages.Length; i++ )
                {
                    string actualreceivedMessageLength = Messages[i].Split( ' ' )[0];
                    if( actualreceivedMessageLength == "" )
                    {
                        break;
                    }
                    int actualExpectedMessageLength    = Convert.ToInt16( actualreceivedMessageLength ) - FormatString.MessageEndTerminator.Length;
                    if( Messages[i].Length == actualExpectedMessageLength )
                    {
                        if( MessageReceived != null )
                        {
                            receivedMessage = Messages[i];
                            MessageReceived( receivedMessage );
                        }
                        tail = FormatString.Empty;
                    }
                    // last message is not complete - save tail 
                    if( Messages[i].Length < actualExpectedMessageLength )
                    {
                        tail = Messages[i];
                    }
                }
            }
            catch( Exception ex )
            {
                stopread = true;
                Services.TraceMessage( TimeUtil.GetTimestamp() + " "+ ex.Message );
            }
        }

        // can stop readloop
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Convert.ToInt32(System.String)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "CompleteMessage")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "SystemServices.Services.TraceMessage(System.String,System.String,System.String,System.Int32)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "System.Console.WriteLine(System.String)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "string")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Complete")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Full")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Received")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "4#")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "1#")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "7#")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "5#")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "9#")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "3#")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "2#")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "8#")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "10#")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "6#")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "0#")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Argumente von öffentlichen Methoden validieren", MessageId = "1")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Argumente von öffentlichen Methoden validieren", MessageId = "9")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Argumente von öffentlichen Methoden validieren", MessageId = "3")]
        public static void ReadLoop(  ref TcpClient            tcpClient,
                                      ref NetworkStream        clientStream,
                                      ref byte[]               message,
                                      ref StringBuilder        FullMessagString,
                                      ref int                  bytesRead,
                                      ref string               encodedmessage,
                                      ref string               tail,
                                      ref string               CompleteMessage,
                                      ref int                  ReceivedMessageLenghtInformation,
                                      ref ASCIIEncoding        encoder,
                                      ref bool                 stopread,
                                      string[]                 stringSeparators )
        {
            bool stopreadeventfired = false;
            bytesRead = 0;
            do
            {
                try
                {
                   if ( stopread )
                   {
                        if ( !stopreadeventfired )
                        {
                             StopReadEvent( null, EventArgs.Empty );
                             stopreadeventfired = true;
                        }
                        Thread.Sleep( 100 );
                        return;
                    }
                    // blocks until a client sends a message
                    bytesRead = clientStream.Read( message, 0, ComunicationConstants.DEFAULT_BUFFER_SIZE );
                    if( ServerPort != 0 )
                    {
                        MyNetwork.GetConnectionInformation( ref tcpClient, ServerPort );
                    }
                }
                catch( Exception ex )
                {
                        // fire event disconnected
                        EndpointDisconnected_( null, EventArgs.Empty );
                        // a socket error has occured
                        Console.WriteLine( TimeUtil.GetTimestamp( ) + " "+ "SERVER disconnected... " );
                        // get better console alignment - only for increasing readability
                        Console.WriteLine( TimeUtil.GetTimestamp( ) + " "+  ex.Message );
                        Console.WriteLine();
                        stopread = true;
                        break;
                }
                encodedmessage = encoder.GetString( message, 0, bytesRead );
                //  check length information
                if( encodedmessage.Length < ComunicationConstants.MANDATORY_EXPECTED_STREAM_LENGTH )
                {
                    continue; // read as long as full length information sent by endpoint is available
                }
                else // length information available
                {
                    try
                    {
                        ReceivedMessageLenghtInformation = Convert.ToInt32( encodedmessage.Substring( 0, ComunicationConstants.MANDATORY_EXPECTED_STREAM_LENGTH ) );
                        if( encodedmessage.Length < ReceivedMessageLenghtInformation )
                        {
                            continue;
                        }
                    }
                    catch( Exception ex )
                    {
                        Services.TraceMessage( TimeUtil.GetTimestamp( ) + " "+  ex.Message );
                    }
                }

                // when we have a tail it is appended first
                FullMessagString.AppendFormat( "{0}{1}", tail, encodedmessage );

                HandleMessages( ref  FullMessagString,
                                ref  ReceivedMessageLenghtInformation,
                                ref  stringSeparators,
                                ref  tail,
                                ref  stopread
                               );

                FullMessagString.Clear( );
            }
            while( clientStream.DataAvailable );
        }


        public static int ServerPort { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Convert.ToInt32(System.String)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "tcpClient")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "SystemServices.Services.TraceMessage(System.String,System.String,System.String,System.Int32)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "System.Console.WriteLine(System.String)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "string")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Full")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Received")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "4#")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "1#")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "5#")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "8#")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "3#")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "2#")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "7#")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "6#")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "0#")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Argumente von öffentlichen Methoden validieren", MessageId = "1")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Argumente von öffentlichen Methoden validieren", MessageId = "8")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Argumente von öffentlichen Methoden validieren", MessageId = "3")]
        public static void ReadLoop(  ref TcpClient                tcpClient,
                                      ref NetworkStream            clientStream,
                                      ref byte[]                   message,
                                      ref StringBuilder            FullMessagString,
                                      ref int                      bytesRead,
                                      ref string                   encodedmessage,
                                      ref string                   tail,
                                      ref int                      ReceivedMessageLenghtInformation,
                                      ref ASCIIEncoding            encoder,
                                      string[]                     stringSeparators )
        {
            bytesRead = 0;
            // message has successfully been received
            // Incoming message may be larger than the buffer size.
            do
            {
                try
                {
                    // blocks until a client/server (comunication partners) sending a message
                    bytesRead = clientStream.Read( message, 0, ComunicationConstants.DEFAULT_BUFFER_SIZE );
                }
                catch( Exception ex )
                {
                    // fire event disconnected
                    EndpointDisconnected_( null, EventArgs.Empty );
                    // a socket error has occured
                    Console.WriteLine( TimeUtil.GetTimestamp( ) + " "+  "SERVER disconnected... " );
                    // get better console alignment - only for increasing readability
                    Console.WriteLine( TimeUtil.GetTimestamp( ) + " " + ex.Message );
                    Console.WriteLine();
                    break;
                }
                encodedmessage = encoder.GetString( message, 0, bytesRead );
                //  check length information
                if ( encodedmessage.Length < ComunicationConstants.MANDATORY_EXPECTED_STREAM_LENGTH )
                {
                     continue; // read as long as full length information sent by endpoint is available
                }
                else // length information available
                {
                    try
                    {
                        ReceivedMessageLenghtInformation = Convert.ToInt32( encodedmessage.Substring( 0, ComunicationConstants.MANDATORY_EXPECTED_STREAM_LENGTH ) );
                        if( encodedmessage.Length < ReceivedMessageLenghtInformation )
                        {
                            continue;
                        }
                    }
                    catch ( Exception ex )
                    {
                        Services.TraceMessage( TimeUtil.GetTimestamp( ) + " "+  ex.Message );
                    }
                }

                // when we have a tail it is appended first
                FullMessagString.AppendFormat( "{0}{1}", tail, encodedmessage );

                bool stopreadnotused = false;

                HandleMessages( ref  FullMessagString,
                                ref  ReceivedMessageLenghtInformation,
                                ref  stringSeparators,
                                ref  tail,
                                ref  stopreadnotused
                             );

                FullMessagString.Clear();
            }
            while( clientStream.DataAvailable );
        }

        // use this pulbic methode for testpurpose only !
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Full")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Received")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "0#")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "1#")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "4#")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "2#")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "3#")]
        public static void JustForTest_HandleMessages( ref StringBuilder FullMessagString,
                                                       ref int           ReceivedMessageLenghtInformation,
                                                       ref string[]      stringSeparators,
                                                       ref string        tail,
                                                       ref bool          stopread )
        {
            HandleMessages( ref  FullMessagString,
                            ref  ReceivedMessageLenghtInformation,
                            ref  stringSeparators, 
                            ref  tail, 
                            ref  stopread 
                          );
        }

    }

    namespace Server_
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces")]
        public class Server
        {
            #region DECLARATIONS
            private TcpListener   tcpListener;
            private System.Threading.Thread listenThread;
            // active client objects
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists")]
            protected List<TcpClient> TcpClientList = new List<TcpClient>();
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
            protected int TcpClientListIndex = 0;
            #endregion

            #region PUBLIC_METHODS
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
            public Server ( )
            {
                try
                {
                    this.tcpListener  = new TcpListener( IPAddress.Any, ComunicationConstants.DEFAULT_PORT );
                    this.listenThread = new Thread( new ThreadStart( ListenForClients ) );
                    this.listenThread.Start( );
                }
                catch( Exception ex )
                {
                    Services.TraceMessage_( ex.Message );
                }
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
            public Server ( int port )
            {
                try
                {
                    this.tcpListener  = new TcpListener( IPAddress.Any, port );
                    this.listenThread = new Thread( new ThreadStart( ListenForClients ) );
                    this.listenThread.Start( );
                }
                catch( Exception ex )
                {
                    Services.TraceMessage_( ex.Message );
                }

            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
            public void Abort ( )
            {
                try
                {
                    tcpListener.Stop( );
                    listenThread.IsBackground = false;
                    listenThread.Abort( );
                }
                catch( Exception ex )
                {
                    Services.TraceMessage_( ex.Message );
                }

            }
            #endregion

            #region PRIVATE_METHODS
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Int32.ToString")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "SystemServices.Services.TraceMessage(System.String,System.String,System.String,System.Int32)")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "System.Console.WriteLine(System.String)")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
            private void ListenForClients ( )
            {
                this.tcpListener.Start( );

                while( true )
                {
                    try
                    {
                        //blocks until a client has connected to the server
                        TcpClient client = this.tcpListener.AcceptTcpClient();
                        TcpClientList.Add( client );
                    }
                    catch ( Exception ex )
                    {
                        SystemServices.Services.TraceMessage(  TimeUtil.GetTimestamp() + " " + "CLient 'accept or registration' Failed!" );
                        Console.WriteLine( TimeUtil.GetTimestamp( ) + " " + ex.Message );
                        return;
                    }

                    if( ( TcpClientList.Count > 0 ) && ( TcpClientListIndex < TcpClientList.Count ) )
                    {
                        TcpClientListIndex = TcpClientList.Count - 1;
                    }
                    else
                    {
                        tcpListener.Stop( );
                        Console.WriteLine( TimeUtil.GetTimestamp( ) + " " + "Could not connect with desired client ID: " + TcpClientListIndex.ToString( ) );
                        return;
                    }
                    string ConnectedClientID = TcpClientList[TcpClientListIndex].Client.RemoteEndPoint.ToString();
                    Console.WriteLine( TimeUtil.GetTimestamp() + " " + "SERVER is connected with " + ConnectedClientID);

                    //create a thread to handle communication 
                    //with connected client
                    Thread clientThread = new Thread( new ParameterizedThreadStart( HandleClientComm ) );
                    clientThread.Start( TcpClientList[TcpClientListIndex] );
                }
            }
            #endregion

            #region PROTECTED_METHODS
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "System.Console.WriteLine(System.String)")]
            protected virtual void HandleClientComm ( object client )
            {
                TcpClient tcpClient = ( TcpClient ) client;
                NetworkStream clientStream = tcpClient.GetStream( );

                byte[] message = new byte[ComunicationConstants.DEFAULT_BUFFER_SIZE];
                int bytesRead;

                while( true )
                {
                    bytesRead = 0;

                    try
                    {
                        //blocks until a client sends a message
                        bytesRead = clientStream.Read( message, 0, ComunicationConstants.DEFAULT_BUFFER_SIZE );
                    }
                    catch( SocketException se )
                    {
                        //a socket error has occured
                        Console.WriteLine( se.Message );
                        break;
                    }

                    if( bytesRead == 0 )
                    {
                        //the client has disconnected from the server
                        Console.WriteLine( TimeUtil.GetTimestamp( ) + " " + "CLIENT " + tcpClient.Client.RemoteEndPoint.ToString( ) + " is disconnected" );
                        break;
                    }

                    //message has successfully been received
                    ASCIIEncoding encoder = new ASCIIEncoding( );
                    Console.WriteLine( TimeUtil.GetTimestamp( ) + " " + tcpClient.Client.RemoteEndPoint.ToString( ) + FormatString.SpaceHolder1 + encoder.GetString( message, 0, bytesRead ) );
                }
            }
            #endregion
        }
 
        public class ServerQueue : Server
        {
            #region DECLARATIONS
            Queue<string> _MessageQueue;

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
            public delegate void FirstMessageUpdated( string clientid );
            public event         FirstMessageUpdated EFirstMessageUpdated;

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
            public delegate void MessageReceivedHandler( string receivedmessage );
            public event         MessageReceivedHandler MessageReceivedFromClient;

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Which")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
            public delegate void ClientDisconnected(KeyValuePair<string, string> WhichClient);
            public event         ClientDisconnected CLientHasDisconnected;

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Which")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
            public delegate void ClientConnected( KeyValuePair<string, string> WhichClient );
            public event         ClientConnected CLientHasConnected;

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Re")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Given")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
            public delegate void ClientReConnected(string GivenName );
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Re")]
            public event         ClientReConnected CLientHasReConnected;

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
            public delegate void EProtocollError( string msg );
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
            public event         EProtocollError EProtocollError_;

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
            public delegate void FirstMessageSent();
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
            public event         FirstMessageSent FirstMessageSent_;

            string LastdisconnectedCLientName              = "";
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
            bool _ShowInternalReceivedMessageFromClient;
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
            bool _EchoData;
           
            // client names for identification
            List<KeyValuePair<string, string>> TcpClientFirstMessageList = new List<KeyValuePair<string, string>>( );
            private Object thisLock = new Object( );
            #endregion

            #region CONSTRUCTOR
            public ServerQueue ( int port ) : base( port )
            {
                _MessageQueue = new Queue<string>();
                this.MessageReceivedFromClient        += ServerQueue_MessageReceivedFromClient;
                ComunicationReadLoop.MessageReceived  += ComunicationReadLoop_MessageReceived;
                ComunicationReadLoop.EProtocollError_ += ComunicationReadLoop_EProtocollError_;
            }
            #endregion

            #region PROPERTIES
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly")]
            public bool ShowInternalReceivedMessageFromClient
            {
                set
                {
                    _ShowInternalReceivedMessageFromClient = value;
                }
            }

            // true - information can be sent back to client
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly")]
            public bool EchoData
            {
                set
                {
                    _EchoData = value;
                }
            }

            public Queue<string> MessageQueue
            {
                get{
                    return _MessageQueue;
                }
            }
            #endregion

            #region PRIVATE_METHODS
            // put all received messages in a queue
            void ServerQueue_MessageReceivedFromClient ( string receivedmessage )
            {
                QueueHandling.Queue_MessageReceived( ref _MessageQueue, receivedmessage ); 
            }
            // event is used for others than console applications
            void ComunicationReadLoop_EProtocollError_( string msg )
            {
                if( EProtocollError_ != null )
                {
                    EProtocollError_( msg );
                }
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "addresslist")]
            private void InitialSendDataToClient( TcpClient tcpClient )
            {
                NetworkStream clientStream = tcpClient.GetStream( );
                ASCIIEncoding encoder = new ASCIIEncoding( );
                string Myhostname = Dns.GetHostName();
                // TODO figure out how to get hostname
                IPAddress[] addresslist = Dns.GetHostAddresses(Myhostname);
                string greetingmessage = " Hello "                                   +
                                         tcpClient.Client.RemoteEndPoint.ToString( ) +
                                         FormatString.SpaceHolder1                   +
                                         "this is your server "                      +
                                         Myhostname;
                                         
                int messagelength = greetingmessage.Length + ComunicationConstants.MANDATORY_EXPECTED_STREAM_LENGTH + FormatString.MessageEndTerminator.Length;
                string completegreetingmessage =  String.Format( ComunicationConstants.FORMAT_LENGTH, messagelength ) + greetingmessage + FormatString.MessageEndTerminator;
                byte[] buffer = encoder.GetBytes( completegreetingmessage );
                clientStream.Write( buffer, 0, buffer.Length );
                clientStream.Flush( );
                if( FirstMessageSent_ != null )
                {
                    FirstMessageSent_( );
                }
            }
            // message is analysed wether length and terminators are matching - message is correct
            void ComunicationReadLoop_MessageReceived( string msg )
            {
                // fire event
                if( MessageReceivedFromClient != null )
                {
                    MessageReceivedFromClient( msg );
                }
            }
            #endregion

            #region PUBLIC_METHODS
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Int32.ToString")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "entries")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "System.Console.WriteLine(System.String)")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "SystemServices.Services.TraceMessage_(System.String,System.String,System.String,System.Int32)")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Argumente von öffentlichen Methoden validieren", MessageId = "0")]
            public void SendMessageToClient( string message, string clientId )
            {
                if( TcpClientList  == null || TcpClientFirstMessageList == null )
                {
                    return;
                }
                int SelectedClientIndex = 0;
                int FirstMessageListIndex = 0;

                // search for key and set Index for selecting proper client
                foreach( KeyValuePair<string, string> pair in TcpClientFirstMessageList )
                {
                    // we got entry in list - lets find proper endpoint at TcpClientList
                    if( pair.Key == clientId )
                    {
                        foreach( var entries in TcpClientList )
                        {
                            if( pair.Value == TcpClientList[SelectedClientIndex].Client.RemoteEndPoint.ToString( ) )
                            {
                                break;
                            }
                            SelectedClientIndex++;
                        }
                        break;
                    }
                    else
                    {
                        // key not found
                        if( FirstMessageListIndex == TcpClientFirstMessageList.Count-1 )
                        {
                            return;
                        }
                    }
                    FirstMessageListIndex++;
                }

                if( ( TcpClientList.Count > 0 ) && ( TcpClientListIndex < TcpClientList.Count ) )
                {

                    TcpClient SelectedClient = TcpClientList[SelectedClientIndex];
                    try
                    {
                        NetworkStream clientStream = SelectedClient.GetStream( );
                        ASCIIEncoding encoder      = new ASCIIEncoding( );
                        string timestamp = TimeUtil.GetTimestamp();
                        // IMPORTANT - make this always equal with the real length of complete message!
                        // none matching length will mess up received information !
                        int messagelength = message.Length                                         + 
                                            ComunicationConstants.MANDATORY_EXPECTED_STREAM_LENGTH + 
                                            FormatString.SpaceHolder1.Length                       + 
                                            FormatString.MessageEndTerminator.Length               +
                                            FormatString.SpaceHolder1.Length                       +
                                            timestamp.Length;
                        string completemessage = FormatString.Empty;
                        // when adding something here, add this also at messagelenth information! 
                        completemessage = // LENGTH INFORMATION
                                          String.Format( ComunicationConstants.FORMAT_LENGTH, messagelength ) + 
                                          FormatString.SpaceHolder1                                           + 
                                          // TIMESTAMP
                                          timestamp                                                           +
                                          FormatString.SpaceHolder1                                           +
                                          // USER MESSAGE
                                          message                                                             + 
                                          // MESSAGE TERMINATOR
                                          FormatString.MessageEndTerminator;
                        byte[] buffer = encoder.GetBytes( completemessage );
                        clientStream.Write( buffer, 0, buffer.Length );
                        clientStream.Flush( );
                    }
                    catch( Exception ex )
                    {
                        Services.TraceMessage_( "Could not send data to client " );
                        Console.WriteLine( ex.Message );
                    }
                }
                else
                {
                    Console.WriteLine( TimeUtil.GetTimestamp( ) + " " + "Desired client " + LastdisconnectedCLientName +" with Index: " + TcpClientListIndex.ToString( ) + " is not connected!" );
                }
            }
            #endregion

            #region PROTECTED_METHODS
            // Comunication clients to server
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "subs")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "SystemServices.Services.TraceMessage_(System.String,System.String,System.String,System.Int32)")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "System.Console.WriteLine(System.String)")]
            override protected void HandleClientComm ( object client )
            {
                TcpClient tcpClient                  = ( TcpClient ) client;
                NetworkStream clientStream           = tcpClient.GetStream( );
                bool reconnected                     = false;
                bool firstmessage_                   = false;

                int bytesRead                        = 0;
                int ReceivedMessageLenghtInformation = 0;
                byte[] message                       = new byte[ComunicationConstants.DEFAULT_BUFFER_SIZE];
                StringBuilder FullMessagString       = new StringBuilder( );
                string encodedmessage                = FormatString.Empty;
                string tail                          = FormatString.Empty;
                string RemoteEndPoint                = FormatString.Empty;
                ASCIIEncoding encoder                = new ASCIIEncoding( );

                while( true )
                {
                    bytesRead = 0;
                    ComunicationReadLoop.ReadLoop( ref  tcpClient,
                                                   ref  clientStream,
                                                   ref  message,
                                                   ref  FullMessagString,
                                                   ref  bytesRead,
                                                   ref  encodedmessage,
                                                   ref  tail,
                                                   ref  ReceivedMessageLenghtInformation,
                                                   ref  encoder,
                                                   new string[] { FormatString.MessageEndTerminator } );

                    RemoteEndPoint = tcpClient.Client.RemoteEndPoint.ToString( );
                    #region disconnect
                    // the client has disconnected from the server
                    if( bytesRead == 0 )
                    {
                        // after reconnect we want to have again a first message
                        firstmessage_ = false;

                        int CliListIndex = 0;
                        // remove disconnected client objects
                        foreach( KeyValuePair<string, string> pair in TcpClientFirstMessageList )
                        {
                            if( pair.Value == RemoteEndPoint )
                            {
                                // fire event with client information
                                if( CLientHasDisconnected != null )
                                {
                                    CLientHasDisconnected( pair );
                                }
                                TcpClientList.RemoveAt( CliListIndex );
                                break;
                            }
                            CliListIndex++;
                        }
                        int ListIndex = 0;
                        // search for first matching key by value
                        foreach( KeyValuePair<string, string> pair in TcpClientFirstMessageList )
                        {
                            // found actual endpoint of closed client
                            if( pair.Value == RemoteEndPoint )
                            {
                                break;
                            }
                            ListIndex++;
                        }
                        if( TcpClientFirstMessageList.Count > 0 )
                        {
                            LastdisconnectedCLientName = TcpClientFirstMessageList[ListIndex].Key.ToString( );
                            TcpClientFirstMessageList.RemoveAt( ListIndex );
                        }
                        else
                        {
                            Services.TraceMessage_( "Could not remove message list, because no items added before!" );
                        }
                        Console.WriteLine( TimeUtil.GetTimestamp() +  " CLIENT " + RemoteEndPoint + "( Name: " + LastdisconnectedCLientName + ")" + " is disconnected" );
                        ReceivedMessageLenghtInformation = 0;
                        break;
                    }
                    #endregion disconnect

                    string[] messageParts = encodedmessage.Split( Seperators.delimiterChars );

                    #region reconnectionevent
                    if( encodedmessage.Contains( ComunicationInfoString.RECONNECTED ) )
                    {
                        int substring_index = 0;
                        foreach( string subs in messageParts )
                        {
                            if( messageParts[substring_index].Contains( ComunicationInfoString.CLIENT_NAME_PREFIX ) )
                            {
                                break;
                            }
                            substring_index++;
                        }

                        if( substring_index < messageParts.Count( ) )
                        {
                            // fire connected event
                            if( CLientHasReConnected != null )
                            {
                                reconnected = true;
                                InitialSendDataToClient( tcpClient );
                                CLientHasReConnected( messageParts[substring_index] );
                            }
                        }
                    }
                    #endregion

                    #region firstconnection
                    lock( thisLock )
                    {
                        if( !firstmessage_ )
                        {
                            int substring_index = 0;
                            foreach( string subs in messageParts )
                            {
                                if( messageParts[substring_index].Contains( ComunicationInfoString.CLIENT_NAME_PREFIX ) )
                                {
                                    break;
                                }
                                substring_index++;
                            }
                            if( substring_index >= messageParts.Count( ) )
                            {
                                Console.WriteLine( TimeUtil.GetTimestamp( ) + " " + "CLIENT ID defined by user not found - try again" );
                                return;
                            }
                            KeyValuePair<string, string> pair = new KeyValuePair<string, string>( messageParts[substring_index], RemoteEndPoint );
                            TcpClientFirstMessageList.Add( pair );

                            string clientID = messageParts[substring_index];

                            // fire connected event
                            if( CLientHasConnected != null )
                            {
                                if( !reconnected )
                                {
                                    InitialSendDataToClient( tcpClient );
                                    CLientHasConnected( pair );
                                }
                            }

                            if( EFirstMessageUpdated != null )
                            {
                                EFirstMessageUpdated( clientID ); 
                            }

                            firstmessage_ = true;
                        }
                        encodedmessage  = FormatString.Empty;
                        FullMessagString.Clear( );
                    }
                  }
                #endregion
            }
            #endregion
        }
    }
}
