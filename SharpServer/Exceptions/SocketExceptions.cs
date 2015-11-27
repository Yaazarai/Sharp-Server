using System;

namespace SharpServer.Exceptions {
    /// <summary>
    /// Exception to be thrown when a UDP or TCP server attempts to start before it has been setup.
    /// </summary>
    public class StartedBeforeSetupException : Exception {
        /// <summary>
        /// Instantiates a new instance of the SharpServer.Exceptions class.
        /// </summary>
        public StartedBeforeSetupException() : base() {}
        /// <summary>
        /// Instantiates a new instance of the SharpServer.Exceptions class with a specified error message.
        /// </summary>
        /// <param name="message"></param>
        public StartedBeforeSetupException( string message ) : base( message ) {}
    }
}