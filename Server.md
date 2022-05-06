# Server : [BaseServer]()<[ServerClient](https://github.com/KaiNet-X/Network/blob/master/ServerClient.md), [Channel](https://github.com/KaiNet-X/Network/blob/master/Channel.md)>
The out-of-the-box server implementation allows sending objects, directly accessing [ServerClient](https://github.com/KaiNet-X/Network/blob/master/ServerClient.md) objects, and follows an event-based approach for receiving data.
#### Constructors

- `Server(IPAddress address, int port, ushort maxClients, NetSettings settings = null)` - Server that listens on one address/port combo
- `Server(IPEndPoint endpoint, ushort maxClients, NetSettings settings = null)` - Server that listens on one address/port combo
- `Server(List<IPEndPoint> endpoints, ushort maxClients, NetSettings settings = null)` - Server that listens on multiple address/port combos

#### Fields/Properties

- `bool Active { get; private set; }` - True when the server is listening and managing connections
- `bool Listening { get; private set; }` - True when the server is listening for new connections
- `List<`[ServerClient](https://github.com/KaiNet-X/Network/blob/master/ServerClient.md)`> Clients`- List of clients
- `ushort MaxClients` - Maximum number of allowed connections at a given time
- `ushort LoopDelay` - Delay between client updates; highly reduces CPU usage
- `readonly IPEndPoint[] Endpoints` - All endpoints the server is listening on
- `readonly NetSettings Settings` - Settings

#### Events/Delegates

- `event Action<[Channel](https://github.com/KaiNet-X/Network/blob/master/Channel.md), `[ServerClient](https://github.com/KaiNet-X/Network/blob/master/ServerClient.md)`> OnClientChannelOpened` - Inkvoked when a new channel is opened
- `event Action<object, `[ServerClient](https://github.com/KaiNet-X/Network/blob/master/ServerClient.md)`> OnClientObjectReceived` - Invoked when an object gets recieved
- `event Action<`[ServerClient](https://github.com/KaiNet-X/Network/blob/master/ServerClient.md)`> OnClientConnected` - Invoked when a client connects
- `event Action<`[ServerClient](https://github.com/KaiNet-X/Network/blob/master/ServerClient.md)`, bool> OnClientDisconnected` - Invoked when a client disconnects

#### Methods
- `void SendObjectToAll<T>(T obj)` - Sends an object of any type to all clients
- `async Task SendObjectToAllAsync<T>(T obj)` - Sends an object of any type to all clients
- `void SendMessageToAll(`[MessageBase]()` msg)` - Sends a message to all clients
- `async Task SendMessageToAllAsync(`[MessageBase]()` msg)` - Sends a message to all clients
- `void Start()` - Starts the server and listens for incoming connections
- `void StartAsync()` - Starts the server and listens for incoming connections
- `void Stop()` - Stops listening for incoming connections
- `async Task StopAsync()` - Stops listening for incoming connections
- `void ShutDown()` - Stops listening and closes/removes all clients
- `async Task ShutDownAsync()` - Stops listening and closes/removes all clients