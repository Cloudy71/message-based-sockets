namespace MessageBasedSockets.Messages {
    public struct MessageData : IMessage {
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