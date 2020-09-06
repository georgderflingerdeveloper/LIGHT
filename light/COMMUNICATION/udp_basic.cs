﻿using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using SystemServices;

namespace Communication
{
    namespace UDP
    {
        // some predefined default settings
        static class UDPConfig
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "addresslist")]
            public static void GetIPAdr ( string hostname )
            {
                IPAddress[] addresslist = Dns.GetHostAddresses( hostname );
            }
            public static int    port              =  7000;
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
            public static string IpAdressBroadcast =  "192.168.0.255";
            public static string IpAdress          =  "127.0.0.1";       //"192.168.0.105";
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
            public static int    TimeOutCycles     = 10;
        }

        class ReceivedEventargs : EventArgs
        {
            public string Adress { get; set; }
            public string Port { get; set; }
            public string Payload { get; set; }

        }

        class UdpReceive
        {
            Thread     receiveThread;
            UdpClient  client;
            IPEndPoint anyIP;
            ReceivedEventargs receivedEventargs = new ReceivedEventargs( );

            public int port; // define > init

            public UdpReceive ( int port_ )
            {
                port = port_;
                // ----------------------------
                // Abhören
                // ----------------------------
                // Lokalen Endpunkt definieren (wo Nachrichten empfangen werden).
                // Einen neuen Thread für den Empfang eingehender Nachrichten erstellen.
                client = new UdpClient( port );
                anyIP = new IPEndPoint( IPAddress.Any, port );
                receiveThread = new Thread( new ThreadStart( ReceiveData ) );
                receiveThread.IsBackground = true;
                receiveThread.Start( );
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public UdpReceive ( IPAddress adress, int port_ )
            {
                port = port_;
                // ----------------------------
                // Abhören
                // ----------------------------
                // Lokalen Endpunkt definieren (wo Nachrichten empfangen werden).
                // Einen neuen Thread für den Empfang eingehender Nachrichten erstellen.
                client = new UdpClient( port );
                client.ExclusiveAddressUse = false;
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                client.Client.Bind(new IPEndPoint(adress,port));
                receiveThread = new Thread( new ThreadStart( ReceiveData ) );
                receiveThread.IsBackground = true;
                receiveThread.Start( );
            }

            // infos
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
            public string lastReceivedUDPPacket="";
            public string allReceivedUDPPackets=""; 
            string _receivedText;

            public delegate void DataReceived ( string e );
            public event         DataReceived EDataReceived;

            public delegate void Received( object sender, ReceivedEventargs e );
            public event Received DatagrammReceived;

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Int32.ToString")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "System.Console.WriteLine(System.String)")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
            private void ReceiveData ( )
            {
                _receivedText = "";
                if( client != null )
                {
                    while( true )
                    {
                        try
                        {
                            if( anyIP != null )
                            {
                                // Bytes empfangen.
                                byte[] data = client.Receive( ref anyIP );

                                if( (data != null) && (data.Length > 0) )
                                {
                                    receivedEventargs.Adress = anyIP.Address.ToString();
                                    receivedEventargs.Port = anyIP.Port.ToString( );

                                    // Bytes mit der UTF8-Kodierung in das Textformat kodieren.
                                    _receivedText = Encoding.UTF8.GetString( data );
                                    if (!String.IsNullOrEmpty( _receivedText ))
                                    {
                                        EDataReceived?.Invoke( _receivedText );
                                        receivedEventargs.Payload = _receivedText;
                                        DatagrammReceived?.Invoke( this, receivedEventargs );
                                    }
                                    // latest UDPpacket
                                    lastReceivedUDPPacket = _receivedText;
                                    allReceivedUDPPackets = allReceivedUDPPackets + _receivedText;
                                }
                           }
                        }
                        catch( Exception err )
                        {
                            Services.TraceMessage_( err.Message );
                            Console.WriteLine( TimeUtil.GetTimestamp( ) + " " + err.Data );
                            Console.WriteLine( TimeUtil.GetTimestamp( ) + " " + err.Source );
                            Console.WriteLine( TimeUtil.GetTimestamp( ) + " " + err.InnerException );
                        }
                    }
                }
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public string ReceivedText
            {
                get
                {
                    return _receivedText;
                }
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public void Abort( )
            {
                if( client != null )
                {
                    client.Close();
                    receiveThread.Abort();
                }
            }
        }

        class UdpSend 
        {
            private string IP = UDPConfig.IpAdress;  
            public int port   = UDPConfig.port;  

            IPEndPoint remoteEndPoint;
            UdpClient  client;

            public UdpSend ( )
            {
                remoteEndPoint = new IPEndPoint( IPAddress.Parse( IP ), port );
                client         = new UdpClient( );
            }

            public UdpSend ( string ip_, int port_ )
            {
                remoteEndPoint = new IPEndPoint( IPAddress.Parse( ip_ ), port_ );
                client         = new UdpClient(  );
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "SystemServices.Services.TraceMessage_(System.String,System.String,System.String,System.Int32)")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
            private void sendString ( string message )
            {
                if( String.IsNullOrWhiteSpace( message ) )
                {
                    Services.TraceMessage_( "Tried to send empty string!" );
                    return;
                }
                try
                {
                    byte[] data = Encoding.UTF8.GetBytes( message );
                    client?.SendAsync( data, data.Length, remoteEndPoint );
                }
                catch( Exception err )
                {
                    Services.TraceMessage_( err.Message + " " + err.Data + " " );
                }
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "SystemServices.Services.TraceMessage_(System.String,System.String,System.String,System.Int32)")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
            private void SendString_Sync( string message )
            {
                if ( String.IsNullOrWhiteSpace( message ) )
                {
                    Services.TraceMessage_( "Tried to send empty string!" );
                    return;
                }
                try
                {
                    byte[] data = Encoding.UTF8.GetBytes( message );
                    client?.Send( data, data.Length, remoteEndPoint );
                }
                catch ( Exception err )
                {
                    Services.TraceMessage_( err.Message + " " + err.Data + " " );
                }
            }

            public void SendString( string message )
            {
                sendString ( message );
            }

            public void SendStringSync( string message )
            {
                SendString_Sync( message );
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
            string _SendText;
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public string SendText
            {
                set
                {
                    _SendText = value;
                    sendString( value );
                }
            }
        }
    }
}
