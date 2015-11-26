using System.Threading;
using System.Net;
using System.Net.Sockets;
using SharpServer.Buffers;
using SharpServer.Exceptions;

namespace SharpServer.Sockets {
    public class UdpServerHandler : SocketContainer {
        public UdpClient Listener { get; private set; }
        public int Port { get; set; }
        public bool Status { get; set; }
        public int Alignment { get; private set; }
        
        private bool running;
        public bool IsRunning { get { return running; } }

		public delegate void StartedEventDelegate( UdpServerHandler receiver );
		public delegate void ReceivedEventDelegate( UdpServerHandler receiver, BufferStream readBuffer, IPEndPoint endPoint );
		public delegate void ClosedEventDelegate( UdpServerHandler receiver );
		public event StartedEventDelegate StartedDelegate;
		public event ReceivedEventDelegate ReceivedDelegate;
		public event ClosedEventDelegate ClosedDelegate;
		
        public UdpServerHandler( SocketBinder binder, int port, int alignment ) : base( binder ) {
			Port = port;
            Alignment = ( alignment > 0 ) ? alignment : 1;
            running = false;

			Listener = new UdpClient( port );
            Listener.EnableBroadcast = true;
			Listener.AllowNatTraversal( true );
		}

		public bool Start() {
            if ( Listener == null ) throw new ServerStartedBeforeSetupException( "UDP server started before initialization." );
            if ( running ) return !running;
            Status = true;
            ThreadPool.QueueUserWorkItem( thread => Handle() );
            return !running;
		}

        public void Restart() {
            Close();
			Listener = new UdpClient( Port );
            Listener.EnableBroadcast = true;
			Listener.AllowNatTraversal( true );
            Status = false;
            running = false;
            Start();
        }

		private void Handle() {
			running = true;
            StartedDelegate( this );

			while( Status ) {
				if ( Listener.Available > 0 ) {
					IPEndPoint endPoint = null;

                    byte[] datagram = Listener.Receive( ref endPoint );
                    int length = datagram.Length;

                    BufferStream readBuffer = new BufferStream( length, Alignment );
                    System.Array.Copy( datagram, 0, readBuffer.Memory, 0, length );
                    ThreadPool.QueueUserWorkItem( thread => ReceivedDelegate( this, readBuffer, endPoint ) );
				}
			}

			ClosedDelegate( this );
            running = false;
			Close();
		}

		private void Close() {
			if ( IsBound ) {
                Unbind();
			}

            if ( Listener != null ) {
                Listener.Close();
                Listener = null;
            }
		}

		~UdpServerHandler() {
            Close();
		}
    }
}