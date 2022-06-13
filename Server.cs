using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MessageBasedSockets {
    /// <summary>
    /// A class for setting up a server socket.
    /// </summary>
    public class Server {
        /// <summary>
        /// A delegate used for client connection event.
        /// </summary>
        public delegate void ClientConnection(ServerClient client);

        /// <summary>
        /// A delegate used for client disconnection event.
        /// </summary>
        public delegate void ClientDisconnection(ServerClient client);

        /// <summary>
        /// A delegate used for client message receive event.
        /// </summary>
        public delegate void ClientMessageReceived(ServerClient client, IMessage message);

        /// <summary>
        /// IP Address
        /// </summary>
        public string Ip { get; }

        /// <summary>
        /// Port
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// Client list
        /// </summary>
        public List<ServerClient> Clients { get; }

        /// <summary>
        /// An event for client connection.
        /// </summary>
        public event ClientConnection OnClientConnected;

        /// <summary>
        /// An event for client disconnection.
        /// </summary>
        public event ClientDisconnection OnClientDisconnected;

        /// <summary>
        /// An event for client message receive.
        /// </summary>
        public event ClientMessageReceived OnClientMessageReceived;

        private Socket _socket;

        /// <summary>
        /// Constructor for Server class specifying IP Address and Port it will be listening on.
        /// </summary>
        /// <param name="ip">IP Address</param>
        /// <param name="port">Port</param>
        public Server(string ip, int port) {
            Ip = ip;
            Port = port;
            Clients = new List<ServerClient>();
        }

        /// <summary>
        /// Starts listening on specified IP and Port.
        /// </summary>
        /// <returns>This object</returns>
        /// <exception cref="ApplicationException">If server is already listening</exception>
        public Server Start() {
            if (_socket != null)
                throw new ApplicationException("Server is already running.");

            IMessage.RegisterMessageTypes();
            IPHostEntry ipHostEntry = Dns.GetHostEntry(Ip);
            IPAddress ipAddress = ipHostEntry.AddressList
                                             .FirstOrDefault(address => address.AddressFamily == AddressFamily.InterNetwork);
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, Port);
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            Logging.Debug("SERVER", "Starting server...");
            _socket.Bind(ipEndPoint);
            _socket.Listen(1000);
            Acceptor();
            Logging.Debug("SERVER", "Finished");
            return this;
        }

        /// <summary>
        /// Stops the connected socket.
        /// </summary>
        /// <exception cref="ApplicationException">If no connection has been made</exception>
        public void Stop() {
            if (_socket == null)
                throw new ApplicationException("Server is not running.");

            _socket.Close(0);
            _socket = null;
            Clients.Clear();
            Logging.Debug("SERVER", "Stopped");
        }

        /// <summary>
        /// Sends message to all connected clients.
        /// </summary>
        /// <param name="message">Message object</param>
        public void SendAll(ref IMessage message) {
            foreach (var client in Clients) {
                client.Messenger.Send(ref message);
            }
        }

        /// <summary>
        /// Sends message to all connected clients.
        /// </summary>
        /// <param name="message">Message object</param>
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
                ServerClient client = new ServerClient(this, socket);
                Logging.Debug("SERVER", "Connection established");
                Clients.Add(client);
                client.Messenger.Start();
                client.Messenger.OnMessageReceived += message => OnClientMessageReceived?.Invoke(client, message);
                OnClientConnected?.Invoke(client);
                Acceptor();
            }
            catch (Exception ex) {
                Logging.Error(ex.Message);
            }
        }

        internal void NotifyDisconnect(ServerClient socket) {
            Clients.Remove(socket);
            OnClientDisconnected?.Invoke(socket);
        }
    }
}