using System.Net;
using System.Net.Sockets;
using SharpServer.Sockets;

namespace SharpServer.Buffers {
    /// <summary>
    /// Provides an interface for sending datagrams to a specified IP and port.
    /// </summary>
    public static class DatagramStream {
        /// <summary>
        /// Asynchronously sends a buffer(datagram) through the specified UDP client.
        /// </summary>
        /// <param name="client">UDP client to send the datagram through.</param>
        /// <param name="buffer">Buffer that contains the datagram to be sent.</param>
        /// <param name="endPoint">IPEndpoint(address/port) to send the datagram too.</param>
        /// <exception cref="System.ArugmentNullException"/>
        /// <exception cref="System.InvalidOperationException"/>
        /// <exception cref="System.ObjectDisposedException"/>
        /// <exception cref="System.Net.Sockets.SocketException"/>
        public static async void SendAsync( UdpClient client, BufferStream buffer, IPEndPoint endPoint ) {
            await client.SendAsync( buffer.Memory, buffer.Iterator, endPoint );
        }

        /// <summary>
        /// Synchonously sends a buffer(datagram) through the specified UDP client.
        /// </summary>
        /// <param name="client">UDP client to send the datagram through.</param>
        /// <param name="buffer">Buffer that contains the datagram to be sent.</param>
        /// <param name="endPoint">IPEndpoint(address/port) to send the datagram too.</param>
        /// <exception cref="System.ArugmentNullException"/>
        /// <exception cref="System.InvalidOperationException"/>
        /// <exception cref="System.ObjectDisposedException"/>
        /// <exception cref="System.Net.Sockets.SocketException"/>
        public static void SendSync( UdpClient client, BufferStream buffer, IPEndPoint endPoint ) {
            client.Send( buffer.Memory, buffer.Iterator, endPoint );
        }
    }
}
