using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using SharpServer.Exceptions;
using SharpServer.Buffers;

namespace SharpServer.Sockets {
    public class TcpServerHandler : SocketContainer {
        private bool running;
        public bool IsRunning { get { return running; } }

        public TcpListener Listener { get; private set; }
		public Dictionary<int,TcpClientHandler> ClientMap { get; private set; }
		public IPEndPoint EndPoint { get; private set; }
        public int MaxConnections { get; private set; }
        public byte[] PacketHeader { get; private set; }
        public int Alignment { get; private set; }
        public IPAddress IP { get; private set; }
        public int Port { get; private set; }
		public bool Status { get; set; }

		public delegate void ServiceEventDelegate( TcpServerHandler host );
		public event ServiceEventDelegate StartedDelegate;
		public event ServiceEventDelegate ClosedDelegate;

		public delegate void ReceivedEventDelegate( TcpServerHandler host, TcpClientHandler guest, BufferStream readBuffer );
		public delegate void ConnectionEventDelegate( TcpServerHandler host, TcpClientHandler guest );
		public event ConnectionEventDelegate ConnectedDelegate;
        public event ConnectionEventDelegate AttemptReconnectDelegate;
        public event ConnectionEventDelegate ClientOverflowDelegate;
        public event ConnectionEventDelegate DisconnectedDelegate;
        public event ReceivedEventDelegate ReceivedDelegate;

        public TcpServerHandler( SocketBinder binder, int port, int maxConnections, int alignment, byte[] packetHeader, IPAddress ip = null ) : base( binder ) {
            if ( maxConnections > 0 ) {
                MaxConnections = maxConnections;
                ClientMap = new Dictionary<int,TcpClientHandler>( maxConnections );
            } else {
                MaxConnections = -1;
                ClientMap = new Dictionary<int,TcpClientHandler>();
            }

            Port = port;
            Alignment = alignment;
            PacketHeader = packetHeader;

            BeginSetup( port, ip );
        }

        private void BeginSetup( int port, IPAddress ip = null ) {
            IPEndPoint endPoint = null;

            if ( ip == null ) {
                endPoint = new IPEndPoint( IPAddress.Any, port );
            } else {
                endPoint = new IPEndPoint( ip, port );
            }

            EndSetup( endPoint );
        }

        private void EndSetup( IPEndPoint endPoint ) {
            EndPoint = endPoint;
            Listener = new TcpListener( EndPoint );
            Listener.AllowNatTraversal( true );
        }

        public bool Start() {
            if ( running ) return !running;
            
            if ( EndPoint != null && Listener != null ) {
                Listener.Start();
                Status = true;
                ThreadPool.QueueUserWorkItem( thread => Accept() );
            } else {
                throw new ServerStartedBeforeSetupException( "TCP server started before initialization." );
            }

            return !running;
        }

        public void Restart() {
            Close();
            BeginSetup( Port, IP );
            Start();
        }

        private void Accept() {
            running = true;
            if ( StartedDelegate != null ) StartedDelegate( this );

            while( Status ) {
                if ( !Listener.Pending() ) continue;

                TcpClientHandler client = new TcpClientHandler( Binder, this );
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
                    if ( ClientOverflowDelegate != null ) ThreadPool.QueueUserWorkItem( thread => ClientOverflowDelegate( this, client ) );
                    client.Close();
                }
            }

            if ( ClosedDelegate != null )  ClosedDelegate( this );
            running = false;
            Close();
        }

        private void Handle( TcpClientHandler client ) {
            if ( ConnectedDelegate != null ) ConnectedDelegate( this, client );

            while( client.Connected ) {
                if ( !client.Receiver.Connected ) {
                    if ( AttemptReconnectDelegate != null ) AttemptReconnectDelegate( this, client );
                    client.Connected = client.Receiver.Connected;
                }

                if ( client.Stream.DataAvailable ) {
                    int packet = client.Receiver.Available;
                    BufferStream buffer = new BufferStream( packet, 1 );
                    client.Stream.Read( buffer.Memory, 0, packet );
                    if ( ReceivedDelegate != null ) ReceivedDelegate( this, client, buffer );
                }
            }

            if ( DisconnectedDelegate != null ) DisconnectedDelegate( this, client );
        }

        public void Close() {
			Status = false;
        }

        ~TcpServerHandler() {
            Unbind();
        }
    }
}
