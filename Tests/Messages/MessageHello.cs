namespace MessageBasedSockets.Messages {
    public struct MessageHello : IMessage {
        public ulong  Id;
        public string Author;
        public string Message;
    }
}