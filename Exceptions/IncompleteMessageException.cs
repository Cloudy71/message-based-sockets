using System;

namespace MessageBasedSockets.Exceptions {
    /// <summary>
    /// Exception which is thrown internally to signalize the message reader that the data are not completed
    ///     and a wait for the rest is required.
    /// </summary>
    public class IncompleteMessageException : Exception {
        /// <summary>
        /// Offset from where the reading started.
        /// </summary>
        public int Offset { get; }

        /// <summary>
        /// Size of the buffer part.
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// Message size read specified by the message.
        /// Size is always smaller than MessageSize.
        /// </summary>
        public int MessageSize { get; }

        /// <summary>
        /// A constructor for exception
        /// </summary>
        /// <param name="offset">Offset</param>
        /// <param name="size">Size</param>
        /// <param name="messageSize">Message Size</param>
        public IncompleteMessageException(int offset, int size, int messageSize) {
            Offset = offset;
            Size = size;
            MessageSize = messageSize;
        }
    }
}