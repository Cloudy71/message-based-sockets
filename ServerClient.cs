using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace MessageBasedSockets {
    public class ServerClient {
        internal Socket          Socket    { get; }
        internal SocketMessenger Messenger { get; }


        public ServerClient(Socket socket) {
            Socket = socket;
            Messenger = new SocketMessenger(Socket);
        }
    }
}