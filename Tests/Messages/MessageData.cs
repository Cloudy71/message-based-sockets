using MessageBasedSockets.Attributes;
using NUnit.Framework;

namespace MessageBasedSockets.Messages {
    [MessageId(1)]
    internal struct MessageData : IMessage {
        public ulong    Index;
        public float    X;
        public float    Y;
        public float    Z;
        public double[] DoubleArray;
        public sbyte    SByte;
        public char     Char;
        public string[] StringArray;
    }
}