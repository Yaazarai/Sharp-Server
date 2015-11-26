using System.Net.Sockets;

namespace SharpServer.Sockets {
    public class TcpClientHandler : SocketContainer {
		public TcpServerHandler Server { get; set; }
        public TcpClient Receiver { get; set; }
		public NetworkStream Stream { get; set; }
        public bool Connected { get; set; }

		public TcpClientHandler( SocketBinder binder, TcpServerHandler server ) : base( binder ) {
			Server = server;
            Receiver = null;
			Stream = null;
		}

		public void Close() {
			if ( IsBound ) {
                Unbind();
			}

            if ( Receiver != null ) {
                Receiver.Close();
				Receiver = null;
            }

            if ( Stream != null ) {
                Stream = null;
            }
		}

		~TcpClientHandler() {
			Close();
		}
    }
}