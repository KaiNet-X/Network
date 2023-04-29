# Getting started

This page shows an example of how to use the most important features of this library. To see some more practical examples, see the example projects in the repository.

#### Note

By default, the connection uses encryption, and both the main connection and udp channels share the same settings.
See [Server](https://github.com/KaiNet-X/Network/blob/master/Server.md)
## Server

```c#
// Creates a new server that listens for connections on all network interfaces on port 5555
// Accepts up to 10 clients
var server = new Server(IPAddress.Any, 5555, 10);

server.OnClientObjectReceived += RecievedObject;
server.OnClientConnected += OnConnect;
server.OnClientDisconnected += OnDisconnect;
server.OnClientChannelOpened += OnChannelOpened;
// Makes the server start listening for connections
// Also has an async version
server.Start();


// After some time, send message to all clients
SomeWaitFunction();
server.SendObjectToAll("Hello world");

void RecievedObject(object obj, ServerClient sc)
{
    // Do stuff with object (you can use "obj is [type] t" to check if it is a given type
    Console.WriteLine($"{sc.RemoteEndpoint}: {obj}");
}

void OnConnect(ServerClient sc)
{
    // Invoked when a new client connects
    sc.SendObject<string>("Thanks for connecting!");
}

void OnDisconnect(ServerClient sc, bool graceful)
{
    // Invoked when a client disconnects and provides the client object
    Console.WriteLine($"{sc.RemoteEndpoint} disconnected");
}

void OnChannelOpened(Channel c, ServerClient sc)
{
    byte[] data = c.ReceiveBytes();
    WriteToSomeFile(data);
    sc.CloseChannel(c);
}

```

## Client
See [Client](https://github.com/KaiNet-X/Network/blob/master/Client.md)

```c#
// Creates a client targeting localhost on port 5555
var client = new Client(IPAddress.Localhost, 5555);

client.OnReceiveObject += RecieveObject;
client.OnDisconnect += Disconnected;
client.OnChannelOpened += ChannelOpened;

// Tries to connect a maximum of 3 times, if it fails do not throw exception
// There is also an async version
client.Connect(3, false);
client.OpenChannel().SendBytes(SomeFile.GetBytes());

void RecieveObject(object obj)
{
    // Do stuff with object (works the same as with the server)
    Console.WriteLine(obj);
}

void Disconnected(bool graceful)
{
    // Detects disconnections and wheather it was graceful or forced
    Console.WriteLine("Disconnected");
}

void ChannelOpened(Channel c)
{
    // Do stuff with channel
}
```

## Custom message
Here is the filerequestmessage from a sample project. This hasn't been fully engineered to reliably send files of every size, and is just meant as an example.
Use properties for data you want to transfer. See [MessageBase](https://github.com/KaiNet-X/Network/blob/master/MessageBase.md)
```c#

// Always make sure to include this, otherwise net won't recognize your custom message
[RegisterMessage]
internal class FileRequestMessage : MessageBase
{
    public FileRequestType RequestType { get; set; }
    public string FileName { get; set; }
    public byte[] FileData { get; set; }
    public string PathRequest { get; set; }

    public enum FileRequestType
    {
        Download,
        Upload,
        Tree
    }
}
```