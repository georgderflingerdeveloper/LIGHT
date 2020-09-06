using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using HomeAutomation.HardConfig_Collected;
using SystemServices;


namespace Communication
{
    namespace Client_
    {
      // basic simple client
        class Client
        {
            protected byte[] buffer              = null;
            protected NetworkStream clientStream = null;
            protected ASCIIEncoding encoder      = null;
            protected TcpClient client = new TcpClient( );
            protected IPEndPoint serverEndPoint;
            protected string  _hostname = FormatString.Empty;

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
            public Client ( string ipadress, int port, string message )
            {
                client = new TcpClient( );
                try
                {
                    _hostname = System.Net.Dns.GetHostName( );
                    serverEndPoint = new IPEndPoint( IPAddress.Parse( ipadress ), port );
                    client.Connect( serverEndPoint );
                    clientStream = client.GetStream( );
                    encoder = new ASCIIEncoding( );
                    // prepares first message with " greeting"
                    buffer = encoder.GetBytes( message + " my name is: " + _hostname );
                    clientStream.Write( buffer, 0, buffer.Length );
                    clientStream.Flush( );
                }
                catch( Exception ExClient )
                {
                    Services.TraceMessage_( ExClient.Message );
                }
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public bool Connected
            {
                get
                {
                    return client.Connected;
                }
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public void WriteMessage ( string message )
            {
                if( buffer != null && clientStream != null && encoder != null )
                {
                    buffer = encoder.GetBytes( message );
                    clientStream.Write( buffer, 0, buffer.Length );
                    clientStream.Flush( );
                }
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public void Disconnect ( )
            {
                if( client != null )
                {
                    client.Close( );
                }
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public void Connect( string ipadress, int port)
            {
                serverEndPoint = new IPEndPoint(IPAddress.Parse(ipadress), port);
                if( client!= null )
                {
                    if( !client.Connected )
                    {
                        client.Connect( serverEndPoint );
                    }
                }
            }
        }

        // client with some metadata
        class ClientMeta
        {
            protected byte[]                buffer                    = null;
            protected int                   messagelength             = 0;
            string                          initialmessagegreeting    = null;
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
            protected string                completemessage           = null;
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
            protected string                messagemetadata           = null;
            protected NetworkStream         clientStream              = null;
            protected ASCIIEncoding         encoder                   = null;
            protected TcpClient             client                    = new TcpClient( );
            protected IPEndPoint            serverEndPoint;
            protected string                _hostname                 = FormatString.Empty;
            protected bool                  stopread_;
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
            string _SoftwareVersion;


            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public ClientMeta ( string ipadress, int port, string message )
            {
                InitiateConnection( ipadress, port, message );
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "completemessage")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "SystemServices.Services.TraceMessage_(System.String,System.String,System.String,System.Int32)")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "System.Console.WriteLine(System.String)")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
            protected void InitiateConnection(string ipadress, int port, string message)
            {
                 client = new TcpClient( );
                _hostname = System.Net.Dns.GetHostName( );
                try
                {
                    serverEndPoint = new IPEndPoint( IPAddress.Parse( ipadress ), port );
                    client.Connect( serverEndPoint );
                    if( client.Connected )
                    {
                        stopread_ = false;
                        clientStream = client.GetStream();
                        encoder = new ASCIIEncoding();
                        // prepares first message with " greeting"
                        initialmessagegreeting = FormatString.SpaceHolder1 + message + " my name is: " + _hostname;
                        messagelength = ComunicationConstants.MANDATORY_EXPECTED_STREAM_LENGTH + initialmessagegreeting.Length + FormatString.MessageEndTerminator.Length;
                        string completemessage = String.Format( ComunicationConstants.FORMAT_LENGTH, messagelength ) + initialmessagegreeting + FormatString.MessageEndTerminator;
                        buffer = encoder.GetBytes(completemessage);
                        clientStream.Write(buffer, 0, buffer.Length);
                        clientStream.Flush();
                    }
                }
                catch( Exception ExClient )
                {
                    Services.TraceMessage_( " Initial connection with server failed!" );
                    Console.WriteLine( TimeUtil.GetTimestamp( ) + " " + ExClient.Message );
                }
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public string TransmittedSoftwareVersion
            {
                set
                {
                    _SoftwareVersion = value;
                }
            }
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
            public bool Connected
            {
                get
                {
                    if( client != null )
                    {
                        try
                        {
                            bool isConnected = client.Connected;
                            return (isConnected);
                        }
                        catch
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Int32.ToString")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public void WriteMessage ( string message )
            {
                if( buffer != null && clientStream != null && encoder != null )
                {
                    messagelength = 0;
                    messagelength = ComunicationConstants.MANDATORY_EXPECTED_STREAM_LENGTH + message.Length;
                    buffer = encoder.GetBytes( messagelength.ToString() + message );
                    clientStream.Write( buffer, 0, buffer.Length );
                    clientStream.Flush( );
                }
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public void Disconnect ( )
            {
                if( client != null )
                {
                    client.Close( );
                }
            }
      }

        // this client sends more metainformation to the server - so he is more talktive
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
        class ClientTalktive : Client
        {
           const int IP_ASSIGNMENT_INDEX = 7; // TODO - find out a better way - so far the IP Adress stands at this location - within the string array
           public ClientTalktive ( string ipadress, int port, string message )
                : base( ipadress, port, message )
           {
           }

            // TODO - find a proper solution to implement this into constructor
            // GET IP ADRESS FROM HOSTNAME
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
            public void ClientTalktive_ ( string hostname_endpoint, int port, string message, bool throwIfMoreThanOneIP )
           {
                client = new TcpClient( );
                _hostname = System.Net.Dns.GetHostName( );

                try
                {
                    serverEndPoint = GetIPEndPointFromHostName( hostname_endpoint, port, throwIfMoreThanOneIP );
                    client.Connect( serverEndPoint );
                    clientStream = client.GetStream( );
                    encoder = new ASCIIEncoding( );
                    // prepares first message with " greeting"
                    buffer = encoder.GetBytes( message + " my name is: " + _hostname );
                    clientStream.Write( buffer, 0, buffer.Length );
                    clientStream.Flush( );
                }
                catch( Exception ExClient )
                {
                    Services.TraceMessage_( ExClient.Message );
                }
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            static IPEndPoint GetIPEndPointFromHostName ( string hostName, int port, bool throwIfMoreThanOneIP )
            {
                var addresses = System.Net.Dns.GetHostAddresses( hostName );
                if( addresses.Length == 0 )
                {
                    throw new ArgumentException(
                        "Unable to retrieve address from specified host name.",
                        "hostName"
                    );
                }
                else if( throwIfMoreThanOneIP && addresses.Length > 1 )
                {
                    throw new ArgumentException(
                        "There is more that one IP address to the specified host.",
                        "hostName"
                    );
                }
                return new IPEndPoint( addresses[IP_ASSIGNMENT_INDEX], port ); // Port gets validated here.
            }

            // extended information - more talktive
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
            public void WriteMessageWithHostname ( string message )
           {
               try
               {
                   if( buffer != null && clientStream != null && encoder != null )
                   {
                       buffer = encoder.GetBytes( _hostname + " says: " + message );
                       clientStream.Write( buffer, 0, buffer.Length );
                       clientStream.Flush( );
                   }
               }
               catch( Exception ex )
               {
                   Services.TraceMessage_( ex.Message );
                   Console.Read( );
                   Environment.Exit( 0 );
               }
           }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.DateTime.ToString")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
            public void WriteMessageWithHostnameAndTimestamp ( string message )
            {
                try
                {
                        if( buffer != null && clientStream != null && encoder != null )
                        {
                            buffer = encoder.GetBytes( DateTime.Now + FormatString.SpaceHolder1 + _hostname + " says: " + message );
                            clientStream.Write( buffer, 0, buffer.Length );
                            clientStream.Flush( );
                        }
                }
                catch( Exception ex )
                {
                    Services.TraceMessage( ex.Message );
                    Console.Read( );
                    Environment.Exit( 0 );
                }
            }

           public string HostName
            {
                get { return _hostname; }
            }
        }

        // contains some metadata to send, further reads information back from server
        class ClientTalktive_ : ClientMeta
        {
            const int IP_ASSIGNMENT_INDEX     = 7; // TODO - find out a better way
            decimal messageTransactionCounter = 0;
            Thread DataFromServerThread;
            Queue<string> _ReceivedMessageQueue;
            public event EventHandler EndpointDisconnected = delegate { };

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public ClientTalktive_ ( string ipadress, int port, string message )
                : base( ipadress, port, message )
            {
                DataFromServerThread = new Thread( new ParameterizedThreadStart( GetDataFromServer ) );
                DataFromServerThread.Start( base.client );
                _ReceivedMessageQueue = new Queue<string>();
                ComunicationReadLoop.StopReadEvent         += Stop_ReadEvent;
                ComunicationReadLoop.MessageReceived       += ComunicationReadLoop_MessageReceived;
                ComunicationReadLoop.EndpointDisconnected_ += ComunicationReadLoop_EndpointDisconnected_;
                ComunicationReadLoop.RegisterTCPStateEventHandler();
                ComunicationReadLoop.ServerPort = port;
            }
 
            void ComunicationReadLoop_EndpointDisconnected_( object sender, EventArgs e )
            {
                // fire disconnected event
                EndpointDisconnected( null, EventArgs.Empty );
            }

            void Stop_ReadEvent( object sender, EventArgs e )
            {
            }

            public Queue<string> ReceivedMessageQueue
            {
                get
                {
                    return _ReceivedMessageQueue;
                }
            }

            // client initiates disconnecting from server
            new public void Disconnect()
            {
                if (client != null)
                {
                    client.Close();
                    DataFromServerThread.Abort();
                }
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "Communication.Client_.ClientMeta.InitiateConnection(System.String,System.Int32,System.String)")]
            public void ReConnect(string ipadress, int port)
            {
                InitiateConnection( ipadress, port, ComunicationInfoString.RECONNECTED );
                DataFromServerThread = new Thread( new ParameterizedThreadStart(GetDataFromServer) );
                DataFromServerThread.Start( base.client );
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "Communication.Client_.ClientMeta.InitiateConnection(System.String,System.Int32,System.String)")]
            public void ReConnect( string ipadress, int port, string UserDefinedClientID )
            {
                InitiateConnection( ipadress, port,ComunicationInfoString.RECONNECTED + ComunicationInfoString.CLIENT_NAME_PREFIX + UserDefinedClientID );
                DataFromServerThread = new Thread( new ParameterizedThreadStart(GetDataFromServer) );
                DataFromServerThread.Start( base.client );
            }
            // extended information - more talktive
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Int32.ToString")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
            public void WriteMessageWithHostname ( string message )
            {
                try
                {
                    if( buffer != null && clientStream != null && encoder != null )
                    {
                        messagelength = 0;
                        string message_ = FormatString.SpaceHolder1 + _hostname + " says: " + message;
                        messagelength = ComunicationConstants.MANDATORY_EXPECTED_STREAM_LENGTH + message_.Length;
                        buffer = encoder.GetBytes( String.Format( ComunicationConstants.FORMAT_LENGTH, messagelength.ToString( ) ) + message_ );
                        clientStream.Write( buffer, 0, buffer.Length );
                        clientStream.Flush( );
                    }
                }
                catch( Exception ex )
                {
                    Services.TraceMessage_(  ex.Message );
                    Console.Read( );
                    Environment.Exit( 0 );
                }
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Decimal.ToString(System.String)")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
            public void WriteMessageWithHostnameAndTimestamp ( string message, string talkingtyp )
            {
                try
                {
                    if( buffer != null && clientStream != null && encoder != null )
                    {
                        messagelength = 0;
                        messageTransactionCounter++;
                        string FormatedTransactionCounter =   messageTransactionCounter.ToString(FormatConstants.TransactionNumberFormat_); 
                        // example:
                        // 0419 000000001 03032015:20h15m12s053ms GEORGLAPTOP says: CENTER_KITCHEN_LIVING_ROOM_TO_SERVER Hello ....
                        string message_ = FormatString.SpaceHolder1             + 
                                          FormatedTransactionCounter            +
                                          FormatString.SpaceHolder1             + 
                                          TimeUtil.GetTimestamp()               + 
                                          FormatString.SpaceHolder1             + 
                                          _hostname                             +
                                          talkingtyp                            + 
                                          message                               +
                                          FormatString.MessageEndTerminator;
                        messagelength = message_.Length + ComunicationConstants.MANDATORY_EXPECTED_STREAM_LENGTH;
                        string completemessage =  String.Format( ComunicationConstants.FORMAT_LENGTH, messagelength ) + message_;
                        buffer = encoder.GetBytes( completemessage );
                        clientStream.Write( buffer, 0, buffer.Length );
                        clientStream.Flush( );
                    }
                }
                catch( Exception ex )
                {
                    Services.TraceMessage_( ex.Message );
                    Console.Read( );
                    Environment.Exit( 0 );
                }
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
            public void WriteMessageWithLengthInformation( string message )
            {
                try
                {
                    if( buffer != null && clientStream != null && encoder != null )
                    {
                        messagelength = 0;
                        messageTransactionCounter++;
                        string message_ = FormatString.SpaceHolder1 +
                                          message;
                        messagelength = message_.Length + ComunicationConstants.MANDATORY_EXPECTED_STREAM_LENGTH + FormatString.MessageEndTerminator.Length;
                        string completemessage =  String.Format( ComunicationConstants.FORMAT_LENGTH, messagelength ) + message_ + FormatString.MessageEndTerminator;
                        buffer = encoder.GetBytes( completemessage );
                        clientStream.Write( buffer, 0, buffer.Length );
                        clientStream.Flush();
                    }
                }
                catch ( Exception ex )
                {
                    Services.TraceMessage_( ex.Message );
                    Console.Read();
                    Environment.Exit( 0 );
                }
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public string HostName
            {
                get { return _hostname; }
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public int MessageLength
            {
                get { return messagelength; }
            }
 
            public delegate void MessageReceivedHandlerSrv ( string receivedmessage );
            public event  MessageReceivedHandlerSrv MessageReceivedFromServer;

            void ComunicationReadLoop_MessageReceived( string msg )
            {
                if( MessageReceivedFromServer != null )
                {
                    // fire event
                    MessageReceivedFromServer( msg );
                    // furthermore store data in QUEUE
                    QueueHandling.Queue_MessageReceived( ref _ReceivedMessageQueue, msg );
                }
            }

            public void GetDataFromServer ( object client )
            {
                if (client == null)
                {
                    return;
                }

                if( base.Connected )
                {
                    TcpClient tcpClient = ( TcpClient ) client;
                    NetworkStream clientStream = tcpClient.GetStream( );
                    byte[] message = new byte[ComunicationConstants.DEFAULT_BUFFER_SIZE];
                    int bytesRead;
                    string encodedmessage  = FormatString.Empty;
                    string CompleteMessage = FormatString.Empty;
                    string tail = FormatString.Empty;
                    int ReceivedMessageLenghtInformation = 0;
                    StringBuilder FullMessagString = new StringBuilder( );
                    stopread_ = false;

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
                                                     ref  CompleteMessage,
                                                     ref  ReceivedMessageLenghtInformation,
                                                     ref  encoder,
                                                     ref  stopread_,
                                                     new string[] { FormatString.MessageEndTerminator } );

                        ReceivedMessageLenghtInformation = 0;
                        encodedmessage                   = FormatString.Empty;
                        CompleteMessage                  = FormatString.Empty;
                        FullMessagString.Clear( );
                    }
                }
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Convert.ToInt16(System.String)")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "System.Console.WriteLine(System.String)")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
            public void ClientAutoReconnect( ref ClientTalktive_ Client_, string userdefinedClientID, string serveripadress, string serverPort )
            {
                if( !Client_.Connected )
                {
                    try
                    {
                        Client_.Disconnect( );
                        Console.WriteLine( TimeUtil.GetTimestamp( ) + " Try to reconnect ..." );
                        Client_.ReConnect( serveripadress, Convert.ToInt16( serverPort ), userdefinedClientID );
                    }
                    catch( Exception ex )
                    {
                        Console.WriteLine( TimeUtil.GetTimestamp( ) + " Auto Reconnecting failed" );
                        Services.TraceMessage_( ex.Message );
                    }
                }
            }
        }
    }
}
