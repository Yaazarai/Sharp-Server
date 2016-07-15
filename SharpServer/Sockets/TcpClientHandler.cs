using System.Net.Sockets;

namespace SharpServer.Sockets {
    /// <summary>
    /// Provides an implementation for handling client connections on a TCP server.
    /// </summary>
    public class TcpClientHandler : SocketContainer {
        /// <summary>
        /// Gets TCP server this client connection belongs too.
        /// </summary>
        public TcpServerHandler Server { get; private set; }
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
        /// Instantiates an instance of the TcpClientHandler class with the specified SocketBinder and parent TcpServerHandler.
        /// </summary>
        /// <param name="binder">SocketBinder to bind the client's new socket too.</param>
        /// <param name="server">TcpServerHandler this client connection belongs too.</param>
        public TcpClientHandler( SocketBinder binder, TcpServerHandler server, uint timeout ) : base( binder ) {
            Server = server;
            Timeout = timeout;
            Receiver = null;
            Stream = null;
        }

        /// <summary>
        /// Closes this TCP client connection and unbinds it's socket.
        /// </summary>
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

            System.GC.SuppressFinalize( this );
        }

        /// <summary>
        /// Cleans up and closes this client connection if it hasn't been done already.
        /// </summary>
        ~TcpClientHandler() {
            Close();
        }
    }
}
