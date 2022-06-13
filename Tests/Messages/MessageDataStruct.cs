using MessageBasedSockets.Attributes;
using MessageBasedSockets.Types;

namespace MessageBasedSockets.Messages {
    [MessageId(2)]
    internal struct MessageDataStruct : IMessage {
        public MessageData    Data;
        public MessageData[]  DataArray;
        public TestVector     Vector;
        public TestVector[]   VectorArray;
        public TestNestedData NestedData;
    }
}