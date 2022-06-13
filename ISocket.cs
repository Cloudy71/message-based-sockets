namespace MessageBasedSockets {
    public interface ISocket {
        internal void NotifyDisconnect(ISocket socket);
    }
}