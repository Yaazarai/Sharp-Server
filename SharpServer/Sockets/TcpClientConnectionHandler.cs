using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using SharpServer.Exceptions;
using SharpServer.Buffers;

namespace SharpServer.Sockets {
    public class TcpClientConnectionHandler : SocketContainer {
        private bool running;
        private IPEndPoint endPoint;
        /// <summary>
        /// Get if the server is running/processing.
        /// </summary>
        public bool IsRunning { get { return running; } }
        /// <summary>
        /// Gest the TCP client that handles this client connection.
        /// </summary>
        public TcpClient Receiver { get; set; }
        /// <summary>
        /// NetworkStream that handles incoming and outgoing data.
        /// </summary>
        public NetworkStream Stream { get; set; }
        /// <summary>
        /// Gets/Sets the online status of this client connection.
        /// </summary>
        public bool Connected { get; set; }
        /// <summary>
        /// Amount of time in milliseconds that has to pass for the client to timeout.
        /// </summary>
        public uint Timeout { get; set; }
        /// <summary>
        /// Status (online = true/offline = false) of the TCP server.
        /// </summary>
        public bool Status { get; set; }

        /// <summary>
        /// Instance of the IP which the server listens for client connections on.
        /// </summary>
        public IPAddress IP { get; private set; }
        /// <summary>
        /// Port number to listen for client connections on.
        /// </summary>
        public int Port { get; private set; }
        
        /// <summary>
        /// Event handler for when the server receives a packet from a client.
        /// </summary>
        /// <param name="readBuffer">BufferStream which holds the received packet.</param>
        public delegate void ReceivedEventDelegate( BufferStream readBuffer );
        /// <summary>
        /// Event thrown when a client receives a packet.
        /// </summary>
        public event ReceivedEventDelegate ReceivedEvent;
        /// <summary>
        /// Event handler for when the server receives a client connection.
        /// </summary>
        public delegate void ConnectionEventDelegate();
        /// <summary>
        /// Event thrown when a client requests a connection to the server.
        /// </summary>
        public event ConnectionEventDelegate ConnectedEvent;
        /// <summary>
        /// Event thrown when the server attempts to check an ambiguous client connection.
        /// </summary>
        public event ConnectionEventDelegate AttemptReconnectEvent;
        /// <summary>
        /// Event to be thrown when a client disconnections from the server.
        /// </summary>
        public event ConnectionEventDelegate DisconnectedEvent;

        /// <summary>
        /// Instantiates an instance of the TcpClientHandler class with the specified SocketBinder and parent TcpServerHandler.
        /// </summary>
        /// <param name="binder">SocketBinder to bind the client's new socket too.</param>
        /// <param name="server">TcpServerHandler this client connection belongs too.</param>
        public TcpClientConnectionHandler( SocketBinder binder, uint timeout, IPAddress ip, int port ) : base( binder ) {
            Timeout = timeout;
            Stream = null;
            BeginSetup(port, ip);
        }

        /// <summary>
        /// Initiates the IPEndpoint(IP and Port) for running the server.
        /// <para>(Called by constructor.)</para>
        /// </summary>
        /// <param name="port">Port to receive client connections and packets on.</param>
        /// <param name="ip">IP address to listen on--or NULL to listen on all available IP addresses on this device.</param>
        private void BeginSetup( int port, IPAddress ip = null ) {
            if ( ip == null ) {
                IP = IPAddress.Any;
                endPoint = new IPEndPoint( IPAddress.Any, port );
            } else {
                IP = ip;
                endPoint = new IPEndPoint( ip, port );
            }

            Receiver = new TcpClient( endPoint );
        }

        /// <summary>
        /// Starts running the TCP server if it is NOT already running.
        /// </summary>
        /// <returns>Returns true if the server has started, false if the server is already running.</returns>
        /// <exception cref="SharpServer.Exceptions.StartedBeforeSetupException"/>
        public bool Start() {
            if ( running ) return !running;
            
            if ( Receiver != null ) {
                Receiver.Connect( endPoint );
                Status = true;
                ThreadPool.QueueUserWorkItem( thread => Handle() );
            } else {
                throw new StartedBeforeSetupException( "TCP server started before initialization." );
            }

            return !running;
        }

        /// <summary>
        /// Closes, resets and restarts the TCP server.
        /// </summary>
        public void Restart() {
            Close();
            BeginSetup( Port, IP );
            Start();
        }

        /// <summary>
        /// Listens for packets from a server and manages the client's connection status.
        /// </summary>
        private void Handle() {
            running = true;
            if ( ConnectedEvent != null ) ConnectedEvent();

            Stopwatch watch = new Stopwatch();
            watch.Start();

            while( Connected && Status ) {
                if ( watch.ElapsedMilliseconds >= Timeout ) {
                    if ( AttemptReconnectEvent != null ) AttemptReconnectEvent();
                    Connected = Receiver.Connected;
                }

                if ( Stream.DataAvailable ) {
                    int packet = Receiver.Available;
                    BufferStream buffer = new BufferStream( packet, 1 );
                    Stream.Read( buffer.Memory, 0, packet );
                    if ( ReceivedEvent != null ) ReceivedEvent( buffer );
                }

                watch.Reset();
            }

            running = false;
            Status = false;
            if ( DisconnectedEvent != null ) DisconnectedEvent();
            Close();
        }

        /// <summary>
        /// Closes the TCP server and unbinds it's socket.
        /// </summary>
        public void Close() {
            Status = false;
            System.GC.SuppressFinalize( this );
        }

        /// <summary>
        /// Cleans up and closes the TCP server if it hasn't been done already.
        /// </summary>
        ~TcpClientConnectionHandler() {
            Unbind();
        }
    }
}
