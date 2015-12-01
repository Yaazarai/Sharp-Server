using System.Threading;
using System.Net;
using System.Net.Sockets;
using SharpServer.Buffers;
using SharpServer.Exceptions;

namespace SharpServer.Sockets {
    /// <summary>
    /// Provides an interface for handling a UDP server and receiving datagrams.
    /// </summary>
    public class UdpServerHandler : SocketContainer {
        private bool running;
        /// <summary>
        /// Get if the server is running/processing.
        /// </summary>
        public bool IsRunning { get { return running; } }
        /// <summary>
        /// Underlying UDP socket listener to send/receive on.
        /// </summary>
        public UdpClient Listener { get; private set; }
        /// <summary>
        /// Port to receive on.
        /// </summary>
        public int Port { get; set; }
        /// <summary>
        /// Default alignment for receive buffers of the server.
        /// </summary>
        public int Alignment { get; private set; }
        /// <summary>
        /// Status (online = true/offline = false) of the UDP server.
        /// </summary>
        public bool Status { get; set; }

        /// <summary>
        /// Event handler for the UDP server started event.
        /// </summary>
        /// <param name="receiver">UDP server that threw the event.</param>
        public delegate void StartedEventDelegate( UdpServerHandler server );
        /// <summary>
        /// Event handler for the UDP server datagram received event.
        /// </summary>
        /// <param name="receiver">UDP server that threw the event.</param>
        /// <param name="readBuffer">BufferStream with the received datagram.</param>
        /// <param name="endPoint">IP and Port information of the sender.</param>
        public delegate void ReceivedEventDelegate( UdpServerHandler server, BufferStream readBuffer, IPEndPoint endPoint );
        /// <summary>
        /// Event handler for the UDP server closed event.
        /// </summary>
        /// <param name="receiver">UDP server that threw the event.</param>
        public delegate void ClosedEventDelegate( UdpServerHandler server );
        /// <summary>
        /// Event thrown when the UDP server is started.
        /// </summary>
        public event StartedEventDelegate StartedEvent;
        /// <summary>
        /// Event thrown when the UDP server receives a datagram.
        /// </summary>
        public event ReceivedEventDelegate ReceivedEvent;
        /// <summary>
        /// Event thrown when the UDP server closes.
        /// </summary>
        public event ClosedEventDelegate ClosedEvent;
		
        /// <summary>
        /// Instantiates an instances of the UdpServerHandler class using the specified SocketBinder and parameters.
        /// </summary>
        /// <param name="binder">SocketBinder to bind a new socket too.</param>
        /// <param name="port">Port used for receiving.</param>
        /// <param name="alignment">Default alignment in bytes for buffers.</param>
        /// <param name="packetHeader">Collection of bytes used for packet verification.</param>
        public UdpServerHandler( SocketBinder binder, int port, int alignment ) : base( binder ) {
            Port = port;
            Alignment = ( alignment > 0 ) ? alignment : 1;
            running = false;

            Listener = new UdpClient( port );
            Listener.EnableBroadcast = true;
            Listener.AllowNatTraversal( true );
        }

        /// <summary>
        /// Starts running the UDP server if it is NOT already running.
        /// </summary>
        /// <returns>Returns true if the server has started, false if the server is already running.</returns>
        /// <exception cref="SharpServer.Exceptions.StartedBeforeSetupException"/>
        public bool Start() {
            if ( Listener == null ) throw new StartedBeforeSetupException( "UDP server started before initialization." );
            if ( running ) return !running;
            Status = true;
            ThreadPool.QueueUserWorkItem( thread => Handle() );
            return !running;
        }

        /// <summary>
        /// Closes, resets and restarts the UDP server.
        /// </summary>
        public void Restart() {
            Close();
            Listener = new UdpClient( Port );
            Listener.EnableBroadcast = true;
            Listener.AllowNatTraversal( true );
            Status = false;
            running = false;
            Start();
        }

        /// <summary>
        /// Handles incoming datagrams.
        /// </summary>
        private void Handle() {
            running = true;
            StartedEvent( this );

            while( Status ) {
                if ( Listener.Available > 0 ) {
                    IPEndPoint endPoint = null;

                    byte[] datagram = Listener.Receive( ref endPoint );
                    int length = datagram.Length;

                    BufferStream readBuffer = new BufferStream( length, Alignment );
                    System.Array.Copy( datagram, 0, readBuffer.Memory, 0, length );
                    ThreadPool.QueueUserWorkItem( thread => ReceivedEvent( this, readBuffer, endPoint ) );
                }
            }

            ClosedEvent( this );
            running = false;
            Close();
        }

        /// <summary>
        /// Closes the UDP server and unbinds it's socket.
        /// </summary>
        private void Close() {
            if ( IsBound ) {
                Unbind();
            }

            if ( Listener != null ) {
                Listener.Close();
                Listener = null;
            }

            System.GC.SuppressFinalize( this );
        }

        /// <summary>
        /// Cleans up and closes the UDP server if it hasn't been done already.
        /// </summary>
        ~UdpServerHandler() {
            Close();
        }
    }
}
