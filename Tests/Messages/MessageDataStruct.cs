using MessageBasedSockets.Types;

namespace MessageBasedSockets.Messages {
    public struct MessageDataStruct : IMessage {
        public MessageData    Data;
        public MessageData[]  DataArray;
        public TestVector     Vector;
        public TestVector[]   VectorArray;
        public TestNestedData NestedData;
    }
}