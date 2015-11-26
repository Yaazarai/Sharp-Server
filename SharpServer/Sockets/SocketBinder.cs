using System.Collections.Generic;

namespace SharpServer.Sockets {
    public class SocketBinder {
        private List<int> Unbound;
        private int Assigner;

        public SocketBinder() {
            Unbound = new List<int>();
            Assigner = 0;
        }

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

        public void Unbind( int socket ) {
            if ( socket >= 0 && socket < Assigner && IsBound( socket ) ) {
                Unbound.Add( socket );
            }
        }

        public bool IsBound( int socket ) {
            return ( !Unbound.Contains( socket ) ) && ( socket >= 0 && socket < Assigner );
        }
    }

    public class SocketContainer {
        public SocketBinder Binder { get; private set; }
        public int Socket { get; private set; }
        public bool IsBound { get { return ( Binder != null ) ? Binder.IsBound( Socket ) : false; } }

        public SocketContainer( SocketBinder binder ) {
            Binder = binder;
            Socket = Binder.Bind();
        }

        public void Bind( SocketBinder binder ) {
            if ( Binder != null && Socket >= 0 ) return;
            Binder = binder;
            Socket = Binder.Bind();
        }

        public void Unbind() {
            if ( Binder == null && Socket < 0 ) return;
            Binder.Unbind( Socket );
            Binder = null;
            Socket = -1;
        }

        ~SocketContainer() {
            Unbind();
        }
    }
}