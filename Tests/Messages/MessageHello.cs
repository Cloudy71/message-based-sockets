using MessageBasedSockets.Attributes;

namespace MessageBasedSockets.Messages {
    [MessageId(0)]
    internal struct MessageHello : IMessage {
        public ulong  Id;
        public string Author;
        public string Message;
    }
}