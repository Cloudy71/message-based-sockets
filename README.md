# Message Based Sockets

Is a library which wraps and utilizes .Net sockets to create
quality and reliable message based networking.

### How does it work?

The concept is simple. Messages are structure instances of `IMessage`  which
can contain primitive types or other structures. Structures can have nested structures
and so on...  
This message is serialized and then send over network.
The recipient will receive serialized object as byte array which is
right away transformed back into the correct type with all the data.

### Client & Server

The connection between client and server has been made as simplest as possible.

```csharp
public class ServerProgram {
    public static void Main(string[] args) {
        // ...
    }

    private static Server CreateServer() {
        Server server = new Server("localhost", 4455)
            .Start();

        server.OnClientConnected += OnClientConnected;
        server.OnClientDisconnected += OnClientDisconnected;
        server.OnClientMessageReceived += ServerOnClientMessageReceived;

        return server;
    }

    private static void OnClientConnected(ServerClient client) {
        Console.WriteLine("Client has connected");
    }

    private static void OnClientDisconnected(ServerClient client) {
        Console.WriteLine("Client has disconnected");
    }

    private static void ServerOnClientMessageReceived(ServerClient client, IMessage message) {
        Console.WriteLine($"Message: {message}");
        // Do something with the message...
    }
}
```

```csharp
public class ClientProgram {
    private static Client _client;

    public static void Main(string[] args) {
        _client = CreateClient("localhost", 4455);
        // ...
    }

    private static Client CreateClient(string ip, int port) {
        Client client = new Client(ip, port)
            .Connect();

        client.OnConnect += ClientOnConnect;
        client.OnDisconnect += ClientOnDisconnect;

        return client;
    }

    private static void ClientOnConnect() {
        Console.WriteLine("Connected to the server..");

        _client.Messenger.OnMessageReceived += MessengerOnMessageReceived;
    }

    private static void ClientOnDisconnect() {
        Console.WriteLine("Disconnected from the server.");

        _client.Messenger.OnMessageReceived -= MessengerOnMessageReceived;
    }

    private static void MessengerOnMessageReceived(IMessage message) {
        Console.WriteLine($"Message: {message}");
    }
}
```

This is basically all needed to setup your networking.  
Networking is build on asynchronous processing, so all message sending
or receiving is non-blocking.

### Message Types

Message types are created as structures which contains only fields
of our needs.

```csharp
public struct MessagePlayerInformation : IMessage {
    public string            Nickname;
    public int               Level;
    public Vector3           SpawnPosition;
    public Quaternion        SpawnRotation;
    public WeaponInformation WeaponInformation;
    
    [NonSerialized] 
    public ImportantServerData ImportantServerData;
}
```

Fields which should not be serialized and sent over network must have
`NonSerialized` attribute on them.

### Serialization

The aim is to send as few bytes as possible.

- Every message starts with 1 byte information of structure type
- Two bytes are for `ushort` value which represents the size of message
- Everything else is message data

Arrays and strings always starts with two bytes `short` value of their size,
*-1* means null arrays or string, *0* and bigger values are existing arrays of this size.  
Serialization of nested structures do not need any more bytes since the client and the server
already knows the structure of serialized or deserialized type, so only data bytes are sent.

### TO-DO List
- Serialization of classes if they have their own Writer and Reader methods
- Easier message receiving instead of using event system
- Better code coverage for tests