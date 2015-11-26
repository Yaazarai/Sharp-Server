using System;

namespace SharpServer.Exceptions {
    public class ServerStartedBeforeSetupException : Exception {
        public ServerStartedBeforeSetupException() : base() {}
        public ServerStartedBeforeSetupException( string message ) : base( message ) {}
    }
}