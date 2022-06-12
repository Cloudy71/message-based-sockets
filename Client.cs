using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MessageBasedSockets {
    public class Client {
        public delegate void ServerConnected();

        public delegate void ServerDisconnected();

        public string Ip   { get; }
        public int    Port { get; }

        public bool IsConnected => _socket is { Connected: true };

        public SocketMessenger Messenger { get; private set; }

        public event ServerConnected    OnConnect;
        public event ServerDisconnected OnDisconnect;

        private Socket _socket;


        public Client(string ip, int port) {
            Ip = ip;
            Port = port;
        }

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

            Logging.Debug("CLIENT","Connecting...");
            _socket.BeginConnect(
                ipEndPoint,
                ar => HandleConnect(ar),
                _socket
            );
            return this;
        }

        public void Disconnect() {
            if (_socket == null) {
                throw new ApplicationException("Client is not running.");
            }

            _socket.Disconnect(false);
            _socket = null;
            Logging.Debug("CLIENT","Disconnected");
            OnDisconnect?.Invoke();
        }

        private void HandleConnect(IAsyncResult ar) {
            _socket.EndConnect(ar);
            Logging.Debug("CLIENT","Connected to the server");
            Messenger = new SocketMessenger(_socket);
            Messenger.Start();
            OnConnect?.Invoke();
        }
    }
}