using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using SharpServer.Exceptions;
using SharpServer.Buffers;

namespace SharpServer.Sockets {
    public class TcpServerHandler : SocketContainer {
        private bool running;
        /// <summary>
        /// Get if the server is running/processing.
        /// </summary>
        public bool IsRunning { get { return running; } }
        /// <summary>
        /// Underlying TCP socket listener to listen for client connections on.
        /// </summary>
        public TcpListener Listener { get; private set; }
        /// <summary>
        /// Collection of all existing client connections by key-pair: <socket ID, TcpClientHandler>.
        /// </summary>
        public Dictionary<int,TcpClientHandler> ClientMap { get; private set; }
        /// <summary>
        /// Maximum number clients to be allowed before more client connections are denied.
        /// </summary>
        public int MaxConnections { get; private set; }
        /// <summary>
        /// Default alignment for receive buffers.
        /// </summary>
        public int Alignment { get; private set; }
        /// <summary>
        /// Instance of the IP which the server listens for client connections on.
        /// </summary>
        public IPAddress IP { get; private set; }
        /// <summary>
        /// Port number to listen for client connections on.
        /// </summary>
        public int Port { get; private set; }
        /// <summary>
        /// Status (online = true/offline = false) of the TCP server.
        /// </summary>
        public bool Status { get; set; }

        /// <summary>
        /// Number of milliseconds that have to pass before the client check's it's connection status.
        /// </summary>
        public uint ClientTimeout { get; set; }

        /// <summary>
        /// Event handler for any basic server events.
        /// </summary>
        /// <param name="host">TCP server that threw the event.</param>
        public delegate void ServerEventDelegate( TcpServerHandler host );
        /// <summary>
        /// Event thrown when the TCP server starts.
        /// </summary>
        public event ServerEventDelegate StartedEvent;
        /// <summary>
        /// Event thrown when the TCP server closes.
        /// </summary>
        public event ServerEventDelegate ClosedEvent;
        /// <summary>
        /// Event handler for when the server creates a new client.
        /// </summary>
        /// <returns>Returns a new TcpClientHandler when a client is connected.</returns>
        public delegate TcpClientHandler ClientCreatedEventDelegate( SocketBinder binder, TcpServerHandler server, uint ClientTimeout );
        /// <summary>
        /// Event thrown when the server creates a client.
        /// </summary>
        public event ClientCreatedEventDelegate ClientCreatedEvent;

        /// <summary>
        /// Event handler for when the server receives a packet from a client.
        /// </summary>
        /// <param name="server">Server which received the event.</param>
        /// <param name="client">Client which received the packet.</param>
        /// <param name="readBuffer">BufferStream which holds the received packet.</param>
        public delegate void ReceivedEventDelegate( TcpClientHandler client, BufferStream readBuffer );
        /// <summary>
        /// Event thrown when a client receives a packet.
        /// </summary>
        public event ReceivedEventDelegate ReceivedEvent;
        /// <summary>
        /// Event handler for when the server receives a client connection.
        /// </summary>
        /// <param name="server">Server which received the event.</param>
        /// <param name="client">Client which requests a server connection.</param>
        public delegate void ConnectionEventDelegate( TcpClientHandler client );
        /// <summary>
        /// Event thrown when a client requests a connection to the server.
        /// </summary>
        public event ConnectionEventDelegate ConnectedEvent;
        /// <summary>
        /// Event thrown when the server attempts to check an ambiguous client connection.
        /// </summary>
        public event ConnectionEventDelegate AttemptReconnectEvent;
        /// <summary>
        /// Event thrown when the client requests a connection, but the server has already reached maximum client connections.
        /// </summary>
        public event ConnectionEventDelegate ClientOverflowEvent;
        /// <summary>
        /// Event to be thrown when a client disconnections from the server.
        /// </summary>
        public event ConnectionEventDelegate DisconnectedEvent;

        /// <summary>
        /// Instantiates an instance of the TcpServerHandler class with the specified SocketBinder and setup parameters.
        /// </summary>
        /// <param name="binder">SocketBinder to bind a new socket too.</param>
        /// <param name="port">Port to receive client connections and packets on.</param>
        /// <param name="maxConnections">Maximum number of allowed client connections. Set to <= 0 for uncapped.</param>
        /// <param name="alignment">Default alignment for receive buffers.</param>
        /// <param name="packetHeader">Collection of bytes used for packet verification.</param>
        /// <param name="ip">IP address to listen on--or NULL to listen on all available IP addresses on this device.</param>
        public TcpServerHandler( SocketBinder binder, int port, int maxConnections, int alignment, uint clientTimeout, IPAddress ip = null ) : base( binder ) {
            if ( maxConnections > 0 ) {
                MaxConnections = maxConnections;
                ClientMap = new Dictionary<int,TcpClientHandler>( maxConnections );
            } else {
                MaxConnections = -1;
                ClientMap = new Dictionary<int,TcpClientHandler>();
            }

            Port = port;
            Alignment = alignment;
            ClientTimeout = clientTimeout;

            BeginSetup( port, ip );
        }

