using System.Net;
using System.Net.Sockets;
using SharpServer.Sockets;

namespace SharpServer.Buffers {
    /// <summary>
    /// Provides an interface for sending packets to a TCP client.
    /// </summary>
    public static class PacketStream {
        /// <summary>
        /// Asynchronously sends a buffer(packet) through the specified stream.
        /// </summary>
        /// <param name="stream">The particular stream of a TCP client to send through.</param>
        /// <param name="buffer">Buffer containing the packet to be sent.</param>
        /// <exception cref="System.ArugmentNullException"/>
        /// <exception cref="System.ArgumentOutOfRangeException"/>
        /// <exception cref="System.IO.IOException"/>
        /// <exception cref="System.ObjectDisposedException"/>
        public static async void SendAsync( NetworkStream stream, BufferStream buffer ) {
            stream.Write( buffer.Memory, 0 , buffer.Iterator );
            await stream.FlushAsync();
        }

        /// <summary>
        /// Synchronously sends a buffer(packet) through the specified stream.
        /// </summary>
        /// <param name="stream">The particular stream of a TCP client to send through.</param>
        /// <param name="buffer">Buffer containing the packet to be sent.</param>
        /// <exception cref="System.ArugmentNullException"/>
        /// <exception cref="System.ArgumentOutOfRangeException"/>
        /// <exception cref="System.IO.IOException"/>
        /// <exception cref="System.ObjectDisposedException"/>
        public static void SendSync( NetworkStream stream, BufferStream buffer ) {
            stream.Write( buffer.Memory, 0 , buffer.Iterator );
            stream.Flush();
        }
    }
}
