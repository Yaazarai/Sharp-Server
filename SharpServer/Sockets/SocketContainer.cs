namespace SharpServer.Sockets {
    /// <summary>
    /// Provides an interface for any bound socket.
    /// </summary>
    public class SocketContainer {
        /// <summary>
        /// SocketBinder this container is bound too.
        /// </summary>
        public SocketBinder Binder { get; private set; }
        /// <summary>
        /// Socket ID that was bound too.
        /// </summary>
        public int Socket { get; private set; }
        /// <summary>
        /// Gets whether this socket is currently bound.
        /// </summary>
        public bool IsBound { get { return ( Binder != null ) ? Binder.IsBound( Socket ) : false; } }

        /// <summary>
        /// Instantiates an instance of the SocketContainer class usign the specified SocketBinder.
        /// </summary>
        /// <param name="binder"></param>
        public SocketContainer( SocketBinder binder ) {
            Binder = binder;
            Socket = Binder.Bind();
        }

        /// <summary>
        /// Binds this container to a new socket if it is NOT already bound.
        /// </summary>
        /// <param name="binder"></param>
        public void Bind( SocketBinder binder ) {
            if ( Binder != null && Socket >= 0 ) return;
            Binder = binder;
            Socket = Binder.Bind();
        }

        /// <summary>
        /// Unbinds this container if it is already bound to a socket.
        /// </summary>
        public void Unbind() {
            if ( Binder == null && Socket < 0 ) return;
            Binder.Unbind( Socket );
            Binder = null;
            Socket = -1;
            System.GC.SuppressFinalize( this );
        }

        /// <summary>
        /// Cleans up and unbinds this container if it has not been done already.
        /// </summary>
        ~SocketContainer() {
            Unbind();
        }
    }
}
