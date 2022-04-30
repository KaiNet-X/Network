# Getting started

## Server

```c#
// Creates a new server that listens for connections on all network interfaces on port 5555
// Accepts up to 10 clients
var server = new Server(IPAddress.Any, 5555, 10);

server.OnClientObjectReceived += RecievedObject;

// Makes the server start listening for connections
// Also has an async version
server.Start();

void RecievedObject(object obj, ServerClient c)
{
    // Do stuff with object (you can use "obj is [type] t" to check if it is a given type
}

```

## Client

```c#
// Creates a client targeting localhost on port 5555
var client = new Client(IPAddress.Localhost, 5555);

client.OnReceiveObject += RecieveObject;

// Tries to connect a maximum of 3 times, if it fails do not throw exception
// There is also an async version
client.Connect(3, false);

void RecieveObject(object obj)
{
    // Do stuff with object (works the same as with the server)
}
``` 