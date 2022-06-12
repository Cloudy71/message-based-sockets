using System;
using System.Collections.Generic;
using System.Net.Sockets;
using MessageBasedSockets.Exceptions;

namespace MessageBasedSockets {
    public class SocketMessenger {
        public delegate void MessageReceived(IMessage message);

        internal static readonly int SegmentSize = ushort.MaxValue;

        public event MessageReceived OnMessageReceived;

        internal Socket Socket { get; }

        internal byte[] InSegment  { get; private set; }
        internal byte[] OutSegment { get; private set; }

        // private int _currentSegmentSize = SegmentSize;
        private int             _bufferOffset = 0;
        private Queue<IMessage> _queue        = new();
        private bool            _sendFlag     = false;

        internal SocketMessenger(Socket socket) {
            socket.ReceiveBufferSize = SegmentSize;
            socket.SendBufferSize = SegmentSize;
            InSegment = new byte[socket.ReceiveBufferSize];
            OutSegment = new byte[socket.SendBufferSize];
            Socket = socket;
        }

        public void Send(ref IMessage message) {
            if (_sendFlag) {
                _queue.Enqueue(message);
                Logging.Debug("Enqueue");
                return;
            }

            SendForce(ref message);
        }

        public void Send(IMessage message) {
            Send(ref message);
        }

        private void SendForce(ref IMessage message) {
            var size = MessageWriter.Serialize(ref message, OutSegment);
            _sendFlag = true;
            Socket.BeginSend(
                OutSegment,
                0,
                size,
                SocketFlags.None,
                ar => HandleSend(ar),
                Socket
            );
        }

        public void RegisterHandler<T>(Action<T> action) where T : IMessage {
            OnMessageReceived += message => {
                                     if (message is T msg)
                                         action(msg);
                                 };
        }

        public void ClearHandlers<T>() where T : IMessage {
        }

        internal void Start() {
            Logging.Debug("Waiting for message");
            Socket.BeginReceive(
                InSegment,
                _bufferOffset,
                InSegment.Length - _bufferOffset,
                SocketFlags.None,
                ar => HandleData(ar),
                null
            );
        }

        private void HandleData(IAsyncResult result) {
            int len = Socket.EndReceive(result) + _bufferOffset;
            _bufferOffset = 0;
            Logging.Debug($"Received {len} bytes, deserializing...");
            int offset = 0;
            while (offset < len) {
                try {
                    var message = MessageReader.Deserialize(InSegment, offset, len - offset, out var readLen);
                    Logging.Debug($"Deserialized {message}");
                    OnMessageReceived?.Invoke(message);
                    offset += readLen;
                }
                catch (IncompleteMessageException exception) {
                    Logging.Debug($"Incomplete message {exception.Offset}, {exception.Size}, {exception.MessageSize}");
                    Array.Copy(InSegment, exception.Offset, InSegment, 0, exception.Size);
                    _bufferOffset = exception.Size;
                    break;
                }
            }

            Start();
        }

        private void HandleSend(IAsyncResult result) {
            int len = Socket.EndSend(result);
            Logging.Debug($"Sent {len} bytes");
            if (_queue.Count > 0) {
                var dequeue = _queue.Dequeue();
                SendForce(ref dequeue);
                return;
            }

            _sendFlag = false;
        }
    }
}