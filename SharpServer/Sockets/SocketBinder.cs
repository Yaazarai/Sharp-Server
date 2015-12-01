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
}