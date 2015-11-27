using System.Collections.Generic;

namespace SharpServer.Sockets {
    /// <summary>
    /// Provides an interface for binding and unbinding socket IDs.
    /// </summary>
    public class SocketBinder {
        private List<int> Unbound;
        private int Assigner;

        /// <summary>
        /// Instantiates an instance of the SocketBinder class.
        /// </summary>
        public SocketBinder() {
            Unbound = new List<int>();
            Assigner = 0;
        }

        /// <summary>
        /// Binds a new socket(socket ID) to the SocketBinder.
        /// <para>Or reuses an existing socket that was unbinded from.</para>
        /// </summary>
        /// <returns>Returns the new bound socket ID.</returns>
        public int Bind() {
            int socket = -1;

            if ( Unbound.Count == 0 ) {
                socket = Assigner ++;
            } else {
                socket = Unbound[ 0 ];
                Unbound.RemoveAt( 0 );
                Unbound.Sort();
            }

            return socket;
        }

        /// <summary>
        /// Unbinds the specified socket ID if it's a valid socket ID.
        /// </summary>
        /// <param name="socket">Socket ID to unbind.</param>
        public void Unbind( int socket ) {
            if ( socket >= 0 && socket < Assigner && IsBound( socket ) ) {
                Unbound.Add( socket );
            }
        }

        /// <summary>
        /// Checks if a socket ID is bound or not.
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        public bool IsBound( int socket ) {
            return ( !Unbound.Contains( socket ) ) && ( socket >= 0 && socket < Assigner );
        }
    }

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