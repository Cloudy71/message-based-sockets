using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MessageBasedSockets {
    /// <summary>
    /// Client class used for creating a connection between this client and a server.
    /// </summary>
    public class Client {
        /// <summary>
        /// A delegate for server connection event.
        /// </summary>
        public delegate void ServerConnected();

        /// <summary>
        /// A delegate for server disconnection event.
        /// </summary>
        public delegate void ServerDisconnected();

        /// <summary>
        /// IP Address 
        /// </summary>
        public string Ip { get; }

        /// <summary>
        /// Port
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// Checks if there's any connection created and if it's alive.
        /// </summary>
        public bool IsConnected => _socket is { Connected: true };

        /// <summary>
        /// Messenger for current socket used for communication.
        /// </summary>
        public SocketMessenger Messenger { get; private set; }

        /// <summary>
        /// An event for connection to the server.
        /// </summary>
        public event ServerConnected OnConnect;

        /// <summary>
        /// An event for disconnection from the server.
        /// </summary>
        public event ServerDisconnected OnDisconnect;

        private Socket _socket;

        /// <summary>
        /// Constructor for Client class specifying IP Address and Port it will be connecting to.
        /// </summary>
        /// <param name="ip">IP Address</param>
        /// <param name="port">Port</param>
        public Client(string ip, int port) {
            Ip = ip;
            Port = port;
        }

        /// <summary>
        /// Starts connecting to the server on specified IP Address and Port.
        /// </summary>
        /// <returns>This object</returns>
        /// <exception cref="ApplicationException">If client socket is already running</exception>
        public Client Connect() {
            if (_socket != null) {
                throw new ApplicationException("Client is already running.");
            }

            IMessage.RegisterMessageTypes();
            IPHostEntry ipHostEntry = Dns.GetHostEntry(Ip);
            IPAddress ipAddress = ipHostEntry.AddressList
                                             .FirstOrDefault(address => address.AddressFamily == AddressFamily.InterNetwork);
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, Port);
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            Logging.Debug("CLIENT", "Connecting...");
            _socket.BeginConnect(
                ipEndPoint,
                ar => HandleConnect(ar),
                _socket
            );
            return this;
        }

        /// <summary>
        /// Disconnects from the server.
        /// </summary>
        /// <exception cref="ApplicationException">If no connection has been made</exception>
        public void Disconnect() {
            if (_socket == null) {
                throw new ApplicationException("Client is not running.");
            }

            _socket.Disconnect(false);
            _socket = null;
            Logging.Debug("CLIENT", "Disconnected");
            OnDisconnect?.Invoke();
        }

        private void HandleConnect(IAsyncResult ar) {
            _socket.EndConnect(ar);
            Logging.Debug("CLIENT", "Connected to the server");
            Messenger = new SocketMessenger(_socket);
            Messenger.Start();
            OnConnect?.Invoke();
        }
    }
}