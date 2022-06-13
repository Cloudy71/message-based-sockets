using System;

namespace MessageBasedSockets.Attributes {
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public class MessageIdAttribute : Attribute {
        public byte Id;

        public MessageIdAttribute(byte id) {
            Id = id;
        }
    }
}