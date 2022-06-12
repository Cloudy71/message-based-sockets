using System;

namespace MessageBasedSockets.Exceptions {
    public class IncompleteMessageException : Exception {
        public int Offset      { get; }
        public int Size        { get; }
        public int MessageSize { get; }

        public IncompleteMessageException(int offset, int size, int messageSize) {
            Offset = offset;
            Size = size;
            MessageSize = messageSize;
        }
    }
}