        /// <summary>
        /// Initiates the IPEndpoint(IP and Port) for running the server.
        /// <para>(Called by constructor.)</para>
        /// </summary>
        /// <param name="port">Port to receive client connections and packets on.</param>
        /// <param name="ip">IP address to listen on--or NULL to listen on all available IP addresses on this device.</param>
        private void BeginSetup( int port, IPAddress ip = null ) {
            IPEndPoint endPoint = null;

            if ( ip == null ) {
                IP = IPAddress.Any;
                endPoint = new IPEndPoint( IPAddress.Any, port );
            } else {
                IP = ip;
                endPoint = new IPEndPoint( ip, port );
            }

            EndSetup( endPoint );
        }

        /// <summary>
        /// Initiates the TCP listener with the specified IPEndpoint.
        /// <para>(Called by BeginSetup.)</para>
        /// </summary>
        /// <param name="endPoint">The IP and Port information to listen on.</param>
        private void EndSetup( IPEndPoint endPoint ) {
            Listener = new TcpListener( endPoint );
            Listener.AllowNatTraversal( true );
        }

        /// <summary>
        /// Starts running the TCP server if it is NOT already running.
        /// </summary>
        /// <returns>Returns true if the server has started, false if the server is already running.</returns>
        /// <exception cref="SharpServer.Exceptions.StartedBeforeSetupException"/>
        public bool Start() {
            if ( running ) return !running;
            
            if ( Listener != null ) {
                Listener.Start();
                Status = true;
                ThreadPool.QueueUserWorkItem( thread => Accept() );
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
        /// Accepts pending client connections and begins listening for packets on each accepted client.
        /// </summary>
        private void Accept() {
            running = true;
            if ( StartedEvent != null ) StartedEvent( this );

            while( Status ) {
                if ( !Listener.Pending() ) continue;

                TcpClientHandler client = ( ClientCreatedEvent != null ) ? ClientCreatedEvent( Binder, this, ClientTimeout ) : new TcpClientHandler( Binder, this, ClientTimeout );
                client.Receiver = Listener.AcceptTcpClient();
                client.Connected = true;

                if ( ( ClientMap.Count <= MaxConnections && MaxConnections >= 0 ) || MaxConnections <= 0 ) {
                    TcpClient receiver = client.Receiver;
                    receiver.LingerState = new LingerOption( false, 0 );
                    receiver.NoDelay = true;
                    client.Stream = receiver.GetStream();
                    ClientMap.Add( client.Socket, client );
                    ThreadPool.QueueUserWorkItem( thread => Handle( client ) );
                } else {
                    if ( ClientOverflowEvent != null ) ThreadPool.QueueUserWorkItem( thread => ClientOverflowEvent( client ) );
                    client.Close();
                }
            }

            if ( ClosedEvent != null )  ClosedEvent( this );
            running = false;
            Close();
        }

        /// <summary>
        /// Listens for packets from a client and manages that client's connection status.
        /// </summary>
        /// <param name="client"></param>
        private void Handle( TcpClientHandler client ) {
            if ( ConnectedEvent != null ) ConnectedEvent( client );

            Stopwatch watch = new Stopwatch();
            watch.Start();

            while( client.Connected ) {
                if ( watch.ElapsedMilliseconds >= client.Timeout ) {
                    if ( AttemptReconnectEvent != null ) AttemptReconnectEvent( client );
                    client.Connected = client.Receiver.Connected;
                    watch.Reset();
                }

                if ( client.Stream.DataAvailable ) {
                    int packet = client.Receiver.Available;
                    BufferStream buffer = new BufferStream( packet, 1 );
                    client.Stream.Read( buffer.Memory, 0, packet );
                    if ( ReceivedEvent != null ) ReceivedEvent( client, buffer );
                }
            }

            if ( DisconnectedEvent != null ) DisconnectedEvent( client );
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
        ~TcpServerHandler() {
            Unbind();
        }
    }
}
