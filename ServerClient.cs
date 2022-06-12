using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace MessageBasedSockets {
    /// <summary>
    /// A class which represents the client connected to the server.
    /// Main usage is to use communication messenger made for this client socket.
    /// </summary>
    public class ServerClient {
        /// <summary>
        /// Revealed socket object of this connection.
        /// </summary>
        public Socket Socket { get; }

        /// <summary>
        /// Messenger for this connection.
        /// </summary>
        public SocketMessenger Messenger { get; }

        internal ServerClient(Socket socket) {
            Socket = socket;
            Messenger = new SocketMessenger(Socket);
        }
    }
}