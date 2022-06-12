using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MessageBasedSockets {
    public class Server {
        public delegate void ClientConnection(ServerClient client);

        public delegate void ClientDisconnection(ServerClient client);

        public delegate void ClientMessageReceived(ServerClient client, IMessage message);

        public string             Ip      { get; }
        public int                Port    { get; }
        public List<ServerClient> Clients { get; }

        public event ClientConnection      OnClientConnected;
        public event ClientDisconnection   OnClientDisconnected;
        public event ClientMessageReceived OnClientMessageReceived;

        private Socket _socket;

        public Server(string ip, int port) {
            Ip = ip;
            Port = port;
            Clients = new List<ServerClient>();
        }

        public Server Start() {
            if (_socket != null) {
                throw new ApplicationException("Server is already running.");
            }

            IMessage.RegisterMessageTypes();
            IPHostEntry ipHostEntry = Dns.GetHostEntry(Ip);
            IPAddress ipAddress = ipHostEntry.AddressList
                                             .FirstOrDefault(address => address.AddressFamily == AddressFamily.InterNetwork);
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, Port);
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            Logging.Debug("SERVER", "Starting server...");
            _socket.Bind(ipEndPoint);
            _socket.Listen(100);
            Acceptor();
            Logging.Debug("SERVER", "Finished");
            return this;
        }

        public void Stop() {
            if (_socket == null) {
                throw new ApplicationException("Server is not running.");
            }

            _socket.Close(0);
            _socket = null;
            Logging.Debug("SERVER", "Stopped");
        }

        public void SendAll(ref IMessage message) {
            foreach (var client in Clients) {
                client.Messenger.Send(ref message);
            }
        }

        public void SendAll(IMessage message) {
            SendAll(ref message);
        }

        private void Acceptor() {
            Logging.Debug("SERVER", "Accepting connections...");
            _socket.BeginAccept(
                ar => HandleAccept(ar),
                null
            );
        }

        private void HandleAccept(IAsyncResult ar) {
            try {
                Socket socket = _socket.EndAccept(ar);
                ServerClient client = new ServerClient(socket);
                Logging.Debug("SERVER", "Connection established");
                Clients.Add(client);
                client.Messenger.Start();
                client.Messenger.OnMessageReceived += message => OnClientMessageReceived?.Invoke(client, message);
                OnClientConnected?.Invoke(client);
                Acceptor();
            }
            catch (Exception ex) {
                Logging.Error(ex.Message);
                Stop();
            }
        }
    }
}