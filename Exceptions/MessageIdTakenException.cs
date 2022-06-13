using System;

namespace MessageBasedSockets.Exceptions {
    public class MessageIdTakenException : Exception {
        public MessageIdTakenException(byte id) : base($"Message id {id} is already taken") {
        }
    }
}