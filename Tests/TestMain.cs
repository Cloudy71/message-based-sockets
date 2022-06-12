namespace MessageBasedSockets {
    public class TestMain {
        public static void Main() {
            Tests tests = new Tests();
            tests.Setup();
            // tests.Test1ClientSendOneMessage();
            // tests.Test2ClientSendThousandMessages();
            tests.Test3ClientSendMessagesWithStruct();
        }
    }
}